using ChatAppApi.Models; // Certifique-se que esta linha está presente para ver suas classes de modelo
using Microsoft.EntityFrameworkCore;

namespace ChatAppApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets para suas tabelas principais
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Chat> Chats { get; set; } = null!;
        public DbSet<ChatParticipant> ChatParticipants { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;

        // NOVO: DbSet para os insights de chat gerados pela IA
        public DbSet<ChatInsight> ChatInsights { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configurações para a tabela ChatParticipant (tabela de junção muitos-para-muitos)
            modelBuilder.Entity<ChatParticipant>()
                .HasKey(cp => cp.Id); // Chave primária simples

            // Índice único para garantir que um usuário não possa participar do mesmo chat duas vezes
            modelBuilder.Entity<ChatParticipant>()
                .HasIndex(cp => new { cp.ChatId, cp.UserId })
                .IsUnique();

            modelBuilder.Entity<ChatParticipant>()
                .HasOne(cp => cp.Chat) // Um participante pertence a um Chat
                .WithMany(c => c.Participants) // Um Chat pode ter muitos participantes
                .HasForeignKey(cp => cp.ChatId); // Chave estrangeira para Chat

            modelBuilder.Entity<ChatParticipant>()
                .HasOne(cp => cp.User) // Um participante está associado a um User
                .WithMany(u => u.ChatParticipants) // Um User pode participar de muitos Chats
                .HasForeignKey(cp => cp.UserId); // Chave estrangeira para User

            // Configurações para a tabela Message
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Chat) // Uma mensagem pertence a um Chat
                .WithMany(c => c.Messages) // Um Chat pode ter muitas mensagens
                .HasForeignKey(m => m.ChatId); // Chave estrangeira para Chat

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender) // Uma mensagem tem um remetente (User)
                .WithMany(u => u.SentMessages) // Um User pode enviar muitas mensagens
                .HasForeignKey(m => m.SenderId) // Chave estrangeira para User
                .OnDelete(DeleteBehavior.Restrict); // Impede a exclusão de um User se ele tiver mensagens associadas

            // NOVO: Configurações para a tabela ChatInsight
            modelBuilder.Entity<ChatInsight>()
                .HasOne(ci => ci.Chat) // Um insight se refere a um Chat
                .WithMany() // Um Chat pode ter muitos insights (sem propriedade de navegação na entidade Chat se não for estritamente necessário)
                .HasForeignKey(ci => ci.ChatId) // Chave estrangeira para Chat
                .OnDelete(DeleteBehavior.Cascade); // Se o chat for excluído, seus insights também são

            // Opcional: Se ChatInsight se refere a uma mensagem específica
            modelBuilder.Entity<ChatInsight>()
                .HasOne(ci => ci.Message) // Um insight pode se referir a uma mensagem específica
                .WithMany() // Uma mensagem pode ter muitos insights (ou nenhum)
                .HasForeignKey(ci => ci.MessageId)
                .IsRequired(false) // MessageId pode ser nulo se o insight for sobre o chat inteiro
                .OnDelete(DeleteBehavior.Restrict); // Impede exclusão da mensagem se tiver insight

            // Se estiver usando PostgreSQL, configure o tipo JSONB para KeywordsExtracted
            modelBuilder.Entity<ChatInsight>()
                .Property(ci => ci.KeywordsExtracted)
                .HasColumnType("jsonb");

            base.OnModelCreating(modelBuilder);
        }
    }
}