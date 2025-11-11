namespace Bio_ISAC_Group13_GroupProject3.Models;

public class Message
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int TokensUsed { get; set; } = 0;

    public Conversation Conversation { get; set; } = null!;
}

public enum MessageRole
{
    User,
    Assistant,
    System
}

