namespace Bio_ISAC_Group13_GroupProject3.DTOs;

public record UploadDocumentRequest(
    string DocumentType,
    string FileName,
    string ContentType
);

public record DocumentDto(
    int Id,
    string DocumentType,
    string FileName,
    long FileSize,
    string ContentType,
    DateTime UploadedAt
);

public record DocumentUploadResponse(
    int DocumentId,
    string Message
);

