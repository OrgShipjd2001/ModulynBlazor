using System.ComponentModel.DataAnnotations;

namespace Modulyn.Server.Bl.IdentityGroups;

public class ApplicationGroup
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    public bool IsSystem { get; set; } = false;

    public ICollection<ApplicationUserGroup> UserGroups { get; set; } = new List<ApplicationUserGroup>();

    public ICollection<ApplicationGroupGroup> ParentGroups { get; set; } = new List<ApplicationGroupGroup>();
    public ICollection<ApplicationGroupGroup> ChildGroups { get; set; } = new List<ApplicationGroupGroup>();
}
