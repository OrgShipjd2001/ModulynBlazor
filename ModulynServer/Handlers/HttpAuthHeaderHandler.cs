using Azure.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
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
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public HttpAuthHeaderHandler(
            IOptionsMonitor<HttpAuthHeaderOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
            : base(options, logger, encoder, clock) 
        {
            _userManager = userManager;
            _signInManager = signInManager;
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
                user = new IdentityUser
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

            claims.AddRange(userClaims);

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);

            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
    }
}
