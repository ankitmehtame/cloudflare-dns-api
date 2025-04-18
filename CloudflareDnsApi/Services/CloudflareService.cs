using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CloudflareDnsApi.Errors;
using CloudflareDnsApi.Models;
using OneOf;

namespace CloudflareDnsApi.Services;

public class CloudflareService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiToken;
    private readonly string _zoneId;
    private readonly ILogger<CloudflareService> _logger;

    private const string API_TOKEN_KEY = "CLOUDFLARE_API_TOKEN";
    private const string ZONE_ID_KEY = "CLOUDFLARE_ZONE_ID";
    private readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions().ConfigureOptions();

    public CloudflareService(IHttpClientFactory httpClientFactory, ILogger<CloudflareService> logger)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _apiToken = Environment.GetEnvironmentVariable(API_TOKEN_KEY) ?? string.Empty;
        _zoneId = Environment.GetEnvironmentVariable(ZONE_ID_KEY) ?? string.Empty;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (string.IsNullOrEmpty(_apiToken) || string.IsNullOrEmpty(_zoneId))
        {
            throw new InvalidOperationException($"Cloudflare API token ({API_TOKEN_KEY}) and/or Zone ID ({ZONE_ID_KEY}) environment variable is not set.");
        }
    }

    public async Task<OneOf<string, CloudflareApiError, DnsRecordNotFoundError>> GetDnsRecordContentAsync(string recordName)
    {
        try
        {
            // Step 1: List DNS records to find the one with the matching name
            var listRecordsUrl = $"https://api.cloudflare.com/client/v4/zones/{_zoneId}/dns_records";
            var response = await _httpClient.GetAsync($"{listRecordsUrl}?name={recordName}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Record {record} not found - {message}", recordName, errorContent);
                    return new DnsRecordNotFoundError(recordName);
                }

                return new CloudflareApiError((int)response.StatusCode, errorContent);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Response: {responseStatus} {responseContent}", (int)response.StatusCode, responseContent);
            var listResponse = JsonSerializer.Deserialize<CloudflareListDnsRecordsResponse>(responseContent, jsonOptions);
            _logger.LogDebug("Parsed - {parsedResponse}", listResponse);

            if (listResponse?.Result?.Length > 0)
            {
                // Assuming the first record with the matching name is the one we want
                var recordId = listResponse.Result[0].Id;

                // Step 2: Get the specific DNS record using its ID
                var getRecordUrl = $"https://api.cloudflare.com/client/v4/zones/{_zoneId}/dns_records/{recordId}";
                var getResponse = await _httpClient.GetAsync(getRecordUrl);
                if (!getResponse.IsSuccessStatusCode)
                {
                    var errorContent = await getResponse.Content.ReadAsStringAsync();
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogWarning("Record {record} not found - {message}", recordName, errorContent);
                        return new DnsRecordNotFoundError(recordName);
                    }
                    return new CloudflareApiError((int)getResponse.StatusCode, errorContent);
                }
                var getResponseContent = await getResponse.Content.ReadAsStringAsync();
                _logger.LogDebug("Response: {responseStatus} {responseContent}", (int)getResponse.StatusCode, getResponseContent);
                var getRecordResponse = JsonSerializer.Deserialize<CloudflareGetDnsRecordResponse>(getResponseContent, jsonOptions);

                var jsonResponse = getRecordResponse?.Result?.Content;
                if (jsonResponse == null)
                {
                    _logger.LogError("Unable to parse " + nameof(CloudflareGetDnsRecordResponse) + " - {response}", getResponseContent);
                    return new CloudflareApiError(500, $"Unable to parse {nameof(CloudflareGetDnsRecordResponse)}");
                }
                return jsonResponse;
            }

            _logger.LogWarning("Record {record} not found", recordName);
            return new DnsRecordNotFoundError(recordName);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("Error getting DNS record for {recordName} - {error}", recordName, ex.Message);
            throw;
        }
    }

    public async Task<OneOf<bool, CloudflareApiError, DnsRecordNotFoundError>> UpdateDnsRecordAsync(string recordName, DnsRecordType recordType, string content)
    {
        try
        {
            // Step 1: List DNS records to find the one with the matching name and type
            var listRecordsUrl = $"https://api.cloudflare.com/client/v4/zones/{_zoneId}/dns_records";
            var listResponse = await _httpClient.GetAsync($"{listRecordsUrl}?name={recordName}");
            if (!listResponse.IsSuccessStatusCode)
            {
                var errorContent = await listResponse.Content.ReadAsStringAsync();
                return new CloudflareApiError((int)listResponse.StatusCode, errorContent);
            }
            var listResponseContent = await listResponse.Content.ReadAsStringAsync();
            _logger.LogDebug("Response: {responseStatus} {responseContent}", (int)listResponse.StatusCode, listResponseContent);
            var listResult = JsonSerializer.Deserialize<CloudflareListDnsRecordsResponse>(listResponseContent, jsonOptions);

            if (listResult?.Result?.Length > 0)
            {
                // Assuming the first record with the matching name and type is the one we want to update
                var recordId = listResult.Result.FirstOrDefault(x => x.Type == recordType)?.Id ?? listResult.Result[0].Id;

                // Step 2: Update the DNS record using its ID
                var updateRecordUrl = $"https://api.cloudflare.com/client/v4/zones/{_zoneId}/dns_records/{recordId}";
                var payload = new
                {
                    type = recordType.ToString().ToUpperInvariant(),
                    name = recordName,
                    content = content,
                    ttl = 60 // You can adjust the TTL as needed
                };
                var jsonPayload = JsonSerializer.Serialize(payload);
                var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var updateResponse = await _httpClient.PutAsync(updateRecordUrl, httpContent);
                if (!updateResponse.IsSuccessStatusCode)
                {
                    var errorContent = await updateResponse.Content.ReadAsStringAsync();
                    return new CloudflareApiError((int)updateResponse.StatusCode, errorContent);
                }
                var updateResponseContent = await updateResponse.Content.ReadAsStringAsync();
                _logger.LogDebug("Response: {responseStatus} {responseContent}", (int)updateResponse.StatusCode, updateResponseContent);
                var updateResult = JsonSerializer.Deserialize<CloudflareUpdateDnsRecordResponse>(updateResponseContent, jsonOptions);

                return updateResult?.Success ?? false;
            }
            _logger.LogWarning("Unable to find DNS record {record} and {recordType} to update", recordName, recordType);

            return new DnsRecordNotFoundError(recordName);
        }
        catch (HttpRequestException ex)
        {
            // Log the error or handle it appropriately
            _logger.LogError("Error updating DNS record: {error}", ex.Message);
            return new CloudflareApiError(500, ex.Message);
        }
    }
}

public record class CloudflareListDnsRecordsResponse(CloudflareDnsRecordResult[] Result, bool Success);

public record class CloudflareGetDnsRecordResponse(CloudflareDnsRecordResult Result, bool Success);

public record class CloudflareDnsRecordResult(string Id, DnsRecordType Type, string Name, string Content);

public record class CloudflareUpdateDnsRecordResponse(bool Success);
