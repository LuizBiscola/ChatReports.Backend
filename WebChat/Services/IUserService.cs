using ChatAppApi.Models;

namespace ChatAppApi.Services
{
    public interface IUserService
    {
        Task<User> CreateUserAsync(string username);
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<bool> UserExistsAsync(int userId);
        Task<User> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int userId);
    }
}