using Modulyn.Server.Interface;
using System.Reflection;

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

            retList.Add(new TestModuleNavItem(string.Empty, "Test Module", null, Image));
            retList.Add(new TestModuleNavItem("Test Module", "Test1", null, "img/testbutton.jpg", ModulynAuthRole.Admin));
            retList.Add(new TestModuleNavItem("Test Module\\Test1", "Test Module 1", "/testmodule", "img/testbutton.jpg", ModulynAuthRole.Admin));
            retList.Add(new TestModuleNavItem("Test Module", "TestModule2", null, "img/testkeyboard.jpg", ModulynAuthRole.PowerUser));
            retList.Add(new TestModuleNavItem("Test Module\\TestModule2", "Test2", null, "img/testkeyboard.jpg", ModulynAuthRole.PowerUser));
            retList.Add(new TestModuleNavItem("Test Module\\TestModule2\\Test2", "Test Module 2.2", "/testmodule2", "img/testkeyboard.jpg", ModulynAuthRole.PowerUser));

            return retList;
        }

        public List<ModuleBuilderService>? GetWebBuilderServices()
        {
            return null;
        }

        public Dictionary<Type, List<object>> GetWebAppMiddleware()
        {
            return new Dictionary<Type, List<object>>();
        }

        public ModuleAppUseFlags GetModuleAppUseFlags()
        {
            return ModuleAppUseFlags.None;
        }
    }
}
