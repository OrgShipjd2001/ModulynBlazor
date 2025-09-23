using Lumberjack.Interface;
using Modulyn.Server.Interface;
using System.Reflection;


namespace Modulyn.Server.Bl
{
    public class WebServerModuleManager
    {
        private string m_moduleDir = "Modules";

        private Dictionary<string, IWebServerModule> m_moduleList = new Dictionary<string, IWebServerModule>();

        public WebServerNavManager NavManager { get; set; } = new WebServerNavManager();

        public WebServerModuleManager() 
        {
            ModuleAssemblyResolver.Initialze();

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

            WebServerNavItem rootItem = NavManager.GetModuleRoot(module.ModuleId);
            foreach(IWebModuleNavEntry entry in navEntries)
            {
                if (rootItem == null)
                    NavManager.AddNavEntry(entry);
                else
                    NavManager.AddNavEntry(rootItem, entry);
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

            Logging.LogInfo("Discover Modules", "Modulyn");

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

            Logging.LogInfo("Modules Path: " + modulePath, "Modulyn");

            if (!Directory.Exists(modulePath))
            {
                Logging.LogWarning("Modules Path does not exist: " + modulePath, "Modulyn");
                return;
            }

            // Update the assembly resolver
            foreach(string dirname in Directory.GetDirectories(modulePath))
            {
                ModuleAssemblyResolver.AddDirectory(dirname);
            }

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
                            Logging.LogInfo("Found Module: " + dll, "Modulyn");
                            try
                            {
                                IWebServerModule module = (IWebServerModule)modAsm.CreateInstance(asmType.FullName);
                                AddModule(module);
                            }
                            catch (Exception ex)
                            {
                                Logging.LogError("Failed to create module instance: " + dll + Environment.NewLine + ex.ToString(), "Modulyn");
                            }
                    }
                }
                catch (Exception exc)
                {
                    Logging.LogWarning("Discover Modules - Failed to load assembly: " + dll + " - " + exc.Message, "Modulyn");
                }
            }
        }
    }
}
