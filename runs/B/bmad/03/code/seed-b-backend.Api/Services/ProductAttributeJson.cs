using System.Text.Json;

namespace seed_b_backend.Api.Services;

public static class ProductAttributeJson
{
    public static bool Matches(string attributesJson, string attributeName, string filterValue)
        => TryFormatValue(attributesJson, attributeName) == filterValue;

    public static string? TryFormatValue(string attributesJson, string attributeName)
    {
        using var doc = JsonDocument.Parse(attributesJson);
        if (!doc.RootElement.TryGetProperty(attributeName, out var element))
        {
            return null;
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number => element.GetRawText(),
            _ => null // Null/Array/Object: kein filterbarer Skalarwert (z. B. P10s "KapazitaetLiter": null)
        };
    }
}
