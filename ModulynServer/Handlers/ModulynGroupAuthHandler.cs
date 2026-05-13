using Microsoft.AspNetCore.Authorization;
using Modulyn.Server.Bl;
using Microsoft.EntityFrameworkCore;
using Modulyn.Server.Bl.IdentityGroups;
using System.Security.Claims;

namespace ModulynServer.Handlers;

public sealed class ModulynGroupAuthHandler : AuthorizationHandler<ModulynGroupAuthRequirement>
{
    private readonly ApplicationDbContext _db;

    public ModulynGroupAuthHandler(ApplicationDbContext db)
    {
        _db = db;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ModulynGroupAuthRequirement requirement)
    {
        if (WebServerSettings.Instance.Authentication == false)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        var groups = user.FindAll(GroupClaimTypes.Group)
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return HandleRequirementWithGroupHierarchyAsync(context, requirement, groups);
    }

    private async Task HandleRequirementWithGroupHierarchyAsync(
        AuthorizationHandlerContext context,
        ModulynGroupAuthRequirement requirement,
        HashSet<string> userGroups)
    {
        if (userGroups.Contains(requirement.RequiredGroup))
        {
            context.Succeed(requirement);
            return;
        }

        var requiredGroupId = await _db.Groups
            .AsNoTracking()
            .Where(g => g.Name == requirement.RequiredGroup)
            .Select(g => (Guid?)g.Id)
            .FirstOrDefaultAsync();

        if (requiredGroupId is null)
        {
            context.Fail(new AuthorizationFailureReason(this, "Required group is missing"));
            return;
        }

        // Resolve required group and all of its transitive child groups.
        var allowedGroupNames = await GetRequiredAndChildGroupNamesAsync(requiredGroupId.Value);

        if (userGroups.Overlaps(allowedGroupNames))
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail(new AuthorizationFailureReason(this, "Required group (or a required child group) is missing"));
    }

    private async Task<HashSet<string>> GetRequiredAndChildGroupNamesAsync(Guid requiredGroupId)
    {
        // Walk down the hierarchy: required group -> child groups -> ...
        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        visited.Add(requiredGroupId);
        queue.Enqueue(requiredGroupId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            var childIds = await _db.GroupGroups
                .AsNoTracking()
                .Where(gg => gg.ParentGroupId == current)
                .Select(gg => gg.ChildGroupId)
                .ToListAsync();

            foreach (var childId in childIds)
            {
                if (visited.Add(childId))
                    queue.Enqueue(childId);
            }
        }

        var names = await _db.Groups
            .AsNoTracking()
            .Where(g => visited.Contains(g.Id))
            .Select(g => g.Name)
            .ToListAsync();

        return new HashSet<string>(names.Where(n => !string.IsNullOrWhiteSpace(n)), StringComparer.OrdinalIgnoreCase);
    }
}
