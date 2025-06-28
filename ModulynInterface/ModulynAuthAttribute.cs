
using Microsoft.AspNetCore.Authorization;

namespace Modulyn.Server.Interface
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class ModulynAuthAttribute : AuthorizeAttribute
    {
        public ModulynAuthRole Role { get; set; }

        public ModulynAuthAttribute(ModulynAuthRole role)
        {
            Role = role;
        }
    }
}
