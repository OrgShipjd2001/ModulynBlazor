using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ModulynServer.Handlers;

public sealed class ModulynGroupAuthPolicyProvider : DefaultAuthorizationPolicyProvider
{
    private const string PolicyPrefix = "GroupAccessPolicy:";

    public ModulynGroupAuthPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
    {
    }

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var group = policyName.Substring(PolicyPrefix.Length);
            var policy = new AuthorizationPolicyBuilder();
            policy.AddRequirements(new ModulynGroupAuthRequirement(group));
            return Task.FromResult<AuthorizationPolicy?>(policy.Build());
        }

        return base.GetPolicyAsync(policyName);
    }
}
