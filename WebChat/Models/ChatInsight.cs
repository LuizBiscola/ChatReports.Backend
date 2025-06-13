using System.ComponentModel.DataAnnotations.Schema; // Para [Column(TypeName = "jsonb")] em PostgreSQL

namespace ChatAppApi.Models
{
    public class ChatInsight
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public Chat Chat { get; set; } = null!; // Propriedade de navegação

        public int? MessageId { get; set; } // Opcional: se o insight é sobre uma mensagem específica
        public Message? Message { get; set; } // Propriedade de navegação

        public DateTime AnalysisTimestamp { get; set; } = DateTime.UtcNow;

        public string MainContactReason { get; set; } = string.Empty; // Ex: "Dúvida sobre fatura", "Problema técnico"
        public string Sentiment { get; set; } = string.Empty; // Ex: "positive", "negative", "neutral"

        // Para armazenar arrays de strings ou objetos JSON
        // Se for PostgreSQL, pode usar [Column(TypeName = "jsonb")] para melhor performance com JSON
        // Para outros DBs, ou se não quiser JSONb, use string e serialize/deserialize manualmente.
        public string KeywordsExtracted { get; set; } = "[]"; // Armazenar como JSON string (e.g., "[\"palavra1\", \"palavra2\"]")

        public bool IsProblemRelated { get; set; } = false; // Flag booleana
        public string AnalysisVersion { get; set; } = "1.0"; // Para controle de versão do modelo de IA
    }
}