# FormFeeder API

A .NET 9 Minimal API application for collecting form submissions from static websites and storing them in PostgreSQL with extensible connector support for follow-up actions.

## Features

- Accepts form submissions via `multipart/form-data` and `application/x-www-form-urlencoded`
- Stores form data dynamically in PostgreSQL using JSONB
- Captures client metadata (IP address, user agent, referer)
- CORS support with per-form domain restrictions
- Per-form rate limiting configuration
- Privacy mode for forms (no database persistence)
- Extensible connector system for follow-up actions (email, webhooks, etc.)
- Background task processing for async operations
- Connection resiliency with Polly retry policies
- Swagger UI for API documentation (configurable)

## Prerequisites

- .NET 9 SDK
- PostgreSQL database
- Entity Framework Core tools (for migrations)

## Setup

1. **Install EF Core tools** (if not already installed):
```bash
dotnet tool install --global dotnet-ef
```

2. **Configure PostgreSQL connection** in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Database=formfeeder;Username=postgres;Password=postgres"
  }
}
```

3. **Apply database migrations**:
```bash
cd FormFeeder.Api
dotnet ef database update
```

4. **Run the application**:
```bash
dotnet run
```

The API will be available at `https://localhost:5500` (HTTPS only in development).

## Docker Support

### Using Pre-built Images from GitHub Container Registry

```bash
# Pull the latest image
docker pull ghcr.io/yourusername/formfeeder-oss:latest

# Run the container
docker run -d \
  --name formfeeder \
  -p 8080:8080 \
  -e ConnectionStrings__PostgreSQL="Host=host.docker.internal;Database=formfeeder;Username=postgres;Password=yourpassword" \
  -e MailJet__ApiKey="your-api-key" \
  -e MailJet__ApiSecret="your-api-secret" \
  ghcr.io/yourusername/formfeeder-oss:latest
```

### Building Locally

```bash
# Build the Docker image
docker build -t formfeeder ./FormFeeder.Api

# Run with docker-compose (recommended for development)
docker-compose up -d

# Or run standalone
docker run -d \
  --name formfeeder \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ConnectionStrings__PostgreSQL="Host=host.docker.internal;Database=formfeeder;Username=postgres;Password=yourpassword" \
  formfeeder
```

### Available Tags

- `latest` - Latest stable release
- `v1.0.0`, `v1.1.0`, etc. - Specific version tags
- `main-<sha>` - Development builds from main branch

## API Endpoints

### Submit Form
- **POST** `/api/form/{formId}`
- **Content-Type**: `application/x-www-form-urlencoded` or `multipart/form-data`
- **Path Parameter**: `formId` - Unique identifier for the form/website

### Example HTML Form

```html
<form action="https://localhost:5500/api/form/contact-form" method="POST">
    <input type="text" name="name" required>
    <input type="email" name="email" required>
    <textarea name="message"></textarea>
    <button type="submit">Submit</button>
</form>
```

### Example AJAX Submission

```javascript
const formData = new FormData();
formData.append('name', 'John Doe');
formData.append('email', 'john@example.com');
formData.append('message', 'Hello!');

fetch('https://localhost:5500/api/form/contact-form', {
    method: 'POST',
    body: formData
})
.then(response => response.json())
.then(data => console.log('Success:', data))
.catch(error => console.error('Error:', error));
```

## Testing

Open `test-form.html` in a browser to test the API with sample forms.

## Swagger Documentation

When enabled in configuration, access Swagger UI at:
`https://localhost:5500/swagger`

Enable/disable Swagger in `appsettings.json`:
```json
{
  "EnableSwagger": true
}
```

## Configuration

### Form Configuration
Configure forms with their specific settings in `appsettings.json`:
```json
{
  "Forms": [
    {
      "FormId": "contact-form",
      "AllowedDomains": ["https://example.com", "*"],
      "Description": "Contact form from main website",
      "Enabled": true,
      "PrivacyMode": false,  // Set to true to skip database persistence
      "RateLimit": {
        "RequestsPerWindow": 100,
        "WindowMinutes": 1
      },
      "Connectors": [
        {
          "Type": "MailJet",
          "Name": "EmailNotification",
          "Enabled": true,
          "Settings": {
            "ApiKey": "your-api-key",
            "ApiSecret": "your-api-secret",
            "FromEmail": "no-reply@example.com",
            "ToEmail": "admin@example.com",
            "Subject": "New Form Submission",
            "TemplateId": "optional-template-id"
          }
        },
        {
          "Type": "Slack",
          "Name": "SlackNotification",
          "Enabled": true,
          "Settings": {
            "WebhookUrl": "https://hooks.slack.com/services/YOUR/WEBHOOK/URL",
            "Username": "FormFeeder",
            "IconEmoji": ":incoming_envelope:"
          }
        }
      ]
    }
  ]
}
```

### Rate Limiting
Configure per-form rate limits to prevent abuse:
```json
{
  "RateLimit": {
    "RequestsPerWindow": 100,  // Number of allowed requests
    "WindowMinutes": 1          // Time window in minutes
  }
}
```

