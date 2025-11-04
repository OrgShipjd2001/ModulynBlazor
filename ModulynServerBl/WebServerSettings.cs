using System.Reflection;
using System.Xml;

namespace Modulyn.Server.Bl
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
        public bool Authentication { get; set; } = false;
        public string AuthDbConnectionString { get; set; } = "Data Source=localhost;Initial Catalog=ModulynAuth;Integrated Security=True;Pooling=False;MultipleActiveResultSets=True;";

        public List<WebServerAuthSettings> AuthSettings { get; set; } = new List<WebServerAuthSettings>();


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

            node = xmlNode.SelectSingleNode("Authentication/AuthDbConnectionString");
            if (node != null)
                if (!string.IsNullOrEmpty(node.InnerText))
                    AuthDbConnectionString = node.InnerText;

            node = xmlNode.SelectSingleNode("Authentication/Enabled");
            if (node != null)
                if (!string.IsNullOrEmpty(node.InnerText))
                    Authentication = node.InnerText.Equals("true", StringComparison.InvariantCultureIgnoreCase);

            XmlNodeList nodeList = xmlNode.SelectNodes("Authentication/AuthSettings");
            if (nodeList != null)
            {
                foreach (XmlNode authNode in nodeList)
                {
                    WebServerAuthSettings authSettings = new WebServerAuthSettings();
                    authSettings.LoadSettings(authNode);
                    AuthSettings.Add(authSettings);
                }
            }
        }

        public bool IsAuthEnabled(string provider)
        {
            if (!Authentication)
                return false;

            foreach(var authSettings in AuthSettings)
            {
                if (authSettings.Provider.Equals(provider, StringComparison.InvariantCultureIgnoreCase))
                {
                    return authSettings.Enabled;
                }
            }

            return false;
        }

        public WebServerAuthSettings GetAuthProvider(string provider)
        {
            foreach (var authSettings in AuthSettings)
            {
                if (authSettings.Provider.Equals(provider, StringComparison.InvariantCultureIgnoreCase))
                {
                    return authSettings;
                }
            }

            return null;
        }
    }
}
