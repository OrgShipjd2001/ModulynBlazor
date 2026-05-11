using Microsoft.AspNetCore.Authorization;

namespace Modulyn.Server.Interface
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class RestApiAuthAttribute : AuthorizeAttribute
    {
        public const string SchemeName = "ApiBearer";

        public RestApiAuthAttribute()
        {
            AuthenticationSchemes = SchemeName;
        }
    }
}
