using System.Text.Json;
using FormFeeder.Api.Utilities;

namespace FormFeeder.Api.Services;

public sealed record ClientInfo(
    string? IpAddress,
    string? UserAgent,
    string? Referer,
    string? ContentType);

public interface IFormDataExtractionService
{
    Task<Dictionary<string, object>> ExtractFormDataAsync(HttpRequest request);
    ClientInfo ExtractClientInfo(HttpRequest request);
}

public sealed class FormDataExtractionService : IFormDataExtractionService
{
    public async Task<Dictionary<string, object>> ExtractFormDataAsync(HttpRequest request)
    {
        var result = new Dictionary<string, object>();
        
        var contentType = request.ContentType?.ToLowerInvariant();
        
        if (contentType?.Contains("application/json") == true)
        {
            return await ExtractJsonDataAsync(request);
        }
        
        if (request.HasFormContentType)
        {
            return ExtractFormEncodedData(request);
        }
        
        return result;
    }
    
    private async Task<Dictionary<string, object>> ExtractJsonDataAsync(HttpRequest request)
    {
        var result = new Dictionary<string, object>();
        
        try
        {
            request.EnableBuffering(); // Allow multiple reads of the body
            request.Body.Position = 0;
            
            using var reader = new StreamReader(request.Body, leaveOpen: true);
            var jsonContent = await reader.ReadToEndAsync();
            
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return result;
            }
            
            var jsonDocument = JsonDocument.Parse(jsonContent);
            return ExtractJsonElement(jsonDocument.RootElement);
        }
        catch (JsonException)
        {
            // Return empty dictionary for malformed JSON
            return result;
        }
        catch (Exception)
        {
            // Return empty dictionary for any other issues
            return result;
        }
    }
    
    private Dictionary<string, object> ExtractFormEncodedData(HttpRequest request)
    {
        var result = new Dictionary<string, object>();
        var form = request.Form;
        
        foreach (var field in form)
        {
            result[field.Key] = field.Value.Count switch
            {
                1 => field.Value.ToString(),
                > 1 => field.Value.ToArray(),
                _ => string.Empty
            };
        }

        if (form.Files.Count > 0)
        {
            var fileMetadata = form.Files.Select(file => new
            {
                fieldName = file.Name,
                fileName = file.FileName,
                contentType = file.ContentType,
                length = file.Length
            }).ToArray();
            
            result["_files"] = fileMetadata;
        }

        return result;
    }
    
    private Dictionary<string, object> ExtractJsonElement(JsonElement element)
    {
        var result = new Dictionary<string, object>();
        
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                result[property.Name] = ConvertJsonValue(property.Value);
            }
        }
        
        return result;
    }
    
    private object ConvertJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonValue).ToArray(),
            JsonValueKind.Object => ExtractJsonElement(element),
            _ => element.GetRawText()
        };
    }

    public ClientInfo ExtractClientInfo(HttpRequest request)
    {
        return new ClientInfo(
            IpAddress: request.GetClientIpAddress(),
            UserAgent: request.Headers.UserAgent.ToString(),
            Referer: request.Headers.Referer.ToString(),
            ContentType: request.ContentType
        );
    }

}