### Privacy Mode
Enable privacy mode to process form submissions without storing them in the database:
```json
{
  "FormId": "gdpr-compliant-form",
  "PrivacyMode": true,  // Submissions sent to connectors only, not stored in DB
  "Connectors": [
    {
      "Type": "Slack",
      "Enabled": true,
      // ... connector settings
    }
  ]
}
```

**Important**: Privacy mode requires at least one enabled connector. If no connectors are enabled, privacy mode will be automatically disabled with a warning.

## Connectors

FormFeeder supports extensible connectors that execute after successful form submissions. Connectors run asynchronously in the background to avoid blocking the form submission response.

### Built-in Connectors

#### Slack Webhook Connector
Sends form submissions to Slack channels via incoming webhooks:
- Rich message formatting with attachments
- Customizable username and emoji
- Field-by-field display with smart formatting
- Optional raw JSON data inclusion
- Automatic field name formatting (camelCase/snake_case to Title Case)

Configuration:
```json
{
  "Type": "Slack",
  "Settings": {
    "WebhookUrl": "https://hooks.slack.com/services/T00000000/B00000000/XXXXXXXXXXXXXXXXXXXX",
    "Channel": "#form-submissions",    // Optional: Override default channel
    "Username": "FormFeeder Bot",      // Optional: Bot username (default: "FormFeeder")
    "IconEmoji": ":email:",            // Optional: Bot emoji (default: ":envelope:")
    "IncludeRawJson": "true"           // Optional: Include raw JSON (default: "false")
  }
}
```

To get a webhook URL:
1. Go to your Slack workspace settings
2. Navigate to "Apps" → "Incoming Webhooks"
3. Create a new webhook and select the target channel
4. Copy the webhook URL

#### MailJet Email Connector
Sends email notifications using the MailJet API:
- Supports both template-based and content-based emails
- Template variables include all form fields as a dictionary
- Automatic retry on transient failures

Configuration:
```json
{
  "Type": "MailJet",
  "Settings": {
    "ApiKey": "your-mailjet-api-key",
    "ApiSecret": "your-mailjet-api-secret",
    "FromEmail": "sender@example.com",
    "FromName": "Your App Name",
    "ToEmail": "recipient@example.com",
    "ToName": "Recipient Name",
    "Subject": "New Form Submission",
    "TemplateId": "7251954"  // Optional: Use MailJet template
  }
}
```

##### MailJet Template Variables
When using a MailJet template, the following variables are available:

- `{{var:formId}}` - The form identifier
- `{{var:submittedAt}}` - Submission timestamp
- `{{var:ipAddress}}` - Client IP address
- `{{var:userAgent}}` - Browser user agent
- `{{var:referer}}` - Referring URL
- `{{var:formData}}` - Raw JSON of all form fields
- `{{var:formFields}}` - Dictionary for iteration:
  ```html
  {% for key, value in var:formFields %}
    <tr>
      <td>{{key}}</td>
      <td>{{value}}</td>
    </tr>
  {% endfor %}
  ```

### Creating Custom Connectors

Implement the `IConnector` interface to create custom connectors:

```csharp
public interface IConnector
{
    string Type { get; }
    string Name { get; }
    bool Enabled { get; set; }
    Task<ConnectorResult> ExecuteAsync(
        FormSubmission submission, 
        Dictionary<string, object>? configuration = null
    );
}
```

Example custom connector:
```csharp
public class WebhookConnector : IConnector
{
    public string Type => "Webhook";
    public string Name { get; private set; }
    public bool Enabled { get; set; } = true;

    public async Task<ConnectorResult> ExecuteAsync(
        FormSubmission submission, 
        Dictionary<string, object>? configuration = null)
    {
        // Your webhook implementation
        return ConnectorResult.Successful("Webhook sent");
    }
}
```

Register your connector in `Program.cs`:
```csharp
builder.Services.AddScoped<IConnector, WebhookConnector>();
```

## Project Structure

```
FormFeeder.Api/
├── Connectors/
│   ├── IConnector.cs            # Connector interface
│   ├── MailJetConnector.cs      # MailJet email connector
│   └── TransactionalEmails.cs   # MailJet helper classes
├── Data/
│   └── AppDbContext.cs          # EF Core database context
├── Models/
│   ├── FormSubmission.cs        # Entity model
│   ├── FormConfiguration.cs     # Form config model
│   └── ConnectorResult.cs       # Connector result model
├── Services/
│   ├── FormSubmissionService.cs # Form submission logic
│   ├── ConnectorService.cs      # Connector orchestration
│   ├── BackgroundTaskQueue.cs   # Async task processing
│   └── EmailTemplateService.cs  # Email template generation
├── Endpoints/
│   └── FormEndpoints.cs         # API endpoints
├── Migrations/                   # EF Core migrations
├── Program.cs                    # Application startup
└── appsettings.json             # Configuration
```