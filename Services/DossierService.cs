using Bio_ISAC_Group13_GroupProject3.Data;
using Bio_ISAC_Group13_GroupProject3.Models;
using Bio_ISAC_Group13_GroupProject3.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Bio_ISAC_Group13_GroupProject3.Services;

public class DossierService
{
    private readonly VettingDbContext _context;
    private readonly AIService _aiService;
    private readonly ILogger<DossierService> _logger;

    public DossierService(VettingDbContext context, AIService aiService, ILogger<DossierService> logger)
    {
        _context = context;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task ExtractAndSaveDossierInfoAsync(int clientId, int conversationId, string messageContent)
    {
        try
        {
            // Get recent conversation context (last 10 messages)
            var recentMessages = await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(10)
                .OrderBy(m => m.CreatedAt)
                .Select(m => $"{m.Role}: {m.Content}")
                .ToListAsync();

            var conversationText = string.Join("\n", recentMessages);

            // Use AI to extract structured information
            var extractionResult = await _aiService.ExtractDossierInformationAsync(conversationText);

            // Check if response is valid JSON before parsing
            if (string.IsNullOrWhiteSpace(extractionResult) || 
                (!extractionResult.TrimStart().StartsWith("{") && !extractionResult.TrimStart().StartsWith("[")))
            {
                _logger.LogWarning("AI service returned non-JSON response: {Response}", extractionResult);
                return;
            }

            // Parse JSON response
            Dictionary<string, List<DossierExtraction>>? dossierData;
            try
            {
                dossierData = JsonSerializer.Deserialize<Dictionary<string, List<DossierExtraction>>>(extractionResult);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse dossier extraction JSON: {Response}", extractionResult);
                return;
            }

            if (dossierData != null)
            {
                foreach (var (category, entries) in dossierData)
                {
                    foreach (var entry in entries)
                    {
                        // Check if this information already exists
                        var existing = await _context.DossierEntries
                            .FirstOrDefaultAsync(de => 
                                de.ClientId == clientId && 
                                de.KeyName == entry.Key &&
                                de.Category.ToString().ToLower() == category.Replace("_", "").ToLower()
                            );

                        if (existing != null)
                        {
                            // Update if new confidence is higher
                            if (entry.Confidence > (double)existing.ConfidenceScore)
                            {
                                existing.Value = entry.Value;
                                existing.ConfidenceScore = (decimal)entry.Confidence;
                                existing.UpdatedAt = DateTime.UtcNow;
                            }
                        }
                        else
                        {
                            // Create new entry
                            var dossierEntry = new DossierEntry
                            {
                                ClientId = clientId,
                                Category = ParseCategory(category),
                                KeyName = entry.Key,
                                Value = entry.Value,
                                ConfidenceScore = (decimal)entry.Confidence,
                                SourceMessageId = await _context.Messages
                                    .Where(m => m.ConversationId == conversationId)
                                    .OrderByDescending(m => m.CreatedAt)
                                    .Select(m => m.Id)
                                    .FirstOrDefaultAsync()
                            };
                            _context.DossierEntries.Add(dossierEntry);
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }

            // Also check for red flags
            await DetectRedFlagsAsync(clientId, conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting dossier information for client {ClientId}", clientId);
        }
    }

    private async Task DetectRedFlagsAsync(int clientId, int conversationId)
    {
        try
        {
            var activeRedFlags = await _context.RedFlags
                .Where(rf => rf.IsActive)
                .ToListAsync();

            if (!activeRedFlags.Any())
            {
                return;
            }

            // Get conversation text
            var conversationText = await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => $"{m.Role}: {m.Content}")
                .ToListAsync();

            var fullText = string.Join("\n", conversationText);

            // Simple keyword detection
            foreach (var redFlag in activeRedFlags)
            {
                if (!string.IsNullOrEmpty(redFlag.DetectionKeywords))
                {
                    var keywords = redFlag.DetectionKeywords.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var keyword in keywords)
                    {
                        if (fullText.Contains(keyword.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            // Check if already detected
                            var existing = await _context.RedFlagDetections
                                .AnyAsync(rfd => rfd.ClientId == clientId && rfd.RedFlagId == redFlag.Id);

                            if (!existing)
                            {
                                var detection = new RedFlagDetection
                                {
                                    ClientId = clientId,
                                    RedFlagId = redFlag.Id,
                                    DetectionReason = $"Keyword detected: {keyword.Trim()}",
                                    ConfidenceScore = 0.7m,
                                    DetectedAt = DateTime.UtcNow
                                };
                                _context.RedFlagDetections.Add(detection);
                            }
                        }
                    }
                }
            }

            // Use AI for more sophisticated red flag detection
            var redFlagDescriptions = activeRedFlags.Select(rf => rf.Description ?? rf.Name).ToList();
            var aiDetectionResult = await _aiService.DetectRedFlagsAsync(fullText, redFlagDescriptions);

            // Check if response is valid JSON before parsing
            if (string.IsNullOrWhiteSpace(aiDetectionResult) || 
                !aiDetectionResult.TrimStart().StartsWith("["))
            {
                _logger.LogWarning("AI service returned non-JSON response for red flags: {Response}", aiDetectionResult);
                return;
            }

            try
            {
                var detections = JsonSerializer.Deserialize<List<RedFlagAIDetection>>(aiDetectionResult);
                if (detections != null)
                {
                    foreach (var detection in detections)
                    {
                        if (detection.RedFlagIndex > 0 && detection.RedFlagIndex <= activeRedFlags.Count)
                        {
                            var redFlag = activeRedFlags[detection.RedFlagIndex - 1];
                            
                            var existing = await _context.RedFlagDetections
                                .AnyAsync(rfd => rfd.ClientId == clientId && rfd.RedFlagId == redFlag.Id);

                            if (!existing)
                            {
                                var newDetection = new RedFlagDetection
                                {
                                    ClientId = clientId,
                                    RedFlagId = redFlag.Id,
                                    DetectionReason = detection.Reason,
                                    ConfidenceScore = (decimal)detection.Confidence,
                                    DetectedAt = DateTime.UtcNow
                                };
                                _context.RedFlagDetections.Add(newDetection);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse AI red flag detection response");
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting red flags for client {ClientId}", clientId);
        }
    }

    public async Task<decimal> EvaluateClientAsync(int clientId)
    {
        try
        {
            var client = await _context.Clients
                .Include(c => c.Conversations)
                .ThenInclude(conv => conv.Messages)
                .FirstOrDefaultAsync(c => c.Id == clientId);

            if (client == null)
            {
                throw new InvalidOperationException("Client not found");
            }

            // Get all conversation text
            var allMessages = client.Conversations
                .SelectMany(c => c.Messages)
                .OrderBy(m => m.CreatedAt)
                .Select(m => $"{m.Role}: {m.Content}");

            var conversationText = string.Join("\n", allMessages);

            // Get active criteria
            var activeCriteria = await _context.Criteria
                .Where(c => c.IsActive)
                .ToListAsync();

            decimal totalScore = 0;
            decimal totalWeight = 0;

            foreach (var criteria in activeCriteria)
            {
                if (!string.IsNullOrEmpty(criteria.EvaluationPrompt))
                {
                    var score = await _aiService.EvaluateByCriteriaAsync(
                        conversationText,
                        criteria.Name,
                        criteria.EvaluationPrompt
                    );

                    totalScore += score * criteria.Weight;
                    totalWeight += criteria.Weight;
                }
            }

            // Calculate weighted average
            var overallScore = totalWeight > 0 ? totalScore / totalWeight : 50;

            // Apply red flag penalty
            var redFlagCount = await _context.RedFlagDetections
                .Where(rfd => rfd.ClientId == clientId)
                .CountAsync();

            if (redFlagCount > 0)
            {
                overallScore -= Math.Min(redFlagCount * 5, 30); // Max 30 point penalty
            }

            overallScore = Math.Clamp(overallScore, 0, 100);

            // Update client score and status
            client.OverallScore = overallScore;
            
            if (overallScore >= 70)
            {
                client.Status = ClientStatus.Approved;
            }
            else if (overallScore < 50 || redFlagCount >= 2)
            {
                client.Status = ClientStatus.Rejected;
            }
            else
            {
                client.Status = ClientStatus.Pending; // Needs manual review
            }

            await _context.SaveChangesAsync();

            return overallScore;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating client {ClientId}", clientId);
            throw;
        }
    }

    public async Task<DossierDto> GetClientDossierAsync(int clientId)
    {
        var client = await _context.Clients
            .Include(c => c.DossierEntries)
            .Include(c => c.RedFlagDetections)
            .ThenInclude(rfd => rfd.RedFlag)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == clientId);

        if (client == null)
        {
            throw new InvalidOperationException("Client not found");
        }

        var entries = client.DossierEntries
            .Select(de => new DossierEntryDto(
                de.Id,
                de.Category.ToString(),
                de.KeyName,
                de.Value,
                de.ConfidenceScore,
                de.CreatedAt
            ))
            .ToList();

        var redFlags = client.RedFlagDetections
            .Select(rfd => new RedFlagDetectionDto(
                rfd.Id,
                rfd.RedFlag.Name,
                rfd.RedFlag.Severity.ToString(),
                rfd.DetectionReason,
                rfd.ConfidenceScore,
                rfd.DetectedAt
            ))
            .ToList();

        return new DossierDto(
            client.Id,
            client.Email,
            client.FirstName,
            client.LastName,
            client.Status.ToString(),
            client.OverallScore,
            entries,
            redFlags,
            client.CreatedAt
        );
    }

    public async Task<List<ClientSummaryDto>> GetAllClientsAsync()
    {
        return await _context.Clients
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ClientSummaryDto(
                c.Id,
                c.Email,
                c.Username,
                c.FirstName,
                c.LastName,
                c.Status.ToString(),
                c.OverallScore,
                c.Conversations.Count,
                c.DossierEntries.Count,
                c.RedFlagDetections.Count,
                c.CreatedAt,
                c.UpdatedAt
            ))
            .ToListAsync();
    }

    public async Task<ClientSummaryDto?> GetClientByEmailAsync(string email)
    {
        var client = await _context.Clients
            .Where(c => c.Email == email)
            .Select(c => new ClientSummaryDto(
                c.Id,
                c.Email,
                c.Username,
                c.FirstName,
                c.LastName,
                c.Status.ToString(),
                c.OverallScore,
                c.Conversations.Count,
                c.DossierEntries.Count,
                c.RedFlagDetections.Count,
                c.CreatedAt,
                c.UpdatedAt
            ))
            .FirstOrDefaultAsync();

        return client;
    }

    public async Task DeleteClientAsync(int clientId)
    {
        try
        {
            var client = await _context.Clients
                .Include(c => c.Conversations)
                .ThenInclude(conv => conv.Messages)
                .Include(c => c.Conversations)
                .ThenInclude(conv => conv.QuestionAnswers)
                .Include(c => c.DossierEntries)
                .Include(c => c.RedFlagDetections)
                .FirstOrDefaultAsync(c => c.Id == clientId);

            if (client == null)
            {
                throw new InvalidOperationException("Client not found");
            }

            // Delete all related data (cascading deletes should handle this, but being explicit)
            _context.Messages.RemoveRange(client.Conversations.SelectMany(c => c.Messages));
            _context.QuestionAnswers.RemoveRange(client.Conversations.SelectMany(c => c.QuestionAnswers));
            _context.Conversations.RemoveRange(client.Conversations);
            _context.DossierEntries.RemoveRange(client.DossierEntries);
            _context.RedFlagDetections.RemoveRange(client.RedFlagDetections);
            
            // Delete the client
            _context.Clients.Remove(client);
            
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting client {ClientId}", clientId);
            throw;
        }
    }

    public async Task UpdateClientStatusAsync(int clientId, string status)
    {
        try
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null)
            {
                throw new InvalidOperationException("Client not found");
            }

            if (Enum.TryParse<ClientStatus>(status, true, out var clientStatus))
            {
                client.Status = clientStatus;
                client.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new ArgumentException($"Invalid status: {status}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating client status {ClientId} to {Status}", clientId, status);
            throw;
        }
    }

    public async Task<ClientSummaryDto> CreateOrUpdateClientAsync(CreateClientRequest request)
    {
        try
        {
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == request.Email);
            
            if (client == null)
            {
                // Create new client
                client = new Client
                {
                    Email = request.Email,
                    Username = request.Username,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Status = ClientStatus.Pending
                };
                _context.Clients.Add(client);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created new client with email: {Email}", request.Email);
            }
            else
            {
                // Update existing client
                if (!string.IsNullOrEmpty(request.Username))
                    client.Username = request.Username;
                if (!string.IsNullOrEmpty(request.FirstName))
                    client.FirstName = request.FirstName;
                if (!string.IsNullOrEmpty(request.LastName))
                    client.LastName = request.LastName;
                client.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated client with email: {Email}", request.Email);
            }

            return await GetClientByEmailAsync(request.Email) ?? throw new InvalidOperationException("Failed to retrieve client");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating client {Email}", request.Email);
            throw;
        }
    }

    public async Task<ClientSummaryDto> UpdateClientAsync(int clientId, UpdateClientRequest request)
    {
        try
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null)
            {
                throw new InvalidOperationException("Client not found");
            }

            if (request.Username != null)
                client.Username = request.Username;
            if (request.FirstName != null)
                client.FirstName = request.FirstName;
            if (request.LastName != null)
                client.LastName = request.LastName;
            
            client.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetClientByEmailAsync(client.Email) ?? throw new InvalidOperationException("Failed to retrieve client");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating client {ClientId}", clientId);
            throw;
        }
    }

    private DossierCategory ParseCategory(string category)
    {
        return category.Replace("_", "").ToLower() switch
        {
            "personallife" => DossierCategory.PersonalLife,
            "businesslife" => DossierCategory.BusinessLife,
            "family" => DossierCategory.Family,
            "childhood" => DossierCategory.Childhood,
            "education" => DossierCategory.Education,
            "values" => DossierCategory.Values,
            "goals" => DossierCategory.Goals,
            "background" => DossierCategory.Background,
            "financial" => DossierCategory.Financial,
            _ => DossierCategory.Other
        };
    }
}

public class DossierExtraction
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

public class RedFlagAIDetection
{
    public int RedFlagIndex { get; set; }
    public string Reason { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

