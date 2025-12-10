using Bio_ISAC_Group13_GroupProject3.Data;
using Bio_ISAC_Group13_GroupProject3.Services;
using Bio_ISAC_Group13_GroupProject3.DTOs;
using Bio_ISAC_Group13_GroupProject3.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure logging to reduce noise from expected SQL errors
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

// Add services
builder.Services.AddDbContext<VettingDbContext>(options =>
{
    var connectionString = "Server=lmag6s0zwmcswp5w.cbetxkdyhwsb.us-east-1.rds.amazonaws.com;Database=hnp9v03267rgl2r9;User=awqbqufonvl8dolk;Password=thq78e8xx5089je2;";
    // Use MySQL 8.0 server version instead of auto-detecting (faster and more reliable)
    var serverVersion = ServerVersion.Parse("8.0.33-mysql");
    options.UseMySql(connectionString, serverVersion, mysqlOptions =>
    {
        mysqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });
});

builder.Services.AddHttpClient();
builder.Services.AddScoped<AIService>();
builder.Services.AddScoped<ConversationService>();
builder.Services.AddScoped<DossierService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add session management
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure JSON serialization to use camelCase
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VettingDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Test database connection first
        if (!db.Database.CanConnect())
        {
            logger.LogError("Cannot connect to database. Please check your connection string.");
            throw new InvalidOperationException("Database connection failed");
        }
        
    db.Database.EnsureCreated();
        logger.LogInformation("Database connection successful");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to connect to database or create tables");
        // Continue anyway - might be a temporary issue
    }
    
    // Run migration to add new columns if they don't exist
    try
    {
        // Check if conversations table exists before trying to alter it
        var conversationsTableExists = false;
        try
        {
            db.Database.ExecuteSqlRaw("SELECT 1 FROM conversations LIMIT 1");
            conversationsTableExists = true;
        }
        catch
        {
            conversationsTableExists = false;
        }
        
        if (conversationsTableExists)
    {
        // Try to add columns - if they exist, MySQL will throw an error which we'll catch
        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE conversations ADD COLUMN current_question_id INT NULL");
            logger.LogInformation("Added current_question_id column to conversations table");
        }
            catch (Exception ex) when (ex.Message.Contains("Duplicate column") || ex.Message.Contains("already exists") || ex.Message.Contains("Duplicate column name"))
        {
            logger.LogInformation("current_question_id column already exists");
        }
        catch (Exception ex)
        {
                logger.LogWarning(ex, "Could not add current_question_id column: {Message}", ex.Message);
        }
        
        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE conversations ADD COLUMN waiting_for_additional_info BOOLEAN DEFAULT false");
            logger.LogInformation("Added waiting_for_additional_info column to conversations table");
        }
            catch (Exception ex) when (ex.Message.Contains("Duplicate column") || ex.Message.Contains("already exists") || ex.Message.Contains("Duplicate column name"))
        {
            logger.LogInformation("waiting_for_additional_info column already exists");
        }
        catch (Exception ex)
        {
                logger.LogWarning(ex, "Could not add waiting_for_additional_info column: {Message}", ex.Message);
        }
        
        // Try to add review_status column to conversations table
        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE conversations ADD COLUMN review_status VARCHAR(255) NULL");
            logger.LogInformation("Added review_status column to conversations table");
        }
        catch (Exception ex) when (ex.Message.Contains("Duplicate column") || ex.Message.Contains("already exists") || ex.Message.Contains("Duplicate column name"))
        {
            logger.LogInformation("review_status column already exists");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not add review_status column: {Message}", ex.Message);
        }
        }
        
        // Check if clients table exists before trying to alter it
        var clientsTableExists = false;
        try
        {
            db.Database.ExecuteSqlRaw("SELECT 1 FROM clients LIMIT 1");
            clientsTableExists = true;
        }
        catch
        {
            clientsTableExists = false;
        }
        
        if (clientsTableExists)
        {
        // Try to add username column to clients table
        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE clients ADD COLUMN username VARCHAR(100) NULL");
            logger.LogInformation("Added username column to clients table");
        }
        catch (Exception ex) when (ex.Message.Contains("Duplicate column") || 
                                   ex.Message.Contains("already exists") ||
                                   ex.Message.Contains("Duplicate column name"))
        {
            logger.LogInformation("username column already exists");
        }
        catch (Exception ex)
        {
                logger.LogWarning(ex, "Could not add username column: {Message}", ex.Message);
            }
            
            // Try to add password_hash column to clients table
            try
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE clients ADD COLUMN password_hash VARCHAR(255) NULL");
                logger.LogInformation("Added password_hash column to clients table");
            }
            catch (Exception ex) when (ex.Message.Contains("Duplicate column") || 
                                       ex.Message.Contains("already exists") ||
                                       ex.Message.Contains("Duplicate column name"))
            {
                logger.LogInformation("password_hash column already exists");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not add password_hash column: {Message}", ex.Message);
            }
        }
        
        // Create question_answers table if it doesn't exist
        try
        {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS question_answers (
                id INT AUTO_INCREMENT PRIMARY KEY,
                conversation_id INT NOT NULL,
                question_id INT NOT NULL,
                answer TEXT NOT NULL,
                additional_info TEXT NULL,
                answered_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (conversation_id) REFERENCES conversations(id) ON DELETE CASCADE,
                FOREIGN KEY (question_id) REFERENCES questions(id) ON DELETE CASCADE,
                INDEX idx_conversation (conversation_id),
                INDEX idx_question (question_id)
            )
        ");
            logger.LogInformation("question_answers table ensured");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not create question_answers table: {Message}", ex.Message);
        }
        
        // Try to add foreign key constraint (ignore if it exists)
        if (conversationsTableExists)
        {
            try
            {
                // Check if constraint already exists to avoid the error
                var constraintCheck = db.Database.SqlQueryRaw<int>(@"
                    SELECT COUNT(*) FROM information_schema.table_constraints 
                    WHERE constraint_schema = DATABASE() 
                    AND constraint_name = 'fk_conversation_current_question'
                ").FirstOrDefault();
                
                if (constraintCheck == 0)
        {
            db.Database.ExecuteSqlRaw(@"
                ALTER TABLE conversations 
                ADD CONSTRAINT fk_conversation_current_question 
                FOREIGN KEY (current_question_id) REFERENCES questions(id) ON DELETE SET NULL
            ");
            logger.LogInformation("Added foreign key constraint for current_question_id");
                }
                else
                {
                    logger.LogInformation("Foreign key constraint already exists");
                }
        }
        catch (Exception ex) when (ex.Message.Contains("Duplicate key") || 
                                    ex.Message.Contains("already exists") || 
                                    ex.Message.Contains("Duplicate foreign key constraint") ||
                                        ex.Message.Contains("Duplicate foreign key") ||
                                        ex.Message.Contains("Duplicate constraint"))
        {
            logger.LogInformation("Foreign key constraint already exists");
        }
        catch (Exception ex)
        {
            // Check if it's actually a duplicate error that we didn't catch
            if (ex.Message.Contains("Duplicate") || ex.Message.Contains("already exists"))
            {
                logger.LogInformation("Foreign key constraint already exists");
            }
            else
            {
                    logger.LogWarning(ex, "Could not add foreign key constraint: {Message}", ex.Message);
            }
            }
        }
        
        // Create documents table if it doesn't exist
        try
        {
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS documents (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    client_id INT NOT NULL,
                    document_type VARCHAR(100) NOT NULL,
                    file_name VARCHAR(255) NOT NULL,
                    file_path VARCHAR(500) NOT NULL,
                    file_size BIGINT NOT NULL,
                    content_type VARCHAR(100) NOT NULL,
                    uploaded_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (client_id) REFERENCES clients(id) ON DELETE CASCADE,
                    INDEX idx_client (client_id),
                    INDEX idx_document_type (document_type)
                )
            ");
            logger.LogInformation("documents table ensured");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not create documents table: {Message}", ex.Message);
        }
        
        // Create red_flag_wordlists table if it doesn't exist
        try
        {
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS red_flag_wordlists (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    file_name VARCHAR(255) NOT NULL,
                    file_path VARCHAR(500) NOT NULL,
                    file_size BIGINT NOT NULL,
                    content_type VARCHAR(100) NOT NULL,
                    keywords TEXT NOT NULL,
                    uploaded_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    is_active BOOLEAN DEFAULT true,
                    INDEX idx_is_active (is_active)
                )
            ");
            logger.LogInformation("red_flag_wordlists table ensured");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not create red_flag_wordlists table: {Message}", ex.Message);
        }
        
        logger.LogInformation("Database migration completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during database migration: {Message}", ex.Message);
    }
    
    // Seed database
    try
    {
    DbSeeder.Seed(db);
        logger.LogInformation("Database seeding completed");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Error during database seeding: {Message}", ex.Message);
    }
}

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseSession();

