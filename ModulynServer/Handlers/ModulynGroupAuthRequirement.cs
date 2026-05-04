using Microsoft.AspNetCore.Authorization;

namespace ModulynServer.Handlers;

public sealed class ModulynGroupAuthRequirement : IAuthorizationRequirement
{
    public string RequiredGroup { get; }

    public ModulynGroupAuthRequirement(string requiredGroup)
    {
        RequiredGroup = requiredGroup;
    }
}
