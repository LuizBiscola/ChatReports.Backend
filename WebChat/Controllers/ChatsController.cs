using ChatAppApi.DTOs;
using ChatAppApi.Models;
using ChatAppApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatAppApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IUserService _userService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, IUserService userService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _userService = userService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<ChatDto>> CreateChat(CreateChatDto request)
        {
            if (request.ParticipantUserIds == null || request.ParticipantUserIds.Count < 2)
            {
                return BadRequest("Um chat precisa de pelo menos 2 participantes");
            }

            try
            {
                // Validate that all participant IDs are valid users
                foreach (var participantId in request.ParticipantUserIds)
                {
                    var user = await _userService.GetUserByIdAsync(participantId);
                    if (user == null)
                    {
                        return BadRequest($"User with ID {participantId} not found");
                    }
                }

                // For direct chats, generate a name based on participants if not provided
                string chatName = request.Name;
                if (request.ParticipantUserIds.Count == 2 && string.IsNullOrWhiteSpace(chatName))
                {
                    var user1 = await _userService.GetUserByIdAsync(request.ParticipantUserIds[0]);
                    var user2 = await _userService.GetUserByIdAsync(request.ParticipantUserIds[1]);
                    chatName = $"{user1!.Username}, {user2!.Username}";
                }

                var chat = await _chatService.CreateChatAsync(chatName, request.ParticipantUserIds);
                
                _logger.LogInformation($"Chat created/retrieved: {chat.Name} (ID: {chat.Id})");
                return CreatedAtAction(nameof(GetChatById), new { id = chat.Id }, chat.ToDto());
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chat");
                return StatusCode(500, "Ocorreu um erro ao criar o chat");
            }
        }

        // POST: api/chats/direct
        [HttpPost("direct")]
        public async Task<ActionResult<ChatDto>> GetOrCreateDirectChat([FromBody] CreateDirectChatDto request)
        {
            try
            {
                if (request == null || request.UserId1 <= 0 || request.UserId2 <= 0)
                {
                    return BadRequest("Both user IDs must be provided and valid");
                }

                if (request.UserId1 == request.UserId2)
                {
                    return BadRequest("Cannot create a chat with the same user");
                }

                // Validate that both users exist
                var user1 = await _userService.GetUserByIdAsync(request.UserId1);
                var user2 = await _userService.GetUserByIdAsync(request.UserId2);
                
                if (user1 == null)
                {
                    return BadRequest($"User with ID {request.UserId1} not found");
                }
                
                if (user2 == null)
                {
                    return BadRequest($"User with ID {request.UserId2} not found");
                }

                // Create or get existing direct chat
                var chatName = $"{user1.Username}, {user2.Username}";
                var participantIds = new List<int> { request.UserId1, request.UserId2 };
                
                var chat = await _chatService.CreateChatAsync(chatName, participantIds);
                
                _logger.LogInformation($"Direct chat created/retrieved between users {request.UserId1} and {request.UserId2}: {chat.Id}");
                return Ok(chat.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating/getting direct chat between users {request.UserId1} and {request.UserId2}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChatDto>>> GetAllChats()
        {
            var chats = await _chatService.GetAllChatsAsync();
            return Ok(chats.Select(c => c.ToDto()));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ChatDto>> GetChatById(int id)
        {
            var chat = await _chatService.GetChatByIdAsync(id);

            if (chat == null)
                return NotFound();

            return Ok(chat.ToDto());
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<ChatDto>>> GetUserChats(int userId)
        {
            var chats = await _chatService.GetUserChatsAsync(userId);
            return Ok(chats.Select(c => c.ToDto()));
        }

        // DELETE: api/chats/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteChat(int id, [FromQuery] int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest("User ID is required to delete a chat");
                }

                // Verify the user exists
                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return BadRequest($"User with ID {userId} not found");
                }

                var deleted = await _chatService.DeleteChatAsync(id, userId);
                
                if (!deleted)
                {
                    return NotFound($"Chat with ID {id} not found");
                }

                _logger.LogInformation($"Chat {id} deleted by user {userId}");
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, $"Unauthorized attempt to delete chat {id} by user {userId}");
                return Forbid("You are not authorized to delete this chat");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting chat {id}");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}