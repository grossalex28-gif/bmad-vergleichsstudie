using System.Net.Http.Json;
using A1Testsuite.Treiber;

namespace A1Testsuite.Implementierungen;

// Adapter fuer a_solo_1, gebaut ausschliesslich anhand von
// a1-testsuite/openapi/a_solo_1.json (Abschnitt 6 der Akzeptanztest-
// Architektur). BerechnePreis: siehe Kopfkommentar in ABmad1Adapter.cs,
// Punkt 1 -- kein Vorschau-Endpunkt vorhanden, lokale Summierung ueber
// echte, vom Server gelieferte Preiskategorien.
public sealed class ASolo1Adapter : HttpAdapterBasis, IProjektATreiber
{
    public ASolo1Adapter(string basisUrl) : base(basisUrl) { }

    private sealed record VeranstaltungListItemDto(string Id, string Titel, string SpielstaetteId, string SpielstaetteName, DateTime Zeitpunkt);
    private sealed record PreiskategorieDto(string Id, string Name, decimal Preis);
    private sealed record VeranstaltungDetailDto(string Id, string Titel, string Beschreibung, int DauerMinuten, int Altersfreigabe, string SpielstaetteId, string SpielstaetteName, string RaumId, string RaumName, DateTime Zeitpunkt, List<PreiskategorieDto> Preiskategorien);
    private sealed record SitzplatzPositionDto(string Reihe, int Spalte);
    private sealed record SitzplanDto(string RaumName, List<string> Reihen, int Spalten, List<int> GangSpalten, string? GangHinweis, List<SitzplatzPositionDto> BelegtePlaetze);
    private sealed record BuchungPositionResponseDto(string Reihe, int Spalte, string PreiskategorieId, string PreiskategorieName, decimal Preis);
    private sealed record BuchungResponseDto(string Referenz, string Name, string Email, string Status, DateTime ErstelltAm, string VeranstaltungId, string VeranstaltungTitel, DateTime VeranstaltungZeitpunkt, string SpielstaetteName, string RaumName, List<BuchungPositionResponseDto> Positionen, decimal Gesamtpreis);
    private sealed record BuchungPositionRequestDto(string Reihe, int Spalte, string PreiskategorieId);
    private sealed record BuchungCreateRequestDto(string VeranstaltungId, string Name, string Email, List<BuchungPositionRequestDto> Sitzplaetze);

    public async Task<IReadOnlyList<VeranstaltungUebersicht>> ListeVeranstaltungen(
        DateTime? vonDatum = null, DateTime? bisDatum = null, string? spielstaette = null)
    {
        var query = new List<string>();
        if (vonDatum is not null) query.Add($"von={vonDatum:yyyy-MM-ddTHH:mm:ss}");
        if (bisDatum is not null) query.Add($"bis={bisDatum:yyyy-MM-ddTHH:mm:ss}");
        var url = "api/veranstaltungen" + (query.Count > 0 ? "?" + string.Join("&", query) : "");

        var liste = await Http.GetFromJsonAsync<List<VeranstaltungListItemDto>>(url) ?? [];
        // Raum ist in der Listenantwort nicht enthalten, bleibt leer -- kein
        // Testfall prueft Raum in der Veranstaltungsuebersicht.
        return liste
            .Where(v => spielstaette is null || v.SpielstaetteName == spielstaette)
            .Select(v => new VeranstaltungUebersicht(v.Id, v.Titel, v.SpielstaetteName, "", v.Zeitpunkt))
            .ToList();
    }

    public async Task<VeranstaltungDetail> HoleVeranstaltung(string id)
    {
        var v = await HoleDetail(id);
        return new VeranstaltungDetail(v.Id, v.Beschreibung, v.DauerMinuten, v.Altersfreigabe, v.SpielstaetteName, v.RaumName, v.Zeitpunkt);
    }

