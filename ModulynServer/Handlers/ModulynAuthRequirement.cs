using Microsoft.AspNetCore.Authorization;
using Modulyn.Server.Interface;

namespace ModulynServer.Handlers
{
    public class ModulynAuthRequirement : IAuthorizationRequirement
    {
        public ModulynAuthRole RequiredRole { get; private set; }

        public ModulynAuthRequirement()
        {

        }

        public ModulynAuthRequirement(ModulynAuthRole requiredRole)
        {
            RequiredRole = requiredRole;
        }

    }
}
