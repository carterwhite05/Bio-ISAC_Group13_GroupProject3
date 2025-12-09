namespace Bio_ISAC_Group13_GroupProject3.DTOs;

public record CreateClientRequest(
    string Email,
    string? Username = null,
    string? FirstName = null,
    string? LastName = null
);

public record UpdateClientRequest(
    string? Username = null,
    string? FirstName = null,
    string? LastName = null
);

public record UpdateClientStatusRequest(string Status);
