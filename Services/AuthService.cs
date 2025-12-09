using System.Security.Cryptography;
using System.Text;
using Bio_ISAC_Group13_GroupProject3.Data;
using Bio_ISAC_Group13_GroupProject3.Models;
using Microsoft.EntityFrameworkCore;

namespace Bio_ISAC_Group13_GroupProject3.Services;

public class AuthService
{
    private readonly VettingDbContext _db;

    public AuthService(VettingDbContext db)
    {
        _db = db;
    }

    public async Task<Client?> RegisterAsync(string email, string username, string password, string? firstName = null, string? lastName = null)
    {
        // Check if email already exists
        if (await _db.Clients.AnyAsync(c => c.Email == email))
        {
            return null; // Email already registered
        }

        // Hash password
        var passwordHash = HashPassword(password);

        // Create new client
        var client = new Client
        {
            Email = email,
            Username = username,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Status = ClientStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        return client;
    }

    public async Task<Client?> LoginAsync(string email, string password)
    {
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Email == email);
        
        if (client == null)
        {
            return null; // Client not found
        }

        // If password hash is empty (legacy accounts), allow login but require password update
        if (string.IsNullOrEmpty(client.PasswordHash))
        {
            // For legacy accounts without passwords, we'll allow login but they should set a password
            // For now, we'll reject them to force password setup
            return null;
        }

        // Verify password
        if (!VerifyPassword(password, client.PasswordHash))
        {
            return null; // Invalid password
        }

        return client;
    }

    public async Task<Client?> GetClientByIdAsync(int clientId)
    {
        return await _db.Clients.FindAsync(clientId);
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private bool VerifyPassword(string password, string passwordHash)
    {
        var hashOfInput = HashPassword(password);
        return hashOfInput == passwordHash;
    }
}

