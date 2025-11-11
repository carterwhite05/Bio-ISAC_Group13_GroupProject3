using Bio_ISAC_Group13_GroupProject3.Data;
using Bio_ISAC_Group13_GroupProject3.Models;
using Bio_ISAC_Group13_GroupProject3.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Bio_ISAC_Group13_GroupProject3.Services;

public class AdminService
{
    private readonly VettingDbContext _context;

    public AdminService(VettingDbContext context)
    {
        _context = context;
    }

    // Question Management
    public async Task<List<QuestionDto>> GetAllQuestionsAsync()
    {
        return await _context.Questions
            .OrderByDescending(q => q.Priority)
            .Select(q => new QuestionDto(
                q.Id,
                q.QuestionText,
                q.Category,
                q.Priority,
                q.IsRequired,
                q.IsActive
            ))
            .ToListAsync();
    }

    public async Task<QuestionDto> CreateQuestionAsync(CreateQuestionRequest request)
    {
        var question = new Question
        {
            QuestionText = request.QuestionText,
            Category = request.Category,
            Priority = request.Priority,
            IsRequired = request.IsRequired
        };

        _context.Questions.Add(question);
        await _context.SaveChangesAsync();

        return new QuestionDto(
            question.Id,
            question.QuestionText,
            question.Category,
            question.Priority,
            question.IsRequired,
            question.IsActive
        );
    }

    public async Task<QuestionDto> UpdateQuestionAsync(int id, UpdateQuestionRequest request)
    {
        var question = await _context.Questions.FindAsync(id);
        if (question == null)
        {
            throw new InvalidOperationException("Question not found");
        }

        if (request.QuestionText != null) question.QuestionText = request.QuestionText;
        if (request.Category != null) question.Category = request.Category;
        if (request.Priority.HasValue) question.Priority = request.Priority.Value;
        if (request.IsRequired.HasValue) question.IsRequired = request.IsRequired.Value;
        if (request.IsActive.HasValue) question.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync();

        return new QuestionDto(
            question.Id,
            question.QuestionText,
            question.Category,
            question.Priority,
            question.IsRequired,
            question.IsActive
        );
    }

    public async Task DeleteQuestionAsync(int id)
    {
        var question = await _context.Questions.FindAsync(id);
        if (question != null)
        {
            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
        }
    }

    // Criteria Management
    public async Task<List<CriteriaDto>> GetAllCriteriaAsync()
    {
        return await _context.Criteria
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Name)
            .Select(c => new CriteriaDto(
                c.Id,
                c.Name,
                c.Description,
                c.Category,
                c.Weight,
                c.IsActive,
                c.EvaluationPrompt
            ))
            .ToListAsync();
    }

    public async Task<CriteriaDto> CreateCriteriaAsync(CreateCriteriaRequest request)
    {
        var criteria = new Criteria
        {
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            Weight = request.Weight,
            EvaluationPrompt = request.EvaluationPrompt
        };

        _context.Criteria.Add(criteria);
        await _context.SaveChangesAsync();

        return new CriteriaDto(
            criteria.Id,
            criteria.Name,
            criteria.Description,
            criteria.Category,
            criteria.Weight,
            criteria.IsActive,
            criteria.EvaluationPrompt
        );
    }

    public async Task<CriteriaDto> UpdateCriteriaAsync(int id, UpdateCriteriaRequest request)
    {
        var criteria = await _context.Criteria.FindAsync(id);
        if (criteria == null)
        {
            throw new InvalidOperationException("Criteria not found");
        }

        if (request.Name != null) criteria.Name = request.Name;
        if (request.Description != null) criteria.Description = request.Description;
        if (request.Category != null) criteria.Category = request.Category;
        if (request.Weight.HasValue) criteria.Weight = request.Weight.Value;
        if (request.IsActive.HasValue) criteria.IsActive = request.IsActive.Value;
        if (request.EvaluationPrompt != null) criteria.EvaluationPrompt = request.EvaluationPrompt;
        criteria.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new CriteriaDto(
            criteria.Id,
            criteria.Name,
            criteria.Description,
            criteria.Category,
            criteria.Weight,
            criteria.IsActive,
            criteria.EvaluationPrompt
        );
    }

    public async Task DeleteCriteriaAsync(int id)
    {
        var criteria = await _context.Criteria.FindAsync(id);
        if (criteria != null)
        {
            _context.Criteria.Remove(criteria);
            await _context.SaveChangesAsync();
        }
    }

    // Red Flag Management
    public async Task<List<RedFlagDto>> GetAllRedFlagsAsync()
    {
        return await _context.RedFlags
            .OrderByDescending(rf => rf.Severity)
            .ThenBy(rf => rf.Name)
            .Select(rf => new RedFlagDto(
                rf.Id,
                rf.Name,
                rf.Description,
                rf.Severity.ToString(),
                rf.IsActive,
                rf.DetectionKeywords
            ))
            .ToListAsync();
    }

    public async Task<RedFlagDto> CreateRedFlagAsync(CreateRedFlagRequest request)
    {
        if (!Enum.TryParse<RedFlagSeverity>(request.Severity, true, out var severity))
        {
            throw new InvalidOperationException("Invalid severity value");
        }

        var redFlag = new RedFlag
        {
            Name = request.Name,
            Description = request.Description,
            Severity = severity,
            DetectionKeywords = request.DetectionKeywords
        };

        _context.RedFlags.Add(redFlag);
        await _context.SaveChangesAsync();

        return new RedFlagDto(
            redFlag.Id,
            redFlag.Name,
            redFlag.Description,
            redFlag.Severity.ToString(),
            redFlag.IsActive,
            redFlag.DetectionKeywords
        );
    }

    public async Task<RedFlagDto> UpdateRedFlagAsync(int id, UpdateRedFlagRequest request)
    {
        var redFlag = await _context.RedFlags.FindAsync(id);
        if (redFlag == null)
        {
            throw new InvalidOperationException("Red flag not found");
        }

        if (request.Name != null) redFlag.Name = request.Name;
        if (request.Description != null) redFlag.Description = request.Description;
        if (request.Severity != null && Enum.TryParse<RedFlagSeverity>(request.Severity, true, out var severity))
        {
            redFlag.Severity = severity;
        }
        if (request.IsActive.HasValue) redFlag.IsActive = request.IsActive.Value;
        if (request.DetectionKeywords != null) redFlag.DetectionKeywords = request.DetectionKeywords;
        redFlag.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new RedFlagDto(
            redFlag.Id,
            redFlag.Name,
            redFlag.Description,
            redFlag.Severity.ToString(),
            redFlag.IsActive,
            redFlag.DetectionKeywords
        );
    }

    public async Task DeleteRedFlagAsync(int id)
    {
        var redFlag = await _context.RedFlags.FindAsync(id);
        if (redFlag != null)
        {
            _context.RedFlags.Remove(redFlag);
            await _context.SaveChangesAsync();
        }
    }

    // System Settings
    public async Task<List<SystemSettingDto>> GetAllSettingsAsync()
    {
        return await _context.SystemSettings
            .Select(s => new SystemSettingDto(
                s.SettingKey,
                s.SettingValue,
                s.Description
            ))
            .ToListAsync();
    }

    public async Task UpdateSettingAsync(UpdateSettingRequest request)
    {
        var setting = await _context.SystemSettings.FindAsync(request.SettingKey);
        
        if (setting == null)
        {
            setting = new SystemSetting
            {
                SettingKey = request.SettingKey,
                SettingValue = request.SettingValue
            };
            _context.SystemSettings.Add(setting);
        }
        else
        {
            setting.SettingValue = request.SettingValue;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}

