using Microsoft.AspNetCore.Identity;
using Modulyn.Server.Bl.IdentityGroups;

namespace Modulyn.Server.Bl
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public ICollection<ApplicationUserGroup> UserGroups { get; set; } = new List<ApplicationUserGroup>();
    }

}
