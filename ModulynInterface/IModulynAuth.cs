using Modulyn.Server.Interface;

namespace ModulynInterface
{
    public interface IModulynAuth
    {
        Task<bool> IsAuthenticationEnabledAsync();
        Task<bool> IsAuthenticatedAsync();
        Task<bool> IsAuthorizedAsync(ModulynAuthRole role);
    }
}
