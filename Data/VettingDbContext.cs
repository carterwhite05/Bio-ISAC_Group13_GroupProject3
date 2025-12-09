using Microsoft.EntityFrameworkCore;
using Bio_ISAC_Group13_GroupProject3.Models;

namespace Bio_ISAC_Group13_GroupProject3.Data;

public class VettingDbContext : DbContext
{
    public VettingDbContext(DbContextOptions<VettingDbContext> options) : base(options) { }

    public DbSet<Client> Clients { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<DossierEntry> DossierEntries { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<AskedQuestion> AskedQuestions { get; set; }
    public DbSet<QuestionAnswer> QuestionAnswers { get; set; }
    public DbSet<Criteria> Criteria { get; set; }
    public DbSet<RedFlag> RedFlags { get; set; }
    public DbSet<RedFlagDetection> RedFlagDetections { get; set; }
    public DbSet<SystemSetting> SystemSettings { get; set; }
    public DbSet<Document> Documents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Client configuration
        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("clients");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
            entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
            entity.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(100);
            entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(100);
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(e => e.OverallScore).HasColumnName("overall_score").HasPrecision(5, 2);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(e => e.Email);
        });

        // Conversation configuration
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable("conversations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.EndedAt).HasColumnName("ended_at");
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(e => e.TotalMessages).HasColumnName("total_messages");
            entity.Property(e => e.CurrentQuestionId).HasColumnName("current_question_id");
            entity.Property(e => e.WaitingForAdditionalInfo).HasColumnName("waiting_for_additional_info");
            entity.Property(e => e.ReviewStatus).HasColumnName("review_status").HasMaxLength(255);
            entity.HasOne(e => e.Client).WithMany(c => c.Conversations).HasForeignKey(e => e.ClientId);
        });

        // Message configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
            entity.Property(e => e.Role).HasColumnName("role").HasConversion<string>();
            entity.Property(e => e.Content).HasColumnName("content").HasColumnType("text");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.TokensUsed).HasColumnName("tokens_used");
            entity.HasOne(e => e.Conversation).WithMany(c => c.Messages).HasForeignKey(e => e.ConversationId);
        });

        // DossierEntry configuration
        modelBuilder.Entity<DossierEntry>(entity =>
        {
            entity.ToTable("dossier_entries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.Category).HasColumnName("category").HasConversion<string>();
            entity.Property(e => e.KeyName).HasColumnName("key_name").HasMaxLength(255);
            entity.Property(e => e.Value).HasColumnName("value").HasColumnType("text");
            entity.Property(e => e.ConfidenceScore).HasColumnName("confidence_score").HasPrecision(5, 2);
            entity.Property(e => e.SourceMessageId).HasColumnName("source_message_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(e => e.Client).WithMany(c => c.DossierEntries).HasForeignKey(e => e.ClientId);
            entity.HasOne(e => e.SourceMessage).WithMany().HasForeignKey(e => e.SourceMessageId);
        });

        // Question configuration
        modelBuilder.Entity<Question>(entity =>
        {
            entity.ToTable("questions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.QuestionText).HasColumnName("question_text").HasColumnType("text");
            entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(100);
            entity.Property(e => e.Priority).HasColumnName("priority");
            entity.Property(e => e.IsRequired).HasColumnName("is_required");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        // AskedQuestion configuration
        modelBuilder.Entity<AskedQuestion>(entity =>
        {
            entity.ToTable("asked_questions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.AskedAt).HasColumnName("asked_at");
            entity.Property(e => e.Answered).HasColumnName("answered");
            entity.HasOne(e => e.Conversation).WithMany(c => c.AskedQuestions).HasForeignKey(e => e.ConversationId);
            entity.HasOne(e => e.Question).WithMany(q => q.AskedQuestions).HasForeignKey(e => e.QuestionId);
            entity.HasIndex(e => new { e.ConversationId, e.QuestionId }).IsUnique();
        });

        // QuestionAnswer configuration
        modelBuilder.Entity<QuestionAnswer>(entity =>
        {
            entity.ToTable("question_answers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.Answer).HasColumnName("answer").HasColumnType("text");
            entity.Property(e => e.AdditionalInfo).HasColumnName("additional_info").HasColumnType("text");
            entity.Property(e => e.AnsweredAt).HasColumnName("answered_at");
            entity.HasOne(e => e.Conversation).WithMany(c => c.QuestionAnswers).HasForeignKey(e => e.ConversationId);
            entity.HasOne(e => e.Question).WithMany(q => q.QuestionAnswers).HasForeignKey(e => e.QuestionId);
        });

        // Criteria configuration
        modelBuilder.Entity<Criteria>(entity =>
        {
            entity.ToTable("criteria");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255);
            entity.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
            entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(100);
            entity.Property(e => e.Weight).HasColumnName("weight").HasPrecision(5, 2);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.EvaluationPrompt).HasColumnName("evaluation_prompt").HasColumnType("text");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // RedFlag configuration
        modelBuilder.Entity<RedFlag>(entity =>
        {
            entity.ToTable("red_flags");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255);
            entity.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
            entity.Property(e => e.Severity).HasColumnName("severity").HasConversion<string>();
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.DetectionKeywords).HasColumnName("detection_keywords").HasColumnType("text");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // RedFlagDetection configuration
        modelBuilder.Entity<RedFlagDetection>(entity =>
        {
            entity.ToTable("red_flag_detections");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.RedFlagId).HasColumnName("red_flag_id");
            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.DetectionReason).HasColumnName("detection_reason").HasColumnType("text");
            entity.Property(e => e.ConfidenceScore).HasColumnName("confidence_score").HasPrecision(5, 2);
            entity.Property(e => e.DetectedAt).HasColumnName("detected_at");
            entity.HasOne(e => e.Client).WithMany(c => c.RedFlagDetections).HasForeignKey(e => e.ClientId);
            entity.HasOne(e => e.RedFlag).WithMany(r => r.Detections).HasForeignKey(e => e.RedFlagId);
            entity.HasOne(e => e.Message).WithMany().HasForeignKey(e => e.MessageId);
        });

        // SystemSetting configuration
        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.ToTable("system_settings");
            entity.HasKey(e => e.SettingKey);
            entity.Property(e => e.SettingKey).HasColumnName("setting_key").HasMaxLength(255);
            entity.Property(e => e.SettingValue).HasColumnName("setting_value").HasColumnType("text");
            entity.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        // Document configuration
        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.DocumentType).HasColumnName("document_type").HasMaxLength(100);
            entity.Property(e => e.FileName).HasColumnName("file_name").HasMaxLength(255);
            entity.Property(e => e.FilePath).HasColumnName("file_path").HasMaxLength(500);
            entity.Property(e => e.FileSize).HasColumnName("file_size");
            entity.Property(e => e.ContentType).HasColumnName("content_type").HasMaxLength(100);
            entity.Property(e => e.UploadedAt).HasColumnName("uploaded_at");
            entity.HasOne(e => e.Client).WithMany(c => c.Documents).HasForeignKey(e => e.ClientId);
            entity.HasIndex(e => e.ClientId);
            entity.HasIndex(e => e.DocumentType);
        });
    }
}

