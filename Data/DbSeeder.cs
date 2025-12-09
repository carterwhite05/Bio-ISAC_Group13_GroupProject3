using Bio_ISAC_Group13_GroupProject3.Models;
using Microsoft.EntityFrameworkCore;

namespace Bio_ISAC_Group13_GroupProject3.Data;

public static class DbSeeder
{
    public static void Seed(VettingDbContext context)
    {
        // Check if already seeded
        if (context.SystemSettings.Any())
        {
            return; // Database already seeded
        }

        // System Settings
        context.SystemSettings.AddRange(
            new SystemSetting { SettingKey = "ai_api_provider", SettingValue = "mock", Description = "AI provider: openai, anthropic, gemini, ollama (local), mock (no API key)" },
            new SystemSetting { SettingKey = "ai_model", SettingValue = "gemini-1.5-flash", Description = "AI model to use for conversations" },
            new SystemSetting { SettingKey = "ai_temperature", SettingValue = "0.7", Description = "Temperature for AI responses (0-1)" },
            new SystemSetting { SettingKey = "ai_max_tokens", SettingValue = "500", Description = "Max tokens per AI response" },
            new SystemSetting { SettingKey = "system_prompt", SettingValue = "You are a professional interviewer conducting a thorough vetting conversation. Be friendly, empathetic, and ask follow-up questions naturally.", Description = "Base system prompt for AI" },
            new SystemSetting { SettingKey = "min_messages_threshold", SettingValue = "20", Description = "Minimum messages before evaluation" },
            new SystemSetting { SettingKey = "auto_evaluate", SettingValue = "true", Description = "Automatically evaluate client after conversation ends" }
        );

        // Questions
        context.Questions.AddRange(
            new Question { QuestionText = "Can you tell me about your current business or professional situation?", Category = "business_life", Priority = 10, IsRequired = true },
            new Question { QuestionText = "What are your main goals for seeking our services?", Category = "goals", Priority = 10, IsRequired = true },
            new Question { QuestionText = "Tell me about your family and personal life.", Category = "personal_life", Priority = 8, IsRequired = true },
            new Question { QuestionText = "What was your childhood like? Where did you grow up?", Category = "childhood", Priority = 7, IsRequired = false },
            new Question { QuestionText = "What is your educational background?", Category = "education", Priority = 7, IsRequired = true },
            new Question { QuestionText = "What are your core values?", Category = "values", Priority = 9, IsRequired = true },
            new Question { QuestionText = "Can you describe your financial situation?", Category = "financial", Priority = 8, IsRequired = true },
            new Question { QuestionText = "Have you worked with similar services before?", Category = "background", Priority = 6, IsRequired = false },
            new Question { QuestionText = "What challenges are you currently facing?", Category = "business_life", Priority = 8, IsRequired = true },
            new Question { QuestionText = "Who are the most important people in your life?", Category = "family", Priority = 7, IsRequired = false }
        );

        // Criteria
        context.Criteria.AddRange(
            new Criteria { Name = "Financial Stability", Description = "Assess the client's financial situation and stability", Category = "financial", Weight = 1.5m, EvaluationPrompt = "Evaluate the client's financial stability based on their statements about income, assets, debts, and financial planning." },
            new Criteria { Name = "Professional Background", Description = "Evaluate professional experience and current business status", Category = "business", Weight = 1.2m, EvaluationPrompt = "Assess the client's professional background, experience, and current business or employment situation." },
            new Criteria { Name = "Communication Skills", Description = "Assess clarity and professionalism in communication", Category = "personal", Weight = 1.0m, EvaluationPrompt = "Evaluate how clearly and professionally the client communicates." },
            new Criteria { Name = "Alignment with Values", Description = "Check if client's values align with company values", Category = "values", Weight = 1.3m, EvaluationPrompt = "Determine if the client's stated values and principles align with the company's core values." },
            new Criteria { Name = "Realistic Expectations", Description = "Evaluate if client has realistic expectations", Category = "goals", Weight = 1.1m, EvaluationPrompt = "Assess whether the client has realistic expectations about outcomes and timelines." }
        );

        // Red Flags
        context.RedFlags.AddRange(
            new RedFlag { Name = "Inconsistent Information", Description = "Client provides contradictory information", Severity = RedFlagSeverity.High, DetectionKeywords = "inconsistent,contradiction,changed story" },
            new RedFlag { Name = "Financial Distress", Description = "Signs of severe financial problems", Severity = RedFlagSeverity.Critical, DetectionKeywords = "bankruptcy,debt,foreclosure,repossession" },
            new RedFlag { Name = "Unrealistic Expectations", Description = "Extremely unrealistic goals or expectations", Severity = RedFlagSeverity.Medium, DetectionKeywords = "overnight success,guaranteed,get rich quick" },
            new RedFlag { Name = "Poor Communication", Description = "Inability to communicate clearly or professionally", Severity = RedFlagSeverity.Medium, DetectionKeywords = "rude,disrespectful,unclear" },
            new RedFlag { Name = "Legal Issues", Description = "Mentions of ongoing legal problems", Severity = RedFlagSeverity.High, DetectionKeywords = "lawsuit,criminal,investigation,indicted" },
            new RedFlag { Name = "Lack of Commitment", Description = "Shows minimal commitment or seriousness", Severity = RedFlagSeverity.Low, DetectionKeywords = "maybe,not sure,just browsing" }
        );

        context.SaveChanges();
    }
}