// Create uploads directory if it doesn't exist
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// Create wordlists directory if it doesn't exist
var wordlistsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "wordlists");
if (!Directory.Exists(wordlistsPath))
{
    Directory.CreateDirectory(wordlistsPath);
}

// Configure static files with default file
var staticFileOptions = new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
    }
};
app.UseStaticFiles(staticFileOptions);

// Add default route to serve index.html
app.MapGet("/", () => Results.Redirect("/index.html"));

// Helper function to get current user from session
static int? GetCurrentUserId(HttpContext context)
{
    var clientId = context.Session.GetInt32("ClientId");
    return clientId;
}

// ===== AUTHENTICATION ENDPOINTS =====

app.MapPost("/api/auth/register", async (RegisterRequest request, AuthService authService, HttpContext context, ILogger<Program> logger) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { message = "Email and password are required" });
        }

        var client = await authService.RegisterAsync(
            request.Email,
            request.Username,
            request.Password,
            request.FirstName,
            request.LastName
        );

        if (client == null)
        {
            return Results.BadRequest(new { message = "Email already registered" });
        }

        // Set session
        context.Session.SetInt32("ClientId", client.Id);
        context.Session.SetString("Email", client.Email);
        await context.Session.CommitAsync();

        return Results.Ok(new AuthResponse(
            client.Id,
            client.Email,
            client.Username,
            client.FirstName,
            client.LastName
        ));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in register endpoint");
        return Results.Problem(ex.Message);
    }
})
.WithName("Register");

