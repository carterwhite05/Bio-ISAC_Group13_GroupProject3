namespace Bio_ISAC_Group13_GroupProject3.DTOs;

public record DossierDto(
    int ClientId,
    string ClientEmail,
    string? FirstName,
    string? LastName,
    string Status,
    decimal OverallScore,
    List<DossierEntryDto> Entries,
    List<RedFlagDetectionDto> RedFlags,
    DateTime CreatedAt
);

public record DossierEntryDto(
    int Id,
    string Category,
    string KeyName,
    string Value,
    decimal ConfidenceScore,
    DateTime CreatedAt
);

public record RedFlagDetectionDto(
    int Id,
    string RedFlagName,
    string Severity,
    string? DetectionReason,
    decimal ConfidenceScore,
    DateTime DetectedAt
);

public record ClientSummaryDto(
    int Id,
    string Email,
    string? FirstName,
    string? LastName,
    string Status,
    decimal OverallScore,
    int ConversationCount,
    int DossierEntryCount,
    int RedFlagCount,
    DateTime CreatedAt
);

