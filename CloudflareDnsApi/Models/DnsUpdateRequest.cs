namespace CloudflareDnsApi.Models
{
    public record class DnsUpdateRequest(string Name, DnsRecordType Type, string Content);
}