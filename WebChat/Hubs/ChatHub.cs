using Microsoft.AspNetCore.SignalR;
using ChatAppApi.Services;
using ChatAppApi.Models;
using System.Collections.Concurrent;

namespace ChatAppApi.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly IUserService _userService;
        private readonly ILogger<ChatHub> _logger;
        
        // Thread-safe dictionary to track online users
        private static readonly ConcurrentDictionary<string, UserConnection> _connections = new();

        public ChatHub(IChatService chatService, IUserService userService, ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _userService = userService;
            _logger = logger;
        }

        // User connects and joins their chat groups
        public async Task JoinUser(int userId, string username)
        {
            try
            {
                // Store user connection info
                _connections[Context.ConnectionId] = new UserConnection
                {
                    UserId = userId,
                    Username = username,
                    ConnectionId = Context.ConnectionId
                };

                _logger.LogInformation($"🔗 USER CONNECTED: {username} (ID: {userId}) connected with connection {Context.ConnectionId}");

                // Get user's chats and join those groups
                var userChats = await _chatService.GetUserChatsAsync(userId);
                _logger.LogInformation($"📋 JOINING CHATS: User {username} will join {userChats.Count()} chat groups");
                
                foreach (var chat in userChats)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chat.Id}");
                    _logger.LogInformation($"✅ JOINED GROUP: User {username} joined group 'chat_{chat.Id}'");
                }

                // Notify others that user is online
                await Clients.All.SendAsync("UserOnline", userId, username);
                
                _logger.LogInformation($"🟢 USER ONLINE: Notified all clients that {username} (ID: {userId}) is online");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in JoinUser for user {userId}");
                throw;
            }
        }

        // Join a specific chat room
        public async Task JoinChat(int chatId)
        {
            try
            {
                if (_connections.TryGetValue(Context.ConnectionId, out var userConnection))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatId}");
                    await Clients.Group($"chat_{chatId}").SendAsync("UserJoinedChat", userConnection.UserId, userConnection.Username, chatId);
                    
                    _logger.LogInformation($"🔗 USER JOINED: {userConnection.Username} (ID: {userConnection.UserId}) joined chat group 'chat_{chatId}' with connection {Context.ConnectionId}");
                }
                else
                {
                    _logger.LogWarning($"❌ JOIN FAILED: Connection {Context.ConnectionId} not found in connections dictionary");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error joining chat {chatId}");
                throw;
            }
        }

        // Leave a specific chat room
        public async Task LeaveChat(int chatId)
        {
            try
            {
                if (_connections.TryGetValue(Context.ConnectionId, out var userConnection))
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_{chatId}");
                    await Clients.Group($"chat_{chatId}").SendAsync("UserLeftChat", userConnection.UserId, userConnection.Username, chatId);
                    
                    _logger.LogInformation($"User {userConnection.Username} left chat {chatId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error leaving chat {chatId}");
                throw;
            }
        }

        // Send message to a chat (real-time only, persistence handled by API)
        public async Task SendMessageToChat(int chatId, string message)
        {
            try
            {
                if (_connections.TryGetValue(Context.ConnectionId, out var userConnection))
                {
                    // Create message object
                    var messageData = new
                    {
                        Id = Guid.NewGuid(), // Temporary ID for real-time
                        ChatId = chatId,
                        SenderId = userConnection.UserId,
                        SenderUsername = userConnection.Username,
                        Content = message,
                        Timestamp = DateTime.UtcNow,
                        Status = "sent"
                    };

                    // Send to all users in the chat group
                    await Clients.Group($"chat_{chatId}").SendAsync("ReceiveMessage", messageData);
                    
                    _logger.LogInformation($"Message sent by {userConnection.Username} to chat {chatId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending message to chat {chatId}");
                throw;
            }
        }

        // Send typing indicator
        public async Task SendTyping(int chatId, bool isTyping)
        {
            try
            {
                if (_connections.TryGetValue(Context.ConnectionId, out var userConnection))
                {
                    await Clients.OthersInGroup($"chat_{chatId}").SendAsync("UserTyping", userConnection.UserId, userConnection.Username, isTyping);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending typing indicator for chat {chatId}");
            }
        }

        // Mark messages as read
        public async Task MarkMessagesAsRead(int chatId, int lastReadMessageId)
        {
            try
            {
                if (_connections.TryGetValue(Context.ConnectionId, out var userConnection))
                {
                    await Clients.OthersInGroup($"chat_{chatId}").SendAsync("MessagesRead", userConnection.UserId, chatId, lastReadMessageId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking messages as read for chat {chatId}");
            }
        }

        // Get online users
        public async Task GetOnlineUsers()
        {
            try
            {
                var onlineUsers = _connections.Values.Select(c => new { c.UserId, c.Username }).Distinct().ToList();
                await Clients.Caller.SendAsync("OnlineUsers", onlineUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting online users");
            }
        }

        // Connection events
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                if (_connections.TryRemove(Context.ConnectionId, out var userConnection))
                {
                    // Notify others that user went offline
                    await Clients.All.SendAsync("UserOffline", userConnection.UserId, userConnection.Username);
                    _logger.LogInformation($"User {userConnection.Username} disconnected");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling disconnection");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }

    // Helper class to track user connections
    public class UserConnection
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
    }
}