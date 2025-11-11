namespace Bio_ISAC_Group13_GroupProject3.Models;

public class DossierEntry
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public DossierCategory Category { get; set; }
    public string KeyName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; } = 0;
    public int? SourceMessageId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Client Client { get; set; } = null!;
    public Message? SourceMessage { get; set; }
}

public enum DossierCategory
{
    PersonalLife,
    BusinessLife,
    Family,
    Childhood,
    Education,
    Values,
    Goals,
    Background,
    Financial,
    Other
}

