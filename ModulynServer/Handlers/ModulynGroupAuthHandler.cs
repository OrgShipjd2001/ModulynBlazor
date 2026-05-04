using Microsoft.AspNetCore.Authorization;
using Modulyn.Server.Bl;
using System.Security.Claims;

namespace ModulynServer.Handlers;

public sealed class ModulynGroupAuthHandler : AuthorizationHandler<ModulynGroupAuthRequirement>
{
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

        if (groups.Contains(requirement.RequiredGroup))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        context.Fail(new AuthorizationFailureReason(this, "Required group is missing"));
        return Task.CompletedTask;
    }
}
