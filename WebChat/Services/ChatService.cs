using ChatAppApi.Data;
using ChatAppApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ChatAppApi.Services
{
    public class ChatService : IChatService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ChatService> _logger;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

        public ChatService(ApplicationDbContext context, IMemoryCache cache, ILogger<ChatService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        // --- User Methods ---
        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            try
            {
                var cacheKey = $"user_username_{username.ToLower()}";
                if (_cache.TryGetValue(cacheKey, out User? cachedUser))
                {
                    return cachedUser;
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
                
                if (user != null)
                {
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

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                var cacheKey = $"user_{userId}";
                if (_cache.TryGetValue(cacheKey, out User? cachedUser))
                {
                    return cachedUser;
                }

                var user = await _context.Users.FindAsync(userId);
                
                if (user != null)
                {
                    _cache.Set(cacheKey, user, _cacheExpiration);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user by ID: {userId}");
                throw;
            }
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            try
            {
                const string cacheKey = "all_users";
                if (_cache.TryGetValue(cacheKey, out IEnumerable<User>? cachedUsers))
                {
                    return cachedUsers!;
                }

                var users = await _context.Users.OrderBy(u => u.Username).ToListAsync();
                _cache.Set(cacheKey, users, TimeSpan.FromMinutes(5));
                
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                throw;
            }
        }

        // --- Chat Methods ---
        public async Task<Chat> CreateChatAsync(string name, List<int> participantIds)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (participantIds.Count < 2)
                {
                    throw new ArgumentException("A chat must have at least 2 participants");
                }

                // Validate all participants exist
                var existingUsers = await _context.Users
                    .Where(u => participantIds.Contains(u.Id))
                    .Select(u => u.Id)
                    .ToListAsync();

                var missingUsers = participantIds.Except(existingUsers).ToList();
                if (missingUsers.Any())
                {
                    throw new ArgumentException($"Users not found: {string.Join(", ", missingUsers)}");
                }

                // For direct chats (2 participants), check if a chat already exists
                if (participantIds.Count == 2)
                {
                    var existingDirectChat = await GetExistingDirectChatAsync(participantIds[0], participantIds[1]);
                    if (existingDirectChat != null)
                    {
                        _logger.LogInformation($"Returning existing direct chat (ID: {existingDirectChat.Id}) between users {participantIds[0]} and {participantIds[1]}");
                        return existingDirectChat;
                    }
                }

                var newChat = new Chat
                {
                    Name = name,
                    Type = participantIds.Count == 2 ? "direct" : "group",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Chats.Add(newChat);
                await _context.SaveChangesAsync();

                // Add participants
                var participants = participantIds.Select(userId => new ChatParticipant
                {
                    ChatId = newChat.Id,
                    UserId = userId,
                    JoinedAt = DateTime.UtcNow
                }).ToList();

                _context.ChatParticipants.AddRange(participants);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Invalidate relevant caches
                foreach (var userId in participantIds)
                {
                    _cache.Remove($"user_chats_{userId}");
                }

                // Load the complete chat with participants
                var completeChat = await GetChatByIdAsync(newChat.Id);
                
                _logger.LogInformation($"Created new chat: {name} (ID: {newChat.Id}) with {participantIds.Count} participants");
                return completeChat!;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error creating chat: {name}");
                throw;
            }
        }

        public async Task<Chat?> GetChatByIdAsync(int chatId)
        {
            try
            {
                var cacheKey = $"chat_{chatId}";
                
                if (_cache.TryGetValue(cacheKey, out Chat? cachedChat))
                {
                    return cachedChat;
                }

                var chat = await _context.Chats
                    .Include(c => c.Participants)
                        .ThenInclude(cp => cp.User)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == chatId);

                if (chat != null)
                {
                    _cache.Set(cacheKey, chat, _cacheExpiration);
                }

                return chat;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting chat by ID: {chatId}");
                throw;
            }
        }

        public async Task<IEnumerable<Chat>> GetAllChatsAsync()
        {
            try
            {
                const string cacheKey = "all_chats";
                if (_cache.TryGetValue(cacheKey, out IEnumerable<Chat>? cachedChats))
                {
                    return cachedChats!;
                }

                var chats = await _context.Chats
                    .Include(c => c.Participants)
                        .ThenInclude(cp => cp.User)
                    .AsNoTracking()
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                _cache.Set(cacheKey, chats, TimeSpan.FromMinutes(5));
                return chats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all chats");
                throw;
            }
        }

        public async Task<IEnumerable<Chat>> GetUserChatsAsync(int userId)
        {
            try
            {
                var cacheKey = $"user_chats_{userId}";
                
                if (_cache.TryGetValue(cacheKey, out IEnumerable<Chat>? cachedChats))
                {
                    return cachedChats!;
                }

                var chats = await _context.ChatParticipants
                    .Where(cp => cp.UserId == userId)
                    .Include(cp => cp.Chat)
                        .ThenInclude(c => c.Participants)
                            .ThenInclude(cp => cp.User)
                    .AsNoTracking()
                    .Select(cp => cp.Chat)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                _cache.Set(cacheKey, chats, TimeSpan.FromMinutes(5));
                return chats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting chats for user: {userId}");
                throw;
            }
        }

        // --- Message Methods ---
        public async Task<Message> AddMessageToChatAsync(int chatId, int senderId, string content)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(content))
                {
                    throw new ArgumentException("Message content cannot be empty");
                }

                if (content.Length > 4000)
                {
                    throw new ArgumentException("Message content is too long");
                }

                // Verify chat exists and user is participant
                var chat = await _context.Chats
                    .Include(c => c.Participants)
                    .FirstOrDefaultAsync(c => c.Id == chatId);

                if (chat == null)
                {
                    throw new ArgumentException($"Chat {chatId} not found");
                }

                var isParticipant = chat.Participants.Any(p => p.UserId == senderId);
                if (!isParticipant)
                {
                    throw new UnauthorizedAccessException("User is not a participant in this chat");
                }

                var newMessage = new Message
                {
                    ChatId = chatId,
                    SenderId = senderId,
                    Content = content.Trim(),
                    Timestamp = DateTime.UtcNow,
                    Status = "sent"
                };

                _context.Messages.Add(newMessage);
                await _context.SaveChangesAsync();

                // Load the complete message with sender info
                var completeMessage = await _context.Messages
                    .Include(m => m.Sender)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == newMessage.Id);

                // Invalidate cache for user chats (to update last message info if needed)
                var participantIds = chat.Participants.Select(p => p.UserId).ToList();
                foreach (var participantId in participantIds)
                {
                    _cache.Remove($"user_chats_{participantId}");
                }

                _logger.LogInformation($"Added message to chat {chatId} by user {senderId}");
                return completeMessage ?? newMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding message to chat {chatId}");
                throw;
            }
        }

        public async Task<IEnumerable<Message>> GetChatMessagesAsync(int chatId, int take = 100, int skip = 0, int? beforeMessageId = null)
        {
            try
            {
                // Validate parameters
                take = Math.Min(Math.Max(take, 1), 100); // Limit between 1 and 100
                skip = Math.Max(skip, 0);

                var cacheKey = $"chat_messages_{chatId}_{take}_{skip}_{beforeMessageId}";
                if (_cache.TryGetValue(cacheKey, out IEnumerable<Message>? cachedMessages))
                {
                    return cachedMessages!;
                }

                IQueryable<Message> query = _context.Messages
                    .Where(m => m.ChatId == chatId)
                    .Include(m => m.Sender)
                    .AsNoTracking();

                if (beforeMessageId.HasValue)
                {
                    query = query.Where(m => m.Id < beforeMessageId.Value);
                }

                var messages = await query
                    .OrderByDescending(m => m.Timestamp)
                    .Skip(skip)
                    .Take(take)
                    .OrderBy(m => m.Timestamp) // Return in chronological order
                    .ToListAsync();

                _cache.Set(cacheKey, messages, TimeSpan.FromMinutes(2));
                return messages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting messages for chat {chatId}");
                throw;
            }
        }

        public async Task<Message?> GetMessageByIdAsync(int messageId)
        {
            try
            {
                var cacheKey = $"message_{messageId}";
                if (_cache.TryGetValue(cacheKey, out Message? cachedMessage))
                {
                    return cachedMessage;
                }

                var message = await _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Chat)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (message != null)
                {
                    _cache.Set(cacheKey, message, _cacheExpiration);
                }

                return message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting message by ID: {messageId}");
                throw;
            }
        }

        public async Task<bool> UpdateMessageStatusAsync(int messageId, string status)
        {
            try
            {
                var validStatuses = new[] { "sent", "delivered", "read" };
                if (!validStatuses.Contains(status.ToLower()))
                {
                    throw new ArgumentException($"Invalid status: {status}");
                }

                var message = await _context.Messages.FindAsync(messageId);
                if (message == null)
                {
                    return false;
                }

                message.Status = status.ToLower();
                await _context.SaveChangesAsync();

                // Invalidate cache
                _cache.Remove($"message_{messageId}");

                _logger.LogInformation($"Updated message {messageId} status to {status}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating message status for message {messageId}");
                throw;
            }
        }

        public async Task<bool> AddUserToChatAsync(int chatId, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Check if user is already a participant
                var existingParticipant = await _context.ChatParticipants
                    .FirstOrDefaultAsync(cp => cp.ChatId == chatId && cp.UserId == userId);

                if (existingParticipant != null)
                {
                    return false; // User is already a participant
                }

                // Verify chat and user exist
                var chatExists = await _context.Chats.AnyAsync(c => c.Id == chatId);
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);

                if (!chatExists || !userExists)
                {
                    return false;
                }

                var participant = new ChatParticipant
                {
                    ChatId = chatId,
                    UserId = userId,
                    JoinedAt = DateTime.UtcNow
                };

                _context.ChatParticipants.Add(participant);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Invalidate cache
                _cache.Remove($"chat_{chatId}");
                _cache.Remove($"user_chats_{userId}");

                _logger.LogInformation($"Added user {userId} to chat {chatId}");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error adding user {userId} to chat {chatId}");
                throw;
            }
        }

        public async Task<bool> RemoveUserFromChatAsync(int chatId, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var participant = await _context.ChatParticipants
                    .FirstOrDefaultAsync(cp => cp.ChatId == chatId && cp.UserId == userId);

                if (participant == null)
                {
                    return false; // User is not a participant
                }

                _context.ChatParticipants.Remove(participant);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Invalidate cache
                _cache.Remove($"chat_{chatId}");
                _cache.Remove($"user_chats_{userId}");

                _logger.LogInformation($"Removed user {userId} from chat {chatId}");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error removing user {userId} from chat {chatId}");
                throw;
            }
        }

        public async Task<bool> DeleteChatAsync(int chatId, int requestingUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get the chat with participants
                var chat = await _context.Chats
                    .Include(c => c.Participants)
                    .FirstOrDefaultAsync(c => c.Id == chatId);

                if (chat == null)
                {
                    return false; // Chat not found
                }

                // Verify the requesting user is a participant of the chat
                var isParticipant = chat.Participants.Any(p => p.UserId == requestingUserId);
                if (!isParticipant)
                {
                    throw new UnauthorizedAccessException("User is not authorized to delete this chat");
                }

                var messageCount = await _context.Messages.CountAsync(m => m.ChatId == chatId);
                var participantIds = chat.Participants.Select(p => p.UserId).ToList();

                _logger.LogInformation($"Deleting chat {chatId} (Type: {chat.Type}, Name: '{chat.Name}', Messages: {messageCount}) by user {requestingUserId}");

                // Delete all messages first (due to foreign key constraints)
                if (messageCount > 0)
                {
                    await _context.Messages.Where(m => m.ChatId == chatId).ExecuteDeleteAsync();
                }

                // Delete all participants
                await _context.ChatParticipants.Where(cp => cp.ChatId == chatId).ExecuteDeleteAsync();

                // Delete the chat itself
                _context.Chats.Remove(chat);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Invalidate cache for all participants
                foreach (var participantId in participantIds)
                {
                    _cache.Remove($"user_chats_{participantId}");
                }
                _cache.Remove($"chat_{chatId}");
                _cache.Remove("all_chats");

                _logger.LogInformation($"Successfully deleted chat {chatId}");
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                await transaction.RollbackAsync();
                throw; // Re-throw authorization exceptions
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error deleting chat {chatId} by user {requestingUserId}");
                throw;
            }
        }

        // Private method to check for existing direct chat between two users
        private async Task<Chat?> GetExistingDirectChatAsync(int userId1, int userId2)
        {
            try
            {
                var cacheKey = $"direct_chat_{Math.Min(userId1, userId2)}_{Math.Max(userId1, userId2)}";
                if (_cache.TryGetValue(cacheKey, out Chat? cachedChat))
                {
                    return cachedChat;
                }

                // Find chats where both users are participants and it's a direct chat
                var existingChat = await _context.Chats
                    .Where(c => c.Type == "direct")
                    .Where(c => c.Participants.Count == 2)
                    .Where(c => c.Participants.Any(p => p.UserId == userId1))
                    .Where(c => c.Participants.Any(p => p.UserId == userId2))
                    .Include(c => c.Participants)
                        .ThenInclude(cp => cp.User)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (existingChat != null)
                {
                    _cache.Set(cacheKey, existingChat, _cacheExpiration);
                }

                return existingChat;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking for existing direct chat between users {userId1} and {userId2}");
                throw;
            }
        }
    }
}