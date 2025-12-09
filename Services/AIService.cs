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
    private readonly IConfiguration _configuration;

    public AIService(VettingDbContext context, IHttpClientFactory httpClientFactory, ILogger<AIService> logger, IConfiguration configuration)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<string> GetAIResponseAsync(List<ConversationMessage> messages, string? customPrompt = null)
    {
        var settings = await GetSystemSettingsAsync();
        var provider = settings.GetValueOrDefault("ai_api_provider", "gemini");
        
        return provider.ToLower() switch
        {
            "openai" => await GetOpenAIResponseAsync(messages, settings, customPrompt),
            "anthropic" => await GetAnthropicResponseAsync(messages, settings, customPrompt),
            "gemini" => await GetGeminiResponseAsync(messages, settings, customPrompt),
            "ollama" => await GetOllamaResponseAsync(messages, settings, customPrompt),
            "mock" => await GetMockResponseAsync(messages, settings, customPrompt),
            _ => throw new InvalidOperationException($"Unsupported AI provider: {provider}")
        };
    }

    private async Task<string> GetOpenAIResponseAsync(List<ConversationMessage> messages, Dictionary<string, string> settings, string? customPrompt)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
                     ?? _configuration["OPENAI_API_KEY"];
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogError("OPENAI_API_KEY not found in environment variables or configuration");
            return "I apologize, but I'm currently unable to process your message due to a configuration issue. Please contact support.";
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

        try
        {
            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
            
            return result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI API");
            return "I apologize, but I'm having trouble processing your message right now. Please try again.";
        }
    }

    private async Task<string> GetAnthropicResponseAsync(List<ConversationMessage> messages, Dictionary<string, string> settings, string? customPrompt)
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") 
                     ?? _configuration["ANTHROPIC_API_KEY"];
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogError("ANTHROPIC_API_KEY not found in environment variables or configuration");
            return "I apologize, but I'm currently unable to process your message due to a configuration issue. Please contact support.";
        }

        var model = settings.GetValueOrDefault("ai_model", "claude-3-5-sonnet-20241022");
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

        try
        {
            var response = await client.PostAsync("https://api.anthropic.com/v1/messages", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Anthropic API error {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
                return "I apologize, but I'm having trouble processing your message right now. Please try again.";
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

            return result.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Anthropic API");
            return "I apologize, but I'm having trouble processing your message right now. Please try again.";
        }
    }

    private async Task<string> GetGeminiResponseAsync(List<ConversationMessage> messages, Dictionary<string, string> settings, string? customPrompt)
    {
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") 
                     ?? _configuration["GEMINI_API_KEY"];
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogError("GEMINI_API_KEY not found in environment variables or configuration");
            return "I apologize, but I'm currently unable to process your message due to a configuration issue. Please contact support.";
        }

        var model = settings.GetValueOrDefault("ai_model", "gemini-1.5-flash");
        var temperature = decimal.Parse(settings.GetValueOrDefault("ai_temperature", "0.7"));
        var maxTokens = int.Parse(settings.GetValueOrDefault("ai_max_tokens", "500"));

        var systemPrompt = customPrompt ?? settings.GetValueOrDefault("system_prompt",
            "You are a professional interviewer conducting a thorough vetting conversation.");

        // Convert messages to Gemini format
        var contents = new List<object>();
        
        // Add system instruction as first user message
        contents.Add(new { 
            role = "user", 
            parts = new[] { new { text = systemPrompt + "\n\nNow begin the conversation." } } 
        });
        contents.Add(new { 
            role = "model", 
            parts = new[] { new { text = "Understood. I'm ready to conduct the interview professionally." } } 
        });

        // Add conversation messages
        foreach (var msg in messages)
        {
            var role = msg.Role.ToLower() == "assistant" ? "model" : "user";
            contents.Add(new { 
                role, 
                parts = new[] { new { text = msg.Content } } 
            });
        }

        var requestBody = new
        {
            contents,
            generationConfig = new
            {
                temperature,
                maxOutputTokens = maxTokens
            }
        };

        var client = _httpClientFactory.CreateClient();
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync(url, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API error {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
                return "I apologize, but I'm having trouble processing your message right now. Please try again.";
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

            return result.GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");
            return "I apologize, but I'm having trouble processing your message right now. Please try again.";
        }
    }

    private async Task<string> GetOllamaResponseAsync(List<ConversationMessage> messages, Dictionary<string, string> settings, string? customPrompt)
    {
        var model = settings.GetValueOrDefault("ai_model", "llama3.2");
        var ollamaUrl = _configuration["OLLAMA_URL"] ?? "http://localhost:11434";
        
        var systemPrompt = customPrompt ?? settings.GetValueOrDefault("system_prompt",
            "You are a professional interviewer conducting a thorough vetting conversation.");

        // Build conversation context
        var conversationText = systemPrompt + "\n\n";
        foreach (var msg in messages)
        {
            conversationText += $"{msg.Role}: {msg.Content}\n";
        }
        conversationText += "assistant:";

        var requestBody = new
        {
            model,
            prompt = conversationText,
            stream = false
        };

        var client = _httpClientFactory.CreateClient();
        var url = $"{ollamaUrl}/api/generate";

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync(url, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Ollama API error {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
                return "I apologize, but I'm having trouble processing your message right now. Please try again.";
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

            return result.TryGetProperty("response", out var responseProp) 
                ? responseProp.GetString() ?? "" 
                : "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Ollama API. Make sure Ollama is running at {Url}", ollamaUrl);
            return "I apologize, but I'm having trouble processing your message right now. Please ensure Ollama is running locally.";
        }
    }

    private async Task<string> GetMockResponseAsync(List<ConversationMessage> messages, Dictionary<string, string> settings, string? customPrompt)
    {
        // Simple mock responses for development/testing - no API key needed
        await Task.Delay(500); // Simulate API delay

        var lastUserMessage = messages.LastOrDefault(m => m.Role.ToLower() == "user");
        if (lastUserMessage == null)
        {
            return "Hello! I'm here to help conduct your interview. How can I assist you today?";
        }

        var content = lastUserMessage.Content.ToLower();

        // Simple keyword-based responses
        if (content.Contains("hello") || content.Contains("hi") || content.Contains("hey"))
        {
            return "Hello! Thank you for taking the time to speak with me today. I'd like to learn more about you. Can you tell me a bit about your background?";
        }
        else if (content.Contains("name"))
        {
            return "That's great to know. Can you tell me about your professional experience?";
        }
        else if (content.Contains("work") || content.Contains("job") || content.Contains("career"))
        {
            return "Interesting! What do you enjoy most about your current role?";
        }
        else if (content.Contains("family") || content.Contains("children") || content.Contains("spouse"))
        {
            return "Thank you for sharing that. How does your family situation impact your work-life balance?";
        }
        else if (content.Contains("education") || content.Contains("school") || content.Contains("degree"))
        {
            return "That's valuable experience. How has your education prepared you for your career?";
        }
        else if (content.Contains("goal") || content.Contains("future") || content.Contains("plan"))
        {
            return "Those are great aspirations. What steps are you taking to achieve those goals?";
        }
        else if (content.Contains("why") || content.Contains("what") || content.Contains("how"))
        {
            return "That's a thoughtful question. Can you elaborate on that a bit more?";
        }
        else
        {
            return "Thank you for that information. Can you tell me more about that?";
        }
    }

    public async Task<Dictionary<string, string>> GetSystemSettingsAsync()
    {
        var settings = await _context.SystemSettings.ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue ?? "");
        return settings;
    }

    public async Task<string> ExtractDossierInformationAsync(string conversationText)
    {
        var settings = await GetSystemSettingsAsync();
        var provider = settings.GetValueOrDefault("ai_api_provider", "gemini");
        
        // For mock mode, return empty JSON structure
        if (provider.ToLower() == "mock")
        {
            return "{}";
        }

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
        var settings = await GetSystemSettingsAsync();
        var provider = settings.GetValueOrDefault("ai_api_provider", "gemini");
        
        // For mock mode, return empty array
        if (provider.ToLower() == "mock")
        {
            return "[]";
        }

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

