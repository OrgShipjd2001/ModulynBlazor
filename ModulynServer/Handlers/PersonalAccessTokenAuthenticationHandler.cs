using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using ModulynServer.Services;

namespace ModulynServer.Handlers
{
    public sealed class PersonalAccessTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly PersonalAccessTokenService _tokenService;

        public PersonalAccessTokenAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
#pragma warning disable CS0618
            ISystemClock clock,
#pragma warning restore CS0618
            PersonalAccessTokenService tokenService)
            : base(options, logger, encoder, clock)
        {
            _tokenService = tokenService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var auth = Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return AuthenticateResult.NoResult();

            var token = auth.Substring("Bearer ".Length).Trim();
            if (string.IsNullOrWhiteSpace(token))
                return AuthenticateResult.Fail("Missing token");

            var user = await _tokenService.ValidateAndGetUserAsync(token, Context.RequestAborted);
            if (user is null)
                return AuthenticateResult.Fail("Invalid token");

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id)
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
    }
}
