

namespace Application.Common.Extensions;

public static class JsonExtensions
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string ToJson<T>(this T obj, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(obj, options ?? DefaultOptions);
    }

    public static T? FromJson<T>(this string json, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
    }
}