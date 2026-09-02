using System.Net.Http.Json;
using A1Testsuite.Treiber;

namespace A1Testsuite.Implementierungen;

// Adapter fuer a_bmad_1, gebaut ausschliesslich anhand von
// a1-testsuite/openapi/a_bmad_1.json (Abschnitt 6 der Akzeptanztest-
// Architektur). Reine HTTP-Uebersetzung, siehe README.md fuer die beiden
// dokumentierten Abweichungen, die alle zwoelf bzw. alle sechs
// Projekt-A-Adapter gleichermassen betreffen:
//   1. BerechnePreis: es gibt keinen serverseitigen Vorschau-Endpunkt, nur
//      die Buchungserstellung liefert einen Gesamtpreis. Der Adapter
//      summiert deshalb lokal ueber die vom Server gelieferten
//      Preiskategorien (echte SUT-Daten, reine Arithmetik).
//   2. SeatStatus ist in der OpenAPI-Beschreibung nur als "type: integer"
//      dokumentiert (Schema-Ref ohne enum/x-enumNames). Die Live-Stichprobe
//      (a1-testsuite/openapi-samples/a_bmad_1_sitzplan.json) zeigt aber,
//      dass der Server tatsaechlich Strings sendet ("Free"/"Occupied") --
//      vermutlich JsonStringEnumConverter, vom OpenAPI-Generator nicht
//      erkannt. Die OpenAPI-Beschreibung ist hier also irrefuehrend; der
//      Adapter richtet sich nach der tatsaechlichen Live-Antwort und nutzt
//      IstBelegtStatus() (HttpAdapterBasis.cs) statt eines Zahlenvergleichs.
public sealed class ABmad1Adapter : HttpAdapterBasis, IProjektATreiber
{
    public ABmad1Adapter(string basisUrl) : base(basisUrl) { }

    private sealed record EventListItemDto(string Id, string Title, string VenueName, DateTime StartsAt);
    private sealed record EventDetailDto(string Id, string Title, string Description, int DurationMinutes, int AgeRating, string VenueName, string RoomName, DateTime StartsAt);
    private sealed record VenueListItemDto(string Id, string Name);
    private sealed record PriceCategoryDto(string Id, string Name, decimal Price);
    private sealed record SeatDto(string SeatId, string Row, int Column, string Status);
    private sealed record SeatMapDto(string EventId, List<string> Rows, int Columns, List<int> AisleColumns, List<SeatDto> Seats, List<PriceCategoryDto> PriceCategories);
    private sealed record BookingSeatDto(string SeatId, string Row, int Column, string PriceCategoryId, string PriceCategoryName, decimal Price);
    private sealed record BookingDto(string Reference, string EventId, string EventTitle, string VenueName, DateTime StartsAt, string Status, List<BookingSeatDto> Seats, decimal TotalPrice);
    private sealed record CreateBookingSeatRequestDto(string SeatId, string PriceCategoryId);
    private sealed record CreateBookingRequestDto(string EventId, string Name, string Email, List<CreateBookingSeatRequestDto> Seats);

    public async Task<IReadOnlyList<VeranstaltungUebersicht>> ListeVeranstaltungen(
        DateTime? vonDatum = null, DateTime? bisDatum = null, string? spielstaette = null)
    {
        var query = new List<string>();
        if (vonDatum is not null) query.Add($"from={vonDatum:yyyy-MM-dd}");
        if (bisDatum is not null) query.Add($"to={bisDatum:yyyy-MM-dd}");
        var url = "api/events" + (query.Count > 0 ? "?" + string.Join("&", query) : "");

        var events = await Http.GetFromJsonAsync<List<EventListItemDto>>(url) ?? [];
        // Raum ist in der Listenantwort nicht enthalten (nur in der
        // Detailantwort), bleibt hier leer -- kein Testfall prueft Raum in
        // der Veranstaltungsuebersicht.
        return events
            .Where(e => spielstaette is null || e.VenueName == spielstaette)
            .Select(e => new VeranstaltungUebersicht(e.Id, e.Title, e.VenueName, "", e.StartsAt))
            .ToList();
    }

