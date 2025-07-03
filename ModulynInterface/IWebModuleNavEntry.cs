namespace Modulyn.Server.Interface
{
    /// <summary>
    /// The ModulynAuthRole enum defines the roles used for authorization.
    /// The Roles will be implemented with containment:
    ///  - Admins have access to everything.
    ///  - PowerUsers have everything except Admin functionality.
    ///  - Users have everything except PowerUser and Admin functionality.
    /// </summary>
    public enum ModulynAuthRole
    {
        Anonymous = 0,
        Admin = 1,
        PowerUser = 2,
        User = 3,
    }

    public interface IWebModuleNavEntry
    {
        string NavItemPath { get; }
        string NavItemName { get; }
        string? Target { get; }
        string? Icon { get; }
        ModulynAuthRole AuthenticationRole { get; }

    }
}
