using System.Net.Http.Json;
using A1Testsuite.Treiber;

namespace A1Testsuite.Implementierungen;

// Adapter fuer a_solo_3, gebaut ausschliesslich anhand von
// a1-testsuite/openapi/a_solo_3.json. BerechnePreis: siehe Kopfkommentar in
// ABmad1Adapter.cs, Punkt 1.
//
// SitzplatzTyp und SitzplatzStatus sind in der OpenAPI-Beschreibung nur als
// "type: integer" dokumentiert (ohne enum/x-enumNames). Die Live-Stichprobe
// (a1-testsuite/openapi-samples/a_solo_3_sitzplan.json) zeigt aber, dass der
// Server tatsaechlich Strings sendet ("Sitzplatz"/"Gang" bzw.
// "Belegt"/"Frei"/null) -- wie bei a_bmad_1 (siehe dessen Kopfkommentar) ist
// die OpenAPI-Beschreibung hier irrefuehrend. Der Adapter richtet sich nach
// der tatsaechlichen Live-Antwort und nutzt IstGangTyp()/IstBelegtStatus()
// (HttpAdapterBasis.cs) statt eines Zahlenvergleichs.
//
// Nachtrag 31.08.2026 (Rollout a_solo_3): derselbe Irrtum betrifft auch
// BuchungStatus (ebenfalls nur "type: integer" ohne enum in der OpenAPI-
// Beschreibung), wurde beim urspruenglichen Adapterbau aber nicht erkannt,
// weil hole-beispiele.sh/hole-beispiele-2.sh nie tatsaechlich eine Buchung
// angelegt und damit nie eine Live-Antwort von POST /api/buchungen bzw.
// GET /api/buchungen/{referenz} eingesehen haben. Erst beim A1-Smoke-Test
// gegen a_solo_3 aufgefallen (JsonException beim Deserialisieren von
// BuchungDto.Status als int). BuchungDto.Status entsprechend auf string
// umgestellt, UebersetzeBuchung gibt den Wert jetzt direkt durch statt
// .ToString() aufzurufen. Der einzige Testfall, der das Feld prueft
// (Assert.False(string.IsNullOrWhiteSpace(buchung.Status))), verlangt
// keinen konkreten Wert, daher risikoarme Korrektur.
public sealed class ASolo3Adapter : HttpAdapterBasis, IProjektATreiber
{
    public ASolo3Adapter(string basisUrl) : base(basisUrl) { }

    private sealed record SpielstaetteDto(string Id, string Name);
    private sealed record RaumInfoDto(string Id, string Name);
    private sealed record PreiskategorieDto(string Id, string Name, decimal Preis);
    private sealed record VeranstaltungListItemDto(string Id, string Titel, string SpielstaetteId, string SpielstaetteName, DateTime Zeitpunkt);
    private sealed record VeranstaltungDetailDto(string Id, string Titel, string Beschreibung, DateTime Zeitpunkt, int DauerMinuten, int Altersfreigabe, SpielstaetteDto Spielstaette, RaumInfoDto Raum, List<PreiskategorieDto> Preiskategorien);
    private sealed record RaumSitzplanDto(List<string> Reihen, int Spalten, List<int> GangSpalten, string? GangHinweis);
    private sealed record SitzplatzDto(string Reihe, int Spalte, string Typ, string? Status);
    private sealed record SitzplanDto(string VeranstaltungId, RaumSitzplanDto Raum, List<SitzplatzDto> Sitzplaetze, List<PreiskategorieDto> Preiskategorien);
    private sealed record BuchungspositionDto(string Reihe, int Spalte, string PreiskategorieId, string PreiskategorieName, decimal Preis);
    private sealed record BuchungDto(string Referenz, string VeranstaltungId, string VeranstaltungTitel, DateTime VeranstaltungZeitpunkt, string Name, string Email, DateTime ErstelltAm, string Status, List<BuchungspositionDto> Sitzplaetze, decimal Gesamtpreis);
    private sealed record SitzplatzAuswahlDto(string Reihe, int Spalte, string PreiskategorieId);
    private sealed record CreateBuchungRequestDto(string VeranstaltungId, string Name, string Email, List<SitzplatzAuswahlDto> Sitzplaetze);

    public async Task<IReadOnlyList<VeranstaltungUebersicht>> ListeVeranstaltungen(
        DateTime? vonDatum = null, DateTime? bisDatum = null, string? spielstaette = null)
    {
        var query = new List<string>();
        if (vonDatum is not null) query.Add($"von={vonDatum:yyyy-MM-ddTHH:mm:ss}");
        if (bisDatum is not null) query.Add($"bis={bisDatum:yyyy-MM-ddTHH:mm:ss}");
        var url = "api/veranstaltungen" + (query.Count > 0 ? "?" + string.Join("&", query) : "");

        var liste = await Http.GetFromJsonAsync<List<VeranstaltungListItemDto>>(url) ?? [];
        return liste
            .Where(v => spielstaette is null || v.SpielstaetteName == spielstaette)
            .Select(v => new VeranstaltungUebersicht(v.Id, v.Titel, v.SpielstaetteName, "", v.Zeitpunkt))
            .ToList();
    }

    public async Task<VeranstaltungDetail> HoleVeranstaltung(string id)
    {
        var v = await HoleDetail(id);
        return new VeranstaltungDetail(v.Id, v.Beschreibung, v.DauerMinuten, v.Altersfreigabe, v.Spielstaette.Name, v.Raum.Name, v.Zeitpunkt);
    }

    public async Task<Sitzplan> HoleSitzplan(string veranstaltungId)
    {
        var plan = await HolePlan(veranstaltungId);
        var positionen = plan.Sitzplaetze.Select(s => IstGangTyp(s.Typ)
            ? new SitzplanPosition(s.Reihe, s.Spalte, PositionsTyp.Gang, null)
            : new SitzplanPosition(s.Reihe, s.Spalte, PositionsTyp.Sitz, s.Status is not null && IstBelegtStatus(s.Status) ? PositionsStatus.Belegt : PositionsStatus.Frei))
            .ToList();
        return new Sitzplan(plan.Raum.Reihen.Count, plan.Raum.Spalten, positionen);
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
            return new SitzplatzAuswahlDto(reihe, spalte, kategorie.Id);
        }).ToList();

        var antwort = await Http.PostAsJsonAsync("api/buchungen", new CreateBuchungRequestDto(veranstaltungId, name, email, sitzplaetze));
        if (!antwort.IsSuccessStatusCode) throw new BuchungAbgelehntException();

        var buchung = await antwort.Content.ReadFromJsonAsync<BuchungDto>()
            ?? throw new InvalidOperationException("Leere Antwort bei Buchungserstellung.");
        return buchung.Referenz;
    }

    public async Task<Buchung> HoleBuchung(string referenz)
    {
        var b = await Http.GetFromJsonAsync<BuchungDto>($"api/buchungen/{referenz}")
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

    private static Buchung UebersetzeBuchung(BuchungDto b) => new(
        b.Referenz,
        b.Sitzplaetze.Select(p => new Buchungsposition($"{p.Reihe}{p.Spalte}", p.PreiskategorieName, p.Preis)).ToList(),
        b.Gesamtpreis,
        b.Status);
}
