using Azure.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Modulyn.Server.Bl;
using Modulyn.Server.Bl.IdentityGroups;
using Radzen;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace ModulynServer.Handlers
{
    /// <summary>
    /// Custom auth handler that reads X-User from headers and converts it to an authenticated user.
    /// </summary>
    public class HttpAuthHeaderHandler : AuthenticationHandler<HttpAuthHeaderOptions>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _db;

        public HttpAuthHeaderHandler(
            IOptionsMonitor<HttpAuthHeaderOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext db)
            : base(options, logger, encoder, clock) 
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var usernameHeader = Options.UserHeader;
            var emailHeader = Options.EmailHeader;

            if (!Request.Headers.TryGetValue(usernameHeader, out var usernameValues) ||
                string.IsNullOrWhiteSpace(usernameValues))
            {
                return AuthenticateResult.NoResult();
            }

            var username = usernameValues.ToString();
            string? email = null;

            if (!string.IsNullOrEmpty(emailHeader) &&
                Request.Headers.TryGetValue(emailHeader, out var emailValues))
            {
                email = emailValues.ToString();
            }

            // Find or create the user in Identity
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = username,
                    Email = email ?? $"{username}@unknown.local"
                };

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    var reason = string.Join(", ", result.Errors.Select(e => e.Description));
                    return AuthenticateResult.Fail($"Failed to create user: {reason}");
                }

                // Optional: assign default role(s)
                await _userManager.AddToRoleAsync(user, "User");
            }

            // Load user roles and claims
            var userClaims = await _userManager.GetClaimsAsync(user);
            var userRoles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName)
            };

            if (!string.IsNullOrEmpty(user.Email))
                claims.Add(new Claim(ClaimTypes.Email, user.Email));

            foreach (var role in userRoles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var groupNames = await _db.UserGroups
                .Where(ug => ug.UserId == user.Id)
                .Select(ug => ug.Group.Name)
                .ToListAsync();

            foreach (var groupName in groupNames.Distinct(StringComparer.OrdinalIgnoreCase))
                claims.Add(new Claim(GroupClaimTypes.Group, groupName));

            claims.AddRange(userClaims);

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
    }
}
