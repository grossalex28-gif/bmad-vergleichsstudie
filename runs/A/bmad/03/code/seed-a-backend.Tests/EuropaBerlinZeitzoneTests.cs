using seed_a_backend.Api.Data;

namespace seed_a_backend.Tests;

public class EuropaBerlinZeitzoneTests
{
    [Fact]
    public void Tagesbeginn_Sommerzeitdatum_ErgibtMitternachtMitPlusZweiStunden()
    {
        var ergebnis = EuropaBerlinZeitzone.Tagesbeginn(new DateOnly(2026, 9, 5));

        Assert.Equal(TimeSpan.FromHours(2), ergebnis.Offset);
        Assert.Equal(new DateTime(2026, 9, 5, 0, 0, 0), ergebnis.DateTime);
    }

    [Fact]
    public void Tagesbeginn_Winterzeitdatum_ErgibtMitternachtMitPlusEinerStunde()
    {
        var ergebnis = EuropaBerlinZeitzone.Tagesbeginn(new DateOnly(2026, 1, 15));

        Assert.Equal(TimeSpan.FromHours(1), ergebnis.Offset);
        Assert.Equal(new DateTime(2026, 1, 15, 0, 0, 0), ergebnis.DateTime);
    }

    [Fact]
    public void Tagesende_Sommerzeitdatum_ErgibtLetzteSekundeMitPlusZweiStunden()
    {
        var ergebnis = EuropaBerlinZeitzone.Tagesende(new DateOnly(2026, 9, 5));

        Assert.Equal(TimeSpan.FromHours(2), ergebnis.Offset);
        Assert.Equal(new DateTime(2026, 9, 5, 23, 59, 59, 999).AddTicks(9999), ergebnis.DateTime);
    }

    [Fact]
    public void Tagesende_Winterzeitdatum_ErgibtLetzteSekundeMitPlusEinerStunde()
    {
        var ergebnis = EuropaBerlinZeitzone.Tagesende(new DateOnly(2026, 1, 15));

        Assert.Equal(TimeSpan.FromHours(1), ergebnis.Offset);
        Assert.Equal(new DateTime(2026, 1, 15, 23, 59, 59, 999).AddTicks(9999), ergebnis.DateTime);
    }

    [Fact]
    public void Tagesbeginn_TagDerFruehjahrsumstellung_ErgibtMitternachtMitPlusEinerStunde()
    {
        var ergebnis = EuropaBerlinZeitzone.Tagesbeginn(new DateOnly(2026, 3, 29));

        Assert.Equal(TimeSpan.FromHours(1), ergebnis.Offset);
        Assert.Equal(new DateTime(2026, 3, 29, 0, 0, 0), ergebnis.DateTime);
    }

    [Fact]
    public void Tagesende_TagDerFruehjahrsumstellung_ErgibtLetzteSekundeMitPlusZweiStunden()
    {
        var ergebnis = EuropaBerlinZeitzone.Tagesende(new DateOnly(2026, 3, 29));

        Assert.Equal(TimeSpan.FromHours(2), ergebnis.Offset);
    }

    [Fact]
    public void Tagesbeginn_TagDerHerbstumstellung_ErgibtMitternachtMitPlusZweiStunden()
    {
        var ergebnis = EuropaBerlinZeitzone.Tagesbeginn(new DateOnly(2026, 10, 25));

        Assert.Equal(TimeSpan.FromHours(2), ergebnis.Offset);
        Assert.Equal(new DateTime(2026, 10, 25, 0, 0, 0), ergebnis.DateTime);
    }

    [Fact]
    public void Tagesende_TagDerHerbstumstellung_ErgibtLetzteSekundeMitPlusEinerStunde()
    {
        var ergebnis = EuropaBerlinZeitzone.Tagesende(new DateOnly(2026, 10, 25));

        Assert.Equal(TimeSpan.FromHours(1), ergebnis.Offset);
    }

    [Fact]
    public void Tagesbeginn_MinimalesDatum_LiefertDateTimeOffsetMinValueStattExceptionZuWerfen()
    {
        var ergebnis = EuropaBerlinZeitzone.Tagesbeginn(DateOnly.MinValue);

        Assert.Equal(DateTimeOffset.MinValue, ergebnis);
    }

    [Fact]
    public void Tagesende_MaximalesDatum_WirftKeineException()
    {
        var ergebnis = EuropaBerlinZeitzone.Tagesende(DateOnly.MaxValue);

        Assert.Equal(TimeSpan.FromHours(1), ergebnis.Offset);
    }
}
