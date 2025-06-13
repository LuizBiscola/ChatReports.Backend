using ChatAppApi.Data;
using ChatAppApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ChatAppApi.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<UserService> _logger;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);

        public UserService(ApplicationDbContext context, IMemoryCache cache, ILogger<UserService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<User> CreateUserAsync(string username)
        {
            try
            {
                // Check if username already exists
                var existingUser = await GetUserByUsernameAsync(username);
                if (existingUser != null)
                {
                    throw new InvalidOperationException($"Username '{username}' already exists");
                }

                var user = new User
                {
                    Username = username,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Cache the new user
                _cache.Set($"user_{user.Id}", user, _cacheExpiration);
                _cache.Set($"user_username_{username.ToLower()}", user, _cacheExpiration);

                _logger.LogInformation($"Created new user: {username} (ID: {user.Id})");
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating user with username: {username}");
                throw;
            }
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                // Try to get from cache first
                if (_cache.TryGetValue($"user_{userId}", out User? cachedUser))
                {
                    return cachedUser;
                }

                // Get from database
                var user = await _context.Users.FindAsync(userId);
                
                // Cache the result
                if (user != null)
                {
                    _cache.Set($"user_{userId}", user, _cacheExpiration);
                    _cache.Set($"user_username_{user.Username.ToLower()}", user, _cacheExpiration);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user by ID: {userId}");
                throw;
            }
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            try
            {
                var cacheKey = $"user_username_{username.ToLower()}";
                
                // Try to get from cache first
                if (_cache.TryGetValue(cacheKey, out User? cachedUser))
                {
                    return cachedUser;
                }

                // Get from database
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
                
                // Cache the result
                if (user != null)
                {
                    _cache.Set($"user_{user.Id}", user, _cacheExpiration);
                    _cache.Set(cacheKey, user, _cacheExpiration);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user by username: {username}");
                throw;
            }
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            try
            {
                return await _context.Users
                    .OrderBy(u => u.Username)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                throw;
            }
        }

        public async Task<bool> UserExistsAsync(int userId)
        {
            try
            {
                // Check cache first
                if (_cache.TryGetValue($"user_{userId}", out _))
                {
                    return true;
                }

                // Check database
                return await _context.Users.AnyAsync(u => u.Id == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if user exists: {userId}");
                throw;
            }
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // Update cache
                _cache.Set($"user_{user.Id}", user, _cacheExpiration);
                _cache.Set($"user_username_{user.Username.ToLower()}", user, _cacheExpiration);

                _logger.LogInformation($"Updated user: {user.Username} (ID: {user.Id})");
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating user: {user.Id}");
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return false;
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                // Remove from cache
                _cache.Remove($"user_{userId}");
                _cache.Remove($"user_username_{user.Username.ToLower()}");

                _logger.LogInformation($"Deleted user: {user.Username} (ID: {userId})");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user: {userId}");
                throw;
            }
        }
    }
}