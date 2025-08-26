using System.Text.Json;

using FormFeeder.Api.Models;
using FormFeeder.Api.Models.Entities;

using Microsoft.EntityFrameworkCore;

namespace FormFeeder.Api.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<FormSubmission> FormSubmissions { get; set; }

    public DbSet<FormConfigurationEntity> FormConfigurations { get; set; }

    public DbSet<AllowedDomainEntity> AllowedDomains { get; set; }

    public DbSet<RateLimitSettingsEntity> RateLimitSettings { get; set; }

    public DbSet<ConnectorConfigurationEntity> ConnectorConfigurations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureFormSubmission(modelBuilder);
        ConfigureFormConfiguration(modelBuilder);
    }

    private static void ConfigureFormSubmission(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FormSubmission>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.FormId);
            entity.HasIndex(e => e.SubmittedAt);
            entity.HasIndex(e => new { e.FormId, e.SubmittedAt });

            // FormId can be null for some edge cases
            entity.Property(e => e.FormId).IsRequired(false);

            // Configure JsonDocument property with value converter for all providers
            // This works for both in-memory and PostgreSQL
            entity.Property(e => e.FormData)
                .HasConversion(
                    v => v.RootElement.GetRawText(),
                    v => JsonDocument.Parse(v, default(JsonDocumentOptions)))
                .HasColumnType("jsonb")
                .IsRequired();
        });
    }

    private static void ConfigureFormConfiguration(ModelBuilder modelBuilder)
    {
        // FormConfiguration entity configuration
        modelBuilder.Entity<FormConfigurationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Create unique index on FormId for fast lookups and to prevent duplicates
            entity.HasIndex(e => e.FormId).IsUnique();

            entity.Property(e => e.FormId).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);

            // Set default values and configure timestamps
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // One-to-many relationship with AllowedDomains
            entity.HasMany(e => e.AllowedDomains)
                .WithOne(d => d.FormConfiguration)
                .HasForeignKey(d => d.FormConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-one relationship with RateLimit
            entity.HasOne(e => e.RateLimit)
                .WithOne(r => r.FormConfiguration)
                .HasForeignKey<RateLimitSettingsEntity>(r => r.FormConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many relationship with Connectors
            entity.HasMany(e => e.Connectors)
                .WithOne(c => c.FormConfiguration)
                .HasForeignKey(c => c.FormConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AllowedDomain entity configuration
        modelBuilder.Entity<AllowedDomainEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Domain).HasMaxLength(255);

            // Index for better query performance
            entity.HasIndex(e => e.Domain);

            // Unique constraint to prevent duplicate domains per form
            entity.HasIndex(e => new { e.FormConfigurationId, e.Domain }).IsUnique();
        });

        // RateLimitSettings entity configuration
        modelBuilder.Entity<RateLimitSettingsEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Set default values
            entity.Property(e => e.RequestsPerWindow).HasDefaultValue(10);
            entity.Property(e => e.WindowMinutes).HasDefaultValue(1);
        });

        // ConnectorConfiguration entity configuration
        modelBuilder.Entity<ConnectorConfigurationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);

            // Store settings as JSONB for flexibility
            entity.Property(e => e.SettingsJson)
                .HasColumnType("jsonb");

            // Index for better query performance
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Enabled);

            // Unique constraint to prevent duplicate connector names per form
            entity.HasIndex(e => new { e.FormConfigurationId, e.Name }).IsUnique();
        });
    }
}
