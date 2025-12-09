using Bio_ISAC_Group13_GroupProject3.Data;
using Bio_ISAC_Group13_GroupProject3.Models;
using Bio_ISAC_Group13_GroupProject3.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Bio_ISAC_Group13_GroupProject3.Services;

/// <summary>
/// Service for managing structured interview conversations.
/// Flow: User provides email/name -> System asks predetermined questions -> 
/// After each question asks for additional info (yes/no) -> Saves answers by category
/// </summary>
public class ConversationService
{
    private readonly VettingDbContext _context;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(
        VettingDbContext context, 
        ILogger<ConversationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Starts a new interview conversation.
    /// Creates or finds client, creates conversation, and asks the first question.
    /// </summary>
    public async Task<StartConversationResponse> StartConversationAsync(StartConversationRequest request)
    {
        try
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
                _logger.LogInformation("Created new client with email: {Email}", request.Email);
            }
            else
            {
                // Allow multiple interviews - no limit for school project
                // Previously checked for active conversation, but now allowing multiple attempts
                
                // Update client status if starting new conversation
                client.Status = ClientStatus.InProgress;
                client.FirstName = request.FirstName ?? client.FirstName;
                client.LastName = request.LastName ?? client.LastName;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated existing client with email: {Email}", request.Email);
            }

            // Create new conversation
            var conversation = new Conversation
            {
                ClientId = client.Id,
                Status = ConversationStatus.Active
            };
            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created conversation {ConversationId} for client {ClientId}", conversation.Id, client.Id);

            // Get first question (ordered by priority, then by ID)
            var firstQuestion = await _context.Questions
                .Where(q => q.IsActive)
                .OrderByDescending(q => q.Priority)
                .ThenBy(q => q.Id)
                .FirstOrDefaultAsync();

            if (firstQuestion == null)
            {
                _logger.LogError("No active questions found in database");
                throw new InvalidOperationException("No active questions found. Please configure questions in the admin panel.");
            }

            _logger.LogInformation("Found first question: {QuestionId} - {QuestionText}", firstQuestion.Id, firstQuestion.QuestionText);

            conversation.CurrentQuestionId = firstQuestion.Id;
            await _context.SaveChangesAsync();

        // Generate initial greeting
        var greetingName = !string.IsNullOrEmpty(client.FirstName) ? client.FirstName : "there";
        var initialMessage = $"Hello {greetingName}! Thank you for your interest. " +
            "I'll be asking you a series of questions to get to know you better. " +
            $"Let's start:\n\n{firstQuestion.QuestionText}";

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

            return new StartConversationResponse(
                conversation.Id, 
                client.Id, 
                initialMessage,
                firstQuestion.Id,
                firstQuestion.QuestionText
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting conversation for email: {Email}", request.Email);
            throw;
        }
    }

