using Microsoft.Extensions.FileProviders;
using Radzen;
using ModulynServer.Components;
using Modulyn.Server.Bl;
using Modulyn.Server.Interface;
using System.Reflection;
using Lumberjack.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Components.Server;

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

            AddAuthentication(builder);
            builder.Services.AddAuthorization();
            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();



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

            // --- Database Migration Call ---
            // This section should only run if local authentication is enabled.
            // You need to scope a service provider to get the DbContext.
            // This ensures the DbContext is properly disposed after use.
            if (WebServerSettings.Instance.AuthSettings.Any(a => a.Provider.Equals("local", StringComparison.OrdinalIgnoreCase) && a.Enabled))
            {
                using (var scope = app.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    try
                    {
                        var context = services.GetRequiredService<ApplicationDbContext>();
                        context.Database.Migrate();
                        // Optional: Seed initial user/roles if needed
                        // var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
                        // var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                        // await SeedData.Initialize(services, userManager, roleManager); // Call a seeding method
                        Logging.LogInfo("Database migration completed successfully for local authentication.", "Modulyn");
                    }
                    catch (Exception ex)
                    {
                        // Log any errors that occur during migration
                        var logger = services.GetRequiredService<ILogger<Program>>();
                        logger.LogError(ex, "An error occurred while migrating the database for local authentication.");
                        Logging.LogError("An error occurred while migrating the database for local authentication: " + ex.Message, "Modulyn");
                    }
                }
            }
            // --- End Database Migration Call ---

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

        private static void AddAuthentication(WebApplicationBuilder builder)
        {
            WebServerSettings settings = WebServerSettings.Instance;

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            });

            if (!settings.Authentication)
            {
                builder.Services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, NoAuthHandler>("None", null);
                return;
            }

            foreach(WebServerAuthSettings auth in settings.AuthSettings)
            {
                if ((auth.Provider.Equals("local", StringComparison.OrdinalIgnoreCase)) && auth.Enabled)
                {
                    string connectionString = auth.Properties["ConnectionString"];
                    builder.Services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlServer(connectionString));

                    builder.Services.AddDefaultIdentity<IdentityUser>(options =>
                    {
                        options.SignIn.RequireConfirmedAccount = false;
                        options.Password.RequireDigit = true;
                        options.Password.RequiredLength = 8;
                        options.Password.RequireNonAlphanumeric = false;
                        options.Password.RequireUppercase = true;
                        options.Password.RequireLowercase = true;
                        options.Password.RequiredUniqueChars = 1;
                    })
                    .AddEntityFrameworkStores<ApplicationDbContext>();
                }

                if ((auth.Provider.Equals("windows", StringComparison.OrdinalIgnoreCase)) && auth.Enabled)
                {
                    builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
                }

                if ((auth.Provider.Equals("entraid", StringComparison.OrdinalIgnoreCase)) && auth.Enabled)
                {
                    string instance = auth.Properties["Instance"];
                    string tenantId = auth.Properties["TenantId"];
                    string clientId = auth.Properties["ClientId"];
                    string clientSecret = auth.Properties["ClientSecret"];
                    builder.Services.AddAuthentication().AddOpenIdConnect("EntraID", options =>
                    {
                        options.Authority = $"{instance}{tenantId}";
                        options.ClientId = clientId;
                        options.ClientSecret = clientSecret;
                        options.ResponseType = "code";
                        options.SaveTokens = true;
                        // Add other scopes as needed
                    }).AddCookie();
                }

                if ((auth.Provider.Equals("google", StringComparison.OrdinalIgnoreCase)) && auth.Enabled)
                {
                    string clientId = auth.Properties["ClientId"];
                    string clientSecret = auth.Properties["ClientSecret"];
                    builder.Services.AddAuthentication().AddGoogle(options =>
                    {
                        options.ClientId = clientId;
                        options.ClientSecret = clientSecret;
                    });
                }

                if ((auth.Provider.Equals("microsoftaccount", StringComparison.OrdinalIgnoreCase)) && auth.Enabled)
                {
                    string clientId = auth.Properties["ClientId"];
                    string clientSecret = auth.Properties["ClientSecret"];
                    builder.Services.AddAuthentication().AddMicrosoftAccount(options =>
                    {
                        options.ClientId = clientId;
                        options.ClientSecret = clientSecret;
                    });
                }
            }
            
        }
    }
}
