namespace seed_a_backend.Api.Models;

public class Raum
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;

    public string SpielstaetteId { get; set; } = default!;
    public Spielstaette Spielstaette { get; set; } = default!;

    /// Kommagetrennte Liste der Reihenbezeichner, z. B. "A,B,C,D".
    public string ReihenCsv { get; set; } = default!;

    public int Spalten { get; set; }

    /// Kommagetrennte Liste der Spaltennummern, die als Gang gelten. Kann leer sein.
    public string? GangSpaltenCsv { get; set; }

    public string? GangHinweis { get; set; }

    public ICollection<Veranstaltung> Veranstaltungen { get; set; } = new List<Veranstaltung>();

    public IReadOnlyList<string> Reihen =>
        ReihenCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public IReadOnlyList<int> GangSpalten =>
        string.IsNullOrWhiteSpace(GangSpaltenCsv)
            ? Array.Empty<int>()
            : GangSpaltenCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse)
                .ToArray();

    public bool IstGang(int spalte) => GangSpalten.Contains(spalte);

    public bool IstGueltigerSitzplatz(string reihe, int spalte) =>
        Reihen.Contains(reihe) && spalte >= 1 && spalte <= Spalten && !IstGang(spalte);
}
