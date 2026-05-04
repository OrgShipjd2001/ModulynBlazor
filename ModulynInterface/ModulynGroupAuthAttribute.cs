using Microsoft.AspNetCore.Authorization;

namespace Modulyn.Server.Interface;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class ModulynGroupAuthAttribute : AuthorizeAttribute
{
    private const string PolicyPrefix = "GroupAccessPolicy:";

    public string Group { get; }

    public ModulynGroupAuthAttribute(string group)
    {
        Group = group;
        Policy = $"{PolicyPrefix}{group}";
    }
}
