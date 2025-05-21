
using System.Xml;

namespace Modulyn.Server.Bl
{
    public class WebServerAuthSettings
    {
        public string Provider { get; set; } = "None";
        public bool Enabled { get; set; } = false;
        public Dictionary<string,string> Properties { get; set; } = new Dictionary<string, string>();

        public void LoadSettings(XmlNode xmlnode)
        {
            XmlNode node = null;
            node = xmlnode.SelectSingleNode("Provider");
            if (node != null)
                Provider = node.InnerText;

            node = xmlnode.SelectSingleNode("Enabled");
            if (node != null)
                Enabled = bool.Parse(node.InnerText);

            XmlNodeList nodeList = xmlnode.SelectNodes("Property");
            if (nodeList != null)
            {
                foreach (XmlNode propNode in nodeList)
                {
                    string name = propNode.Attributes["name"].Value;
                    string value = propNode.InnerText;
                    Properties[name] = value;
                }
            }
        }

        public void SaveSettings(XmlNode xmlnode)
        {
            XmlDocument xmlDoc = xmlnode.OwnerDocument;

            XmlElement providerNode = xmlDoc.CreateElement("Provider");
            providerNode.InnerText = Provider;
            xmlnode.AppendChild(providerNode);
            XmlElement enabledNode = xmlDoc.CreateElement("Enabled");
            enabledNode.InnerText = Enabled.ToString();
            xmlnode.AppendChild(enabledNode);
            foreach (var prop in Properties)
            {
                XmlElement propNode = xmlDoc.CreateElement("Property");
                propNode.Attributes.Append(xmlDoc.CreateAttribute("name"));
                propNode.Attributes["name"].Value = prop.Key;
                propNode.InnerText = prop.Value;
                xmlnode.AppendChild(propNode);
            }
        }
    }
}
