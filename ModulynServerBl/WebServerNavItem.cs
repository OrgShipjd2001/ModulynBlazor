using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebServerBl
{
    public class WebServerNavItem
    {
        public string NavItemPath { get; set; } = string.Empty;
        public string NavItemName { get; set; } = string.Empty;
        public string? Target { get; set; } = null;
        public string? Icon { get; set; } = null;

        public bool Expanded { get; set; } = false;

        public List<WebServerNavItem> Children { get; private set; } = new List<WebServerNavItem>();
        public WebServerNavItem? Parent { get; set; } = null;

        public WebServerNavItem()
        {

        }

        public WebServerNavItem GetChild(string path)
        {
            foreach (WebServerNavItem child in Children)
            {
                if (child.NavItemName == path)
                    return child;

                WebServerNavItem? foundChild = child.GetChild(path);
                if (foundChild != null)
                    return foundChild;
            }

            return null;
        }
    }
}
