
using Microsoft.AspNetCore.Authorization;

namespace Modulyn.Server.Interface
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class ModulynAuthAttribute : AuthorizeAttribute
    {
        private const string PolicyPrefix = "PageAccessPolicy:";

        public ModulynAuthRole Role { get; set; }

        public ModulynAuthAttribute(ModulynAuthRole role)
        {
            Role = role;
            Policy = $"{PolicyPrefix}{role.ToString()}";
        }
    }
}
