using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Modulyn.Server.Bl;
using Modulyn.Server.Interface;
using System.Security.Claims;

namespace ModulynServer.Handlers
{
    public class ModulynAuthHandler : AuthorizationHandler<ModulynAuthRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ModulynAuthRequirement requirement)
        {
            if (WebServerSettings.Instance.Authentication == false)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            ModulynAuthRole roleToCheck = requirement.RequiredRole;

            // Try to get RouteData (Blazor scenario)
            var routeData = context.Resource as Microsoft.AspNetCore.Components.RouteData;
            if (routeData != null)
            {
                var pageType = routeData.PageType;
                // Get all attributes on the page/component
                var attributes = pageType.GetCustomAttributes(inherit: true);

                // Example: Check for [AllowAnonymous]
                var hasAllowAnonymous = attributes.Any(attr => attr is AllowAnonymousAttribute);
                if (hasAllowAnonymous)
                {
                    // If the page/component has [AllowAnonymous], skip further checks
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }

                // Retrieve the ModulynAuthAttribute instance (if present)
                var modulynAuthAttribute = (ModulynAuthAttribute?)Attribute.GetCustomAttribute(pageType, typeof(ModulynAuthAttribute), inherit: true);
                if (roleToCheck == 0)
                {
                    // If no specific role is required, check if the attribute exists
                    if (modulynAuthAttribute != null)
                    {
                        roleToCheck = modulynAuthAttribute.Role;
                    }
                }
            }

            if (roleToCheck != 0)
            {
                // Get user roles from ClaimsPrincipal
                var user = context.User;
                var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                roles.AddRange(user.FindAll("role").Select(c => c.Value));

                List<string> allowedRoles = new List<string>();
                for (int i = (int)roleToCheck; i > 0; i--)
                {
                    allowedRoles.Add(((ModulynAuthRole)i).ToString());
                }

                // Example: check if user has a required role from the attribute
                if (roles.Any(r => allowedRoles.Contains(r, StringComparer.OrdinalIgnoreCase)))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
                else
                {
                    // Use this information in your logic
                    context.Fail(new AuthorizationFailureReason(this, "Required role is missing"));
                }
            }

            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
