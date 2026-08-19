using System.Globalization;
using System.Text.Json;

namespace seed_b_backend.Api.Data;

public static class SeedValueCanonicalizer
{
    public static string? Canonicalize(object? rawValue)
    {
        switch (rawValue)
        {
            case null:
                return null;
            case bool b:
                return b ? "true" : "false";
            case string s:
                return s;
            case decimal d:
                return d.ToString(CultureInfo.InvariantCulture);
            case double or float or int or long:
                return Convert.ToDouble(rawValue, CultureInfo.InvariantCulture)
                    .ToString(CultureInfo.InvariantCulture);
            case JsonElement element:
                return CanonicalizeJsonElement(element);
            default:
                return Convert.ToString(rawValue, CultureInfo.InvariantCulture);
        }
    }

    private static string? CanonicalizeJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            _ => element.GetRawText(),
        };
    }
}
