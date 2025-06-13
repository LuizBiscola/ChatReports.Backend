using System.Text.Json.Serialization;

namespace ChatAppApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsOnline { get; set; } = false;
        public DateTime? LastSeen { get; set; }

        // Propriedade de navegação para chats que o usuário participa
        [JsonIgnore] // Ignora na serialização para evitar ciclos
        public ICollection<ChatParticipant> ChatParticipants { get; set; } = new List<ChatParticipant>();
        
        [JsonIgnore] // Ignora na serialização para evitar ciclos
        public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    }
}