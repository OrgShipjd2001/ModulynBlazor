
using Modulyn.Server.Interface;
using System.Reflection;
using System.Xml;

namespace Modulyn.Server.Bl
{
    public class WebServerNavManager
    {
        private string SettingsDir = "Data";
        private string SettingsFile = "RootNavigationStructure.xml";

        public List<WebServerNavItem> NavEntries { get; set; } = new List<WebServerNavItem>();

        public WebServerNavManager()
        {
            LoadNavStructure();
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
            navItem.RequiredRole = entry.AuthenticationRole;

            if (string.IsNullOrWhiteSpace(navItem.NavItemPath))
            {
                NavEntries.Add(navItem);
                return;
            }

            WebServerNavItem parent = GetNavEntry(navItem.NavItemPath);
            navItem.Parent = parent;
            parent.Children.Add(navItem);
        }

        public void AddNavEntry(WebServerNavItem rootItem, IWebModuleNavEntry entry)
        {
            WebServerNavItem navItem = new WebServerNavItem();
            navItem.NavItemPath = rootItem.NavItemPath + "\\" + (!string.IsNullOrWhiteSpace(entry.NavItemPath) ? entry.NavItemPath : entry.NavItemName);
            navItem.NavItemName = entry.NavItemName;
            navItem.Target = entry.Target;
            navItem.Icon = entry.Icon;
            navItem.RequiredRole = entry.AuthenticationRole;

            WebServerNavItem parent = rootItem;
            if (!string.IsNullOrWhiteSpace(entry.NavItemPath))
                parent = GetNavEntry(rootItem.NavItemPath + "\\" + entry.NavItemPath);

            if (parent == null)
                throw new InvalidOperationException("Parent item not found in navigation structure.");

            navItem.Parent = parent;
            parent.Children.Add(navItem);
        }

        public void RemoveNavEntry(string itemPath, string itemName)
        {
            WebServerNavItem navItem = GetNavEntry(itemPath, itemName);

            if (navItem != null)
            {
                if (navItem.Parent != null)
                    navItem.Parent.Children.Remove(navItem);
                else
                    NavEntries.Remove(navItem);
            }
        }

        public WebServerNavItem? GetModuleRoot(string moduleId, WebServerNavItem parent = null)
        {
            WebServerNavItem? retItem = null;

            List<WebServerNavItem> itemList = (parent != null) ? parent.Children : NavEntries;
            foreach (WebServerNavItem listItem in itemList)
            {
                if (listItem.ModuleId.Equals(moduleId, StringComparison.InvariantCultureIgnoreCase))
                {
                    retItem = listItem;
                }
                else
                {
                    retItem = GetModuleRoot(moduleId, listItem);
                }

                if (retItem != null)
                    return retItem;
            }

            return retItem;
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

        private void LoadNavStructure()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string settingsFile = Path.Combine(asmPath, SettingsDir, SettingsFile);

            if (!File.Exists(settingsFile))
                return;

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(settingsFile);

            foreach (XmlNode child in xmlDoc.DocumentElement.SelectNodes("NavItem"))
                LoadNavItem(child);
        }

        private void LoadNavItem(XmlNode xmlnode, WebServerNavItem parent = null)
        {
            WebServerNavItem navItem = new WebServerNavItem();

            if (xmlnode.Attributes["Name"] != null)
                navItem.NavItemName = xmlnode.Attributes["Name"].Value;

            if (xmlnode.Attributes["Target"] != null)
                navItem.Target = xmlnode.Attributes["Target"].Value;

            if (xmlnode.Attributes["ModuleId"] != null)
                navItem.ModuleId = xmlnode.Attributes["ModuleId"].Value;

            if (xmlnode.Attributes["Icon"] != null)
                navItem.Icon = xmlnode.Attributes["Icon"].Value;

            if (xmlnode.Attributes["RequiredRole"] != null)
            {
                if (Enum.TryParse(xmlnode.Attributes["RequiredRole"].Value, true, out ModulynAuthRole role))
                    navItem.RequiredRole = role;
            }

            if (parent != null)
            {
                navItem.NavItemPath = parent.NavItemPath + "\\" + navItem.NavItemName;
                parent.Children.Add(navItem);
            }
            else
            {
                navItem.NavItemPath = navItem.NavItemName;
                NavEntries.Add(navItem);
            }

            foreach(XmlNode child in xmlnode.SelectNodes("NavItem"))
            {
                LoadNavItem(child, navItem);
            }
        }
    }
}
