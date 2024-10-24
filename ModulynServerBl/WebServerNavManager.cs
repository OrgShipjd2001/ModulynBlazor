
using Modulyn.Server.Interface;

namespace Modulyn.Server.Bl
{
    public class WebServerNavManager
    {
        public List<WebServerNavItem> NavEntries { get; set; } = new List<WebServerNavItem>();

        public WebServerNavManager()
        {
        }

        public WebServerNavItem CreateNavEntry(WebServerNavItem parentItem, string itemName)
        {
            WebServerNavItem navItem = new WebServerNavItem();
            navItem.NavItemName = itemName;
            navItem.NavItemPath = (!string.IsNullOrWhiteSpace(parentItem.NavItemPath)) ? (parentItem.NavItemPath + "\\" + parentItem.NavItemName) : parentItem.NavItemName;
            navItem.Parent = parentItem;
            parentItem.Children.Add(navItem);

            return navItem;
        }

        public void AddNavEntry(IWebModuleNavEntry entry)
        {
            WebServerNavItem navItem = new WebServerNavItem();
            navItem.NavItemPath = entry.NavItemPath;
            navItem.NavItemName = entry.NavItemName;
            navItem.Target = entry.Target;
            navItem.Icon = entry.Icon;

            if (string.IsNullOrWhiteSpace(navItem.NavItemPath))
            {
                NavEntries.Add(navItem);
                return;
            }

            WebServerNavItem parent = GetNavEntry(navItem.NavItemPath);
            navItem.Parent = parent;
            parent.Children.Add(navItem);
        }

        public void RemoveNavEntry(string itemPath, string itemName)
        {
            WebServerNavItem navItem = GetNavEntry(itemPath, itemName);

            if ((navItem != null) && (navItem.Parent != null))
            {
                navItem.Parent.Children.Remove(navItem);
            }
            else
                NavEntries.Remove(navItem);
        }

        public WebServerNavItem GetNavEntry(string itemPath)
        {
            List<string> navParts = new List<string>(itemPath.Split('\\', StringSplitOptions.RemoveEmptyEntries));

            WebServerNavItem currentItem = null;
            foreach (WebServerNavItem listItem in NavEntries)
            {
                if (listItem.NavItemName.Equals(navParts[0], StringComparison.InvariantCultureIgnoreCase))
                {
                    currentItem = listItem;
                    break;
                }
            }

            if (currentItem == null)
            {
                currentItem = new WebServerNavItem();
                currentItem.NavItemName = navParts[0];
                NavEntries.Add(currentItem);
                navParts.RemoveAt(0);
            }
            else
            {
                navParts.RemoveAt(0);
            }

            while ((navParts.Count > 0) && (currentItem != null))
            {
                string nextPart = navParts[0];
                navParts.RemoveAt(0);

                WebServerNavItem child = currentItem.GetChild(nextPart);
                if (child == null)
                {
                    child = CreateNavEntry(currentItem, nextPart);
                }
                
                currentItem = child;
            }

            return currentItem;
        }

        public WebServerNavItem GetNavEntry(string itemPath, string itemName)
        {
            WebServerNavItem? parentItem = GetNavEntry(itemPath);
            return parentItem.GetChild(itemName);
        }
    }
}
