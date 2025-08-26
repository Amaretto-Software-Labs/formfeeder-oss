using FormFeeder.Api.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FormFeeder.Api.Tests.Infrastructure;

public static class TestDbContextFactory
{
    public static AppDbContext CreateInMemoryDbContext(string? databaseName = null)
    {
        // Create options without internal service provider - let EF manage its own services
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .UseLoggerFactory(LoggerFactory.Create(builder =>
                builder.AddConsole().SetMinimumLevel(LogLevel.Debug)))
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
