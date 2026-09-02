using System.Net.Http.Json;
using A1Testsuite.Treiber;

namespace A1Testsuite.Implementierungen;

// Adapter fuer a_bmad_3, gebaut ausschliesslich anhand von
// a1-testsuite/openapi/a_bmad_3.json. Besonderheiten dieser Implementierung:
//   - Endpunkte liegen auf Wurzelebene ohne "/api"-Praefix.
//   - BerechnePreis: siehe Kopfkommentar in ABmad1Adapter.cs, Punkt 1.
//   - Sitzplatztyp/-status sind nur als "type: string" ohne dokumentierte
//     Werte vorhanden -- Deutung ueber HttpAdapterBasis.IstGangTyp bzw.
//     IstBelegtStatus (deutsch/englisch robust).
//   - Die Antwort von POST /veranstaltungen/{id}/buchungen ist in der
//     generierten OpenAPI-Beschreibung nicht dokumentiert (nur "200: OK"
//     ohne Content-Schema). Angenommen wird dieselbe Form wie
//     BuchungDetailDto (wie bei GET .../buchungen/{referenz}), da alle
//     anderen elf Implementierungen genau diesem Muster folgen -- bei
//     Abweichung schlaegt das beim ersten echten Testlauf sofort und klar
//     erkennbar fehl (Deserialisierungsfehler direkt nach dem POST).
public sealed class ABmad3Adapter : HttpAdapterBasis, IProjektATreiber
{
    public ABmad3Adapter(string basisUrl) : base(basisUrl) { }

    private sealed record VeranstaltungListeDto(string Id, string Titel, string SpielstaetteId, string SpielstaetteName, DateTime Zeitpunkt);
    private sealed record PreiskategorieDto(string Id, string Name, decimal Preis);
    private sealed record VeranstaltungDetailDto(string Id, string Titel, string Beschreibung, int DauerMinuten, int Altersfreigabe, string SpielstaetteName, string RaumName, DateTime Zeitpunkt, List<PreiskategorieDto> Preiskategorien);
    private sealed record SitzplanPositionDto(int Spalte, string Typ, string? Code, string? Status);
    private sealed record SitzplanReiheDto(string Reihe, List<SitzplanPositionDto> Positionen);
    private sealed record SitzplanDto(string VeranstaltungId, string RaumId, string RaumName, List<SitzplanReiheDto> Reihen);
    private sealed record BuchungspositionDto(string SitzplatzCode, string PreiskategorieId, string PreiskategorieName, decimal PreisSnapshot);
    private sealed record BuchungDetailDto(string Referenz, string VeranstaltungId, string VeranstaltungTitel, DateTime VeranstaltungZeitpunkt, string SpielstaetteName, string RaumName, string Name, string Email, string Status, decimal Gesamtpreis, List<BuchungspositionDto> Positionen);
    private sealed record BuchungspositionRequestDto(string SitzplatzCode, string PreiskategorieId);
    private sealed record BuchungAnlegenRequestDto(string Name, string Email, List<BuchungspositionRequestDto> Positionen);

    public async Task<IReadOnlyList<VeranstaltungUebersicht>> ListeVeranstaltungen(
        DateTime? vonDatum = null, DateTime? bisDatum = null, string? spielstaette = null)
    {
        var query = new List<string>();
        if (vonDatum is not null) query.Add($"von={vonDatum:yyyy-MM-dd}");
        if (bisDatum is not null) query.Add($"bis={bisDatum:yyyy-MM-dd}");
        var url = "veranstaltungen" + (query.Count > 0 ? "?" + string.Join("&", query) : "");

        var liste = await Http.GetFromJsonAsync<List<VeranstaltungListeDto>>(url) ?? [];
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
        var plan = await Http.GetFromJsonAsync<SitzplanDto>($"veranstaltungen/{veranstaltungId}/sitzplan")
            ?? throw new InvalidOperationException($"Sitzplan fuer '{veranstaltungId}' nicht gefunden.");

        var positionen = new List<SitzplanPosition>();
        foreach (var reihe in plan.Reihen)
        {
            foreach (var pos in reihe.Positionen)
            {
                if (IstGangTyp(pos.Typ))
                {
                    positionen.Add(new SitzplanPosition(reihe.Reihe, pos.Spalte, PositionsTyp.Gang, null));
                    continue;
                }
                var status = pos.Status is not null && IstBelegtStatus(pos.Status) ? PositionsStatus.Belegt : PositionsStatus.Frei;
                positionen.Add(new SitzplanPosition(reihe.Reihe, pos.Spalte, PositionsTyp.Sitz, status));
            }
        }
        var spaltenAnzahl = plan.Reihen.Count > 0 ? plan.Reihen[0].Positionen.Count : 0;
        return new Sitzplan(plan.Reihen.Count, spaltenAnzahl, positionen);
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
        var plan = await Http.GetFromJsonAsync<SitzplanDto>($"veranstaltungen/{veranstaltungId}/sitzplan")
            ?? throw new InvalidOperationException($"Sitzplan fuer '{veranstaltungId}' nicht gefunden.");

        var positionen = auswahl.Select(a =>
        {
            var (reihe, spalte) = ZerlegeSitzplatzBezeichnung(a.SitzplatzBezeichnung);
            var reiheDto = plan.Reihen.SingleOrDefault(r => r.Reihe == reihe)
                ?? throw new InvalidOperationException($"Reihe '{reihe}' nicht gefunden.");
            var pos = reiheDto.Positionen.SingleOrDefault(p => p.Spalte == spalte)
                ?? throw new InvalidOperationException($"Sitzplatz '{a.SitzplatzBezeichnung}' nicht gefunden.");
            var kategorie = v.Preiskategorien.SingleOrDefault(k => k.Name == a.KategorieBezeichnung)
                ?? throw new InvalidOperationException($"Preiskategorie '{a.KategorieBezeichnung}' nicht gefunden.");
            return new BuchungspositionRequestDto(pos.Code ?? a.SitzplatzBezeichnung, kategorie.Id);
        }).ToList();

        var antwort = await Http.PostAsJsonAsync($"veranstaltungen/{veranstaltungId}/buchungen", new BuchungAnlegenRequestDto(name, email, positionen));
        if (!antwort.IsSuccessStatusCode) throw new BuchungAbgelehntException();

        var buchung = await antwort.Content.ReadFromJsonAsync<BuchungDetailDto>()
            ?? throw new InvalidOperationException("Leere Antwort bei Buchungserstellung.");
        return buchung.Referenz;
    }

    public async Task<Buchung> HoleBuchung(string referenz)
    {
        var b = await Http.GetFromJsonAsync<BuchungDetailDto>($"buchungen/{referenz}")
            ?? throw new InvalidOperationException($"Buchung '{referenz}' nicht gefunden.");
        return UebersetzeBuchung(b);
    }

    public async Task StorniereBuchung(string referenz)
    {
        var antwort = await Http.PostAsync($"buchungen/{referenz}/stornierung", null);
        antwort.EnsureSuccessStatusCode();
    }

    private async Task<VeranstaltungDetailDto> HoleDetail(string id)
        => await Http.GetFromJsonAsync<VeranstaltungDetailDto>($"veranstaltungen/{id}")
           ?? throw new InvalidOperationException($"Veranstaltung '{id}' nicht gefunden.");

    private static Buchung UebersetzeBuchung(BuchungDetailDto b) => new(
        b.Referenz,
        b.Positionen.Select(p => new Buchungsposition(p.SitzplatzCode, p.PreiskategorieName, p.PreisSnapshot)).ToList(),
        b.Gesamtpreis,
        b.Status);
}
