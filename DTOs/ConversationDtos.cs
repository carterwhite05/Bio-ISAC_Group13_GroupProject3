namespace Bio_ISAC_Group13_GroupProject3.DTOs;

public record StartConversationRequest(string Email, string? FirstName, string? LastName);

public record StartConversationResponse(int ConversationId, int ClientId, string InitialMessage);

public record SendMessageRequest(int ConversationId, string Message);

public record SendMessageResponse(
    int MessageId,
    string AssistantMessage,
    bool ConversationEnded,
    int TotalMessages
);

public record ConversationSummary(
    int ConversationId,
    int ClientId,
    string ClientEmail,
    DateTime StartedAt,
    DateTime? EndedAt,
    string Status,
    int TotalMessages
);

public record MessageDto(
    int Id,
    string Role,
    string Content,
    DateTime CreatedAt
);

