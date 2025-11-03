using Microsoft.AspNetCore.Authentication;

namespace ModulynServer.Handlers
{
    public class HttpAuthHeaderOptions : AuthenticationSchemeOptions
    {
        /// <summary>
        /// The HTTP header that contains the authenticated username.
        /// </summary>
        public string UserHeader { get; set; } = "X-User";

        /// <summary>
        /// Optional HTTP header that contains the user’s email address.
        /// </summary>
        public string? EmailHeader { get; set; } = "X-Email";
    }
}
