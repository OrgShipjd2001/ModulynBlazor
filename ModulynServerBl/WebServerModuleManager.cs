using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WebServerInterface;

namespace WebServerBl
{
    public class WebServerModuleManager
    {
        private string m_moduleDir = "Modules";

        private Dictionary<string, IWebServerModule> m_moduleList = new Dictionary<string, IWebServerModule>();

        public WebServerNavManager NavManager { get; set; } = new WebServerNavManager();

        public WebServerModuleManager() 
        {
            DiscoverModules();

            foreach (IWebServerModule module in GetModuleList())
            {
                module.InitializeModule(GetModuleList().Select(x => x.ModuleId).ToList());
            }
        }

        public void AddModule(IWebServerModule module)
        {
            m_moduleList.Add(module.ModuleId, module);

            List<IWebModuleNavEntry> navEntries = module.GetModuleNavEntries();

            foreach(IWebModuleNavEntry entry in navEntries)
            {
                NavManager.AddNavEntry(entry);
            }
        }

        public void RemoveModule(string moduleName)
        {
            IWebServerModule module = null;
            if (m_moduleList.ContainsKey(moduleName))
                module = m_moduleList[moduleName];

            if (module == null)
                return;

            List<IWebModuleNavEntry> navEntries = module.GetModuleNavEntries();

            foreach (IWebModuleNavEntry entry in navEntries)
            {
                NavManager.RemoveNavEntry(entry.NavItemPath, entry.NavItemName);
            }
        }

        public List<IWebServerModule> GetModuleList()
        {
            List<IWebServerModule> modList = new List<IWebServerModule>();

            foreach(string key in m_moduleList.Keys)
                modList.Add(m_moduleList[key]);

            return modList;
        }

        private void DiscoverModules()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string modulePath = string.Empty;

            // Full directory path set in the settings
            if (Directory.Exists(WebServerSettings.Instance.ModulesPath))
            {
                modulePath = WebServerSettings.Instance.ModulesPath;
            }
            else
            {
                // A custom subdirectory is set in the settings
                if (Directory.Exists(Path.Combine(asmPath, WebServerSettings.Instance.ModulesPath)))
                {
                    modulePath = Path.Combine(asmPath, WebServerSettings.Instance.ModulesPath);
                }
                else
                {
                    // Default / nothing is set in the settings
                    if (Directory.Exists(Path.Combine(asmPath, m_moduleDir)))
                        modulePath = Path.Combine(asmPath, m_moduleDir);
                }
            }

            if (!Directory.Exists(modulePath))
                return;

            string[] fileList = Directory.GetFiles(modulePath, "*.dll", SearchOption.AllDirectories);
            foreach (string dll in fileList)
            {
                try
                {
                    Assembly modAsm = Assembly.LoadFrom(dll);

                    foreach (TypeInfo asmType in modAsm.GetTypes())
                    {
                        if (asmType.IsAbstract)
                            continue;

                        if (asmType.GetInterface(typeof(IWebServerModule).FullName) != null)
                        {
                            IWebServerModule module = (IWebServerModule)modAsm.CreateInstance(asmType.FullName);
                            AddModule(module);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
