namespace Bio_ISAC_Group13_GroupProject3.Models;

public class Question
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Priority { get; set; } = 0;
    public bool IsRequired { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AskedQuestion> AskedQuestions { get; set; } = new List<AskedQuestion>();
}

public class AskedQuestion
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int QuestionId { get; set; }
    public DateTime AskedAt { get; set; } = DateTime.UtcNow;
    public bool Answered { get; set; } = false;

    public Conversation Conversation { get; set; } = null!;
    public Question Question { get; set; } = null!;
}

