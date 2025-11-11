namespace Bio_ISAC_Group13_GroupProject3.DTOs;

// Question Management
public record QuestionDto(
    int Id,
    string QuestionText,
    string Category,
    int Priority,
    bool IsRequired,
    bool IsActive
);

public record CreateQuestionRequest(
    string QuestionText,
    string Category,
    int Priority,
    bool IsRequired
);

public record UpdateQuestionRequest(
    string? QuestionText,
    string? Category,
    int? Priority,
    bool? IsRequired,
    bool? IsActive
);

// Criteria Management
public record CriteriaDto(
    int Id,
    string Name,
    string? Description,
    string? Category,
    decimal Weight,
    bool IsActive,
    string? EvaluationPrompt
);

public record CreateCriteriaRequest(
    string Name,
    string? Description,
    string? Category,
    decimal Weight,
    string? EvaluationPrompt
);

public record UpdateCriteriaRequest(
    string? Name,
    string? Description,
    string? Category,
    decimal? Weight,
    bool? IsActive,
    string? EvaluationPrompt
);

// Red Flag Management
public record RedFlagDto(
    int Id,
    string Name,
    string? Description,
    string Severity,
    bool IsActive,
    string? DetectionKeywords
);

public record CreateRedFlagRequest(
    string Name,
    string? Description,
    string Severity,
    string? DetectionKeywords
);

public record UpdateRedFlagRequest(
    string? Name,
    string? Description,
    string? Severity,
    bool? IsActive,
    string? DetectionKeywords
);

// System Settings
public record SystemSettingDto(
    string SettingKey,
    string? SettingValue,
    string? Description
);

public record UpdateSettingRequest(
    string SettingKey,
    string SettingValue
);

