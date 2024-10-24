using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WebServerBl
{
    public class WebServerSettings
    {
        private static WebServerSettings m_instance = null;

        private string SettingsDir = "Data";
        private string SettingsFile = "WebSiteSettings.xml";

        public string SiteName { get; set; } = "WebServer Name";
        public string SiteImage { get; set; } = "webserver.png";
        public bool ShowHomeItem { get; set; } = true;
        public string ModulesPath { get; set; } = "Modules";
        public string StartupPage { get; set; } = "/";

        public static WebServerSettings Instance
        {
            get
            {
                return m_instance ?? (m_instance = new WebServerSettings());
            }
        }

        protected WebServerSettings()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string settingsFile = Path.Combine(asmPath, SettingsDir, SettingsFile);

            if (!File.Exists(settingsFile))
                return;

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(settingsFile);

            LoadData(xmlDoc.DocumentElement);
        }

        private void LoadData(XmlNode xmlNode)
        {
            XmlNode node = null;

            node = xmlNode.SelectSingleNode("WebSiteName");
            if (node != null) 
                if (!string.IsNullOrEmpty(node.InnerText))
                    SiteName = node.InnerText;

            node = xmlNode.SelectSingleNode("SiteImage");
            if (node != null)
                if (!string.IsNullOrEmpty(node.InnerText))
                    SiteImage = node.InnerText;

            node = xmlNode.SelectSingleNode("ShowHomeItem");
            if (node != null)
                if (!string.IsNullOrEmpty(node.InnerText))
                    ShowHomeItem = node.InnerText.Equals("true", StringComparison.InvariantCultureIgnoreCase);

            node = xmlNode.SelectSingleNode("ModulesPath");
            if (node != null)
                if (!string.IsNullOrEmpty(node.InnerText))
                    ModulesPath = node.InnerText;

            node = xmlNode.SelectSingleNode("StartupPage");
            if (node != null)
                if (!string.IsNullOrEmpty(node.InnerText))
                    StartupPage = node.InnerText;
        }
    }
}
