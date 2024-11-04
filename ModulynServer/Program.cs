using Microsoft.Extensions.FileProviders;
using Radzen;
using ModulynServer.Components;
using Modulyn.Server.Bl;
using Modulyn.Server.Interface;
using System.Reflection;

namespace Modulyn.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
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

            WebApplication app = builder.Build();
            
            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            // Add the module folders to the file provider
            List<IFileProvider> providerList = new List<IFileProvider>();
            providerList.Add(app.Environment.WebRootFileProvider);
            foreach(IWebServerModule module in moduleManager.GetModuleList())
            {
                PhysicalFileProvider moduleProvider = new PhysicalFileProvider(Path.Combine(Path.GetDirectoryName(module.ModuleAssembly.Location), "wwwroot"));
                providerList.Add(moduleProvider);
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
