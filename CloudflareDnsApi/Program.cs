using CloudflareDnsApi;
using CloudflareDnsApi.Models;
using CloudflareDnsApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<CloudflareService>();
builder.Services.AddCors();
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.ConfigureOptions());

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.ConfigureOptions());

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Cloudflare DNS API",
        Version = VersionUtils.AssemblyVersion,
        Description = $"v{VersionUtils.InfoVersion} A simple API to manage Cloudflare DNS entries.",
        Contact = new Microsoft.OpenApi.OpenApiContact
        {
            Name = "Ankit",
        },
        License = new Microsoft.OpenApi.OpenApiLicense
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
app.UseCors(b => b
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
);

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/swagger");
        return;
    }
    await next();
});

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

    var result = await cloudflareService.UpdateDnsRecordAsync(request.Name, request.Type, request.Content);

    return result.Match(
        success => success switch
                    {
                        CloudflareDnsUpdate.Updated => Results.Ok("DNS record updated successfully."),
                        CloudflareDnsUpdate.Unsuccessful => Results.NotFound($"DNS record with name '{request.Name}' and type '{request.Type}' not found or update failed."),
                        CloudflareDnsUpdate.UpToDate => Results.Ok("DNS record already up-to-date"),
                        _ => throw new IndexOutOfRangeException($"Unexpected {success.GetType().Name} value {success}"),
                    },
        apiError => Results.StatusCode(apiError.StatusCode), // Return appropriate status code based on Cloudflare API error
        notFound => Results.NotFound($"DNS record with name '{request.Name}' not found.")
    );
})
.WithName("UpdateDnsRecord");

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
.WithName("GetDnsRecord");

var logger = app.Services.GetService<ILogger<Program>>() ?? throw new InvalidOperationException("Logger is not found");
logger.LogInformation("Info version is {InfoVersion}", VersionUtils.InfoVersion);

try
{
    app.Services.GetRequiredService<CloudflareService>();
    app.Run();
}
catch (InvalidOperationException ex)
{
    logger.LogError(ex.Message);
    logger.LogInformation("Exiting...");
}