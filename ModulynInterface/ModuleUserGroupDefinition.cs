namespace Modulyn.Server.Interface;

public sealed class ModuleUserGroupDefinition
{
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Optional list of groups that should be included in this group (i.e., this group contains these child groups).
    /// </summary>
    public List<string> IncludesGroups { get; init; } = new();
}
