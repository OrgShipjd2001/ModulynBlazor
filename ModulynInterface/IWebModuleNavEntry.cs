namespace Modulyn.Server.Interface
{
    public interface IWebModuleNavEntry
    {
        string NavItemPath { get; }
        string NavItemName { get; }
        string Target { get; }
        string Icon { get; }

    }
}
