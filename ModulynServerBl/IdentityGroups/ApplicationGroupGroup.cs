using System.ComponentModel.DataAnnotations;

namespace Modulyn.Server.Bl.IdentityGroups;

public class ApplicationGroupGroup
{
    public Guid ParentGroupId { get; set; }
    public ApplicationGroup ParentGroup { get; set; } = default!;

    public Guid ChildGroupId { get; set; }
    public ApplicationGroup ChildGroup { get; set; } = default!;
}
