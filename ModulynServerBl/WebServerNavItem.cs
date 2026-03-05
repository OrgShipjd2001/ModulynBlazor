using Modulyn.Server.Interface;

namespace Modulyn.Server.Bl
{
    public class WebServerNavItem
    {
        private string? _target = null;

        public string NavItemPath { get; set; } = string.Empty;
        public string NavItemName { get; set; } = string.Empty;
        public string ModuleId { get; set; } = string.Empty;
        public string? Target 
        {
            get => _target;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    if (value.StartsWith("/"))
                    {
                        _target = value.Substring(1);
                    }
                }
                else
                {
                    _target = value;
                }
            }
        }
        public string? Icon { get; set; } = null;
        public ModulynAuthRole RequiredRole { get; set; } = 0;

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
