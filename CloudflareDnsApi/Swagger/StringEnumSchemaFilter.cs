using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CloudflareDnsApi.Swagger;

public class StringEnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            // Clear the existing enum values (which might be numeric)
            schema.Enum.Clear();

            // Get the string names of the enum members
            var enumNames = Enum.GetNames(context.Type).ToList();

            // Add the string names as enum values in the schema
            foreach (var name in enumNames)
            {
                schema.Enum.Add(new OpenApiString(name));
            }

            // Set the type to string
            schema.Type = "string";
            schema.Format = null; // Clear the format (e.g., "int32")
        }
    }
}