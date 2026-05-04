using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Modulyn.Server.Bl;
using Modulyn.Server.Bl.IdentityGroups;
using System.Security.Claims;

namespace ModulynServer.Handlers;

public sealed class GroupClaimsTransformation : IClaimsTransformation
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public GroupClaimsTransformation(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return principal;

        string? userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            // Fallback to Identity user lookup by name.
            var name = principal.Identity?.Name;
            if (string.IsNullOrWhiteSpace(name))
                return principal;

            var user = await _userManager.FindByNameAsync(name);
            userId = user?.Id;
        }

        if (string.IsNullOrWhiteSpace(userId))
            return principal;

        // Ensure we can add claims (some principals may have non-ClaimsIdentity identities)
        var identity = principal.Identities.FirstOrDefault(i => i.IsAuthenticated) as ClaimsIdentity;
        if (identity == null)
            return principal;

        // Refresh group claims: remove existing ones and add current effective memberships.
        foreach (var existing in identity.FindAll(GroupClaimTypes.Group).ToList())
            identity.RemoveClaim(existing);

        var groupNames = await GroupMembershipResolver.GetEffectiveGroupNamesForUserAsync(_db, userId);
        if (groupNames.Count == 0)
            return principal;

        foreach (var groupName in groupNames)
            identity.AddClaim(new Claim(GroupClaimTypes.Group, groupName));

        return principal;
    }
}
