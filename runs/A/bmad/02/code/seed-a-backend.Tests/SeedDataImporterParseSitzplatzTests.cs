using seed_a_backend.Api.Infrastructure;

namespace seed_a_backend.Tests;

public class SeedDataImporterParseSitzplatzTests
{
    [Theory]
    [InlineData("B3", "B", 3)]
    [InlineData("A1", "A", 1)]
    [InlineData("AA12", "AA", 12)]
    public void ParseSitzplatz_trennt_Reihenbuchstaben_und_Spaltenzahl(string sitzplatz, string erwarteteReihe, int erwarteteSpalte)
    {
        var (rowLabel, columnNumber) = SeedDataImporter.ParseSitzplatz(sitzplatz);

        Assert.Equal(erwarteteReihe, rowLabel);
        Assert.Equal(erwarteteSpalte, columnNumber);
    }

    [Theory]
    [InlineData("3")]
    [InlineData("B")]
    [InlineData("")]
    [InlineData("B-3")]
    [InlineData("B3C")]
    [InlineData("A99999999999")]
    public void ParseSitzplatz_wirft_bei_ungueltigem_Format(string sitzplatz)
    {
        Assert.Throws<FormatException>(() => SeedDataImporter.ParseSitzplatz(sitzplatz));
    }
}
