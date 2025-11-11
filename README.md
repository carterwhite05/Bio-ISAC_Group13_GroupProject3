# AI Vetting System

An intelligent conversation-based client vetting system that uses AI to conduct thorough interviews, build comprehensive dossiers, and evaluate potential clients based on customizable criteria.

## Features

- **AI-Powered Conversations**: Natural, adaptive conversations that learn and respond to client inputs
- **Automated Dossier Building**: Extracts and organizes information across multiple categories (personal, business, family, education, etc.)
- **Customizable Criteria**: Define and adjust evaluation criteria with custom weights
- **Red Flag Detection**: Automatic detection of concerning patterns or keywords
- **Comprehensive Scoring**: AI-based evaluation with weighted criteria scoring
- **Admin Panel**: Full control over questions, criteria, red flags, and system settings
- **Real-time Chat Interface**: Clean, modern Bootstrap-based UI
- **Client Management**: View and manage all client dossiers and conversation history

## Technology Stack

### Backend
- **Framework**: ASP.NET Core 8.0 (Minimal APIs)
- **Database**: MySQL 8.0+
- **ORM**: Entity Framework Core with Pomelo MySQL provider
- **AI Integration**: OpenAI GPT-4 / Anthropic Claude (configurable)

### Frontend
- **HTML5, CSS3, Vanilla JavaScript**
- **Bootstrap 5.3.3** (via CDN)
- **Bootstrap Icons** (via CDN)
- **No build tools required** - runs directly in browser

## Project Structure

```
Bio-ISAC_Group13_GroupProject3/
├── Data/
│   └── VettingDbContext.cs          # EF Core database context
├── Models/                           # Domain models
│   ├── Client.cs
│   ├── Conversation.cs
│   ├── Message.cs
│   ├── DossierEntry.cs
│   ├── Question.cs
│   ├── Criteria.cs
│   ├── RedFlag.cs
│   └── SystemSetting.cs
├── DTOs/                             # Data transfer objects
│   ├── ConversationDtos.cs
│   ├── DossierDtos.cs
│   └── AdminDtos.cs
├── Services/                         # Business logic
│   ├── AIService.cs                  # AI provider integration
│   ├── ConversationService.cs        # Conversation management
│   ├── DossierService.cs             # Dossier building & evaluation
│   └── AdminService.cs               # Admin operations
├── wwwroot/                          # Static frontend files
│   ├── index.html                    # Chat interface
│   ├── clients.html                  # Client/dossier viewer
│   ├── admin.html                    # Admin panel
│   ├── scripts/
│   │   ├── chat.js
│   │   ├── clients.js
│   │   └── admin.js
│   └── styles/
│       ├── chat.css
│       └── main.css
├── Program.cs                        # App entry point & API endpoints
├── database_schema.sql               # MySQL schema & seed data
└── appsettings.json                  # Configuration
```

## Setup Instructions

### Prerequisites

- .NET 8.0 SDK
- MySQL 8.0+ database (configured in cursor rules)
- OpenAI API key OR Anthropic API key

### 1. Database Setup

Run the SQL schema to create tables and seed initial data:

```bash
mysql -h lmag6s0zwmcswp5w.cbetxkdyhwsb.us-east-1.rds.amazonaws.com -u awqbqufonvl8dolk -p hnp9v03267rgl2r9 < database_schema.sql
```

### 2. Environment Variables

Set your AI API key:

```bash
# For OpenAI
export OPENAI_API_KEY="your-openai-api-key"

# OR for Anthropic
export ANTHROPIC_API_KEY="your-anthropic-api-key"
```

### 3. Install Dependencies

```bash
dotnet restore
```

### 4. Run the Application

```bash
dotnet run
```

The application will start on `http://localhost:5000` (or the port specified in launchSettings.json).

### 5. Access the Application

- **Chat Interface**: http://localhost:5000/index.html
- **Client Dossiers**: http://localhost:5000/clients.html
- **Admin Panel**: http://localhost:5000/admin.html
- **API Documentation**: http://localhost:5000/swagger (in development mode)

## Configuration

### System Settings (via Admin Panel)

- **ai_api_provider**: `openai` or `anthropic`
- **ai_model**: Model name (e.g., `gpt-4`, `claude-3-sonnet-20240229`)
- **ai_temperature**: Response randomness (0-1)
- **ai_max_tokens**: Maximum tokens per response
- **system_prompt**: Base prompt for AI conversations
- **min_messages_threshold**: Minimum messages before evaluation
- **auto_evaluate**: Auto-evaluate on conversation completion

## API Endpoints

### Conversations
- `POST /api/conversations/start` - Start a new conversation
- `POST /api/conversations/message` - Send a message
- `GET /api/conversations` - List all conversations
- `GET /api/conversations/{id}/messages` - Get conversation messages

### Clients & Dossiers
- `GET /api/clients` - List all clients
- `GET /api/clients/{id}/dossier` - Get client dossier
- `POST /api/clients/{id}/evaluate` - Re-evaluate client