    public async Task<VeranstaltungDetail> HoleVeranstaltung(string id)
    {
        var e = await Http.GetFromJsonAsync<EventDetailDto>($"api/events/{id}")
            ?? throw new InvalidOperationException($"Veranstaltung '{id}' nicht gefunden.");
        return new VeranstaltungDetail(e.Id, e.Description, e.DurationMinutes, e.AgeRating, e.VenueName, e.RoomName, e.StartsAt);
    }

    public async Task<Sitzplan> HoleSitzplan(string veranstaltungId)
    {
        var map = await HoleSeatMap(veranstaltungId);
        var positionen = new List<SitzplanPosition>();
        for (var spalte = 1; spalte <= map.Columns; spalte++)
        {
            foreach (var reihe in map.Rows)
            {
                if (map.AisleColumns.Contains(spalte))
                {
                    positionen.Add(new SitzplanPosition(reihe, spalte, PositionsTyp.Gang, null));
                    continue;
                }
                var sitz = map.Seats.SingleOrDefault(s => s.Row == reihe && s.Column == spalte);
                var status = sitz is not null && IstBelegtStatus(sitz.Status) ? PositionsStatus.Belegt : PositionsStatus.Frei;
                positionen.Add(new SitzplanPosition(reihe, spalte, PositionsTyp.Sitz, status));
            }
        }
        return new Sitzplan(map.Rows.Count, map.Columns, positionen);
    }

    public async Task<IReadOnlyList<Preiskategorie>> HolePreiskategorien(string veranstaltungId)
    {
        var map = await HoleSeatMap(veranstaltungId);
        return map.PriceCategories.Select(p => new Preiskategorie(p.Id, p.Name, p.Price)).ToList();
    }

    public async Task<decimal> BerechnePreis(string veranstaltungId, IReadOnlyList<SitzplatzAuswahl> auswahl)
    {
        // Kein Vorschau-Endpunkt vorhanden, siehe Kopfkommentar Punkt 1.
        var kategorien = await HolePreiskategorien(veranstaltungId);
        return auswahl.Sum(a =>
        {
            var k = kategorien.SingleOrDefault(k => k.Bezeichnung == a.KategorieBezeichnung);
            return k?.Preis ?? throw new InvalidOperationException($"Preiskategorie '{a.KategorieBezeichnung}' nicht gefunden.");
        });
    }

    public async Task<string> LegeBuchungAn(string veranstaltungId, IReadOnlyList<SitzplatzAuswahl> auswahl, string name, string email)
    {
        var map = await HoleSeatMap(veranstaltungId);
        var seats = auswahl.Select(a =>
        {
            var (reihe, spalte) = ZerlegeSitzplatzBezeichnung(a.SitzplatzBezeichnung);
            var seat = map.Seats.SingleOrDefault(s => s.Row == reihe && s.Column == spalte)
                ?? throw new InvalidOperationException($"Sitzplatz '{a.SitzplatzBezeichnung}' nicht gefunden.");
            var kategorie = map.PriceCategories.SingleOrDefault(k => k.Name == a.KategorieBezeichnung)
                ?? throw new InvalidOperationException($"Preiskategorie '{a.KategorieBezeichnung}' nicht gefunden.");
            return new CreateBookingSeatRequestDto(seat.SeatId, kategorie.Id);
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

    private async Task<SeatMapDto> HoleSeatMap(string veranstaltungId)
        => await Http.GetFromJsonAsync<SeatMapDto>($"api/events/{veranstaltungId}/sitzplan")
           ?? throw new InvalidOperationException($"Sitzplan fuer '{veranstaltungId}' nicht gefunden.");

    private static Buchung UebersetzeBuchung(BookingDto b) => new(
        b.Reference,
        b.Seats.Select(s => new Buchungsposition($"{s.Row}{s.Column}", s.PriceCategoryName, s.Price)).ToList(),
        b.TotalPrice,
        b.Status);
}
