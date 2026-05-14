
// Controllers/UsersApiController.cs (in your Blazor Server project)
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Modulyn.Server.Bl;
using Modulyn.Server.Interface;
using ModulynServer.model;

namespace ModulynServer.RestApiControllers
{
    [RestApiAuth]
    [ModulynGroupAuth(ModulynSystemGroupNames.Admins)]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Role = roles.FirstOrDefault() // Or join multiple roles, or send as list
                });
            }
            return Ok(userDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = roles.FirstOrDefault()
            };
            return Ok(userDto);
        }

        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] UserEditDto newUserDto)
        {
            var user = new ApplicationUser { UserName = newUserDto.UserName, Email = newUserDto.Email };
            var result = await _userManager.CreateAsync(user, newUserDto.Password); // Password required for new user

            if (!result.Succeeded) return BadRequest(result.Errors);

            if (!string.IsNullOrEmpty(newUserDto.Role))
            {
                if (!await _roleManager.RoleExistsAsync(newUserDto.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(newUserDto.Role));
                }
                await _userManager.AddToRoleAsync(user, newUserDto.Role);
            }

            var createdUserDto = new UserDto { Id = user.Id, UserName = user.UserName, Email = user.Email, Role = newUserDto.Role };
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, createdUserDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UserEditDto updatedUserDto)
        {
            if (id != updatedUserDto.Id) return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.UserName = updatedUserDto.UserName;
            user.Email = updatedUserDto.Email;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            // Handle role changes
            var currentRoles = await _userManager.GetRolesAsync(user);
            var newRole = updatedUserDto.Role;

            if (!currentRoles.Contains(newRole))
            {
                // Remove old roles and add new one
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!string.IsNullOrEmpty(newRole))
                {
                    if (!await _roleManager.RoleExistsAsync(newRole))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(newRole));
                    }
                    await _userManager.AddToRoleAsync(user, newRole);
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return NoContent();
        }
    }
}
