using Bio_ISAC_Group13_GroupProject3.Data;
using Bio_ISAC_Group13_GroupProject3.Services;
using Bio_ISAC_Group13_GroupProject3.DTOs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<VettingDbContext>(options =>
{
    var connectionString = "Server=lmag6s0zwmcswp5w.cbetxkdyhwsb.us-east-1.rds.amazonaws.com;Database=hnp9v03267rgl2r9;User=awqbqufonvl8dolk;Password=thq78e8xx5089je2;";
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddHttpClient();
builder.Services.AddScoped<AIService>();
builder.Services.AddScoped<ConversationService>();
builder.Services.AddScoped<DossierService>();
builder.Services.AddScoped<AdminService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VettingDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseStaticFiles();

// ===== CONVERSATION ENDPOINTS =====

app.MapPost("/api/conversations/start", async (StartConversationRequest request, ConversationService service) =>
{
    try
    {
        var response = await service.StartConversationAsync(request);
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("StartConversation");

app.MapPost("/api/conversations/message", async (SendMessageRequest request, ConversationService service) =>
{
    try
    {
        var response = await service.ProcessMessageAsync(request);
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("SendMessage")
;

app.MapGet("/api/conversations", async (ConversationService service) =>
{
    try
    {
        var conversations = await service.GetAllConversationsAsync();
        return Results.Ok(conversations);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("GetAllConversations")
;

app.MapGet("/api/conversations/{id}/messages", async (int id, ConversationService service) =>
{
    try
    {
        var messages = await service.GetConversationMessagesAsync(id);
        return Results.Ok(messages);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("GetConversationMessages")
;

// ===== DOSSIER ENDPOINTS =====

app.MapGet("/api/clients", async (DossierService service) =>
{
    try
    {
        var clients = await service.GetAllClientsAsync();
        return Results.Ok(clients);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("GetAllClients")
;

app.MapGet("/api/clients/{id}/dossier", async (int id, DossierService service) =>
{
    try
    {
        var dossier = await service.GetClientDossierAsync(id);
        return Results.Ok(dossier);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("GetClientDossier")
;

app.MapPost("/api/clients/{id}/evaluate", async (int id, DossierService service) =>
{
    try
    {
        var score = await service.EvaluateClientAsync(id);
        return Results.Ok(new { score });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("EvaluateClient")
;

// ===== ADMIN - QUESTIONS =====

app.MapGet("/api/admin/questions", async (AdminService service) =>
{
    try
    {
        var questions = await service.GetAllQuestionsAsync();
        return Results.Ok(questions);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("GetAllQuestions")
;

app.MapPost("/api/admin/questions", async (CreateQuestionRequest request, AdminService service) =>
{
    try
    {
        var question = await service.CreateQuestionAsync(request);
        return Results.Created($"/api/admin/questions/{question.Id}", question);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("CreateQuestion")
;

app.MapPut("/api/admin/questions/{id}", async (int id, UpdateQuestionRequest request, AdminService service) =>
{
    try
    {
        var question = await service.UpdateQuestionAsync(id, request);
        return Results.Ok(question);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("UpdateQuestion")
;

app.MapDelete("/api/admin/questions/{id}", async (int id, AdminService service) =>
{
    try
    {
        await service.DeleteQuestionAsync(id);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("DeleteQuestion")
;

// ===== ADMIN - CRITERIA =====

app.MapGet("/api/admin/criteria", async (AdminService service) =>
{
    try
    {
        var criteria = await service.GetAllCriteriaAsync();
        return Results.Ok(criteria);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("GetAllCriteria")
;

app.MapPost("/api/admin/criteria", async (CreateCriteriaRequest request, AdminService service) =>
{
    try
    {
        var criteria = await service.CreateCriteriaAsync(request);
        return Results.Created($"/api/admin/criteria/{criteria.Id}", criteria);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("CreateCriteria")
;

app.MapPut("/api/admin/criteria/{id}", async (int id, UpdateCriteriaRequest request, AdminService service) =>
{
    try
    {
        var criteria = await service.UpdateCriteriaAsync(id, request);
        return Results.Ok(criteria);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("UpdateCriteria")
;

app.MapDelete("/api/admin/criteria/{id}", async (int id, AdminService service) =>
{
    try
    {
        await service.DeleteCriteriaAsync(id);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("DeleteCriteria")
;

// ===== ADMIN - RED FLAGS =====

app.MapGet("/api/admin/redflags", async (AdminService service) =>
{
    try
    {
        var redFlags = await service.GetAllRedFlagsAsync();
        return Results.Ok(redFlags);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("GetAllRedFlags")
;

app.MapPost("/api/admin/redflags", async (CreateRedFlagRequest request, AdminService service) =>
{
    try
    {
        var redFlag = await service.CreateRedFlagAsync(request);
        return Results.Created($"/api/admin/redflags/{redFlag.Id}", redFlag);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("CreateRedFlag")
;

app.MapPut("/api/admin/redflags/{id}", async (int id, UpdateRedFlagRequest request, AdminService service) =>
{
    try
    {
        var redFlag = await service.UpdateRedFlagAsync(id, request);
        return Results.Ok(redFlag);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("UpdateRedFlag")
;

app.MapDelete("/api/admin/redflags/{id}", async (int id, AdminService service) =>
{
    try
    {
        await service.DeleteRedFlagAsync(id);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("DeleteRedFlag")
;

// ===== ADMIN - SETTINGS =====

app.MapGet("/api/admin/settings", async (AdminService service) =>
{
    try
    {
        var settings = await service.GetAllSettingsAsync();
        return Results.Ok(settings);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("GetAllSettings")
;

app.MapPut("/api/admin/settings", async (UpdateSettingRequest request, AdminService service) =>
{
    try
    {
        await service.UpdateSettingAsync(request);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("UpdateSetting")
;

app.Run();

