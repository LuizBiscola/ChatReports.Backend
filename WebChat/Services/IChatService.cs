using ChatAppApi.Models;

namespace ChatAppApi.Services
{
    public interface IChatService
    {
        // User methods
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByIdAsync(int userId);
        Task<IEnumerable<User>> GetAllUsersAsync();

        // Chat methods
        Task<Chat> CreateChatAsync(string name, List<int> participantIds);
        Task<Chat?> GetChatByIdAsync(int chatId);
        Task<IEnumerable<Chat>> GetAllChatsAsync();
        Task<IEnumerable<Chat>> GetUserChatsAsync(int userId);
        Task<bool> AddUserToChatAsync(int chatId, int userId);
        Task<bool> RemoveUserFromChatAsync(int chatId, int userId);
        Task<bool> DeleteChatAsync(int chatId, int requestingUserId);

        // Message methods
        Task<Message> AddMessageToChatAsync(int chatId, int senderId, string content);
        Task<IEnumerable<Message>> GetChatMessagesAsync(int chatId, int take = 100, int skip = 0, int? beforeMessageId = null);
        Task<Message?> GetMessageByIdAsync(int messageId);
        Task<bool> UpdateMessageStatusAsync(int messageId, string status);
    }
}