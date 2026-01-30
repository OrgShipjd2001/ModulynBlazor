using Microsoft.AspNetCore.Components.Authorization;
using Modulyn.Server.Interface;
using ModulynInterface;
using System.Security.Claims;

namespace Modulyn.Server.Bl
{
    public class ModulynAuth : IModulynAuth
    {
        private readonly AuthenticationStateProvider _authenticationStateProvider;

        public ModulynAuth(AuthenticationStateProvider authenticationStateProvider)
        {
            _authenticationStateProvider = authenticationStateProvider;
        }

        public async Task<bool> IsAuthenticationEnabledAsync()
        {
            return WebServerSettings.Instance.Authentication;
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            return authState.User.Identity?.IsAuthenticated ?? false;
        }

        public async Task<bool> IsAuthorizedAsync(ModulynAuthRole role)
        {
            // If authentication is not enabled, always authorize
            if (! await IsAuthenticationEnabledAsync())
                return true;

            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (!user.Identity?.IsAuthenticated == true)
                return false;

            var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            roles.AddRange(user.FindAll("role").Select(c => c.Value));

            List<string> allowedRoles = new List<string>();
            for (int i = (int)role; i > 0; i--)
            {
                allowedRoles.Add(((ModulynAuthRole)i).ToString());
            }

            if (roles.Any(r => allowedRoles.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }
    }
}
