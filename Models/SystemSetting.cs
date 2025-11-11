namespace Bio_ISAC_Group13_GroupProject3.Models;

public class SystemSetting
{
    public string SettingKey { get; set; } = string.Empty;
    public string? SettingValue { get; set; }
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

