using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace FormFeeder.Api.Models;

public sealed class FormSubmission
{
    public Guid Id { get; set; }

    public required string? FormId { get; set; }

    [Column(TypeName = "jsonb")]
    public required JsonDocument FormData { get; set; }

    public string? ClientIp { get; set; }

    public string? UserAgent { get; set; }

    public string? Referer { get; set; }

    public DateTime SubmittedAt { get; set; }

    public string? ContentType { get; set; }
}
