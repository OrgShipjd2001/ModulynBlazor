using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ModulynServer.model;

namespace Modulyn.Server.RestApiControllers
{
    [ApiController] // Indicates this is an API controller
    [Route("api/[controller]")] // Sets the base route, e.g., /api/auth
    public class AuthController : ControllerBase
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<AuthController> _logger;
        private readonly UserManager<IdentityUser> _userManager; // Needed for registration

        public AuthController(SignInManager<IdentityUser> signInManager, ILogger<AuthController> logger, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
        }

        // POST /api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromForm] LoginData request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // Return validation errors
            }

            // Perform the sign-in attempt
            var result = await _signInManager.PasswordSignInAsync(
                request.username!, request.password!, isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("User '{username}' logged in via API.", request.username);

                return LocalRedirect("/");
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User '{username}' account locked out via API.", request.username);
                return StatusCode(423, "Account locked out."); // 423 Locked
            }
            if (result.RequiresTwoFactor)
            {
                // You would need a separate endpoint for 2FA verification and redirect client there.
                _logger.LogWarning("User '{username}' requires 2FA.", request.username);
                return BadRequest("Requires 2FA.");
            }

            // If none of the above, it's an invalid login attempt
            _logger.LogWarning("Invalid login attempt for user '{username}'.", request.username);
            return Unauthorized("Invalid username or password."); // 401 Unauthorized
        }

        // POST /api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] RegisterData request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (request.password != request.confirmpassword)
            {
                return BadRequest("Passwords do not match.");
            }

            var user = new IdentityUser { UserName = request.username, Email = request.email };
            var result = await _userManager.CreateAsync(user, request.password!);

            if (result.Succeeded)
            {
                _logger.LogInformation("User '{username}' created a new account via API.", request.username);

                // Optionally sign in the user immediately after registration
                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect("/");
            }

            // Collect and return all errors from IdentityResult
            var errors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(new { Errors = errors }); // Return 400 Bad Request with specific errors
        }

        // POST /api/auth/logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            // After logout, redirect to the home page or a public login page.
            return LocalRedirect("/");
        }
    }
}