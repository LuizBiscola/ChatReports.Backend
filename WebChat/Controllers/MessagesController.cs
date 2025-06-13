using ChatAppApi.Models;
using ChatAppApi.Services;
using ChatAppApi.Hubs;
using ChatAppApi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ChatAppApi.Controllers
{
    [ApiController]
    [Route("api/chats/{chatId}/messages")]
    public class MessagesController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IUserService _userService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(
            IChatService chatService, 
            IUserService userService,
            IHubContext<ChatHub> hubContext,
            ILogger<MessagesController> logger)
        {
            _chatService = chatService;
            _userService = userService;
            _hubContext = hubContext;
            _logger = logger;
        }

        // POST: api/chats/{chatId}/messages
        [HttpPost]
        public async Task<ActionResult<MessageDto>> SendMessage(int chatId, [FromBody] SendMessageDto request)
        {
            try
            {
                // Validation
                if (request == null || request.SenderId <= 0 || string.IsNullOrWhiteSpace(request.Content))
                {
                    return BadRequest("Invalid message data");
                }

                if (request.Content.Length > 4000) // Reasonable message length limit
                {
                    return BadRequest("Message content is too long");
                }

                // Verify chat exists
                var chat = await _chatService.GetChatByIdAsync(chatId);
                if (chat == null)
                {
                    return NotFound($"Chat with ID {chatId} not found");
                }

                // Verify user exists
                var user = await _userService.GetUserByIdAsync(request.SenderId);
                if (user == null)
                {
                    return NotFound($"User with ID {request.SenderId} not found");
                }

                // Verify user is participant in the chat
                var isParticipant = chat.Participants.Any(p => p.UserId == request.SenderId);
                if (!isParticipant)
                {
                    return Forbid("User is not a participant in this chat");
                }

                // Persist the message
                var message = await _chatService.AddMessageToChatAsync(chatId, request.SenderId, request.Content);

                // Send real-time notification via SignalR
                var messageData = new
                {
                    Id = message.Id,
                    ChatId = chatId,
                    SenderId = request.SenderId,
                    SenderUsername = user.Username,
                    Content = message.Content,
                    Timestamp = message.Timestamp,
                    Status = message.Status
                };

                await _hubContext.Clients.Group($"chat_{chatId}").SendAsync("ReceiveMessage", messageData);

                _logger.LogInformation($"Message sent by user {request.SenderId} to chat {chatId}");
                return CreatedAtAction(nameof(GetMessages), new { chatId = chatId }, message.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending message to chat {chatId}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/chats/{chatId}/messages
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessages(
            int chatId, 
            [FromQuery] int take = 50, 
            [FromQuery] int skip = 0,
            [FromQuery] int? beforeMessageId = null)
        {
            try
            {
                // Validate parameters
                if (take <= 0 || take > 100)
                {
                    take = 50; // Default and max limit
                }

                if (skip < 0)
                {
                    skip = 0;
                }

                // Verify chat exists
                var chat = await _chatService.GetChatByIdAsync(chatId);
                if (chat == null)
                {
                    return NotFound($"Chat with ID {chatId} not found");
                }

                // Get messages
                var messages = await _chatService.GetChatMessagesAsync(chatId, take, skip, beforeMessageId);
                
                return Ok(messages.Select(m => m.ToDto()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting messages for chat {chatId}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/chats/{chatId}/messages/{messageId}
        [HttpGet("{messageId}")]
        public async Task<ActionResult<MessageDto>> GetMessage(int chatId, int messageId)
        {
            try
            {
                var message = await _chatService.GetMessageByIdAsync(messageId);
                if (message == null || message.ChatId != chatId)
                {
                    return NotFound($"Message with ID {messageId} not found in chat {chatId}");
                }

                return Ok(message.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting message {messageId} from chat {chatId}");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/chats/{chatId}/messages/{messageId}/status
        [HttpPut("{messageId}/status")]
        public async Task<ActionResult> UpdateMessageStatus(int chatId, int messageId, [FromBody] UpdateMessageStatusDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Status))
                {
                    return BadRequest("Status is required");
                }

                var validStatuses = new[] { "sent", "delivered", "read" };
                if (!validStatuses.Contains(request.Status.ToLower()))
                {
                    return BadRequest("Invalid status. Valid statuses are: sent, delivered, read");
                }

                var success = await _chatService.UpdateMessageStatusAsync(messageId, request.Status);
                if (!success)
                {
                    return NotFound($"Message with ID {messageId} not found");
                }

                // Notify via SignalR
                await _hubContext.Clients.Group($"chat_{chatId}").SendAsync("MessageStatusUpdated", messageId, request.Status);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating message status for message {messageId}");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}