app.MapPost("/api/auth/login", async (LoginRequest request, AuthService authService, HttpContext context, ILogger<Program> logger) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { message = "Email and password are required" });
        }

        var client = await authService.LoginAsync(request.Email, request.Password);

        if (client == null)
        {
            return Results.Unauthorized();
        }

        // Set session
        context.Session.SetInt32("ClientId", client.Id);
        context.Session.SetString("Email", client.Email);
        await context.Session.CommitAsync();

        return Results.Ok(new AuthResponse(
            client.Id,
            client.Email,
            client.Username,
            client.FirstName,
            client.LastName
        ));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in login endpoint");
        return Results.Problem(ex.Message);
    }
})
.WithName("Login");

app.MapPost("/api/auth/logout", (HttpContext context) =>
{
    context.Session.Clear();
    return Results.Ok(new { message = "Logged out successfully" });
})
.WithName("Logout");

app.MapGet("/api/auth/me", async (HttpContext context, AuthService authService) =>
{
    var clientId = GetCurrentUserId(context);
    if (clientId == null)
    {
        return Results.Unauthorized();
    }

    var client = await authService.GetClientByIdAsync(clientId.Value);
    if (client == null)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new AuthResponse(
        client.Id,
        client.Email,
        client.Username,
        client.FirstName,
        client.LastName
    ));
})
.WithName("GetCurrentUser");

// ===== CONVERSATION ENDPOINTS =====

