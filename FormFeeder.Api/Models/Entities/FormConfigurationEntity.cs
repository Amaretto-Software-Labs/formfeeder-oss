using System.ComponentModel.DataAnnotations;

namespace FormFeeder.Api.Models.Entities;

public sealed class FormConfigurationEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public required string FormId { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool Enabled { get; set; } = true;

    public bool PrivacyMode { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<AllowedDomainEntity> AllowedDomains { get; set; } = [];

    public RateLimitSettingsEntity? RateLimit { get; set; }

    public ICollection<ConnectorConfigurationEntity> Connectors { get; set; } = [];
}

public sealed class AllowedDomainEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    public required string Domain { get; set; }

    public Guid FormConfigurationId { get; set; }

    // Navigation property
    public FormConfigurationEntity FormConfiguration { get; set; } = null!;
}

public sealed class RateLimitSettingsEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public int RequestsPerWindow { get; set; } = 10;

    public int WindowMinutes { get; set; } = 1;

    public Guid FormConfigurationId { get; set; }

    // Navigation property
    public FormConfigurationEntity FormConfiguration { get; set; } = null!;
}

public sealed class ConnectorConfigurationEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public required string Type { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    public bool Enabled { get; set; } = true;

    public string? SettingsJson { get; set; }

    public Guid FormConfigurationId { get; set; }

    // Navigation property
    public FormConfigurationEntity FormConfiguration { get; set; } = null!;
}
