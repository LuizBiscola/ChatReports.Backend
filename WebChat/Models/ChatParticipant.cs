using System.Text.Json.Serialization;

namespace ChatAppApi.Models
{
    public class ChatParticipant
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        
        [JsonIgnore] // Ignora na serialização para evitar ciclos
        public Chat Chat { get; set; } = null!;

        public int UserId { get; set; }
        
        [JsonIgnore] // Ignora na serialização para evitar ciclos
        public User User { get; set; } = null!;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        // Adicione outras propriedades como LastReadMessageId se quiser controlar mensagens lidas via DB
    }
}