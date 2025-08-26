using FormFeeder.Api.Data;
using FormFeeder.Api.Endpoints;
using FormFeeder.Api.Middleware;
using FormFeeder.Api.Services;
using FormFeeder.Api.Services.Extensions;

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connectionString);
    dataSourceBuilder.EnableDynamicJson();
    var dataSource = dataSourceBuilder.Build();

    options.UseNpgsql(dataSource);
});

// Core services
builder.Services.AddHttpClient();
builder.Services.AddFormConfiguration(builder.Configuration);
builder.Services.AddScoped<FormSubmissionService>();
builder.Services.AddScoped<FormConfigurationSeedingService>();

// Retry policy services
builder.Services.Configure<RetryPolicyConfiguration>(builder.Configuration.GetSection("RetryPolicy"));
builder.Services.AddSingleton<IRetryPolicyFactory, RetryPolicyFactory>();

// SOLID principle services
builder.Services.AddScoped<IFormValidationService, FormValidationService>();
builder.Services.AddScoped<IFormDataExtractionService, FormDataExtractionService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IConnectorFactory, ConnectorFactory>();
builder.Services.AddScoped<IConnectorService, ConnectorService>();

// Background task processing
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<QueuedHostedService>();

// Add rate limiting
builder.Services.AddFormRateLimiting(builder.Configuration);

// Add private form generation services
builder.Services.AddPrivateFormGeneration(builder.Configuration);

// Configure CORS
builder.Services.AddCors();

// Add OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Initialize database (apply migrations)
await DatabaseBootstrap.InitializeDatabaseAsync(app);

// Configure middleware pipeline
var enableSwagger = builder.Configuration.GetValue<bool>("EnableSwagger", app.Environment.IsDevelopment());
if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<DynamicCorsMiddleware>();

// Add rate limiting middleware
app.UseRateLimiter();

app.UseMiddleware<ClientInfoMiddleware>();

// Map endpoints
app.MapFormEndpoints();
app.MapPrivateFormEndpoints();
app.MapHealthEndpoints();

app.Run();

// Make Program accessible to integration tests
public partial class Program
{
}
