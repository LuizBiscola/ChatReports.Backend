using ChatAppApi.Models;
using ChatAppApi.Services;
using ChatAppApi.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ChatAppApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(users.Select(u => u.ToDto()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound($"User with ID {id} not found");
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/users/username/{username}
        [HttpGet("username/{username}")]
        public async Task<ActionResult<UserDto>> GetUserByUsername(string username)
        {
            try
            {
                var user = await _userService.GetUserByUsernameAsync(username);
                if (user == null)
                {
                    return NotFound($"User with username '{username}' not found");
                }
                return Ok(user.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user by username: {username}");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/users
        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Username))
                {
                    return BadRequest("Username is required");
                }

                if (request.Username.Length < 3 || request.Username.Length > 50)
                {
                    return BadRequest("Username must be between 3 and 50 characters");
                }

                var user = await _userService.CreateUserAsync(request.Username);
                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user.ToDto());
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating user: {request.Username}");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/users/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserDto request)
        {
            try
            {
                var existingUser = await _userService.GetUserByIdAsync(id);
                if (existingUser == null)
                {
                    return NotFound($"User with ID {id} not found");
                }

                if (string.IsNullOrWhiteSpace(request.Username))
                {
                    return BadRequest("Username is required");
                }

                if (request.Username.Length < 3 || request.Username.Length > 50)
                {
                    return BadRequest("Username must be between 3 and 50 characters");
                }

                existingUser.Username = request.Username;
                var updatedUser = await _userService.UpdateUserAsync(existingUser);
                
                return Ok(updatedUser.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating user {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/users/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            try
            {
                var deleted = await _userService.DeleteUserAsync(id);
                if (!deleted)
                {
                    return NotFound($"User with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user {id}");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}