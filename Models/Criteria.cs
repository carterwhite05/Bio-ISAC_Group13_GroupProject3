namespace Bio_ISAC_Group13_GroupProject3.Models;

public class Criteria
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal Weight { get; set; } = 1.0m;
    public bool IsActive { get; set; } = true;
    public string? EvaluationPrompt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

