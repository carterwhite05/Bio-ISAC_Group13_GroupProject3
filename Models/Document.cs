namespace Bio_ISAC_Group13_GroupProject3.Models;

public class Document
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string DocumentType { get; set; } = string.Empty; // ID, Passport, etc.
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Client Client { get; set; } = null!;
}

