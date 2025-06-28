using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Modulyn.Server.Interface;

namespace ModulynServer.Handlers
{
    public class ModulynAuthPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        private const string PolicyPrefix = "PageAccessPolicy:";

        public ModulynAuthPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) 
        {
        }

        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string authRole = policyName.Substring(PolicyPrefix.Length);
                ModulynAuthRole requiredRole = Enum.Parse(typeof(ModulynAuthRole), authRole, true) as ModulynAuthRole? ?? 0;

                var policy = new AuthorizationPolicyBuilder();
                policy.AddRequirements(new ModulynAuthRequirement(requiredRole));
                return await Task.FromResult(policy.Build());
            }

            // Fallback to the default policy provider for other policies
            return await base.GetPolicyAsync(policyName);
        }
    }
}
