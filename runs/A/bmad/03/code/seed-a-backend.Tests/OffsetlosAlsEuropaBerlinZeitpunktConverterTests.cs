using System.Text.Json;
using seed_a_backend.Api.Data;

namespace seed_a_backend.Tests;

public class OffsetlosAlsEuropaBerlinZeitpunktConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new OffsetlosAlsEuropaBerlinZeitpunktConverter() }
    };

    [Fact]
    public void Read_OffsetloserZeitstempelInSommerzeit_ErgibtPlusZweiStunden()
    {
        var ergebnis = JsonSerializer.Deserialize<DateTimeOffset>("\"2026-09-05T19:30:00\"", Options);

        Assert.Equal(TimeSpan.FromHours(2), ergebnis.Offset);
        Assert.Equal(new DateTime(2026, 9, 5, 19, 30, 0), ergebnis.DateTime);
    }

    [Fact]
    public void Read_OffsetloserZeitstempelInWinterzeit_ErgibtPlusEineStunde()
    {
        var ergebnis = JsonSerializer.Deserialize<DateTimeOffset>("\"2026-01-15T19:30:00\"", Options);

        Assert.Equal(TimeSpan.FromHours(1), ergebnis.Offset);
    }

    [Fact]
    public void Read_ZeitstempelMitExplizitemOffset_WirdUnveraendertUebernommen()
    {
        var ergebnis = JsonSerializer.Deserialize<DateTimeOffset>("\"2026-09-05T19:30:00+05:00\"", Options);

        Assert.Equal(TimeSpan.FromHours(5), ergebnis.Offset);
    }

    [Fact]
    public void Read_ZeitstempelMitZSuffix_WirdAlsUtcUebernommen()
    {
        var ergebnis = JsonSerializer.Deserialize<DateTimeOffset>("\"2026-09-05T19:30:00Z\"", Options);

        Assert.Equal(TimeSpan.Zero, ergebnis.Offset);
    }

    [Fact]
    public void Read_ZeitstempelMitKleinbuchstabenZSuffix_WirdAlsUtcUebernommen()
    {
        var ergebnis = JsonSerializer.Deserialize<DateTimeOffset>("\"2026-09-05T19:30:00z\"", Options);

        Assert.Equal(TimeSpan.Zero, ergebnis.Offset);
    }

    [Fact]
    public void Read_DatumOhneZeitanteil_WirdAlsEuropaBerlinZeitInterpretiertStattAlsExplizitenOffsetMissverstanden()
    {
        var ergebnis = JsonSerializer.Deserialize<DateTimeOffset>("\"2026-09-05\"", Options);

        Assert.Equal(TimeSpan.FromHours(2), ergebnis.Offset);
    }

    [Fact]
    public void Read_UnparsebarerZeitstempel_WirftBeschreibendeJsonException()
    {
        var exception = Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<DateTimeOffset>("\"nicht-ein-datum\"", Options));

        Assert.Contains("nicht-ein-datum", exception.Message);
    }
}
