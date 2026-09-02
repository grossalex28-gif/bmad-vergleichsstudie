using System.Net.Http.Json;
using A1Testsuite.Treiber;

namespace A1Testsuite.Implementierungen;

// Adapter fuer a_solo_2, gebaut ausschliesslich anhand von
// a1-testsuite/openapi/a_solo_2.json. BerechnePreis: siehe Kopfkommentar in
// ABmad1Adapter.cs, Punkt 1. Sitzplatztyp/-status nur als "type: string"
// ohne dokumentierte Werte -- Deutung ueber IstGangTyp/IstBelegtStatus.
// BookingDto hat kein Status-Textfeld, nur "isCancelled" (bool) -- wird auf
// die geforderten nichtleeren Statustexte "Aktiv"/"Storniert" uebersetzt
// (Buchung.Status wird von den Testfaellen nur auf Nichtleere geprueft,
// nicht auf einen bestimmten Wortlaut, Abschnitt 7 der Architektur).
public sealed class ASolo2Adapter : HttpAdapterBasis, IProjektATreiber
{
    public ASolo2Adapter(string basisUrl) : base(basisUrl) { }

    private sealed record EventListItemDto(string Id, string Title, string VenueId, string VenueName, DateTime StartsAt);
    private sealed record PriceCategoryDto(string Id, string Name, decimal Price);
    private sealed record EventDetailDto(string Id, string Title, string Description, int DurationMinutes, int AgeRating, string VenueId, string VenueName, string RoomId, string RoomName, DateTime StartsAt, List<PriceCategoryDto> PriceCategories);
    private sealed record SeatDto(int Column, string Type, string Status);
    private sealed record SeatRowDto(string Row, List<SeatDto> Seats);
    private sealed record SeatMapDto(string EventId, string RoomId, string RoomName, List<string> RowLabels, int Columns, List<SeatRowDto> Rows);
    private sealed record BookingSeatDto(string Row, int Column, string PriceCategoryName, decimal Price);
    private sealed record BookingDto(string Reference, string EventId, string EventTitle, DateTime EventStartsAt, string CustomerName, string CustomerEmail, DateTime CreatedAt, bool IsCancelled, decimal TotalPrice, List<BookingSeatDto> Seats);
    private sealed record CreateBookingSeatDto(string Row, int Column, string PriceCategoryId);
    private sealed record CreateBookingRequestDto(string EventId, string CustomerName, string CustomerEmail, List<CreateBookingSeatDto> Seats);

    public async Task<IReadOnlyList<VeranstaltungUebersicht>> ListeVeranstaltungen(
        DateTime? vonDatum = null, DateTime? bisDatum = null, string? spielstaette = null)
    {
        var query = new List<string>();
        if (vonDatum is not null) query.Add($"from={vonDatum:yyyy-MM-ddTHH:mm:ss}");
        if (bisDatum is not null) query.Add($"to={bisDatum:yyyy-MM-ddTHH:mm:ss}");
        var url = "api/events" + (query.Count > 0 ? "?" + string.Join("&", query) : "");

        var events = await Http.GetFromJsonAsync<List<EventListItemDto>>(url) ?? [];
        return events
            .Where(e => spielstaette is null || e.VenueName == spielstaette)
            .Select(e => new VeranstaltungUebersicht(e.Id, e.Title, e.VenueName, "", e.StartsAt))
            .ToList();
    }

    public async Task<VeranstaltungDetail> HoleVeranstaltung(string id)
    {
        var e = await HoleDetail(id);
        return new VeranstaltungDetail(e.Id, e.Description, e.DurationMinutes, e.AgeRating, e.VenueName, e.RoomName, e.StartsAt);
    }

    public async Task<Sitzplan> HoleSitzplan(string veranstaltungId)
    {
        var map = await Http.GetFromJsonAsync<SeatMapDto>($"api/events/{veranstaltungId}/seatmap")
            ?? throw new InvalidOperationException($"Sitzplan fuer '{veranstaltungId}' nicht gefunden.");

        var positionen = new List<SitzplanPosition>();
        foreach (var row in map.Rows)
        {
            foreach (var sitz in row.Seats)
            {
                if (IstGangTyp(sitz.Type))
                {
                    positionen.Add(new SitzplanPosition(row.Row, sitz.Column, PositionsTyp.Gang, null));
                    continue;
                }
                var status = IstBelegtStatus(sitz.Status) ? PositionsStatus.Belegt : PositionsStatus.Frei;
                positionen.Add(new SitzplanPosition(row.Row, sitz.Column, PositionsTyp.Sitz, status));
            }
        }
        return new Sitzplan(map.RowLabels.Count, map.Columns, positionen);
    }

    public async Task<IReadOnlyList<Preiskategorie>> HolePreiskategorien(string veranstaltungId)
    {
        var e = await HoleDetail(veranstaltungId);
        return e.PriceCategories.Select(p => new Preiskategorie(p.Id, p.Name, p.Price)).ToList();
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
        var seats = auswahl.Select(a =>
        {
            var (reihe, spalte) = ZerlegeSitzplatzBezeichnung(a.SitzplatzBezeichnung);
            var kategorie = e.PriceCategories.SingleOrDefault(k => k.Name == a.KategorieBezeichnung)
                ?? throw new InvalidOperationException($"Preiskategorie '{a.KategorieBezeichnung}' nicht gefunden.");
            return new CreateBookingSeatDto(reihe, spalte, kategorie.Id);
        }).ToList();

        var antwort = await Http.PostAsJsonAsync("api/bookings", new CreateBookingRequestDto(veranstaltungId, name, email, seats));
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
        b.Seats.Select(s => new Buchungsposition($"{s.Row}{s.Column}", s.PriceCategoryName, s.Price)).ToList(),
        b.TotalPrice,
        b.IsCancelled ? "Storniert" : "Aktiv");
}
