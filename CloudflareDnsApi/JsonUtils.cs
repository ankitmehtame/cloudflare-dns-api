using System.Text.Json;
using System.Text.Json.Serialization;

namespace CloudflareDnsApi
{
    public static class JsonUtils
    {
        public static JsonSerializerOptions ConfigureOptions(this JsonSerializerOptions options)
        {
            // Configure PropertyNameCaseInsensitive if it's not already set as desired
            options.PropertyNameCaseInsensitive = true;

            // Add the enum converter if it's not already present
            if (!options.Converters.Any(c => c is JsonStringEnumConverter))
            {
                options.Converters.Add(new JsonStringEnumConverter());
            }

            return options; // Return the modified options instance
        }
    }
}