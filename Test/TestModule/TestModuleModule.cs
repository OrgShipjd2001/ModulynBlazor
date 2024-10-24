using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using WebServerInterface;

namespace TestModule
{
    public class TestModuleModule : IWebServerModule
    {
        public string ModuleId { get { return "TestModule"; } }
        public string DisplayName { get { return "Test Module"; } }
        public string Description
        {
            get { return "A test module for testing the website functionality"; }
        }
        public string Image { get { return "img/test.png"; } }

        public Assembly ModuleAssembly { get { return Assembly.GetExecutingAssembly(); } }

        public void InitializeModule(List<string> AvailableServerModuleIds)
        {
        }

        public List<IWebModuleNavEntry> GetModuleNavEntries()
        {
            List<IWebModuleNavEntry> retList = new List<IWebModuleNavEntry>();

            retList.Add(new TestModuleNavItem(string.Empty, "Test Module", "/testmodule", "img/test.png"));
            retList.Add(new TestModuleNavItem("Test Module\\TestModule2\\Test1", "Test Module 2.1", "/testmodule2", "img/test.png"));
            retList.Add(new TestModuleNavItem("Test Module\\TestModule2\\Test2", "Test Module 2.2", "/testmodule2", "img/test.png"));

            return retList;
        }

        public List<ModuleBuilderService>? GetWebBuilderServices()
        {
            return null;
        }
    }
}
