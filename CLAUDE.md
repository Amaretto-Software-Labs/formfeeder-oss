# Claude Code Project Information

## Project Overview
**Name**: FormFeeder  
**Type**: Web API Service  
**Language**: C# / .NET 9  
**Framework**: ASP.NET Core Minimal API  
**Database**: PostgreSQL with Entity Framework Core  
**Testing**: xUnit, Moq, FluentAssertions  

## Key Technologies
- .NET 9 with C# 13
- ASP.NET Core Minimal APIs
- Entity Framework Core 9 with PostgreSQL
- Polly for resilience
- MailJet for email
- Docker for containerization
- GitHub Actions for CI/CD

## Project Structure
```
FormFeeder.Api/          # Main API project
├── Connectors/         # Email, Slack, and custom connectors
├── Data/              # EF Core context and migrations
├── Endpoints/         # API endpoint definitions
├── Middleware/        # Custom middleware components
├── Models/           # Domain models and DTOs
├── Services/         # Business logic services
└── Dockerfile        # Container configuration

FormFeeder.Api.Tests/   # Test project
├── Connectors/       # Connector unit tests
├── Endpoints/        # Endpoint tests
├── Integration/      # Integration tests
├── Models/          # Model tests
└── Services/        # Service tests
```

## Development Commands

### Build and Test
```bash
dotnet build                    # Build solution
dotnet test                     # Run all tests
dotnet test --logger trx        # Generate test results file
dotnet format                   # Format code
dotnet format --verify-no-changes  # Check formatting
```

### Database
```bash
dotnet ef migrations add <Name> -p FormFeeder.Api  # Create migration
dotnet ef database update -p FormFeeder.Api        # Apply migrations
dotnet ef migrations list -p FormFeeder.Api        # List migrations
```

### Run Application
```bash
dotnet run --project FormFeeder.Api                # Run API
dotnet watch run --project FormFeeder.Api          # Run with hot reload
```

### Docker
```bash
# Local development
docker build -t formfeeder ./FormFeeder.Api        # Build image
docker run -p 8080:8080 formfeeder                # Run container

# Using GitHub Container Registry
docker pull ghcr.io/yourusername/formfeeder-oss:latest   # Pull latest
docker pull ghcr.io/yourusername/formfeeder-oss:v1.0.0   # Pull specific version

# Run from registry
docker run -d -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Database=formfeeder;Username=postgres;Password=postgres" \
  ghcr.io/yourusername/formfeeder-oss:latest
```

## Configuration
- **appsettings.json**: Base configuration
- **appsettings.Development.json**: Development overrides
- Environment variables override JSON settings
- Connection string: `ConnectionStrings:DefaultConnection`

## Key Features
1. **Form Processing**: Accept submissions from static sites
2. **Private Forms**: Generate secure, expiring endpoints
3. **Connectors**: Extensible system for email, Slack, webhooks
4. **Rate Limiting**: Per-form request throttling
5. **Background Processing**: Async task queue for reliability
6. **Privacy Mode**: Process without storing data

## Testing Strategy
- Unit tests for all services and models
- Integration tests for API endpoints
- In-memory database for test isolation
- TestWebApplicationFactory for API testing
- Minimum 80% code coverage target

## Code Style
- Follow .editorconfig rules
- Use StyleCop analyzers
- Nullable reference types enabled
- Implicit usings enabled
- File-scoped namespaces preferred

## Git Workflow
1. Feature branches from `main`
2. Pull requests for all changes
3. CI must pass before merge
4. Conventional commit messages
5. Squash merge to main

## CI/CD Pipeline
- **Build**: Multi-platform (.NET 9)
- **Test**: Run all tests with coverage
- **Analyze**: CodeQL security scanning
- **Docker**: Build and push images to GitHub Container Registry
- **Release**: Automated GitHub releases with Docker images
- **Registry**: Images published to `ghcr.io/[owner]/formfeeder-oss`

## Important Files
- `.editorconfig`: Code style rules
- `Directory.Build.props`: Shared project settings
- `.globalconfig`: Analyzer configurations
- `.github/workflows/`: CI/CD definitions

## Common Issues and Solutions

### PostgreSQL Connection
Ensure PostgreSQL is running and connection string is correct.
Default: `Host=localhost;Database=formfeeder;Username=postgres;Password=postgres`

### Migration Errors
Always specify project: `dotnet ef [command] -p FormFeeder.Api`

### Test Failures
Check for port conflicts (5432 for PostgreSQL, 5001 for API)

### Docker Build
Ensure Docker daemon is running and has sufficient resources

## Useful Links
- [ASP.NET Core Minimal APIs](https://docs.microsoft.com/aspnet/core/fundamentals/minimal-apis)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [xUnit Documentation](https://xunit.net/docs)
- [Polly Resilience](https://github.com/App-vNext/Polly)

## Notes for AI Assistants
- Always check existing code patterns before implementing new features
- Use dependency injection for all services
- Follow RESTful conventions for API endpoints
- Write tests alongside implementation
- Update this file when adding major features or changing architecture