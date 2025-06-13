using ChatAppApi.Models;

namespace ChatAppApi.DTOs
{
    public static class DtoExtensions
    {
        public static ChatDto ToDto(this Chat chat)
        {
            return new ChatDto
            {
                Id = chat.Id,
                Name = chat.Name,
                Type = chat.Type,
                CreatedAt = chat.CreatedAt,
                Participants = chat.Participants?.Select(p => p.ToDto()).ToList() ?? new List<ParticipantDto>()
            };
        }

        public static ParticipantDto ToDto(this ChatParticipant participant)
        {
            return new ParticipantDto
            {
                Id = participant.Id,
                UserId = participant.UserId,
                Username = participant.User?.Username ?? string.Empty,
                JoinedAt = participant.JoinedAt
            };
        }

        public static MessageDto ToDto(this Message message)
        {
            return new MessageDto
            {
                Id = message.Id,
                ChatId = message.ChatId,
                SenderId = message.SenderId,
                SenderUsername = message.Sender?.Username ?? string.Empty,
                Content = message.Content,
                Timestamp = message.Timestamp,
                Status = message.Status
            };
        }

        public static UserDto ToDto(this User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                CreatedAt = user.CreatedAt,
                IsOnline = user.IsOnline,
                LastSeen = user.LastSeen
            };
        }
    }
}