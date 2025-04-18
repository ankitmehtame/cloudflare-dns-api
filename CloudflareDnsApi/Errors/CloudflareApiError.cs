namespace CloudflareDnsApi.Errors;

public record class CloudflareApiError(int StatusCode, string Message);