### Admin - Questions
- `GET /api/admin/questions` - List questions
- `POST /api/admin/questions` - Create question
- `PUT /api/admin/questions/{id}` - Update question
- `DELETE /api/admin/questions/{id}` - Delete question

### Admin - Criteria
- `GET /api/admin/criteria` - List criteria
- `POST /api/admin/criteria` - Create criteria
- `PUT /api/admin/criteria/{id}` - Update criteria
- `DELETE /api/admin/criteria/{id}` - Delete criteria

### Admin - Red Flags
- `GET /api/admin/redflags` - List red flags
- `POST /api/admin/redflags` - Create red flag
- `PUT /api/admin/redflags/{id}` - Update red flag
- `DELETE /api/admin/redflags/{id}` - Delete red flag

### Admin - Settings
- `GET /api/admin/settings` - List settings
- `PUT /api/admin/settings` - Update setting

## How It Works

### 1. Conversation Flow

1. Client enters email and name on welcome screen
2. System creates/finds client record and starts conversation
3. AI conducts natural conversation, working in required questions
4. System extracts dossier information in real-time
5. Red flags are detected automatically
6. After minimum message threshold, conversation can conclude
7. Client receives completion message

### 2. Dossier Building

- AI analyzes conversation and extracts structured information
- Information categorized: personal_life, business_life, family, childhood, education, values, goals, background, financial
- Each entry includes confidence score
- Updates existing entries if new information has higher confidence

### 3. Red Flag Detection

- **Keyword matching**: Simple detection based on configured keywords
- **AI analysis**: Sophisticated pattern detection via AI evaluation
- Severity levels: Low, Medium, High, Critical
- Detections linked to specific messages

### 4. Client Evaluation

- AI evaluates conversation against each active criteria
- Scores (0-100) weighted by criteria importance
- Red flag penalty applied (max 30 points)
- Final score determines status: Approved (70+), Rejected (<50 or 2+ red flags), or Pending

### 5. Admin Controls

- **Questions**: Define required and optional questions, set priorities
- **Criteria**: Create evaluation criteria with custom prompts and weights
- **Red Flags**: Configure detection patterns and severity
- **Settings**: Adjust AI behavior, thresholds, and system parameters

## Database Schema

### Key Tables

- **clients**: Client records with status and scores
- **conversations**: Conversation sessions
- **messages**: Individual messages (user/assistant/system)
- **dossier_entries**: Extracted information entries
- **questions**: System questions with priorities
- **asked_questions**: Tracks which questions asked in conversations
- **criteria**: Evaluation criteria
- **red_flags**: Red flag definitions
- **red_flag_detections**: Detected red flags per client
- **system_settings**: Configurable system parameters

## Customization

### Adding New Question Categories

1. Go to Admin Panel > Questions
2. Click "Add Question"
3. Specify category (can be new or existing)
4. Set priority and required status

### Adjusting Evaluation Criteria

1. Admin Panel > Criteria
2. Edit existing or create new criteria
3. Adjust weight (higher = more important)
4. Customize evaluation prompt for AI

### Configuring Red Flags

1. Admin Panel > Red Flags
2. Add/edit red flag definitions
3. Set severity level
4. Define detection keywords (comma-separated)

## AI Provider Configuration

### Switch Between OpenAI and Anthropic

1. Go to Admin Panel > Settings
2. Change `ai_api_provider` to `openai` or `anthropic`
3. Update `ai_model` to appropriate model name
4. Set corresponding API key in environment

### Supported Models

**OpenAI**: `gpt-4`, `gpt-4-turbo`, `gpt-3.5-turbo`  
**Anthropic**: `claude-3-opus-20240229`, `claude-3-sonnet-20240229`, `claude-3-haiku-20240307`

## Security Considerations

- API keys stored in environment variables (never in code)
- All database queries parameterized (SQL injection prevention)
- CORS configured (adjust in production)
- Input validation on all endpoints
- MySQL connection uses SSL (RDS default)

## Development

### Running in Development Mode

```bash
dotnet watch run
```

Changes to C# files auto-reload. Frontend changes require browser refresh.

### Database Migrations

If schema changes needed:

```bash
# Create migration
dotnet ef migrations add MigrationName

# Apply migration
dotnet ef database update
```

## Production Deployment

1. Set `ASPNETCORE_ENVIRONMENT=Production`
2. Configure production connection string
3. Secure API keys in production secrets manager
4. Configure CORS for specific origins
5. Enable HTTPS
6. Consider rate limiting on API endpoints
7. Set up logging and monitoring

## Troubleshooting

### "Failed to start conversation"
- Check database connection
- Verify AI API key is set
- Check network connectivity to AI provider

### "Failed to extract dossier information"
- Check AI API key and quota
- Review system logs for AI response errors
- Verify AI model name is correct

### Frontend not loading
- Ensure `wwwroot` files are included in build
- Check browser console for errors
- Verify API endpoints are accessible

## License

This project is part of Bio-ISAC Group 13 Group Project 3.

## Contributors

MIS321 - Group 13