    public async Task<Sitzplan> HoleSitzplan(string veranstaltungId)
    {
        var plan = await HolePlan(veranstaltungId);
        var belegt = plan.BelegtePlaetze.Select(p => (p.Reihe, p.Spalte)).ToHashSet();
        var positionen = new List<SitzplanPosition>();
        for (var spalte = 1; spalte <= plan.Spalten; spalte++)
        {
            foreach (var reihe in plan.Reihen)
            {
                if (plan.GangSpalten.Contains(spalte))
                {
                    positionen.Add(new SitzplanPosition(reihe, spalte, PositionsTyp.Gang, null));
                    continue;
                }
                var status = belegt.Contains((reihe, spalte)) ? PositionsStatus.Belegt : PositionsStatus.Frei;
                positionen.Add(new SitzplanPosition(reihe, spalte, PositionsTyp.Sitz, status));
            }
        }
        return new Sitzplan(plan.Reihen.Count, plan.Spalten, positionen);
    }

    public async Task<IReadOnlyList<Preiskategorie>> HolePreiskategorien(string veranstaltungId)
    {
        var v = await HoleDetail(veranstaltungId);
        return v.Preiskategorien.Select(p => new Preiskategorie(p.Id, p.Name, p.Preis)).ToList();
    }

    public async Task<decimal> BerechnePreis(string veranstaltungId, IReadOnlyList<SitzplatzAuswahl> auswahl)
    {
        var kategorien = await HolePreiskategorien(veranstaltungId);
        return auswahl.Sum(a =>
        {
            var k = kategorien.SingleOrDefault(k => k.Bezeichnung == a.KategorieBezeichnung);
            return k?.Preis ?? throw new InvalidOperationException($"Preiskategorie '{a.KategorieBezeichnung}' nicht gefunden.");
        });
    }

    public async Task<string> LegeBuchungAn(string veranstaltungId, IReadOnlyList<SitzplatzAuswahl> auswahl, string name, string email)
    {
        var v = await HoleDetail(veranstaltungId);
        var sitzplaetze = auswahl.Select(a =>
        {
            var (reihe, spalte) = ZerlegeSitzplatzBezeichnung(a.SitzplatzBezeichnung);
            var kategorie = v.Preiskategorien.SingleOrDefault(k => k.Name == a.KategorieBezeichnung)
                ?? throw new InvalidOperationException($"Preiskategorie '{a.KategorieBezeichnung}' nicht gefunden.");
            return new BuchungPositionRequestDto(reihe, spalte, kategorie.Id);
        }).ToList();

        var antwort = await Http.PostAsJsonAsync("api/buchungen", new BuchungCreateRequestDto(veranstaltungId, name, email, sitzplaetze));
        if (!antwort.IsSuccessStatusCode) throw new BuchungAbgelehntException();

        var buchung = await antwort.Content.ReadFromJsonAsync<BuchungResponseDto>()
            ?? throw new InvalidOperationException("Leere Antwort bei Buchungserstellung.");
        return buchung.Referenz;
    }

    public async Task<Buchung> HoleBuchung(string referenz)
    {
        var b = await Http.GetFromJsonAsync<BuchungResponseDto>($"api/buchungen/{referenz}")
            ?? throw new InvalidOperationException($"Buchung '{referenz}' nicht gefunden.");
        return UebersetzeBuchung(b);
    }

    public async Task StorniereBuchung(string referenz)
    {
        var antwort = await Http.PostAsync($"api/buchungen/{referenz}/stornieren", null);
        antwort.EnsureSuccessStatusCode();
    }

    private async Task<VeranstaltungDetailDto> HoleDetail(string id)
        => await Http.GetFromJsonAsync<VeranstaltungDetailDto>($"api/veranstaltungen/{id}")
           ?? throw new InvalidOperationException($"Veranstaltung '{id}' nicht gefunden.");

    private async Task<SitzplanDto> HolePlan(string veranstaltungId)
        => await Http.GetFromJsonAsync<SitzplanDto>($"api/veranstaltungen/{veranstaltungId}/sitzplan")
           ?? throw new InvalidOperationException($"Sitzplan fuer '{veranstaltungId}' nicht gefunden.");

    private static Buchung UebersetzeBuchung(BuchungResponseDto b) => new(
        b.Referenz,
        b.Positionen.Select(p => new Buchungsposition($"{p.Reihe}{p.Spalte}", p.PreiskategorieName, p.Preis)).ToList(),
        b.Gesamtpreis,
        b.Status);
}
