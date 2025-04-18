using System.Text.Json;
using CloudflareDnsApi.Services;
using CloudflareDnsApi.Models;

namespace CloudflareDnsApi.Tests;

public class CloudflareApiResponseTests
{
    private const string ListDnsRecordsJson = """
    {
        "result": [
            {
                "id": "record-id-1",
                "name": "first.example.com",
                "type": "A",
                "content": "192.168.1.5",
                "proxiable": true,
                "proxied": true,
                "ttl": 1,
                "settings": {},
                "meta": {},
                "comment": null,
                "tags": [],
                "created_on": "2024-07-17T06:49:17.868239Z",
                "modified_on": "2024-08-30T08:46:45.368927Z"
            },
            {
                "id": "record-id-2",
                "name": "second.example.com",
                "type": "CNAME",
                "content": "something-else.example.com",
                "proxiable": true,
                "proxied": true,
                "ttl": 1,
                "settings": {},
                "meta": {},
                "comment": null,
                "tags": [],
                "created_on": "2024-07-17T06:49:17.868239Z",
                "modified_on": "2024-08-30T08:46:45.368927Z"
            }
        ],
        "success": true,
        "errors": [],
        "messages": [],
        "result_info": {
            "page": 1,
            "per_page": 100,
            "count": 1,
            "total_count": 1,
            "total_pages": 1
        }
    }
    """;

    private const string DnsRecordJson = """
    {
        "result": {
        "id": "record-id-3",
        "type": "A",
        "name": "single.example.com",
        "content": "10.0.0.5",
        "proxied": false,
        "ttl": 300
        },
        "success": true,
        "errors": [],
        "messages": []
    }
    """;

    private readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions().ConfigureOptions();

    [Fact]
    public void CanDeserializeCloudflareListDnsRecordsResponse()
    {
        var response = JsonSerializer.Deserialize<CloudflareListDnsRecordsResponse>(ListDnsRecordsJson, jsonOptions);

        // Assert the main response properties
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Result);
        Assert.Equal(2, response.Result.Length);

        // Assert the first record
        var record1 = response.Result[0];
        Assert.NotNull(record1);
        Assert.Equal("record-id-1", record1.Id);
        Assert.Equal(DnsRecordType.A, record1.Type);
        Assert.Equal("first.example.com", record1.Name);
        Assert.Equal("192.168.1.5", record1.Content);

        // Assert the second record
        var record2 = response.Result[1];
        Assert.NotNull(record2);
        Assert.Equal("record-id-2", record2.Id);
        Assert.Equal(DnsRecordType.CNAME, record2.Type);
        Assert.Equal("second.example.com", record2.Name);
        Assert.Equal("something-else.example.com", record2.Content);
    }

    [Fact]
    public void CanDeserializeCloudflareGetDnsRecordResponse()
    {
        var response = JsonSerializer.Deserialize<CloudflareGetDnsRecordResponse>(DnsRecordJson, jsonOptions);

        // Assert the main response properties
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Result);

        // Assert the record
        var record = response.Result;
        Assert.NotNull(record);
        Assert.Equal("record-id-3", record.Id);
        Assert.Equal(DnsRecordType.A, record.Type);
        Assert.Equal("single.example.com", record.Name);
        Assert.Equal("10.0.0.5", record.Content);
    }
}
