namespace Modulyn.Server.Interface
{
    public enum WebServiceScope
    {
        Singleton,
        Transient,
        Scoped,
        HostedService
    }

    public class ModuleBuilderService
    {
        public WebServiceScope ServiceScope { get; set; }
        public Type ServiceType { get; set; } = null;
        public Type ImplementationType { get; set; } = null;
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

        public ModuleBuilderService(WebServiceScope scope, Type serviceType, Type implementationType) : this(scope, serviceType)
        {
            ImplementationType = implementationType;
        }
    }
}
