using System.Net.Http.Json;
using A1Testsuite.Treiber;

namespace A1Testsuite.Implementierungen;

// Adapter fuer a_bmad_2, gebaut ausschliesslich anhand von
// a1-testsuite/openapi/a_bmad_2.json. BerechnePreis: siehe Kopfkommentar in
// ABmad1Adapter.cs, Punkt 1. Sitzplatz-Status (SeatMapCellDto.status) ist
// nur als "type: string" ohne dokumentierte Werte vorhanden -- Deutung
// ueber HttpAdapterBasis.IstBelegtStatus (deutsch/englisch robust).
public sealed class ABmad2Adapter : HttpAdapterBasis, IProjektATreiber
{
    public ABmad2Adapter(string basisUrl) : base(basisUrl) { }

    private sealed record EventSummaryDto(int Id, string Titel, string Spielstaette, DateTime Zeitpunkt);
    private sealed record PriceCategoryDto(int Id, string Name, decimal Preis);
    private sealed record EventDetailDto(int Id, string Titel, string Beschreibung, int DauerMinuten, int Altersfreigabe, string Spielstaette, string Raum, DateTime Zeitpunkt, List<PriceCategoryDto> Preiskategorien);
    private sealed record SeatMapCellDto(int ColumnNumber, string Status);
    private sealed record SeatMapRowDto(string RowLabel, List<SeatMapCellDto> Cells);
    private sealed record SeatMapDto(List<string> RowLabels, int ColumnCount, List<int> AisleColumns, List<SeatMapRowDto> Rows);
    private sealed record BookingPositionDto(string RowLabel, int ColumnNumber, PriceCategoryDto Preiskategorie);
    private sealed record BookingDto(string Reference, string Name, string Status, List<BookingPositionDto> Positionen, decimal Gesamtpreis);
    private sealed record CreateBookingPositionRequestDto(string RowLabel, int ColumnNumber, int PriceCategoryId);
    private sealed record CreateBookingRequestDto(int EventId, string Name, string Email, List<CreateBookingPositionRequestDto> Positionen);

    public async Task<IReadOnlyList<VeranstaltungUebersicht>> ListeVeranstaltungen(
        DateTime? vonDatum = null, DateTime? bisDatum = null, string? spielstaette = null)
    {
        var query = new List<string>();
        if (vonDatum is not null) query.Add($"von={vonDatum:yyyy-MM-dd}");
        if (bisDatum is not null) query.Add($"bis={bisDatum:yyyy-MM-dd}");
        var url = "api/events" + (query.Count > 0 ? "?" + string.Join("&", query) : "");

        var events = await Http.GetFromJsonAsync<List<EventSummaryDto>>(url) ?? [];
        return events
            .Where(e => spielstaette is null || e.Spielstaette == spielstaette)
            .Select(e => new VeranstaltungUebersicht(e.Id.ToString(), e.Titel, e.Spielstaette, "", e.Zeitpunkt))
            .ToList();
    }

    public async Task<VeranstaltungDetail> HoleVeranstaltung(string id)
    {
        var e = await HoleDetail(id);
        return new VeranstaltungDetail(e.Id.ToString(), e.Beschreibung, e.DauerMinuten, e.Altersfreigabe, e.Spielstaette, e.Raum, e.Zeitpunkt);
    }

    public async Task<Sitzplan> HoleSitzplan(string veranstaltungId)
    {
        var map = await Http.GetFromJsonAsync<SeatMapDto>($"api/events/{veranstaltungId}/seatmap")
            ?? throw new InvalidOperationException($"Sitzplan fuer '{veranstaltungId}' nicht gefunden.");

        var positionen = new List<SitzplanPosition>();
        foreach (var row in map.Rows)
        {
            foreach (var zelle in row.Cells)
            {
                if (map.AisleColumns.Contains(zelle.ColumnNumber))
                {
                    positionen.Add(new SitzplanPosition(row.RowLabel, zelle.ColumnNumber, PositionsTyp.Gang, null));
                    continue;
                }
                var status = IstBelegtStatus(zelle.Status) ? PositionsStatus.Belegt : PositionsStatus.Frei;
                positionen.Add(new SitzplanPosition(row.RowLabel, zelle.ColumnNumber, PositionsTyp.Sitz, status));
            }
        }
        return new Sitzplan(map.RowLabels.Count, map.ColumnCount, positionen);
    }

    public async Task<IReadOnlyList<Preiskategorie>> HolePreiskategorien(string veranstaltungId)
    {
        var e = await HoleDetail(veranstaltungId);
        return e.Preiskategorien.Select(p => new Preiskategorie(p.Id.ToString(), p.Name, p.Preis)).ToList();
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
        var e = await HoleDetail(veranstaltungId);
        var positionen = auswahl.Select(a =>
        {
            var (reihe, spalte) = ZerlegeSitzplatzBezeichnung(a.SitzplatzBezeichnung);
            var kategorie = e.Preiskategorien.SingleOrDefault(k => k.Name == a.KategorieBezeichnung)
                ?? throw new InvalidOperationException($"Preiskategorie '{a.KategorieBezeichnung}' nicht gefunden.");
            return new CreateBookingPositionRequestDto(reihe, spalte, kategorie.Id);
        }).ToList();

        var antwort = await Http.PostAsJsonAsync("api/bookings", new CreateBookingRequestDto(int.Parse(veranstaltungId), name, email, positionen));
        if (!antwort.IsSuccessStatusCode) throw new BuchungAbgelehntException();

        var buchung = await antwort.Content.ReadFromJsonAsync<BookingDto>()
            ?? throw new InvalidOperationException("Leere Antwort bei Buchungserstellung.");
        return buchung.Reference;
    }

    public async Task<Buchung> HoleBuchung(string referenz)
    {
        var b = await Http.GetFromJsonAsync<BookingDto>($"api/bookings/{referenz}")
            ?? throw new InvalidOperationException($"Buchung '{referenz}' nicht gefunden.");
        return UebersetzeBuchung(b);
    }

    public async Task StorniereBuchung(string referenz)
    {
        var antwort = await Http.PostAsync($"api/bookings/{referenz}/cancel", null);
        antwort.EnsureSuccessStatusCode();
    }

    private async Task<EventDetailDto> HoleDetail(string id)
        => await Http.GetFromJsonAsync<EventDetailDto>($"api/events/{id}")
           ?? throw new InvalidOperationException($"Veranstaltung '{id}' nicht gefunden.");

    private static Buchung UebersetzeBuchung(BookingDto b) => new(
        b.Reference,
        b.Positionen.Select(p => new Buchungsposition($"{p.RowLabel}{p.ColumnNumber}", p.Preiskategorie.Name, p.Preiskategorie.Preis)).ToList(),
        b.Gesamtpreis,
        b.Status);
}
