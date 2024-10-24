namespace Modulyn.Server.Interface
{
    public enum WebServiceScope
    {
        Singleton,
        Transient,
        Scoped
    }

    public class ModuleBuilderService
    {
        public WebServiceScope ServiceScope { get; set; }
        public Type ServiceType { get; set; }
        public object? Service { get; set; } = null;

        public ModuleBuilderService(WebServiceScope serviceScope, Type serviceType)
        {
            ServiceScope = serviceScope;
            ServiceType = serviceType;
        }

        public ModuleBuilderService(object service) : this(WebServiceScope.Singleton, service.GetType())
        {
            Service = service;
        }
    }
}
