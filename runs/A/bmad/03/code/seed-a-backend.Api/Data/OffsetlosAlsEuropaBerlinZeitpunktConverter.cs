using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace seed_a_backend.Api.Data;

/// <summary>
/// Parst Zeitstempel ohne UTC-Offset (wie im Anfangsdatenbestand) als lokale Zeit
/// der Zeitzone "Europe/Berlin" statt der Zeitzone der Host-Maschine (Standardverhalten
/// des System.Text.Json-DateTimeOffset-Konverters). Zeitstempel mit explizitem Offset
/// ("...+02:00", "...Z") werden unverändert mit diesem Offset übernommen.
/// </summary>
public sealed class OffsetlosAlsEuropaBerlinZeitpunktConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Zeitpunkt muss als String vorliegen.");
        }

        var roh = reader.GetString()
            ?? throw new JsonException("Zeitpunkt darf nicht null sein.");

        try
        {
            if (HatExplizitenOffset(roh))
            {
                return DateTimeOffset.Parse(roh, CultureInfo.InvariantCulture, DateTimeStyles.None);
            }

            var lokaleZeit = DateTime.Parse(roh, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault);
            var offset = EuropaBerlinZeitzone.Instanz.GetUtcOffset(lokaleZeit);
            return new DateTimeOffset(lokaleZeit, offset);
        }
        catch (FormatException ex)
        {
            throw new JsonException($"Zeitpunkt '{roh}' ist kein gültiger Zeitstempel.", ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }

    private static bool HatExplizitenOffset(string roh)
    {
        if (roh.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var tIndex = roh.IndexOf('T');
        if (tIndex < 0)
        {
            return false;
        }

        var zeitteil = roh[(tIndex + 1)..];
        return zeitteil.Contains('+') || zeitteil.Contains('-');
    }
}
