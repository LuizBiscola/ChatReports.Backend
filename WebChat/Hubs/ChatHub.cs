using Microsoft.AspNetCore.SignalR;
using ChatAppApi.Services;
using ChatAppApi.Models;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace ChatAppApi.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly IUserService _userService;
        private readonly ILogger<ChatHub> _logger;
        
        // Thread-safe dictionary to track online users
        private static readonly ConcurrentDictionary<string, UserConnection> _connections = new();
        private static readonly ConcurrentDictionary<int, HashSet<string>> _userConnections = new();

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
                if (userId <= 0 || string.IsNullOrWhiteSpace(username))
                {
                    _logger.LogWarning($"Invalid user data: userId={userId}, username={username}");
                    await Clients.Caller.SendAsync("Error", "Invalid user data");
                    return;
                }

                // Verify user exists in database
                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User not found: {userId}");
                    await Clients.Caller.SendAsync("Error", "User not found");
                    return;
                }

                // Store user connection info
                var userConnection = new UserConnection
                {
                    UserId = userId,
                    Username = username,
                    ConnectionId = Context.ConnectionId,
                    ConnectedAt = DateTime.UtcNow
                };

                _connections[Context.ConnectionId] = userConnection;

                // Track multiple connections per user
                _userConnections.AddOrUpdate(userId, 
                    new HashSet<string> { Context.ConnectionId },
                    (key, existing) => 
                    {
                        existing.Add(Context.ConnectionId);
                        return existing;
                    });

                _logger.LogInformation($"🔗 USER CONNECTED: {username} (ID: {userId}) connected with connection {Context.ConnectionId}");

                // Update user online status
                user.IsOnline = true;
                user.LastSeen = DateTime.UtcNow;
                await _userService.UpdateUserAsync(user);

                // Get user's chats and join those groups
                var userChats = await _chatService.GetUserChatsAsync(userId);
                _logger.LogInformation($"📋 JOINING CHATS: User {username} will join {userChats.Count()} chat groups");
                
                foreach (var chat in userChats)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chat.Id}");
                    _logger.LogInformation($"✅ JOINED GROUP: User {username} joined group 'chat_{chat.Id}'");
                }

                // Notify others that user is online (only if this is the first connection)
                if (_userConnections[userId].Count == 1)
                {
                    await Clients.All.SendAsync("UserOnline", userId, username);
                    _logger.LogInformation($"🟢 USER ONLINE: Notified all clients that {username} (ID: {userId}) is online");
                }

                // Confirm connection to caller
                await Clients.Caller.SendAsync("Connected", userId, username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in JoinUser for user {userId}");
                await Clients.Caller.SendAsync("Error", "Failed to join user");
            }
        }

        // Join a specific chat room
        public async Task JoinChat(int chatId)
        {
            try
            {
                if (chatId <= 0)
                {
                    await Clients.Caller.SendAsync("Error", "Invalid chat ID");
                    return;
                }

                if (_connections.TryGetValue(Context.ConnectionId, out var userConnection))
                {
                    // Verify user is participant in the chat
                    var chat = await _chatService.GetChatByIdAsync(chatId);
                    if (chat == null)
                    {
                        await Clients.Caller.SendAsync("Error", "Chat not found");
                        return;
                    }

                    var isParticipant = chat.Participants.Any(p => p.UserId == userConnection.UserId);
                    if (!isParticipant)
                    {
                        await Clients.Caller.SendAsync("Error", "Not authorized to join this chat");
                        return;
                    }

                    await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatId}");
                    await Clients.Group($"chat_{chatId}").SendAsync("UserJoinedChat", userConnection.UserId, userConnection.Username, chatId);
                    
                    _logger.LogInformation($"🔗 USER JOINED: {userConnection.Username} (ID: {userConnection.UserId}) joined chat group 'chat_{chatId}' with connection {Context.ConnectionId}");
                }
                else
                {
                    _logger.LogWarning($"❌ JOIN FAILED: Connection {Context.ConnectionId} not found in connections dictionary");
                    await Clients.Caller.SendAsync("Error", "Connection not found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error joining chat {chatId}");
                await Clients.Caller.SendAsync("Error", "Failed to join chat");
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
                var onlineUsers = _connections.Values
                    .GroupBy(c => c.UserId)
                    .Select(g => new { UserId = g.Key, Username = g.First().Username })
                    .ToList();
                
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
            _logger.LogInformation($"Client connected: {Context.ConnectionId} from {Context.GetHttpContext()?.Connection.RemoteIpAddress}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                if (_connections.TryRemove(Context.ConnectionId, out var userConnection))
                {
                    // Remove connection from user tracking
                    if (_userConnections.TryGetValue(userConnection.UserId, out var userConnectionSet))
                    {
                        userConnectionSet.Remove(Context.ConnectionId);
                        
                        // If no more connections for this user, mark as offline
                        if (userConnectionSet.Count == 0)
                        {
                            _userConnections.TryRemove(userConnection.UserId, out _);
                            
                            // Update user offline status in database
                            try
                            {
                                var user = await _userService.GetUserByIdAsync(userConnection.UserId);
                                if (user != null)
                                {
                                    user.IsOnline = false;
                                    user.LastSeen = DateTime.UtcNow;
                                    await _userService.UpdateUserAsync(user);
                                }
                            }
                            catch (Exception dbEx)
                            {
                                _logger.LogError(dbEx, $"Error updating user offline status for user {userConnection.UserId}");
                            }

                            // Notify others that user went offline
                            await Clients.All.SendAsync("UserOffline", userConnection.UserId, userConnection.Username);
                            _logger.LogInformation($"🔴 USER OFFLINE: {userConnection.Username} (ID: {userConnection.UserId}) went offline");
                        }
                    }

                    _logger.LogInformation($"User {userConnection.Username} disconnected (Connection: {Context.ConnectionId})");
                }

                if (exception != null)
                {
                    _logger.LogWarning(exception, $"Client disconnected with exception: {Context.ConnectionId}");
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
        public DateTime ConnectedAt { get; set; }
    }
}