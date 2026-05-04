using Microsoft.EntityFrameworkCore;

namespace Modulyn.Server.Bl.IdentityGroups;

public static class GroupMembershipResolver
{
    public static async Task<HashSet<string>> GetEffectiveGroupNamesForUserAsync(
        ApplicationDbContext db,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var directGroupIds = await db.UserGroups
            .Where(ug => ug.UserId == userId)
            .Select(ug => ug.GroupId)
            .ToListAsync(cancellationToken);

        if (directGroupIds.Count == 0)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var allGroupIds = await GetTransitiveClosureAsync(db, directGroupIds, cancellationToken);

        var names = await db.Groups
            .Where(g => allGroupIds.Contains(g.Id))
            .Select(g => g.Name)
            .ToListAsync(cancellationToken);

        return new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
    }

    public static async Task<HashSet<string>> GetEffectiveGroupNamesForGroupAsync(
        ApplicationDbContext db,
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        var allGroupIds = await GetTransitiveClosureAsync(db, new[] { groupId }, cancellationToken);
        var names = await db.Groups
            .Where(g => allGroupIds.Contains(g.Id))
            .Select(g => g.Name)
            .ToListAsync(cancellationToken);

        return new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<HashSet<Guid>> GetTransitiveClosureAsync(
        ApplicationDbContext db,
        IEnumerable<Guid> startGroupIds,
        CancellationToken cancellationToken)
    {
        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();

        foreach (var id in startGroupIds)
        {
            if (visited.Add(id))
                queue.Enqueue(id);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            // Find all parent groups that include current as child.
            var parentIds = await db.GroupGroups
                .Where(gg => gg.ChildGroupId == current)
                .Select(gg => gg.ParentGroupId)
                .ToListAsync(cancellationToken);

            foreach (var parentId in parentIds)
            {
                if (visited.Add(parentId))
                    queue.Enqueue(parentId);
            }
        }

        return visited;
    }
}