app.MapPost("/api/conversations/start", async (StartConversationRequest request, ConversationService service, HttpContext context, VettingDbContext db, ILogger<Program> logger) =>
{
    var clientId = GetCurrentUserId(context);
    if (clientId == null)
    {
        return Results.Unauthorized();
    }

    // Check if skipUploads is requested (for school project demo)
    // Read from query string (frontend will append ?skipUploads=true to the URL)
    var skipUploads = context.Request.Query.ContainsKey("skipUploads") && 
                      context.Request.Query["skipUploads"].ToString().ToLower() == "true";

    // Only check documents if not skipping
    if (!skipUploads)
    {
        var requiredTypes = new[] { "ID", "Passport", "ProofOfResidency", "VerificationPhoto", "VerificationVideo" };
        var uploadedTypes = await db.Documents
            .Where(d => d.ClientId == clientId.Value && requiredTypes.Contains(d.DocumentType))
            .Select(d => d.DocumentType)
            .Distinct()
            .ToListAsync();

        var missingTypes = requiredTypes.Where(t => !uploadedTypes.Contains(t)).ToList();
        if (missingTypes.Any())
        {
            return Results.BadRequest(new
            {
                message = "Please upload all required documents and complete verification steps before starting the interview",
                missingDocuments = missingTypes,
                canSkip = true
            });
        }
    }

    try
    {
        // Get client from database to ensure it exists
        var client = await db.Clients.FindAsync(clientId.Value);
        if (client == null)
        {
            return Results.Problem("Client not found", statusCode: 401);
        }
        
        var response = await service.StartConversationAsync(clientId.Value, request.FirstName, request.LastName);
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        // Log the full exception including inner exception
        var errorMessage = ex.Message;
        if (ex.InnerException != null)
        {
            errorMessage += $" Inner: {ex.InnerException.Message}";
            logger.LogError(ex.InnerException, "Inner exception in StartConversation endpoint");
        }
        logger.LogError(ex, "Error in StartConversation endpoint: {ErrorMessage}", errorMessage);
        return Results.Problem(
            detail: errorMessage,
            statusCode: 500,
            title: "Failed to start conversation"
        );
    }
})
.WithName("StartConversation");

app.MapPost("/api/conversations/message", async (SendMessageRequest request, ConversationService service, HttpContext context) =>
{
    var clientId = GetCurrentUserId(context);
    if (clientId == null)
    {
        return Results.Unauthorized();
    }

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

app.MapGet("/api/conversations/current", async (HttpContext context, ConversationService service, ILogger<Program> logger) =>
{
    var clientId = GetCurrentUserId(context);
    if (clientId == null)
    {
        logger.LogWarning("GetCurrentUserConversation: No clientId in session");
        return Results.Unauthorized();
    }

    logger.LogInformation("GetCurrentUserConversation: ClientId = {ClientId}", clientId.Value);

    try
    {
        var conversation = await service.GetCurrentUserConversationAsync(clientId.Value);
        if (conversation == null)
        {
            logger.LogInformation("GetCurrentUserConversation: No conversation found for ClientId {ClientId}", clientId.Value);
            return Results.NotFound(new { message = "No conversation found" });
        }
        logger.LogInformation("GetCurrentUserConversation: Found conversation {ConversationId} for ClientId {ClientId}", conversation.ConversationId, clientId.Value);
        return Results.Ok(conversation);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "GetCurrentUserConversation: Error for ClientId {ClientId}", clientId.Value);
        return Results.Problem(ex.Message);
    }
})
.WithName("GetCurrentUserConversation")
;

// ===== DOCUMENT ENDPOINTS =====

app.MapPost("/api/documents/upload", async (HttpContext context, VettingDbContext db, ILogger<Program> logger) =>
{
    var clientId = GetCurrentUserId(context);
    if (clientId == null)
    {
        return Results.Unauthorized();
    }

    try
    {
        if (!context.Request.HasFormContentType)
        {
            return Results.BadRequest(new { message = "Request must be multipart/form-data" });
        }

        var form = await context.Request.ReadFormAsync();
        var file = form.Files["file"];
        var documentType = form["documentType"].ToString();

        if (file == null || file.Length == 0)
        {
            return Results.BadRequest(new { message = "No file uploaded" });
        }

        if (string.IsNullOrWhiteSpace(documentType))
        {
            return Results.BadRequest(new { message = "Document type is required" });
        }

        // Validate file type (images, PDFs, and videos)
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".webm", ".mp4", ".mov" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            return Results.BadRequest(new { message = "Only JPG, PNG, PDF, and video files are allowed" });
        }

        // Validate file size (max 10MB)
        const long maxFileSize = 10 * 1024 * 1024; // 10MB
        if (file.Length > maxFileSize)
        {
            return Results.BadRequest(new { message = "File size must be less than 10MB" });
        }

        // Create uploads directory for this client
        var clientUploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", clientId.Value.ToString());
        if (!Directory.Exists(clientUploadsPath))
        {
            Directory.CreateDirectory(clientUploadsPath);
        }

        // Generate unique filename
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(clientUploadsPath, uniqueFileName);
        var relativePath = $"/uploads/{clientId.Value}/{uniqueFileName}";

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Save document record
        var document = new Document
        {
            ClientId = clientId.Value,
            DocumentType = documentType,
            FileName = file.FileName,
            FilePath = relativePath,
            FileSize = file.Length,
            ContentType = file.ContentType,
            UploadedAt = DateTime.UtcNow
        };

        db.Documents.Add(document);
        await db.SaveChangesAsync();

        logger.LogInformation("Document uploaded: {DocumentId} for ClientId {ClientId}", document.Id, clientId.Value);

        return Results.Ok(new DocumentUploadResponse(document.Id, "Document uploaded successfully"));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error uploading document");
        return Results.Problem(ex.Message);
    }
})
.WithName("UploadDocument");

