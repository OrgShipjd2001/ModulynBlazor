using System.ComponentModel.DataAnnotations;

namespace Modulyn.Server.Bl.IdentityGroups;

public class ApplicationUserGroup
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = default!;

    public Guid GroupId { get; set; }

    public ApplicationGroup Group { get; set; } = default!;
}
