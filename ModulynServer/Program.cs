using Lumberjack.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Modulyn.Server.Bl;
using Modulyn.Server.Interface;
using ModulynServer.Components;
using ModulynServer.Components.Account;
using ModulynServer.Handlers;
using Radzen;
using System.Reflection;

namespace Modulyn.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
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

            if (WebServerSettings.Instance.Authentication)
            {
                Logging.LogInfo("Authentication enabled", "Modulyn");
                ConfigureCoreAuthentication(builder);
                ConfigureAuthenticationProviders(builder);
            }

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

            if (WebServerSettings.Instance.Authentication)
            {
                Logging.LogInfo("Add Authentication Seed Data", "Modulyn");
                await CreateSeedData(app);
            }
            
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
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

            List<Assembly> assemblies = new List<Assembly>();
            foreach (IWebServerModule module in moduleManager.GetModuleList())
            {
                assemblies.Add(module.ModuleAssembly);
            }

            app.MapRazorComponents<App>().AddInteractiveServerRenderMode().AddAdditionalAssemblies(assemblies.ToArray());

            if (WebServerSettings.Instance.Authentication)
            {
                app.UseAuthentication(); // Must be before UseAuthorization
                app.UseAuthorization();
            }

            app.UseAntiforgery();

            if (WebServerSettings.Instance.Authentication)
            {
                // Add additional endpoints required by the Identity /Account Razor components.
                app.MapAdditionalIdentityEndpoints();
            }

            app.Run();
        }

        private static void ConfigureCoreAuthentication(WebApplicationBuilder builder)
        {
            WebServerSettings settings = WebServerSettings.Instance;

            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<IdentityUserAccessor>();
            builder.Services.AddScoped<IdentityRedirectManager>();
            builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
                .AddIdentityCookies();

            var connectionString = settings.AuthDbConnectionString;
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddIdentityCore<ApplicationUser>(
                options =>
                {
                    options.SignIn.RequireConfirmedAccount = true;
                    options.Password.RequireDigit = true;
                    options.Password.RequiredLength = 8;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequiredUniqueChars = 1;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();

            builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

            builder.Services.AddSingleton<IAuthorizationHandler, ModulynAuthHandler>();
            builder.Services.AddSingleton<IAuthorizationPolicyProvider, ModulynAuthPolicyProvider>();

            builder.Services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new ModulynAuthRequirement())
                    .Build();
            });
        }

        private static void ConfigureAuthenticationProviders(WebApplicationBuilder builder)
        {
            WebServerSettings settings = WebServerSettings.Instance;
            foreach (WebServerAuthSettings auth in settings.AuthSettings)
            {
                if ((auth.Provider.Equals("entraid", StringComparison.OrdinalIgnoreCase)) && auth.Enabled)
                {
                    string instance = auth.Properties["Instance"];
                    string tenantId = auth.Properties["TenantId"];
                    string clientId = auth.Properties["ClientId"];
                    string clientSecret = auth.Properties["ClientSecret"];
                    builder.Services.AddAuthentication().AddOpenIdConnect("EntraID", options =>
                    {
                        options.Authority = $"https://login.microsoftonline.com/{tenantId}";
                        options.ClientId = clientId;
                        options.ClientSecret = clientSecret;
                        options.ResponseType = "code";
                        options.SaveTokens = true;
                        options.RequireHttpsMetadata = false;
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

        private static async Task CreateSeedData(WebApplication app)
        {
            // Seed roles and admin user
            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                // Seed roles
                string[] roleNames = Enum.GetNames(typeof(ModulynAuthRole));
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // Seed admin user
                string adminEmail = "admin@example.com";
                string adminPassword = "Admin$123"; // Use a strong password in production
                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                    var result = await userManager.CreateAsync(adminUser, adminPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                }
            }
        }
    }
}
