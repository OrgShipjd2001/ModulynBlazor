using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebServerInterface;

namespace TestModule
{
    public class TestModuleNavItem : IWebModuleNavEntry
    {
        public string NavItemPath { get; set; } = string.Empty;

        public string NavItemName { get; set; } = "Test Module";

        public string Target { get; set; } = "TestModule";

        public string Icon { get; set; } = string.Empty;

        public TestModuleNavItem()
        {
        }

        public TestModuleNavItem(string navItemPath, string navItemName, string target)
        {
            NavItemPath = navItemPath;
            NavItemName = navItemName;
            Target = target;
        }

        public TestModuleNavItem(string navItemPath, string navItemName, string target, string icon)
        {
            NavItemPath = navItemPath;
            NavItemName = navItemName;
            Target = target;
            Icon = icon;
        }
    }
}
