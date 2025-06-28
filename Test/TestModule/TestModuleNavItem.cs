using Modulyn.Server.Interface;

namespace TestModule
{
    public class TestModuleNavItem : IWebModuleNavEntry
    {
        public string NavItemPath { get; set; } = string.Empty;

        public string NavItemName { get; set; } = "Test Module";

        public string? Target { get; set; } = null;

        public string Icon { get; set; } = string.Empty;

        public ModulynAuthRole AuthenticationRole { get; set; } = ModulynAuthRole.User;

        public TestModuleNavItem()
        {
        }

        public TestModuleNavItem(string navItemPath, string navItemName, string? target)
        {
            NavItemPath = navItemPath;
            NavItemName = navItemName;
            Target = target;
        }

        public TestModuleNavItem(string navItemPath, string navItemName, string? target, string icon) : this(navItemPath, navItemName, target)
        {
            Icon = icon;
        }

        public TestModuleNavItem(string navItemPath, string navItemName, string? target, string icon, ModulynAuthRole role) : this(navItemPath, navItemName, target, icon)
        {
            AuthenticationRole = role;
        }
    }
}
