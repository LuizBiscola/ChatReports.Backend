using ChatAppApi.Models;

namespace ChatAppApi.DTOs
{
    public class ChatDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<ParticipantDto> Participants { get; set; } = new();
    }

    public class ParticipantDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }

    public class MessageDto
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public int SenderId { get; set; }
        public string SenderUsername { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastSeen { get; set; }
    }

    public class CreateUserDto
    {
        public string Username { get; set; } = string.Empty;
    }

    public class UpdateUserDto
    {
        public string Username { get; set; } = string.Empty;
    }

    public class SendMessageDto
    {
        public int SenderId { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    public class UpdateMessageStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }

    public class CreateChatDto
    {
        public string Name { get; set; } = string.Empty;
        public List<int> ParticipantUserIds { get; set; } = new List<int>();
    }

    public class CreateDirectChatDto
    {
        public int UserId1 { get; set; }
        public int UserId2 { get; set; }
    }

    public class DeleteChatDto
    {
        public int UserId { get; set; }
        public string? Reason { get; set; }
    }
}