using FormFeeder.Api.Models;

namespace FormFeeder.Api.Services;

public interface IFormConfigurationService
{
    Task<FormConfiguration?> GetFormConfigurationAsync(string formId);

    Task<IEnumerable<FormConfiguration>> GetAllFormConfigurationsAsync();

    Task<bool> IsFormEnabledAsync(string formId);

    Task<bool> IsDomainAllowedForFormAsync(string formId, string? domain);

    Task<HashSet<string>> GetAllAllowedDomainsAsync();
}
