using seed_a_backend.Api.Models;

namespace seed_a_backend.Tests;

public class RaumTests
{
    private static Raum ErstelleRaum() => new()
    {
        Id = "R1",
        Name = "Saal",
        SpielstaetteId = "V1",
        ReihenCsv = "A,B,C",
        Spalten = 5,
        GangSpaltenCsv = "3"
    };

    [Theory]
    [InlineData("A", 1, true)]
    [InlineData("C", 5, true)]
    [InlineData("A", 3, false)] // Gang
    [InlineData("A", 0, false)] // außerhalb des Bereichs
    [InlineData("A", 6, false)] // außerhalb des Bereichs
    [InlineData("D", 1, false)] // unbekannte Reihe
    public void IstGueltigerSitzplatz_PrueftReiheSpalteUndGang(string reihe, int spalte, bool erwartet)
    {
        var raum = ErstelleRaum();
        Assert.Equal(erwartet, raum.IstGueltigerSitzplatz(reihe, spalte));
    }

    [Fact]
    public void Reihen_UndGangSpalten_WerdenAusCsvGeparst()
    {
        var raum = ErstelleRaum();
        Assert.Equal(["A", "B", "C"], raum.Reihen);
        Assert.Equal([3], raum.GangSpalten);
    }

    [Fact]
    public void GangSpalten_OhneGang_IstLeer()
    {
        var raum = ErstelleRaum();
        raum.GangSpaltenCsv = null;
        Assert.Empty(raum.GangSpalten);
    }
}
