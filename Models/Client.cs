namespace Bio_ISAC_Group13_GroupProject3.Models;

public class Client
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public ClientStatus Status { get; set; } = ClientStatus.Pending;
    public decimal OverallScore { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<DossierEntry> DossierEntries { get; set; } = new List<DossierEntry>();
    public ICollection<RedFlagDetection> RedFlagDetections { get; set; } = new List<RedFlagDetection>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}

public enum ClientStatus
{
    Pending,
    Approved,
    Rejected,
    InProgress,
    InterviewCompleted,
    UnderReview
}

