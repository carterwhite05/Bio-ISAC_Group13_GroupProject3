namespace Bio_ISAC_Group13_GroupProject3.DTOs;

public record RegisterRequest(
    string Email,
    string Username,
    string Password,
    string? FirstName = null,
    string? LastName = null
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResponse(
    int Id,
    string Email,
    string? Username,
    string? FirstName,
    string? LastName
);

