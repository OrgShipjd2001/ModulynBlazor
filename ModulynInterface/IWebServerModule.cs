using System.Reflection;

namespace Modulyn.Server.Interface
{
    public enum ModuleAppUseFlags
    {
        None = 0,
        Websockets = 1
    }

    public interface IWebServerModule
    {
        string ModuleId { get; }
        string DisplayName { get; }
        string Description { get; }
        string Image { get; }
        Assembly ModuleAssembly { get; }

        void InitializeModule(List<string> AvailableServerModuleIds);

        List<IWebModuleNavEntry> GetModuleNavEntries();
        List<ModuleBuilderService>? GetWebBuilderServices();
        Dictionary<Type, List<object>> GetWebAppMiddleware();
        ModuleAppUseFlags GetModuleAppUseFlags();

        /// <summary>
        /// Return the list of user groups this module requires/uses. The host can use this for
        /// seeding and validation (e.g., ensuring groups exist in the auth DB).
        /// </summary>
        List<ModuleUserGroupDefinition>? GetRequiredUserGroups();
    }
}
