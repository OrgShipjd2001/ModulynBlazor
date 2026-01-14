using Lumberjack.Interface;
using Modulyn.Server.Interface;
using System.Reflection;
using System.Runtime.Loader;

namespace Modulyn.Server.Bl
{
    public class WebServerModuleManager
    {
        private string m_moduleDir = "Modules";

        // Track both module and its AssemblyLoadContext
        private class ModuleContextInfo
        {
            public IWebServerModule Module { get; set; }
            public AssemblyLoadContext LoadContext { get; set; }
        }

        private Dictionary<string, ModuleContextInfo> m_moduleList = new Dictionary<string, ModuleContextInfo>();

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

        public void AddModule(IWebServerModule module, AssemblyLoadContext loadContext)
        {
            m_moduleList.Add(module.ModuleId, new ModuleContextInfo { Module = module, LoadContext = loadContext });

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
            if (!m_moduleList.ContainsKey(moduleName))
                return;

            var info = m_moduleList[moduleName];
            var module = info.Module;
            var loadContext = info.LoadContext;

            List<IWebModuleNavEntry> navEntries = module.GetModuleNavEntries();

            foreach (IWebModuleNavEntry entry in navEntries)
            {
                NavManager.RemoveNavEntry(entry.NavItemPath, entry.NavItemName);
            }

            m_moduleList.Remove(moduleName);

            // Unload the context
            loadContext.Unload();
        }

        public List<IWebServerModule> GetModuleList()
        {
            return m_moduleList.Values.Select(x => x.Module).ToList();
        }

        private void DiscoverModules()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string modulePath = Path.Combine(asmPath, m_moduleDir); // Default - subdirectory "Modules"

            Logging.LogInfo("Discover Modules", "Modulyn");

            // Full directory path set in the settings
            if (Directory.Exists(WebServerSettings.Instance.ModulesPath))
            {
                DirectoryInfo di = new DirectoryInfo(WebServerSettings.Instance.ModulesPath);
                modulePath = di.FullName;
            }
            else
            {
                // A custom subdirectory is set in the settings
                if (Directory.Exists(Path.Combine(asmPath, WebServerSettings.Instance.ModulesPath)))
                {
                    DirectoryInfo di = new DirectoryInfo(Path.Combine(asmPath, WebServerSettings.Instance.ModulesPath));
                    modulePath = di.FullName;
                }
            }

            Logging.LogInfo("Modules Path: " + modulePath, "Modulyn");

            if (!Directory.Exists(modulePath))
            {
                Logging.LogWarning("Modules Path does not exist: " + modulePath, "Modulyn");
                return;
            }

            // Update the assembly resolver
            foreach (string dirname in Directory.GetDirectories(modulePath))
            {
                //ModuleAssemblyResolver.AddDirectory(dirname);

                string[] fileList = Directory.GetFiles(dirname, "*.dll");

                foreach (string dll in fileList)
                {
                    try
                    {
                        // Use a custom AssemblyLoadContext for each module
                        var alc = new AssemblyLoadContext($"ModuleContext_{Path.GetFileNameWithoutExtension(dll)}", isCollectible: true);
                        alc.Resolving += new ModuleAssemblyResolverHandler(dirname).Resolve;
                        Assembly modAsm = alc.LoadFromAssemblyPath(dll);

                        foreach (TypeInfo asmType in modAsm.GetTypes())
                        {
                            if (asmType.IsAbstract)
                                continue;

                            if (asmType.GetInterface(typeof(IWebServerModule).FullName) != null)
                            {
                                Logging.LogInfo("Found Module: " + dll, "Modulyn");
                                try
                                {
                                    var module = (IWebServerModule)Activator.CreateInstance(asmType.AsType());
                                    AddModule(module, alc);
                                }
                                catch (Exception ex)
                                {
                                    Logging.LogError("Failed to create module instance: " + dll + Environment.NewLine + ex.ToString(), "Modulyn");
                                    alc.Unload();
                                }
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

        // Add this event handler class to resolve module assemblies independently
        private class ModuleAssemblyResolverHandler
        {
            private readonly string _moduleDirectory;

            public ModuleAssemblyResolverHandler(string moduleDirectory)
            {
                _moduleDirectory = moduleDirectory;
            }

            public Assembly? Resolve(AssemblyLoadContext context, AssemblyName assemblyName)
            {
                Assembly? resolved = ModuleAssemblyResolver.ResolveAssembly(assemblyName.FullName);
                if (resolved != null)
                    return resolved;

                // Only resolve assemblies from the module's directory
                string assemblyPath = Path.Combine(_moduleDirectory, $"{assemblyName.Name}.dll");
                if (File.Exists(assemblyPath))
                {
                    return context.LoadFromAssemblyPath(assemblyPath);
                }

                string[] filelist = Directory.GetFiles(_moduleDirectory, $"{assemblyName.Name}.dll", SearchOption.AllDirectories);
                if (filelist.Length > 0)
                {
                    return context.LoadFromAssemblyPath(filelist[0]);
                }
                return null;
            }
        }
    }
}
