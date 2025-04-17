using CloudflareDnsApi.Errors;
using CloudflareDnsApi.Models;
using CloudflareDnsApi.Services;
using OneOf;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddScoped<CloudflareService>();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Cloudflare DNS API",
        Version = "v1",
        Description = "A simple API to manage Cloudflare DNS entries.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Ankit",
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/api/dns", async (DnsUpdateRequest request, CloudflareService cloudflareService) =>
{
    if (request == null || string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Content))
    {
        return Results.BadRequest("Invalid request data.");
    }

    if (request.Type != DnsRecordType.A && request.Type != DnsRecordType.CNAME)
    {
        return Results.BadRequest("Invalid DNS record type. Only 'A' or 'CNAME' are supported.");
    }

    var typeString = request.Type.ToString().ToUpperInvariant();

    var result = await cloudflareService.UpdateDnsRecordAsync(request.Name, typeString, request.Content);

    return result.Match(
        success => success ? Results.Ok("DNS record updated successfully.") : Results.NotFound($"DNS record with name '{request.Name}' and type '{request.Type}' not found or update failed."),
        apiError => Results.StatusCode(apiError.StatusCode), // Return appropriate status code based on Cloudflare API error
        notFound => Results.NotFound($"DNS record with name '{request.Name}' not found.")
    );
})
.WithName("UpdateDnsRecord")
.WithOpenApi();

// Define the GET endpoint for retrieving DNS records
app.MapGet("/api/dns/{name}", async (string name, CloudflareService cloudflareService) =>
{
    if (string.IsNullOrEmpty(name))
    {
        return Results.BadRequest("DNS record name is required.");
    }

    var result = await cloudflareService.GetDnsRecordContentAsync(name);

    return result.Match(
        content => !string.IsNullOrEmpty(content) ? Results.Ok(new { Name = name, Content = content }) : Results.NotFound($"DNS record with name '{name}' not found."),
        apiError => Results.StatusCode(apiError.StatusCode), // Return appropriate status code based on Cloudflare API error
        notFound => Results.NotFound($"DNS record with name '{name}' not found.")
    );
})
.WithName("GetDnsRecord")
.WithOpenApi(); ;

app.Run();
