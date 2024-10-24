using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebServerInterface
{
    public class WebModuleNavEntry : IWebModuleNavEntry
    {
        public string NavItemPath { get; set; } = string.Empty;

        public string NavItemName { get; set; } = string.Empty;

        public string Target { get; set; } = null;

        public string Icon { get; set; } = null;

        public WebModuleNavEntry()
        {
        }

        public WebModuleNavEntry(string navItemPath, string navItemName) : this()
        {
            NavItemPath = navItemPath;
            NavItemName = navItemName;
        }

        public WebModuleNavEntry(string navItemPath, string navItemName, string target) : this(navItemPath, navItemName)
        {
            Target = target;
        }

        public WebModuleNavEntry(string navItemPath, string navItemName, string target, string icon) : this(navItemPath, navItemName, target)
        {
            Icon = icon;
        }
    }
}
