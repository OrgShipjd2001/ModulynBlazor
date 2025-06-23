using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Modulyn.Server.Bl;
using ModulynServer2.Components;
using ModulynServer2.Components.Account;
using ModulynServer2.Data;
using System.Threading.Tasks;

namespace ModulynServer2
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            ConfigureCoreAuthentication(builder);
            ConfigureAuthenticationProviders(builder);

            var app = builder.Build();

            await CreateSeedData(app);

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

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            // Add additional endpoints required by the Identity /Account Razor components.
            app.MapAdditionalIdentityEndpoints();

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
            builder.Services.AddDbContext<ModulynServer2.Data.ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddIdentityCore<ModulynServer2.Data.ApplicationUser>(
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
                .AddEntityFrameworkStores<ModulynServer2.Data.ApplicationDbContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();

            builder.Services.AddSingleton<IEmailSender<ModulynServer2.Data.ApplicationUser>, IdentityNoOpEmailSender>();
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
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ModulynServer2.Data.ApplicationUser>>();

                // Seed roles
                string[] roleNames = { "Admin", "User", "Manager" };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // Seed admin user
                string adminEmail = "admin@example.com";
                string adminPassword = "admin$123"; // Use a strong password in production
                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    adminUser = new ModulynServer2.Data.ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
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
