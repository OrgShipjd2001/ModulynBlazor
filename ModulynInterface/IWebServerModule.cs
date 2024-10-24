using System.Reflection;

namespace Modulyn.Server.Interface
{
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
    }
}
