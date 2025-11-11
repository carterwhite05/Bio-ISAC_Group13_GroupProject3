using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Bio_ISAC_Group13_GroupProject3.Data;
using Microsoft.EntityFrameworkCore;

namespace Bio_ISAC_Group13_GroupProject3.Services;

public class AIService
{
    private readonly VettingDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AIService> _logger;

    public AIService(VettingDbContext context, IHttpClientFactory httpClientFactory, ILogger<AIService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> GetAIResponseAsync(List<ConversationMessage> messages, string? customPrompt = null)
    {
        var settings = await GetSystemSettingsAsync();
        var provider = settings.GetValueOrDefault("ai_api_provider", "openai");
        
        return provider.ToLower() switch
        {
            "openai" => await GetOpenAIResponseAsync(messages, settings, customPrompt),
            "anthropic" => await GetAnthropicResponseAsync(messages, settings, customPrompt),
            _ => throw new InvalidOperationException($"Unsupported AI provider: {provider}")
        };
    }

    private async Task<string> GetOpenAIResponseAsync(List<ConversationMessage> messages, Dictionary<string, string> settings, string? customPrompt)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("OPENAI_API_KEY environment variable not set");
        }

        var model = settings.GetValueOrDefault("ai_model", "gpt-4");
        var temperature = decimal.Parse(settings.GetValueOrDefault("ai_temperature", "0.7"));
        var maxTokens = int.Parse(settings.GetValueOrDefault("ai_max_tokens", "500"));

        var systemPrompt = customPrompt ?? settings.GetValueOrDefault("system_prompt", 
            "You are a professional interviewer conducting a thorough vetting conversation.");

        var requestMessages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };
        requestMessages.AddRange(messages.Select(m => new { role = m.Role, content = m.Content }));

        var requestBody = new
        {
            model,
            messages = requestMessages,
            temperature,
            max_tokens = maxTokens
        };

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
        
        return result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }

    private async Task<string> GetAnthropicResponseAsync(List<ConversationMessage> messages, Dictionary<string, string> settings, string? customPrompt)
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("ANTHROPIC_API_KEY environment variable not set");
        }

        var model = settings.GetValueOrDefault("ai_model", "claude-3-sonnet-20240229");
        var temperature = decimal.Parse(settings.GetValueOrDefault("ai_temperature", "0.7"));
        var maxTokens = int.Parse(settings.GetValueOrDefault("ai_max_tokens", "500"));

        var systemPrompt = customPrompt ?? settings.GetValueOrDefault("system_prompt",
            "You are a professional interviewer conducting a thorough vetting conversation.");

        var requestBody = new
        {
            model,
            max_tokens = maxTokens,
            temperature,
            system = systemPrompt,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToList()
        };

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("https://api.anthropic.com/v1/messages", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

        return result.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
    }

    public async Task<Dictionary<string, string>> GetSystemSettingsAsync()
    {
        var settings = await _context.SystemSettings.ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue ?? "");
        return settings;
    }

    public async Task<string> ExtractDossierInformationAsync(string conversationText)
    {
        var prompt = @"Analyze the following conversation and extract key information about the person being interviewed. 
Format the response as JSON with categories: personal_life, business_life, family, childhood, education, values, goals, background, financial.
Each category should contain key-value pairs of extracted information with confidence scores (0-1).

Example format:
{
  ""personal_life"": [{""key"": ""marital_status"", ""value"": ""married"", ""confidence"": 0.9}],
  ""business_life"": [{""key"": ""current_role"", ""value"": ""CEO"", ""confidence"": 0.95}]
}

Conversation:
" + conversationText;

        var messages = new List<ConversationMessage>
        {
            new ConversationMessage { Role = "user", Content = prompt }
        };

        return await GetAIResponseAsync(messages);
    }

    public async Task<string> DetectRedFlagsAsync(string conversationText, List<string> redFlagDescriptions)
    {
        var redFlagsList = string.Join("\n", redFlagDescriptions.Select((rf, i) => $"{i + 1}. {rf}"));
        
        var prompt = $@"Analyze the following conversation for potential red flags. 
Red flags to look for:
{redFlagsList}

Return a JSON array of detected red flags with format:
[{{""red_flag_index"": 1, ""reason"": ""explanation"", ""confidence"": 0.85}}]

If no red flags are detected, return an empty array [].

Conversation:
{conversationText}";

        var messages = new List<ConversationMessage>
        {
            new ConversationMessage { Role = "user", Content = prompt }
        };

        return await GetAIResponseAsync(messages);
    }

    public async Task<decimal> EvaluateByCriteriaAsync(string conversationText, string criteriaName, string evaluationPrompt)
    {
        var prompt = $@"Evaluate the following conversation based on the criterion: {criteriaName}

Evaluation guideline: {evaluationPrompt}

Return ONLY a score between 0 and 100 (integer). No explanation, just the number.

Conversation:
{conversationText}";

        var messages = new List<ConversationMessage>
        {
            new ConversationMessage { Role = "user", Content = prompt }
        };

        var scoreText = await GetAIResponseAsync(messages);
        
        if (decimal.TryParse(scoreText.Trim(), out var score))
        {
            return Math.Clamp(score, 0, 100);
        }

        _logger.LogWarning("Failed to parse AI score response: {Response}", scoreText);
        return 50; // Default neutral score
    }
}

public class ConversationMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

