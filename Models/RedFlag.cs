namespace Bio_ISAC_Group13_GroupProject3.Models;

public class RedFlag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public RedFlagSeverity Severity { get; set; } = RedFlagSeverity.Medium;
    public bool IsActive { get; set; } = true;
    public string? DetectionKeywords { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RedFlagDetection> Detections { get; set; } = new List<RedFlagDetection>();
}

public enum RedFlagSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public class RedFlagDetection
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int RedFlagId { get; set; }
    public int? MessageId { get; set; }
    public string? DetectionReason { get; set; }
    public decimal ConfidenceScore { get; set; } = 0;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    public Client Client { get; set; } = null!;
    public RedFlag RedFlag { get; set; } = null!;
    public Message? Message { get; set; }
}

