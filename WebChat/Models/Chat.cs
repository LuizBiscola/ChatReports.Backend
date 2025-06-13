using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChatAppApi.Models
{
    public class Chat
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Para chats em grupo
        public string Type { get; set; } = "direct"; // "direct" ou "group"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Propriedades de navegação
        [JsonIgnore] // Ignora na serialização para evitar ciclos
        public ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();
        
        [JsonIgnore] // Ignora na serialização para evitar ciclos
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}