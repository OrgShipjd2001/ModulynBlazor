namespace Modulyn.Server.Interface
{
    public class WebModuleNavEntry : IWebModuleNavEntry
    {
        public string NavItemPath { get; set; } = string.Empty;

        public string NavItemName { get; set; } = string.Empty;

        public string? Target { get; set; } = null;

        public string? Icon { get; set; } = null;

        public ModulynAuthRole AuthenticationRole { get; set; } = ModulynAuthRole.User;

        /// <summary>
        /// Optional group-based requirement for navigation visibility/access.
        /// If set, the host can require the current user to be in this group.
        /// </summary>
        public string? AuthenticationGroup { get; set; } = null;

        public WebModuleNavEntry()
        {
        }

        public WebModuleNavEntry(string navItemPath, string navItemName) : this()
        {
            NavItemPath = navItemPath;
            NavItemName = navItemName;
        }

        public WebModuleNavEntry(string navItemPath, string navItemName, string? target) : this(navItemPath, navItemName)
        {
            Target = target;
        }

        public WebModuleNavEntry(string navItemPath, string navItemName, string? target, string icon) : this(navItemPath, navItemName, target)
        {
            Icon = icon;
        }

        public WebModuleNavEntry(string navItemPath, string navItemName, string? target, string icon, ModulynAuthRole role) : this(navItemPath, navItemName, target, icon)
        {
            AuthenticationRole = role;
        }

        public WebModuleNavEntry(string navItemPath, string navItemName, string? target, string icon, string group) : this(navItemPath, navItemName, target, icon)
        {
            AuthenticationGroup = group;
        }
    }
}
