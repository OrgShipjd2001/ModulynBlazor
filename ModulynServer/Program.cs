using Microsoft.Extensions.FileProviders;
using Radzen;
using ModulynServer.Components;
using Modulyn.Server.Bl;
using Modulyn.Server.Interface;
using System.Reflection;
using Lumberjack.Interface;

namespace Modulyn.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string logPath = Path.Combine(asmPath, "Logs");
            if (!Directory.Exists(logPath))
                Directory.CreateDirectory(logPath);
            string logFile = Path.Combine(logPath, "Log_ModulynServer.log");
            Logging.CreateLogFile(logFile, "Modulyn");

            Logging.LogInfo("Begin WebApplicationBuilder part", "Modulyn");

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            WebServerModuleManager moduleManager = new WebServerModuleManager();

            // Add services to the container.
            builder.Services.AddRazorComponents().AddInteractiveServerComponents();
            builder.Services.AddRadzenComponents();
            builder.Services.AddControllers();
            builder.Services.AddSingleton(WebServerSettings.Instance);
            builder.Services.AddSingleton(moduleManager);

            // Add module services
            foreach (IWebServerModule module in moduleManager.GetModuleList())
            {
                builder.Services.AddControllers().AddApplicationPart(module.ModuleAssembly);

                List<ModuleBuilderService>? services = module.GetWebBuilderServices();
                if(services != null)
                {
                    foreach (ModuleBuilderService service in services)
                    {
                        Logging.LogInfo("Adding service: " + service.ServiceType.Name + " - " + service.ServiceScope.ToString() + " - " + service.Service?.GetType().Name, "Modulyn");
                        switch (service.ServiceScope)
                        {
                            case WebServiceScope.Singleton:
                                builder.Services.AddSingleton(service.ServiceType, service.Service);
                                break;
                            case WebServiceScope.Transient:
                                builder.Services.AddTransient(service.ServiceType);
                                break;
                            case WebServiceScope.Scoped:
                                builder.Services.AddScoped(service.ServiceType);
                                break;
                        }
                    }
                }
            }

            Logging.LogInfo("Begin WebApplication part");
            WebApplication app = builder.Build();
            
            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            // Add the module specific files, middleware, etc.
            List<IFileProvider> providerList = new List<IFileProvider>();
            providerList.Add(app.Environment.WebRootFileProvider);
            foreach(IWebServerModule module in moduleManager.GetModuleList())
            {
                PhysicalFileProvider moduleProvider = new PhysicalFileProvider(Path.Combine(Path.GetDirectoryName(module.ModuleAssembly.Location), "wwwroot"));
                if (moduleProvider.GetDirectoryContents(string.Empty).Exists)
                {
                    Logging.LogInfo("Adding module file provider: " + module.ModuleId, "Modulyn");
                    providerList.Add(moduleProvider);
                }

                Dictionary<Type, List<object>> middleware = module.GetWebAppMiddleware();
                foreach (Type type in middleware.Keys)
                {
                    Logging.LogInfo("Adding middleware: " + type.Name, "Modulyn");
                    app.UseMiddleware(type, middleware[type].ToArray());
                }

                ModuleAppUseFlags moduleAppUseFlags = module.GetModuleAppUseFlags();
                if (moduleAppUseFlags.HasFlag(ModuleAppUseFlags.Websockets))
                {
                    Logging.LogInfo("Adding websockets: " + module.ModuleId, "Modulyn");
                    app.UseWebSockets();
                }
            }
            app.Environment.WebRootFileProvider = new CompositeFileProvider(providerList);

            app.UseStaticFiles();
            app.UseRouting();
            app.MapControllers();
            app.UseAntiforgery();

            List<Assembly> assemblies = new List<Assembly>();
            foreach (IWebServerModule module in moduleManager.GetModuleList())
            {
                assemblies.Add(module.ModuleAssembly);
            }

            app.MapRazorComponents<App>().AddInteractiveServerRenderMode().AddAdditionalAssemblies(assemblies.ToArray());
            
            app.Run();
        }
    }
}
