namespace Bio_ISAC_Group13_GroupProject3.Models;

public class Conversation
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public ConversationStatus Status { get; set; } = ConversationStatus.Active;
    public int TotalMessages { get; set; } = 0;

    public Client Client { get; set; } = null!;
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<AskedQuestion> AskedQuestions { get; set; } = new List<AskedQuestion>();
}

public enum ConversationStatus
{
    Active,
    Completed,
    Abandoned
}

