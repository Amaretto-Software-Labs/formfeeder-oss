using FormFeeder.Api.Models;

namespace FormFeeder.Api.Services;

/// <summary>
/// Extended interface for form configuration management operations.
/// Implements Interface Segregation Principle by separating read-only operations from management operations.
/// </summary>
public interface IFormConfigurationManagementService : IFormConfigurationService
{
    Task<FormConfiguration> CreateFormConfigurationAsync(FormConfiguration config);

    Task<FormConfiguration?> UpdateFormConfigurationAsync(FormConfiguration config);

    Task<bool> DeleteFormConfigurationAsync(string formId);
}
