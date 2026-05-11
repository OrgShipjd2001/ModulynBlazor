using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Modulyn.Server.Interface;
using ModulynInterface;
using Modulyn.Server.Bl.IdentityGroups;
using System.Security.Claims;

namespace Modulyn.Server.Bl
{
    public class ModulynAuth : IModulynAuth
    {
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly ApplicationDbContext _db;

        public ModulynAuth(AuthenticationStateProvider authenticationStateProvider, ApplicationDbContext db)
        {
            _authenticationStateProvider = authenticationStateProvider;
            _db = db;
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

        public async Task<bool> IsInGroupAsync(string group)
        {
            if (!await IsAuthenticationEnabledAsync())
                return true;

            if (string.IsNullOrWhiteSpace(group))
                return false;

            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user?.Identity?.IsAuthenticated != true)
                return false;

            var groups = user.FindAll(GroupClaimTypes.Group).Select(c => c.Value);
            return groups.Any(g => string.Equals(g, group, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<IReadOnlyList<string>> GetAvailableGroupsAsync()
        {
            if (!await IsAuthenticationEnabledAsync())
            {
                return await _db.Groups
                    .AsNoTracking()
                    .OrderBy(g => g.Name)
                    .Select(g => g.Name)
                    .ToListAsync();
            }

            if ((!await IsAuthorizedAsync(ModulynAuthRole.Admin)) && (!await IsInGroupAsync(ModulynSystemGroupNames.Admins)))
                throw new UnauthorizedAccessException("Only Admins can retrieve available groups.");

            return await _db.Groups
                .AsNoTracking()
                .OrderBy(g => g.IsSystem)
                .ThenBy(g => g.Name)
                .Select(g => g.Name)
                .ToListAsync();
        }
    }
}
