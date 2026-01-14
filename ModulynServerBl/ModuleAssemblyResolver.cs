using Lumberjack.Interface;
using System.Reflection;

namespace Modulyn.Server.Bl
{
    public static class ModuleAssemblyResolver
    {
        private static string m_baseDir;
        private static List<string> m_additionalDirs = new List<string>();

        public static void Initialze()
        {
            Logging.LogInfo("Initialize Assembly Resolver", "Modulyn");
            Assembly asm = Assembly.GetEntryAssembly();
            if (asm == null)
                throw new InvalidOperationException("Could not retrieve Entry Assembly");

            m_baseDir = Path.GetDirectoryName(asm.Location);
            if (!Directory.Exists(m_baseDir))
                throw new InvalidOperationException("Base Directory does not exist: " + m_baseDir);

            Logging.LogInfo("Base Directory: " + m_baseDir, "Modulyn");

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
        }

        public static void Initialze(Assembly asm)
        {
            Logging.LogInfo("Initialize Assembly Resolver with provided assembly", "Modulyn");

            if (asm == null)
                throw new InvalidOperationException("Assembly parameter not set");

            m_baseDir = Path.GetDirectoryName(asm.Location);
            if (!Directory.Exists(m_baseDir))
                throw new InvalidOperationException("Base Directory does not exist: " + m_baseDir);

            Logging.LogInfo("Base Directory: " + m_baseDir, "Modulyn");

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
        }

        public static void AddDirectory(string directory)
        {
            Logging.LogInfo("Assembly Resolver Add Directory: " + directory, "Modulyn");

            string checkedDir = GetCheckedDirectory(directory);

            if (string.IsNullOrWhiteSpace(checkedDir))
                return;

            if (!m_additionalDirs.Contains(checkedDir))
                m_additionalDirs.Add(checkedDir);
        }

        public static void RemoveDirectory(string directory)
        {
            Logging.LogInfo("Assembly Resolver Remove Directory: " + directory, "Modulyn");

            string checkedDir = GetCheckedDirectory(directory);

            if (string.IsNullOrWhiteSpace(checkedDir))
                return;

            if (m_additionalDirs.Contains(checkedDir))
                m_additionalDirs.Remove(checkedDir);
        }

        private static string GetCheckedDirectory(string directory)
        {
            if (Directory.Exists(directory))
                return directory;

            DirectoryInfo dirInfo = new DirectoryInfo(Path.Combine(m_baseDir, directory));
            if (dirInfo.Exists)
                return dirInfo.FullName;

            return string.Empty;
        }

        public static Assembly? ResolveAssembly(string assemblyName)
        {
            Assembly resolvedAssembly = null;
            string dllName = GetAssemblyFileName(assemblyName);

            List<string> results = new List<string>();

            results.AddRange(Directory.GetFiles(m_baseDir, dllName, SearchOption.TopDirectoryOnly));
            if (results.Count > 0)
                return Assembly.LoadFrom(results[0]);

            foreach (string directory in m_additionalDirs)
            {
                try
                {
                    results.AddRange(Directory.GetFiles(directory, dllName, SearchOption.AllDirectories));
                }
                catch (Exception exc)
                {
                    // Log error
                    Logging.LogError("Execption in resolve assembly: " + dllName + Environment.NewLine + exc.ToString(), "Modulyn");
                }

                if (results.Count > 0)
                {
                    resolvedAssembly = Assembly.LoadFrom(results[0]);
                    break;
                }
            }

            return resolvedAssembly;
        }

        private static string GetAssemblyFileName(string assemblyName)
        {
            string name = assemblyName;

            int commaIndex = assemblyName.IndexOf(',');
            if (commaIndex > 0)
            {
                name = name.Substring(0, commaIndex);
            }

            return name + ".dll";
        }

        private static Assembly? CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly? resolvedAssembly = null;
            string asmName = GetAssemblyFileName(args.Name);

            resolvedAssembly = ResolveAssembly(asmName);

            return resolvedAssembly;
        }

    }
}
