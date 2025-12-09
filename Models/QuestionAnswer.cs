namespace Bio_ISAC_Group13_GroupProject3.Models;

public class QuestionAnswer
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int QuestionId { get; set; }
    public string Answer { get; set; } = string.Empty;
    public string? AdditionalInfo { get; set; }
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;

    public Conversation Conversation { get; set; } = null!;
    public Question Question { get; set; } = null!;
}

