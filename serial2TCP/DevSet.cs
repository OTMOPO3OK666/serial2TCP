using System;
using System.Xml;

namespace serial2TCP
{
    class DevSet
    {
        public struct Settings
        {
            public string nameComPort;
            public int baudRate;
            public string parity;
            public int dataBits;
            public string stopBits;
            public string ipAddres;
            public int ipPort;
        }
        public Settings[] settings;

        private XmlDocument xmlDoc = new XmlDocument();
        public DevSet(string nameFileXML)
        {
            try
            {
                xmlDoc.Load(nameFileXML);
                XmlElement xRoot = xmlDoc.DocumentElement;
                int count = 0;
                foreach (XmlNode xnode in xRoot)            // количество устройств
                    count++;
                settings = new Settings[count];
                count = 0;
                foreach (XmlNode xnode in xRoot)             // количество устройств
                {
                    XmlNode namePort = xnode.Attributes.GetNamedItem("name");
                    XmlNode baudRate = xnode["baudrate"];
                    XmlNode parity = xnode["parity"];
                    XmlNode dataBits = xnode["databits"];
                    XmlNode stopBits = xnode["stopbits"];
                    XmlNode ipAddres = xnode["ipaddres"];
                    XmlNode ipPort = xnode["ipport"];
                    settings[count].nameComPort = namePort.Value;
                    settings[count].baudRate = Int32.Parse(baudRate.InnerText);
                    settings[count].parity = parity.InnerText;
                    settings[count].dataBits = Int32.Parse(dataBits.InnerText);
                    settings[count].stopBits = stopBits.InnerText;
                    settings[count].ipAddres = ipAddres.InnerText;
                    settings[count].ipPort = Int32.Parse(ipPort.InnerText);
                    count++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now + " Exception: " + ex.Message);
            }
            
        }
    }
}
