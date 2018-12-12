using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace serial2TCP
{
    public class ThreadComPort
    {
        private readonly string nameComPort;
        private readonly int baudRate;
        private readonly string parity;
        private readonly int dataBits;
        private readonly string stopBits;
        private readonly string ipAddres;
        private readonly int ipPort;

        private SerialPort _port;
        private TcpListener _server;
        private TcpClient _client;
        private NetworkStream _stream;
        private readonly byte[] _tcpdata = new byte[256];
        public ThreadComPort(string _nameComPort, int _baudRate, string _parity, int _dataBits, string _stopBits, string _ipAddres, int _ipPort)
        {
            this.nameComPort = _nameComPort;
            this.baudRate = _baudRate;
            this.parity = _parity;
            this.dataBits = _dataBits;
            this.stopBits = _stopBits;
            this.ipAddres = _ipAddres;
            this.ipPort = _ipPort;
        }
        public void Go()
        {
            try
            {
                _port = new SerialPort(nameComPort, baudRate, (Parity)Enum.Parse(typeof(Parity), parity, true), dataBits, (StopBits)Enum.Parse(typeof(StopBits), stopBits, true));
                _port.DataReceived += PortDataReceived;
                _port.ReceivedBytesThreshold = 1;
                _port.Open();
            }
            catch (Exception)
            {
                Console.WriteLine(DateTime.Now + " Couldn't open port ");
                return;
            }

            _server = new TcpListener(IPAddress.Parse(ipAddres), ipPort);
            _server.Start();
            Console.WriteLine(DateTime.Now + " Waiting {0}:{1}:{2}:{3}:{4} over {5}:{6}", nameComPort, baudRate, parity, dataBits, stopBits, ipAddres, ipPort);
            _server.BeginAcceptTcpClient(TcpConnectedIn, null);

        }
        private void TcpConnectedIn(IAsyncResult result)
        {
            try
            {
                TcpClient _client = _server.EndAcceptTcpClient(result);

                _stream = _client.GetStream();

                Console.WriteLine(DateTime.Now + " Client Connected from: " + _client.Client.RemoteEndPoint);

                var tcpdata = new byte[256];
                // Trigger the initial read (Инициировать начальное чтение)
                _stream.BeginRead(tcpdata, 0, tcpdata.Length, TcpReader, null);
            }
            catch (Exception e)
            {
                if (e is ObjectDisposedException)
                    Console.WriteLine("Server shutdown");
                else
                    Console.WriteLine("Server exception: " + e.Message);
            }
        }
        private void PortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            var rxlen = sp.BytesToRead;
            var data = new byte[rxlen];
            sp.Read(data, 0, rxlen);
            //Console.WriteLine(BitConverter.ToString(data, 0, rxlen));

            if (_stream != null)
                if (_stream.CanWrite)
                {
                    try
                    {
                        _stream.Write(data, 0, rxlen);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(DateTime.Now + " Can't write to TCP stream:" + ex.Message);
                        try
                        {
                            _stream.Close();
                        }
                        catch { }

                        _stream = null;
                    }
                }
        }
        void TcpReader(IAsyncResult ar)
        {
            try
            {
                var rxbytes = _stream.EndRead(ar);                                       // Обрабатывает завершение асинхронного чтения запущенную BeginRead методом

                if (rxbytes > 0)
                {
                    _port.Write(_tcpdata, 0, rxbytes);
                }

                if (rxbytes >= 0)
                {
                    try
                    {
                        if (_tcpdata[0] == 0)
                            _tcpdata[1]++;
                        if (_tcpdata[1] == 200)
                            _client.Close();
                        // Read again. This callback will be called again. (Читать снова. Этот обратный вызов будет вызван снова.)
                        _stream.BeginRead(_tcpdata, 0, _tcpdata.Length, TcpReader, null);
                        _tcpdata[0] = 0;
                    }
                    catch
                    {
                        Console.WriteLine(DateTime.Now + " Exception. Client out");
                        try
                        {
                            _stream.Close();
                        }
                        catch { }
                        _stream = null;


                        try
                        {
                            if (_client != null)
                            {
                                _client.Close();
                            }
                        }
                        catch { }
                        _server.BeginAcceptTcpClient(TcpConnectedIn, null);
                    }
                }
            }
            catch (Exception e)
            {
                if (e is ObjectDisposedException)
                    Console.WriteLine("Connection closed");
                else if (e is IOException && e.Message.Contains("closed"))
                    Console.WriteLine("Connection closed");
                else
                    Console.WriteLine(DateTime.Now + " Exception: " + e.Message);

                try
                {
                    _stream.Close();
                }
                catch { }
                _stream = null;

                try
                {
                    try
                    {
                        if (_client != null)
                        {
                            _client.Close();
                            _server.EndAcceptSocket(ar);
                        }
                    }
                    catch { }
                    _server.BeginAcceptTcpClient(TcpConnectedIn, null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Problem reconnecting: " + ex.Message);
                }
            }
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;
            string version = "v" + System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            string nameApp = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;
            Console.Title = nameApp + " " + version;

            ReceivingConfiguration();

            ConsoleKeyInfo cki;
            Console.WriteLine(DateTime.Now + " Нажми Escape (Esc) для выхода");
            do
            {
                cki = Console.ReadKey();
            } while (cki.Key != ConsoleKey.Escape);
        }
        static void ReceivingConfiguration()                    // получение настроек
        {
            DevSet devSet = new DevSet("settings.xml");
            if (devSet.settings != null)
            {
                DevSet.Settings[] set = devSet.settings;
                for (int i = 0; i < set.Length; i++)
                {
                    ThreadComPort threadComPort = new ThreadComPort(set[i].nameComPort,
                        set[i].baudRate,
                        set[i].parity,
                        set[i].dataBits,
                        set[i].stopBits,
                        set[i].ipAddres,
                        set[i].ipPort);
                    Thread myThread = new Thread(new ThreadStart(threadComPort.Go));
                    myThread.Start();
                }
            }
        }
    }
}
