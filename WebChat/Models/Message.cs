using System.Text.Json.Serialization;

namespace ChatAppApi.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        
        [JsonIgnore] // Ignora na serialização para evitar ciclos
        public Chat Chat { get; set; } = null!;

        public int SenderId { get; set; }
        
        [JsonIgnore] // Ignora na serialização para evitar ciclos
        public User Sender { get; set; } = null!;

        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "sent"; // "sent", "delivered", "read"
    }
}