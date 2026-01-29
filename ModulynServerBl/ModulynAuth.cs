using Modulyn.Server.Interface;
using ModulynInterface;
using Microsoft.AspNetCore.Components.Authorization;

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
            return user.Identity?.IsAuthenticated == true && user.IsInRole(role.ToString());
        }
    }
}
