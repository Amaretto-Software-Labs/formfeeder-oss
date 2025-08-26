namespace FormFeeder.Api.Models.DTOs;

public record FormSubmissionResponse(
    Guid Id,
    string FormId,
    DateTime SubmittedAt,
    bool Success,
    string? Message = null,
    FormSubmission? Submission = null);