    /// <summary>
    /// Processes a user message in the interview.
    /// Handles: answering questions, providing additional info (yes/no), and moving to next question.
    /// When all questions are answered, saves all answers to dossier entries organized by category.
    /// </summary>
    public async Task<SendMessageResponse> ProcessMessageAsync(SendMessageRequest request)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Client)
            .Include(c => c.QuestionAnswers)
            .ThenInclude(qa => qa.Question)
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

        string assistantMessage = "";
        bool conversationEnded = false;

        // Check if we're waiting for additional info
        if (conversation.WaitingForAdditionalInfo)
        {
            // User is providing additional info or saying no
            var normalizedMessage = request.Message.Trim().ToLower();
            
            if (normalizedMessage == "yes" || normalizedMessage == "y")
            {
                // Ask for additional info
                assistantMessage = "Please provide any additional information you'd like to share:";
                // Keep WaitingForAdditionalInfo = true, will be set to false when they provide the info
                // Don't save this "yes" as a message, just prompt for info
            }
            else if (normalizedMessage == "no" || normalizedMessage == "n")
            {
                // Move to next question
                conversation.WaitingForAdditionalInfo = false;
                var currentAnswer = conversation.QuestionAnswers
                    .Where(qa => qa.QuestionId == conversation.CurrentQuestionId)
                    .OrderByDescending(qa => qa.AnsweredAt)
                    .FirstOrDefault();
                
                if (currentAnswer != null)
                {
                    currentAnswer.AdditionalInfo = null; // Explicitly no additional info
                }

                var nextQuestion = await GetNextQuestionAsync(conversation);
                
                if (nextQuestion == null)
                {
                    // All questions answered - complete conversation
                    conversationEnded = true;
                    conversation.Status = ConversationStatus.Completed;
                    conversation.EndedAt = DateTime.UtcNow;
                    assistantMessage = "Thank you for answering all the questions! Your responses have been saved. We'll review your information and get back to you soon.";
                    
                    // Save all answers to dossier entries
                    await SaveAnswersToDossierAsync(conversation);
                }
                else
                {
                    conversation.CurrentQuestionId = nextQuestion.Id;
                    assistantMessage = nextQuestion.QuestionText;
                }
            }
            else
            {
                // User provided additional info
                var currentAnswer = conversation.QuestionAnswers
                    .Where(qa => qa.QuestionId == conversation.CurrentQuestionId)
                    .OrderByDescending(qa => qa.AnsweredAt)
                    .FirstOrDefault();
                
                if (currentAnswer != null)
                {
                    currentAnswer.AdditionalInfo = request.Message;
                    await _context.SaveChangesAsync();
                }

                // Move to next question
                conversation.WaitingForAdditionalInfo = false;
                var nextQuestion = await GetNextQuestionAsync(conversation);
                
                if (nextQuestion == null)
                {
                    // All questions answered
                    conversationEnded = true;
                    conversation.Status = ConversationStatus.Completed;
                    conversation.EndedAt = DateTime.UtcNow;
                    assistantMessage = "Thank you for answering all the questions! Your responses have been saved. We'll review your information and get back to you soon.";
                    
                    // Update client status to InterviewCompleted
                    var client = await _context.Clients.FindAsync(conversation.ClientId);
                    if (client != null)
                    {
                        client.Status = ClientStatus.InterviewCompleted;
                        client.UpdatedAt = DateTime.UtcNow;
                    }
                    
                    // Save all answers to dossier entries
                    await SaveAnswersToDossierAsync(conversation);
                }
                else
                {
                    conversation.CurrentQuestionId = nextQuestion.Id;
                    assistantMessage = nextQuestion.QuestionText;
                }
            }
        }
        else
        {
            // User is answering the current question
            var currentQuestion = await _context.Questions.FindAsync(conversation.CurrentQuestionId);
            
            if (currentQuestion == null)
            {
                throw new InvalidOperationException("Current question not found");
            }

            // Save the answer
            var questionAnswer = new QuestionAnswer
            {
                ConversationId = conversation.Id,
                QuestionId = currentQuestion.Id,
                Answer = request.Message
            };
            _context.QuestionAnswers.Add(questionAnswer);
            await _context.SaveChangesAsync();

            // Ask if they want to provide additional info
            conversation.WaitingForAdditionalInfo = true;
            assistantMessage = "Would you like to provide any additional information? (yes/no)";
        }

        // Save assistant message
        var assistantMsg = new Message
        {
            ConversationId = conversation.Id,
            Role = MessageRole.Assistant,
            Content = assistantMessage
        };
        _context.Messages.Add(assistantMsg);
        conversation.TotalMessages++;
        await _context.SaveChangesAsync();

        return new SendMessageResponse(
            assistantMsg.Id,
            assistantMessage,
            conversationEnded,
            conversation.TotalMessages,
            conversation.CurrentQuestionId,
            conversation.WaitingForAdditionalInfo
        );
    }

    private async Task<Question?> GetNextQuestionAsync(Conversation conversation)
    {
        var answeredQuestionIds = conversation.QuestionAnswers
            .Select(qa => qa.QuestionId)
            .ToList();

        // Get next question that hasn't been answered, ordered by priority
        var nextQuestion = await _context.Questions
            .Where(q => q.IsActive && !answeredQuestionIds.Contains(q.Id))
            .OrderByDescending(q => q.Priority)
            .ThenBy(q => q.Id)
            .FirstOrDefaultAsync();

        return nextQuestion;
    }

    /// <summary>
    /// Saves all question answers to dossier entries, organized by question category.
    /// Each answer is saved to the appropriate category section (personal_life, business_life, etc.)
    /// </summary>
    private async Task SaveAnswersToDossierAsync(Conversation conversation)
    {
        var answers = await _context.QuestionAnswers
            .Include(qa => qa.Question)
            .Where(qa => qa.ConversationId == conversation.Id)
            .ToListAsync();

        foreach (var answer in answers)
        {
            // Map question category to DossierCategory (saves to different sections)
            var category = MapCategoryToDossierCategory(answer.Question.Category);
            
            // Save main answer to dossier entry in the appropriate category
            var dossierEntry = new DossierEntry
            {
                ClientId = conversation.ClientId,
                Category = category,
                KeyName = $"question_{answer.QuestionId}",
                Value = answer.Answer,
                ConfidenceScore = 1.0m
            };
            _context.DossierEntries.Add(dossierEntry);

            // Save additional info if provided (also in same category)
            if (!string.IsNullOrWhiteSpace(answer.AdditionalInfo))
            {
                var additionalEntry = new DossierEntry
                {
                    ClientId = conversation.ClientId,
                    Category = category,
                    KeyName = $"question_{answer.QuestionId}_additional",
                    Value = answer.AdditionalInfo,
                    ConfidenceScore = 1.0m
                };
                _context.DossierEntries.Add(additionalEntry);
            }
        }

        await _context.SaveChangesAsync();
    }

    private DossierCategory MapCategoryToDossierCategory(string category)
    {
        return category.ToLower() switch
        {
            "personal_life" => DossierCategory.PersonalLife,
            "business_life" => DossierCategory.BusinessLife,
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

    public async Task<ConversationSummary?> GetCurrentUserConversationAsync(int clientId)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Client)
            .Where(c => c.ClientId == clientId)
            .OrderByDescending(c => c.StartedAt)
            .FirstOrDefaultAsync();

        if (conversation == null)
        {
            return null;
        }

        return new ConversationSummary(
            conversation.Id,
            conversation.ClientId,
            conversation.Client.Email,
            conversation.StartedAt,
            conversation.EndedAt,
            conversation.Status.ToString(),
            conversation.TotalMessages
        );
    }
}