app.MapGet("/api/documents", async (HttpContext context, VettingDbContext db) =>
{
    var clientId = GetCurrentUserId(context);
    if (clientId == null)
    {
        return Results.Unauthorized();
    }

    try
    {
        var documents = await db.Documents
            .Where(d => d.ClientId == clientId.Value)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new DocumentDto(
                d.Id,
                d.DocumentType,
                d.FileName,
                d.FileSize,
                d.ContentType,
                d.UploadedAt
            ))
            .ToListAsync();

        return Results.Ok(documents);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("GetDocuments");

app.MapDelete("/api/documents/{id}", async (int id, HttpContext context, VettingDbContext db, ILogger<Program> logger) =>
{
    var clientId = GetCurrentUserId(context);
    if (clientId == null)
    {
        return Results.Unauthorized();
    }

    try
    {
        var document = await db.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.ClientId == clientId.Value);

        if (document == null)
        {
            return Results.NotFound();
        }

        // Delete physical file
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", document.FilePath.TrimStart('/'));
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        // Delete database record
        db.Documents.Remove(document);
        await db.SaveChangesAsync();

        logger.LogInformation("Document deleted: {DocumentId}", id);

        return Results.Ok(new { message = "Document deleted successfully" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error deleting document");
        return Results.Problem(ex.Message);
    }
})
.WithName("DeleteDocument");

app.MapGet("/api/documents/check", async (HttpContext context, VettingDbContext db) =>
{
    var clientId = GetCurrentUserId(context);
    if (clientId == null)
    {
        return Results.Unauthorized();
    }

    try
    {
        var requiredTypes = new[] { "ID", "Passport", "ProofOfResidency", "VerificationPhoto", "VerificationVideo" };
        var uploadedTypes = await db.Documents
            .Where(d => d.ClientId == clientId.Value && requiredTypes.Contains(d.DocumentType))
            .Select(d => d.DocumentType)
            .Distinct()
            .ToListAsync();

        var missingTypes = requiredTypes.Where(t => !uploadedTypes.Contains(t)).ToList();
        var allUploaded = !missingTypes.Any();

        return Results.Ok(new
        {
            allUploaded,
            uploadedTypes,
            missingTypes,
            message = allUploaded ? "All required documents uploaded" : $"Missing: {string.Join(", ", missingTypes)}"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("CheckDocuments");

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

app.MapGet("/api/clients/by-email", async (string email, DossierService service) =>
{
    try
    {
        var client = await service.GetClientByEmailAsync(email);
        if (client == null)
        {
            return Results.NotFound(new { message = "Client not found" });
        }
        return Results.Ok(client);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("GetClientByEmail")
;

app.MapPost("/api/clients", async (CreateClientRequest request, DossierService service) =>
{
    try
    {
        var client = await service.CreateOrUpdateClientAsync(request);
        return Results.Ok(client);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("CreateClient")
;

app.MapPut("/api/clients/{id}", async (int id, UpdateClientRequest request, DossierService service) =>
{
    try
    {
        var client = await service.UpdateClientAsync(id, request);
        return Results.Ok(client);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("UpdateClient")
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

app.MapDelete("/api/clients/{id}", async (int id, DossierService service) =>
{
    try
    {
        await service.DeleteClientAsync(id);
        return Results.Ok(new { message = "Client deleted successfully" });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("DeleteClient")
;

app.MapPut("/api/clients/{id}/status", async (int id, UpdateClientStatusRequest request, DossierService service) =>
{
    try
    {
        await service.UpdateClientStatusAsync(id, request.Status);
        return Results.Ok(new { message = "Client status updated successfully" });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("UpdateClientStatus")
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

// ===== ADMIN - RED FLAG WORDLISTS =====

app.MapPost("/api/admin/redflags/wordlist/upload", async (HttpContext context, VettingDbContext db, ILogger<Program> logger) =>
{
    try
    {
        if (!context.Request.HasFormContentType)
        {
            return Results.BadRequest(new { message = "Request must be multipart/form-data" });
        }

        var form = await context.Request.ReadFormAsync();
        var file = form.Files["file"];
        var name = form["name"].ToString();

        if (file == null || file.Length == 0)
        {
            return Results.BadRequest(new { message = "No file uploaded" });
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Results.BadRequest(new { message = "Name is required" });
        }

        // Validate file type (text files, CSV, etc.)
        var allowedExtensions = new[] { ".txt", ".csv", ".json" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            return Results.BadRequest(new { message = "Only .txt, .csv, and .json files are allowed" });
        }

        // Validate file size (max 5MB)
        const long maxFileSize = 5 * 1024 * 1024; // 5MB
        if (file.Length > maxFileSize)
        {
            return Results.BadRequest(new { message = "File size must be less than 5MB" });
        }

        // Create wordlists directory
        var wordlistsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "wordlists");
        if (!Directory.Exists(wordlistsPath))
        {
            Directory.CreateDirectory(wordlistsPath);
        }

        // Generate unique filename
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(wordlistsPath, uniqueFileName);
        var relativePath = $"/wordlists/{uniqueFileName}";

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Read and extract keywords from file
        var keywords = new List<string>();
        
        if (fileExtension == ".json")
        {
            // Handle JSON files - read all at once
            try
            {
                var jsonContent = await File.ReadAllTextAsync(filePath);
                var jsonKeywords = System.Text.Json.JsonSerializer.Deserialize<string[]>(jsonContent);
                if (jsonKeywords != null)
                {
                    keywords.AddRange(jsonKeywords);
                }
            }
            catch
            {
                // If JSON parsing fails, try reading line by line
                using (var reader = new StreamReader(filePath))
                {
                    string? line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            keywords.Add(line.Trim());
                        }
                    }
                }
            }
        }
        else
        {
            // Handle text and CSV files - read line by line
            using (var reader = new StreamReader(filePath))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (fileExtension == ".csv")
                    {
                        // CSV - split by comma
                        var csvKeywords = line.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        keywords.AddRange(csvKeywords);
                    }
                    else
                    {
                        // Plain text - one keyword per line
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            keywords.Add(line.Trim());
                        }
                    }
                }
            }
        }

        var keywordsString = string.Join(", ", keywords.Distinct(StringComparer.OrdinalIgnoreCase));

        // Save wordlist record
        var wordlist = new RedFlagWordlist
        {
            Name = name,
            FileName = file.FileName,
            FilePath = relativePath,
            FileSize = file.Length,
            ContentType = file.ContentType,
            Keywords = keywordsString,
            UploadedAt = DateTime.UtcNow,
            IsActive = true
        };

        db.RedFlagWordlists.Add(wordlist);
        await db.SaveChangesAsync();

        logger.LogInformation("Wordlist uploaded: {WordlistId} - {Name} with {KeywordCount} keywords", wordlist.Id, name, keywords.Count);

        return Results.Ok(new
        {
            id = wordlist.Id,
            name = wordlist.Name,
            fileName = wordlist.FileName,
            keywordCount = keywords.Count,
            keywords = keywordsString
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error uploading wordlist");
        return Results.Problem(ex.Message);
    }
})
.WithName("UploadRedFlagWordlist")
;

app.MapGet("/api/admin/redflags/wordlists", async (VettingDbContext db) =>
{
    try
    {
        var wordlists = await db.RedFlagWordlists
            .OrderByDescending(w => w.UploadedAt)
            .Select(w => new
            {
                id = w.Id,
                name = w.Name,
                fileName = w.FileName,
                fileSize = w.FileSize,
                keywordCount = w.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries).Length,
                uploadedAt = w.UploadedAt,
                isActive = w.IsActive
            })
            .ToListAsync();

        return Results.Ok(wordlists);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("GetRedFlagWordlists")
;

app.MapPost("/api/admin/redflags/wordlist/{wordlistId}/scan/{clientId}", async (int wordlistId, int clientId, VettingDbContext db, ILogger<Program> logger) =>
{
    try
    {
        var wordlist = await db.RedFlagWordlists.FindAsync(wordlistId);
        if (wordlist == null || !wordlist.IsActive)
        {
            return Results.NotFound(new { message = "Wordlist not found or inactive" });
        }

        var client = await db.Clients.FindAsync(clientId);
        if (client == null)
        {
            return Results.NotFound(new { message = "Client not found" });
        }

        // Get all interview answers for this client
        var conversations = await db.Conversations
            .Where(c => c.ClientId == clientId)
            .ToListAsync();

        var conversationIds = conversations.Select(c => c.Id).ToList();

        var answers = await db.QuestionAnswers
            .Include(qa => qa.Question)
            .Where(qa => conversationIds.Contains(qa.ConversationId))
            .ToListAsync();

        // Extract keywords from wordlist (comma-separated)
        var keywords = wordlist.Keywords
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(k => k.Trim().ToLowerInvariant())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct()
            .ToList();

        // Scan answers for keyword matches
        var matches = new List<object>();
        var allAnswerText = string.Join(" ", answers.Select(a => $"{a.Answer} {a.AdditionalInfo ?? ""}").Where(t => !string.IsNullOrWhiteSpace(t))).ToLowerInvariant();

        foreach (var keyword in keywords)
        {
            if (allAnswerText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                // Find which answers contain this keyword
                var matchingAnswers = answers
                    .Where(a => 
                        (a.Answer?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (a.AdditionalInfo?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    )
                    .Select(a => new
                    {
                        questionId = a.QuestionId,
                        questionText = a.Question.QuestionText,
                        answer = a.Answer,
                        additionalInfo = a.AdditionalInfo
                    })
                    .ToList();

                matches.Add(new
                {
                    keyword = keyword,
                    matchCount = matchingAnswers.Count,
                    answers = matchingAnswers
                });
            }
        }

        logger.LogInformation("Scanned wordlist {WordlistId} against client {ClientId}: {MatchCount} keywords found", wordlistId, clientId, matches.Count);

        return Results.Ok(new
        {
            wordlistId = wordlistId,
            wordlistName = wordlist.Name,
            clientId = clientId,
            clientEmail = client.Email,
            totalKeywords = keywords.Count,
            matchesFound = matches.Count,
            matches = matches
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error scanning wordlist");
        return Results.Problem(ex.Message);
    }
})
.WithName("ScanRedFlagWordlist")
;

app.MapDelete("/api/admin/redflags/wordlist/{id}", async (int id, VettingDbContext db, ILogger<Program> logger) =>
{
    try
    {
        var wordlist = await db.RedFlagWordlists.FindAsync(id);
        if (wordlist == null)
        {
            return Results.NotFound();
        }

        // Delete physical file
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", wordlist.FilePath.TrimStart('/'));
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        // Delete database record
        db.RedFlagWordlists.Remove(wordlist);
        await db.SaveChangesAsync();

        logger.LogInformation("Wordlist deleted: {WordlistId}", id);

        return Results.Ok(new { message = "Wordlist deleted successfully" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error deleting wordlist");
        return Results.Problem(ex.Message);
    }
})
.WithName("DeleteRedFlagWordlist")
;

// ===== ADMIN - CONVERSATION STATUS =====

app.MapPut("/api/admin/conversations/{id}/status", async (int id, UpdateConversationStatusRequest request, VettingDbContext db, ILogger<Program> logger) =>
{
    try
    {
        var conversation = await db.Conversations.FindAsync(id);
        if (conversation == null)
        {
            return Results.NotFound(new { message = "Conversation not found" });
        }

        conversation.ReviewStatus = request.ReviewStatus;
        await db.SaveChangesAsync();

        logger.LogInformation("Updated review status for conversation {ConversationId} to {ReviewStatus}", id, request.ReviewStatus);

        return Results.Ok(new { message = "Conversation status updated successfully" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error updating conversation status");
        return Results.Problem(ex.Message);
    }
})
.WithName("UpdateConversationStatus");

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

