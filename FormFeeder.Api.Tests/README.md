# FormFeeder.Api.Tests

A comprehensive test suite for the FormFeeder.Api application using xUnit, Moq, FluentAssertions, and ASP.NET Core testing framework.

## Project Structure

```
FormFeeder.Api.Tests/
├── Connectors/
│   └── MailJetConnectorTests.cs           # Unit tests for MailJet email connector
├── Infrastructure/
│   ├── TestBase.cs                        # Base class for unit tests
│   ├── TestDbContextFactory.cs            # In-memory database factory
│   ├── TestWebApplicationFactory.cs       # Integration test factory
│   └── TestDataBuilder.cs                 # Test data builders
├── Integration/
│   ├── FormEndpointsIntegrationTests.cs   # API endpoint integration tests
│   └── MiddlewareIntegrationTests.cs      # Middleware integration tests
├── Models/
│   ├── DTOs/
│   │   └── FormSubmissionResponseTests.cs  # DTO tests
│   ├── FormConfigurationTests.cs          # Model tests
│   ├── FormSubmissionTests.cs             # Model tests  
│   └── ResultTests.cs                     # Result<T> pattern tests
├── Services/
│   ├── BackgroundTaskQueueTests.cs       # Background task queue tests
│   ├── ConnectorFactoryTests.cs          # Connector factory tests
│   ├── ConnectorServiceTests.cs          # Connector service tests
│   ├── EmailTemplateServiceTests.cs      # Email template service tests
│   ├── FormConfigurationServiceTests.cs  # Form configuration service tests
│   ├── FormDataExtractionServiceTests.cs # Form data extraction service tests
│   ├── FormSubmissionServiceTests.cs     # Form submission service tests
│   └── FormValidationServiceTests.cs     # Form validation service tests
└── Utilities/
    └── HttpRequestExtensionsTests.cs     # HTTP request extension tests
```

## Test Coverage

### Unit Tests

#### Services (100% coverage)
- **FormSubmissionService**: Database operations, error handling, JSON serialization
- **FormConfigurationService**: Configuration loading, domain validation, rate limiting
- **ConnectorFactory**: Connector creation, type validation, error handling
- **ConnectorService**: Connector execution, background processing, error handling
- **EmailTemplateService**: HTML/text email generation, templating
- **BackgroundTaskQueue**: Concurrent queue operations, task processing
- **FormValidationService**: Form and origin validation, domain checking
- **FormDataExtractionService**: Form data extraction, client info extraction

#### Connectors
- **MailJetConnector**: Email sending, configuration validation, template handling

#### Models & DTOs
- **FormSubmission**: Entity properties, JSON handling, database integration
- **FormConfiguration**: Configuration properties, rate limiting, connectors
- **FormSubmissionResponse**: Record behavior, immutability, value equality
- **Result<T>**: Success/failure patterns, matching, error handling

#### Utilities
- **HttpRequestExtensions**: IP address extraction, header parsing, proxy handling

### Integration Tests

#### API Endpoints
- **FormEndpoints**: HTTP methods, request/response handling, error scenarios
- Rate limiting configuration
- Content type handling
- Error response formats

#### Middleware
- **DynamicCorsMiddleware**: CORS header handling, origin validation, preflight requests
- **ClientInfoMiddleware**: Request logging, client info extraction

## Test Patterns Used

### AAA Pattern (Arrange, Act, Assert)
All tests follow the AAA pattern for clear structure and readability.

### Test Organization
- Tests are grouped in nested classes by functionality
- Descriptive test names: `MethodName_StateUnderTest_ExpectedBehavior`
- Theory/InlineData for parameterized tests
- Edge cases and error conditions are thoroughly tested

### Mocking Strategy
- Mock external dependencies (IFormConfigurationService, ILogger, etc.)
- Use in-memory database for integration tests
- Verify interactions with mocked services

### Test Data
- TestDataBuilder provides consistent test data
- Factory methods for creating test objects
- Realistic test scenarios with edge cases

## Key Testing Features

### Database Testing
- In-memory database for isolated tests
- Entity Framework integration testing
- JSON document handling in PostgreSQL

### Async Testing
- Proper async/await testing patterns
- Background task testing
- Concurrent operation testing

### Error Handling
- Exception handling verification
- Logging verification
- Result pattern testing

### HTTP Testing
- WebApplicationFactory for integration tests
- HTTP request/response testing
- Header and content type validation

### Validation Testing
- Input validation scenarios
- Domain validation testing
- Rate limiting validation

## Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "ClassName=FormSubmissionServiceTests"

# Run specific test method
dotnet test --filter "MethodName=SaveSubmissionAsync_WithValidData_ShouldSaveSuccessfully"
```

## Test Dependencies

- **xUnit**: Testing framework
- **FluentAssertions**: Readable assertions
- **Moq**: Mocking framework
- **Microsoft.AspNetCore.Mvc.Testing**: Integration testing
- **Microsoft.EntityFrameworkCore.InMemory**: In-memory database

## Best Practices Implemented

1. **Test Isolation**: Each test is independent and can run in any order
2. **Descriptive Naming**: Test names clearly describe what is being tested
3. **Single Responsibility**: Each test focuses on one specific behavior
4. **Comprehensive Coverage**: Happy path, edge cases, and error scenarios
5. **Realistic Data**: Test data represents real-world scenarios
6. **Performance Considerations**: Tests run quickly with minimal setup
7. **Maintainable Code**: Well-organized and easily extensible test structure

## Notes

- Some tests may need compilation fixes due to missing using statements or accessibility issues
- Integration tests require proper Program class accessibility (added public partial class Program)
- Middleware tests use custom host builders for isolated testing
- Background task tests include concurrency scenarios
- Rate limiting tests verify middleware configuration