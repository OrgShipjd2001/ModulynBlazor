using Lumberjack.Interface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Modulyn.Server.Bl;
using Modulyn.Server.Bl.IdentityGroups;
using Modulyn.Server.Interface;
using ModulynInterface;
using ModulynServer.Components;
using ModulynServer.Components.Account;
using ModulynServer.Handlers;
using ModulynServer.Services;
using Radzen;
using System.Reflection;
using System.Text;

namespace Modulyn.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            bool applyMigrations = HasArg(args, "--migrate", "--migrations", "--apply-migrations");

            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string logPath = Path.Combine(asmPath, "Logs");
            if (!Directory.Exists(logPath))
                Directory.CreateDirectory(logPath);

            Logging.CreateConsoleLog();
            string globallogFile = Path.Combine(logPath, "Log_ModulynServer.log");
            Logging.CreateLogFile(globallogFile);

            string logFile = Path.Combine(logPath, "Log_ModulynServer_Modulyn.log");
            Logging.CreateLogFile(logFile, "Modulyn");

            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                Exception ex = (Exception)eventArgs.ExceptionObject;
                Logging.LogError("Unhandled exception: " + ex.ToString(), "Modulyn");
            };

            Logging.LogInfo("Begin WebApplicationBuilder part", "Modulyn");

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            WebServerModuleManager moduleManager = new WebServerModuleManager();

            // Add services to the container.
            builder.Services.AddRazorComponents().AddInteractiveServerComponents();
            builder.Services.AddRadzenComponents();
            builder.Services.AddServerSideBlazor(options =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    options.DetailedErrors = true;
                }
                options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(10);
            })
            .AddHubOptions(options =>
            {
                options.MaximumReceiveMessageSize = 100 * 1024 * 1024; // 100MB
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            });
            builder.Services.AddControllers();
            builder.Services.AddSingleton(WebServerSettings.Instance);
            builder.Services.AddSingleton(moduleManager);

            if (WebServerSettings.Instance.Authentication)
            {
                Logging.LogInfo("Authentication enabled", "Modulyn");
                ConfigureCoreAuthentication(builder);
                ConfigureAuthenticationProviders(builder);
            }

            // Authentication must always be configured, even if authentication is disabled
            builder.Services.AddSingleton<IAuthorizationHandler, ModulynAuthHandler>();
            builder.Services.AddSingleton<IAuthorizationPolicyProvider, ModulynAuthPolicyProvider>();
            builder.Services.AddScoped<IAuthorizationHandler, ModulynGroupAuthHandler>();
            builder.Services.AddSingleton<IAuthorizationPolicyProvider, ModulynGroupAuthPolicyProvider>();
            builder.Services.AddScoped<IClaimsTransformation, GroupClaimsTransformation>();
            builder.Services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    //.AddAuthenticationSchemes(RestApiAuthAttribute.SchemeName)
                    .RequireAuthenticatedUser()
                    .AddRequirements(new ModulynAuthRequirement())
                    .Build();

                foreach(ModulynAuthRole role in Enum.GetValues(typeof(ModulynAuthRole)))
                {
                    // Keep supporting role-name policies, even though pages use the prefixed policy name
                    options.AddPolicy(role.ToString(), policy =>
                    {
                        policy.AddRequirements(new ModulynAuthRequirement(role));
                    });

                    // Support ModulynAuthAttribute's policy name format: "PageAccessPolicy:{Role}"
                    options.AddPolicy($"PageAccessPolicy:{role}", policy =>
                    {
                        policy.AddRequirements(new ModulynAuthRequirement(role));
                    });
                }
            });

            // Register ModulynAuth for DI
            builder.Services.AddScoped<IModulynAuth, ModulynAuth>();

            builder.Services.AddScoped<PersonalAccessTokenService>();

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
                                if (service.Service != null)
                                    builder.Services.AddSingleton(service.ServiceType, service.Service);
                                
                                if ((service.Service == null) && (service.ImplementationType != null))
                                    builder.Services.AddSingleton(service.ServiceType, service.ImplementationType);
                                break;
                            case WebServiceScope.Transient:
                                if (service.ImplementationType == null)
                                    builder.Services.AddTransient(service.ServiceType);
                                else
                                    builder.Services.AddTransient(service.ServiceType, service.ImplementationType);
                                break;
                            case WebServiceScope.Scoped:
                                if (service.ImplementationType == null)
                                    builder.Services.AddScoped(service.ServiceType);
                                else
                                    builder.Services.AddScoped(service.ServiceType, service.ImplementationType);
                                break;
                            case WebServiceScope.HostedService:
                                    builder.Services.AddSingleton(typeof(IHostedService), service.ServiceType);
                                break;
                        }
                    }
                }
            }

            Logging.LogInfo("Begin WebApplication part");
            WebApplication app = builder.Build();

            if (WebServerSettings.Instance.Authentication)
            {
                // Support both the new command-line switch and the legacy file sentinel.
                // Legacy sentinel file can still be used in environments where adding CLI args is difficult.
                if (applyMigrations || File.Exists(Path.Combine(asmPath, "runmigrations.txt")))
                {
                    Logging.LogInfo("Running database migrations", "Modulyn");
                    using (var scope = app.Services.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        db.Database.Migrate();
                    }

                    if (File.Exists(Path.Combine(asmPath, "runmigrations.txt")))
                    {
                        Logging.LogInfo("Deleting migration sentinel file: runmigrations.txt", "Modulyn");
                        File.Delete(Path.Combine(asmPath, "runmigrations.txt"));
                    }
                }

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
            if (app.Environment.WebRootFileProvider is CompositeFileProvider)
            {
                foreach (IFileProvider provider in ((CompositeFileProvider)app.Environment.WebRootFileProvider).FileProviders)
                {
                    if (provider is PhysicalFileProvider)
                        Logging.LogInfo("Adding existing file provider: " + provider.GetType().Name + " Path: " + ((PhysicalFileProvider)provider).Root, "Modulyn");
                    else
                        Logging.LogInfo("Adding existing file provider: " + provider.GetType().Name, "Modulyn");

                    providerList.Add(provider);
                }
            }
            else
            {
                Logging.LogInfo("Adding existing file provider: " + app.Environment.WebRootFileProvider.GetType().Name, "Modulyn");
                providerList.Add(app.Environment.WebRootFileProvider);
            }

            bool providerListChanged = false;
            foreach(IWebServerModule module in moduleManager.GetModuleList())
            {
                providerListChanged = true;
                PhysicalFileProvider moduleProvider = new PhysicalFileProvider(Path.Combine(Path.GetDirectoryName(module.ModuleAssembly.Location), "wwwroot"));
                if (moduleProvider.GetDirectoryContents(string.Empty).Exists)
                {
                    Logging.LogInfo("Adding module file provider: " + module.ModuleId + " Path: " + moduleProvider.Root, "Modulyn");
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
            if (providerListChanged)
                app.Environment.WebRootFileProvider = new CompositeFileProvider(providerList);

            app.UseStaticFiles();
            app.UseRouting();
            app.MapControllers();

            List<Assembly> assemblies = new List<Assembly>();
            foreach (IWebServerModule module in moduleManager.GetModuleList())
            {
                assemblies.Add(module.ModuleAssembly);
            }

            if (WebServerSettings.Instance.Authentication)
            {
                app.UseAuthentication(); // Must be before UseAuthorization
            }
            app.UseAuthorization();

            app.MapRazorComponents<App>().AddInteractiveServerRenderMode().AddAdditionalAssemblies(assemblies.ToArray());
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
                options.DefaultScheme = "SmartScheme";
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddPolicyScheme("SmartScheme", "Select scheme at runtime", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authHeader = context.Request.Headers.Authorization.ToString();

                    if (settings.IsAuthEnabled("HttpAuthHeader"))
                    {
                        var provider = settings.GetAuthProvider("HttpAuthHeader");
                        string userHeader = "X-User";
                        if (provider?.Properties != null &&
                            provider.Properties.TryGetValue("UserHeader", out var headerValue) &&
                            !string.IsNullOrWhiteSpace(headerValue))
                        {
                            userHeader = headerValue;
                        }
                        if (context.Request.Headers.ContainsKey(userHeader))
                            return "HttpAuthHeader";
                    }

                    if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        return RestApiAuthAttribute.SchemeName;

                    return IdentityConstants.ApplicationScheme;
                };
            })
            .AddIdentityCookies();

            builder.Services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, PersonalAccessTokenAuthenticationHandler>(
                    RestApiAuthAttribute.SchemeName,
                    _ => { });

            // REST API authentication for non-browser clients.
            // Uses a symmetric signing key configured via environment variable `MODULYN_API_JWT_KEY`.
            // (You can later swap this to an external IdP / JWKS without changing controller code.)
            // NOTE: You can later swap "ApiBearer" to JwtBearer (external IdP) without changing controllers.

            var connectionString = settings.AuthDbConnectionString;
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddIdentityCore<ApplicationUser>(
                options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
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
        }

        private static void ConfigureAuthenticationProviders(WebApplicationBuilder builder)
        {
            WebServerSettings settings = WebServerSettings.Instance;
            foreach (WebServerAuthSettings auth in settings.AuthSettings)
            {
                if ((auth.Provider.Equals("httpauthheader", StringComparison.OrdinalIgnoreCase)) && auth.Enabled)
                {
                    builder.Services.AddAuthentication()
                        .AddScheme<HttpAuthHeaderOptions, HttpAuthHeaderHandler>("HttpAuthHeader", options =>
                        {
                            options.UserHeader = auth.Properties.ContainsKey("UserHeader") ? auth.Properties["UserHeader"] : "X-User";
                            options.EmailHeader = auth.Properties.ContainsKey("EmailHeader") ? auth.Properties["EmailHeader"] : "X-Email";
                        });
                }
                    
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
                        options.RequireHttpsMetadata = true;
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

        private static bool HasArg(string[] args, params string[] names)
        {
            if (args is null || args.Length == 0)
                return false;

            foreach (string arg in args)
            {
                foreach (string name in names)
                {
                    if (string.Equals(arg, name, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        private static async Task CreateSeedData(WebApplication app)
        {
            // Seed roles and admin user
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var moduleManager = scope.ServiceProvider.GetRequiredService<WebServerModuleManager>();

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

                // Only create the admin user if there are no admins
                IList<ApplicationUser> adminList = await userManager.GetUsersInRoleAsync("Admin");
                if ((adminList.Count == 0) && (adminUser == null))
                {
                    adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                    var result = await userManager.CreateAsync(adminUser, adminPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                }

                // Seed default groups
                var adminsGroup = await db.Groups.FirstOrDefaultAsync(g => g.Name == ModulynSystemGroupNames.Admins);
                if (adminsGroup == null)
                {
                    adminsGroup = new ApplicationGroup { Name = ModulynSystemGroupNames.Admins, IsSystem = true };
                    db.Groups.Add(adminsGroup);
                    await db.SaveChangesAsync();
                }
                else if (!adminsGroup.IsSystem)
                {
                    adminsGroup.IsSystem = true;
                    await db.SaveChangesAsync();
                }

                var powerUsersGroup = await db.Groups.FirstOrDefaultAsync(g => g.Name == ModulynSystemGroupNames.PowerUsers);
                if (powerUsersGroup == null)
                {
                    powerUsersGroup = new ApplicationGroup { Name = ModulynSystemGroupNames.PowerUsers, IsSystem = true };
                    db.Groups.Add(powerUsersGroup);
                    await db.SaveChangesAsync();
                }
                else if (!powerUsersGroup.IsSystem)
                {
                    powerUsersGroup.IsSystem = true;
                    await db.SaveChangesAsync();
                }

                var usersGroup = await db.Groups.FirstOrDefaultAsync(g => g.Name == ModulynSystemGroupNames.Users);
                if (usersGroup == null)
                {
                    usersGroup = new ApplicationGroup { Name = ModulynSystemGroupNames.Users, IsSystem = true };
                    db.Groups.Add(usersGroup);
                    await db.SaveChangesAsync();
                }
                else if (!usersGroup.IsSystem)
                {
                    usersGroup.IsSystem = true;
                    await db.SaveChangesAsync();
                }

                if (adminUser != null)
                {
                    bool isMember = await db.UserGroups.AnyAsync(ug => ug.UserId == adminUser.Id && ug.GroupId == adminsGroup.Id);
                    if (!isMember)
                    {
                        db.UserGroups.Add(new ApplicationUserGroup { UserId = adminUser.Id, GroupId = adminsGroup.Id });
                        await db.SaveChangesAsync();
                    }
                }

                // Ensure module-required groups exist
                foreach (var module in moduleManager.GetModuleList())
                {
                    var required = module.GetRequiredUserGroups();
                    if (required == null)
                        continue;

                    foreach (var def in required)
                    {
                        var groupName = (def?.Name ?? string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(groupName))
                            continue;

                        var existing = await db.Groups
                            .OrderByDescending(g => g.IsSystem)
                            .FirstOrDefaultAsync(g => g.Name.ToLower() == groupName.ToLower());
                        if (existing == null)
                        {
                            db.Groups.Add(new ApplicationGroup { Name = groupName, IsSystem = true });
                            await db.SaveChangesAsync();
                        }
                        else if (!existing.IsSystem)
                        {
                            existing.IsSystem = true;
                            await db.SaveChangesAsync();
                        }
                    }
                }

                // Ensure module-required group nesting exists (Parent contains Child)
                foreach (var module in moduleManager.GetModuleList())
                {
                    var required = module.GetRequiredUserGroups();
                    if (required == null)
                        continue;

                    foreach (var def in required)
                    {
                        if (def == null)
                            continue;

                        var parentName = (def.Name ?? string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(parentName))
                            continue;

                        // If the module-defined parent group already exists, do not re-initialize its nesting.
                        // This prevents resetting plugin group configuration on each server start.
                        bool parentAlreadyExists = await db.Groups.AnyAsync(g => g.Name.ToLower() == parentName.ToLower());
                        if (parentAlreadyExists)
                            continue;

                        var parent = await db.Groups
                            .OrderByDescending(g => g.IsSystem)
                            .FirstOrDefaultAsync(g => g.Name.ToLower() == parentName.ToLower());
                        if (parent == null)
                            continue;

                        foreach (var childRaw in def.IncludesGroups ?? new List<string>())
                        {
                            var childName = (childRaw ?? string.Empty).Trim();
                            if (string.IsNullOrWhiteSpace(childName))
                                continue;

                            var child = await db.Groups
                                .OrderByDescending(g => g.IsSystem)
                                .FirstOrDefaultAsync(g => g.Name.ToLower() == childName.ToLower());
                            if (child == null)
                            {
                                child = new ApplicationGroup { Name = childName };
                                db.Groups.Add(child);
                                await db.SaveChangesAsync();
                            }

                            bool linkExists = await db.GroupGroups.AnyAsync(gg => gg.ParentGroupId == parent.Id && gg.ChildGroupId == child.Id);
                            if (!linkExists)
                            {
                                db.GroupGroups.Add(new ApplicationGroupGroup { ParentGroupId = parent.Id, ChildGroupId = child.Id });
                                await db.SaveChangesAsync();
                            }
                        }
                    }
                }
            }
        }
    }
}
