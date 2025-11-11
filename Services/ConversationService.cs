using Bio_ISAC_Group13_GroupProject3.Data;
using Bio_ISAC_Group13_GroupProject3.Models;
using Bio_ISAC_Group13_GroupProject3.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Bio_ISAC_Group13_GroupProject3.Services;

public class ConversationService
{
    private readonly VettingDbContext _context;
    private readonly AIService _aiService;
    private readonly DossierService _dossierService;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(
        VettingDbContext context, 
        AIService aiService, 
        DossierService dossierService,
        ILogger<ConversationService> logger)
    {
        _context = context;
        _aiService = aiService;
        _dossierService = dossierService;
        _logger = logger;
    }

    public async Task<StartConversationResponse> StartConversationAsync(StartConversationRequest request)
    {
        // Find or create client
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == request.Email);
        
        if (client == null)
        {
            client = new Client
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Status = ClientStatus.InProgress
            };
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
        }
        else
        {
            // Update client status if starting new conversation
            client.Status = ClientStatus.InProgress;
            client.FirstName = request.FirstName ?? client.FirstName;
            client.LastName = request.LastName ?? client.LastName;
            await _context.SaveChangesAsync();
        }

        // Create new conversation
        var conversation = new Conversation
        {
            ClientId = client.Id,
            Status = ConversationStatus.Active
        };
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        // Generate initial greeting
        var greetingName = !string.IsNullOrEmpty(client.FirstName) ? client.FirstName : "there";
        var initialMessage = $"Hello {greetingName}! Thank you for your interest in our services. " +
            "I'm here to get to know you better through a friendly conversation. " +
            "This will help us understand if we're a good fit for each other. " +
            "Let's start with the basics - can you tell me a bit about yourself and what brings you here today?";

        // Save assistant's initial message
        var assistantMessage = new Message
        {
            ConversationId = conversation.Id,
            Role = MessageRole.Assistant,
            Content = initialMessage
        };
        _context.Messages.Add(assistantMessage);
        conversation.TotalMessages++;
        await _context.SaveChangesAsync();

        return new StartConversationResponse(conversation.Id, client.Id, initialMessage);
    }

    public async Task<SendMessageResponse> ProcessMessageAsync(SendMessageRequest request)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Client)
            .Include(c => c.Messages)
            .Include(c => c.AskedQuestions)
            .ThenInclude(aq => aq.Question)
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId);

        if (conversation == null)
        {
            throw new InvalidOperationException("Conversation not found");
        }

        if (conversation.Status != ConversationStatus.Active)
        {
            throw new InvalidOperationException("Conversation is not active");
        }

        // Save user message
        var userMessage = new Message
        {
            ConversationId = conversation.Id,
            Role = MessageRole.User,
            Content = request.Message
        };
        _context.Messages.Add(userMessage);
        conversation.TotalMessages++;
        await _context.SaveChangesAsync();

        // Build conversation history for AI
        var conversationHistory = conversation.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ConversationMessage
            {
                Role = m.Role.ToString().ToLower(),
                Content = m.Content
            }).ToList();

        // Add user's latest message
        conversationHistory.Add(new ConversationMessage
        {
            Role = "user",
            Content = request.Message
        });

        // Determine if we should ask a required question
        var nextQuestion = await GetNextQuestionToAskAsync(conversation);
        
        string systemPrompt = await BuildSystemPromptAsync(conversation, nextQuestion);
        
        // Get AI response
        var aiResponse = await _aiService.GetAIResponseAsync(conversationHistory, systemPrompt);

        // Save assistant message
        var assistantMessage = new Message
        {
            ConversationId = conversation.Id,
            Role = MessageRole.Assistant,
            Content = aiResponse
        };
        _context.Messages.Add(assistantMessage);
        conversation.TotalMessages++;

        // If we suggested a question, mark it as asked
        if (nextQuestion != null)
        {
            var askedQuestion = new AskedQuestion
            {
                ConversationId = conversation.Id,
                QuestionId = nextQuestion.Id,
                Answered = false // Will be marked true later by dossier extraction
            };
            _context.AskedQuestions.Add(askedQuestion);
        }

        // Extract and save dossier information from this exchange
        _ = Task.Run(async () =>
        {
            try
            {
                await _dossierService.ExtractAndSaveDossierInfoAsync(
                    conversation.ClientId, 
                    conversation.Id, 
                    userMessage.Content
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting dossier information");
            }
        });

        // Check if conversation should end
        var minMessages = int.Parse((await _aiService.GetSystemSettingsAsync())
            .GetValueOrDefault("min_messages_threshold", "20"));
        
        bool conversationEnded = false;
        if (conversation.TotalMessages >= minMessages && await AllRequiredQuestionsAskedAsync(conversation))
        {
            // Check if we have enough information - could be more sophisticated
            conversationEnded = conversation.TotalMessages >= minMessages + 5;
            
            if (conversationEnded)
            {
                conversation.Status = ConversationStatus.Completed;
                conversation.EndedAt = DateTime.UtcNow;
                
                // Trigger evaluation asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _dossierService.EvaluateClientAsync(conversation.ClientId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error evaluating client");
                    }
                });
            }
        }

        await _context.SaveChangesAsync();

        return new SendMessageResponse(
            assistantMessage.Id,
            aiResponse,
            conversationEnded,
            conversation.TotalMessages
        );
    }

    private async Task<Question?> GetNextQuestionToAskAsync(Conversation conversation)
    {
        var askedQuestionIds = conversation.AskedQuestions.Select(aq => aq.QuestionId).ToList();
        
        // Get required questions that haven't been asked
        var requiredQuestion = await _context.Questions
            .Where(q => q.IsActive && q.IsRequired && !askedQuestionIds.Contains(q.Id))
            .OrderByDescending(q => q.Priority)
            .FirstOrDefaultAsync();

        if (requiredQuestion != null)
        {
            return requiredQuestion;
        }

        // Get optional questions that haven't been asked
        var optionalQuestion = await _context.Questions
            .Where(q => q.IsActive && !q.IsRequired && !askedQuestionIds.Contains(q.Id))
            .OrderByDescending(q => q.Priority)
            .FirstOrDefaultAsync();

        return optionalQuestion;
    }

    private async Task<string> BuildSystemPromptAsync(Conversation conversation, Question? nextQuestion)
    {
        var settings = await _aiService.GetSystemSettingsAsync();
        var basePrompt = settings.GetValueOrDefault("system_prompt",
            "You are a professional interviewer conducting a thorough vetting conversation.");

        var promptParts = new List<string> { basePrompt };

        promptParts.Add("Your goal is to learn about the person's background, values, goals, and situation in a natural, conversational way.");
        promptParts.Add("Be empathetic, professional, and ask thoughtful follow-up questions based on their responses.");

        if (nextQuestion != null)
        {
            promptParts.Add($"\nIMPORTANT: At an appropriate point in the conversation, naturally work in this question: \"{nextQuestion.QuestionText}\"");
        }

        promptParts.Add("\nKeep responses concise (2-4 sentences) and conversational.");

        return string.Join(" ", promptParts);
    }

    private async Task<bool> AllRequiredQuestionsAskedAsync(Conversation conversation)
    {
        var requiredQuestionIds = await _context.Questions
            .Where(q => q.IsActive && q.IsRequired)
            .Select(q => q.Id)
            .ToListAsync();

        var askedQuestionIds = conversation.AskedQuestions.Select(aq => aq.QuestionId).ToList();

        return requiredQuestionIds.All(id => askedQuestionIds.Contains(id));
    }

    public async Task<List<ConversationSummary>> GetAllConversationsAsync()
    {
        return await _context.Conversations
            .Include(c => c.Client)
            .OrderByDescending(c => c.StartedAt)
            .Select(c => new ConversationSummary(
                c.Id,
                c.ClientId,
                c.Client.Email,
                c.StartedAt,
                c.EndedAt,
                c.Status.ToString(),
                c.TotalMessages
            ))
            .ToListAsync();
    }

    public async Task<List<MessageDto>> GetConversationMessagesAsync(int conversationId)
    {
        return await _context.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageDto(
                m.Id,
                m.Role.ToString(),
                m.Content,
                m.CreatedAt
            ))
            .ToListAsync();
    }
}

