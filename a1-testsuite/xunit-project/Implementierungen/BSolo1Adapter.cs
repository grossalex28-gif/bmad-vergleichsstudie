using System.Net.Http.Json;
using System.Text.Json;
using A1Testsuite.Treiber;

namespace A1Testsuite.Implementierungen;

// Adapter fuer b_solo_1, gebaut ausschliesslich anhand von
// a1-testsuite/openapi/b_solo_1.json. Kein serverseitiger Warenkorb-
// Endpunkt -- LokalerWarenkorb.cs simuliert (siehe deren Kopfkommentar).
// Kein Eigenschaftsfilter-Parameter auf GET /api/products vorhanden --
// Filterung nach Eigenschaft (TF-B-F6) laeuft deshalb clientseitig ueber
// das eigenschaften-Feld, das jedes Produkt in der Liste ohnehin mitliefert
// (reine Filterung echter SUT-Daten, keine Fachlogik). "sort" ist nur als
// "type: string" ohne zulaessige Werte dokumentiert -- Konvention
// "<feld>" fuer aufsteigend, "-<feld>" fuer absteigend angenommen
// (unverifiziert, siehe Uebergabedokument). HoleBestellungen: siehe
// Kopfkommentar in BBmad2Adapter.cs.
public sealed class BSolo1Adapter : HttpAdapterBasis, IProjektBTreiber
{
    public BSolo1Adapter(string basisUrl) : base(basisUrl) { }

    private readonly LokalerWarenkorb _warenkorb = new();
    private readonly List<Bestellung> _angelegteBestellungen = [];

    private sealed record SubcategoryDto(string Id, string Name, List<string>? Eigenschaften);
    private sealed record CategoryDto(string Id, string Name, List<SubcategoryDto> Unterkategorien);
    private sealed record ProductListItemDto(string Id, string Name, string Beschreibung, string KategorieId, string KategorieName, string UnterkategorieId, string UnterkategorieName, Dictionary<string, JsonElement> Eigenschaften, decimal? MinPreis, double? DurchschnittsBewertung, int AnzahlBewertungen, int Aufrufe);
    private sealed record PagedResultDto(List<ProductListItemDto> Items, int Page, int PageSize, int TotalCount, int TotalPages);
    private sealed record OfferDto(string LieferantId, string LieferantName, decimal Preis);
    private sealed record ProductDetailDto(string Id, string Name, string Beschreibung, string KategorieId, string KategorieName, string UnterkategorieId, string UnterkategorieName, Dictionary<string, JsonElement> Eigenschaften, double? DurchschnittsBewertung, int AnzahlBewertungen, int Aufrufe, List<OfferDto> Angebote);
    private sealed record LieferdatenDto(string Name, string Strasse, string Plz, string Ort, string Land);
    private sealed record KontaktdatenDto(string Email, string? Telefon);
    private sealed record PositionCreateDto(string ProduktId, string LieferantId, int Menge);
    private sealed record OrderCreateDto(LieferdatenDto Lieferdaten, KontaktdatenDto Kontakt, List<PositionCreateDto> Positionen);
    private sealed record OrderItemResponseDto(string ProduktId, string ProduktName, string LieferantId, string LieferantName, decimal Preis, int Menge, decimal Zwischensumme);
    private sealed record OrderResponseDto(int Id, string Status, DateTime ErstelltAm, LieferdatenDto Lieferdaten, KontaktdatenDto Kontakt, List<OrderItemResponseDto> Positionen, decimal Gesamtsumme);
    private sealed record RatingCreateDto(string AutorName, int Wert);

    public Task<string> NeueWarenkorbSitzung() => Task.FromResult(_warenkorb.NeueSitzung());

    public async Task<ProduktlisteErgebnis> ListeProdukte(
        int seite, string? kategorieId = null, string? sortierung = null, IReadOnlyDictionary<string, string>? eigenschaftsfilter = null)
    {
        // kategorieId traegt den fachlichen (Unter-)Kategorienamen, keine
        // technische ID (Abschnitt 7 der Architektur). Es gibt keinen
        // Eigenschaftsfilter-Parameter auf dem Endpunkt -- beide Filter
        // laufen deshalb einheitlich clientseitig ueber die Listenantwort,
        // die Kategorienamen und Eigenschaften bereits mitliefert.
        if (kategorieId is not null || (eigenschaftsfilter is not null && eigenschaftsfilter.Count > 0))
        {
            var alle = await AlleSeitenLaden(sortierung);
            var gefiltert = alle
                .Where(p => kategorieId is null || p.UnterkategorieName == kategorieId || p.KategorieName == kategorieId)
                .Where(p => eigenschaftsfilter is null || eigenschaftsfilter.All(f =>
                    p.Eigenschaften.TryGetValue(f.Key, out var wert) && JsonElementAlsText(wert) == f.Value))
                .ToList();
            var produkteF = gefiltert.Select(Uebersetze).ToList();
            return new ProduktlisteErgebnis(produkteF, 1, 1, produkteF.Count);
        }

        var antwort = await Http.GetFromJsonAsync<PagedResultDto>($"api/products?{BaueQuery(seite, sortierung, null)}")
            ?? throw new InvalidOperationException("Leere Produktlisten-Antwort.");
        return new ProduktlisteErgebnis(antwort.Items.Select(Uebersetze).ToList(), antwort.Page, antwort.TotalPages, antwort.TotalCount);
    }

    public async Task<IReadOnlyList<Oberkategorie>> HoleKategoriebaum()
    {
        var kategorien = await Http.GetFromJsonAsync<List<CategoryDto>>("api/categories") ?? [];
        return kategorien.Select(k => new Oberkategorie(k.Name, k.Unterkategorien.Select(u => new Unterkategorie(u.Name)).ToList())).ToList();
    }

    public async Task<IReadOnlyList<ProduktUebersicht>> SucheProdukte(string suchtext)
    {
        var antwort = await Http.GetFromJsonAsync<PagedResultDto>($"api/products?search={Uri.EscapeDataString(suchtext)}")
            ?? throw new InvalidOperationException("Leere Produktlisten-Antwort.");
        return antwort.Items.Select(Uebersetze).ToList();
    }

    public async Task<ProduktDetail> HoleProdukt(string id)
    {
        var p = await Http.GetFromJsonAsync<ProductDetailDto>($"api/products/{id}")
            ?? throw new InvalidOperationException($"Produkt '{id}' nicht gefunden.");
        return new ProduktDetail(
            p.Id, p.Name, p.Beschreibung, p.UnterkategorieName,
            p.Eigenschaften.ToDictionary(e => e.Key, e => JsonElementAlsText(e.Value)),
            p.Angebote.Select(o => new LieferantenAngebot(o.LieferantName, o.Preis)).ToList(),
            p.DurchschnittsBewertung, p.AnzahlBewertungen, p.Aufrufe);
    }

    public async Task LegeInWarenkorb(string sitzung, string produktId, string lieferantId, int menge)
    {
        var p = await Http.GetFromJsonAsync<ProductDetailDto>($"api/products/{produktId}")
            ?? throw new InvalidOperationException($"Produkt '{produktId}' nicht gefunden.");
        var angebot = p.Angebote.SingleOrDefault(o => o.LieferantName == lieferantId)
            ?? throw new WarenkorbAbgelehntException($"Lieferant '{lieferantId}' bietet Produkt '{produktId}' nicht an.");
        if (menge <= 0) throw new WarenkorbAbgelehntException("Menge muss positiv sein.");
        _warenkorb.Hinzufuegen(sitzung, produktId, lieferantId, angebot.LieferantId, angebot.Preis, menge);
    }

    public Task<Warenkorb> HoleWarenkorb(string sitzung) => Task.FromResult(_warenkorb.Hole(sitzung));

    public Task AendereMenge(string sitzung, string positionId, int menge)
    {
        _warenkorb.AendereMenge(sitzung, positionId, menge);
        return Task.CompletedTask;
    }

    public Task EntferneAusWarenkorb(string sitzung, string positionId)
    {
        _warenkorb.Entfernen(sitzung, positionId);
        return Task.CompletedTask;
    }

    public async Task<string> LegeBestellungAn(string sitzung, Lieferdaten lieferdaten, Kontaktdaten kontaktdaten)
    {
        var positionen = _warenkorb.Leeren(sitzung);
        if (positionen.Count == 0) throw new BestellungAbgelehntException("Warenkorb ist leer.");

        var request = new OrderCreateDto(
            new LieferdatenDto(lieferdaten.EmpfaengerName, lieferdaten.StrasseHausnummer, lieferdaten.Postleitzahl, lieferdaten.Ort, "Deutschland"),
            // Treiber-Interface Kontaktdaten kennt keine Telefonnummer (Architektur-
            // Abschnitt 5); dieser SUT verlangt serverseitig aber ein nicht-leeres
            // Telefon-Feld (OrdersController.CreateOrder), sonst 400 fuer jede
            // Bestellung. Platzhalter, analog zum bereits vorhandenen fest
            // codierten Land "Deutschland" in dieser Methode. Echter Adapterfehler
            // (fehlendes Pflichtfeld), kein SUT-Befund -- siehe Uebergabedokument.
            new KontaktdatenDto(kontaktdaten.Email, "000000000"),
            positionen.Select(p => new PositionCreateDto(p.ProduktId, p.LieferantTechnischeId, p.Menge)).ToList());

        var antwort = await Http.PostAsJsonAsync("api/orders", request);
        if (!antwort.IsSuccessStatusCode) throw new BestellungAbgelehntException();

        var order = await antwort.Content.ReadFromJsonAsync<OrderResponseDto>() ?? throw new InvalidOperationException("Leere Bestell-Antwort.");
        _angelegteBestellungen.Add(UebersetzeBestellung(order));
        return order.Id.ToString();
    }

    public Task<IReadOnlyList<Bestellung>> HoleBestellungen(Kontaktdaten kontakt)
        => Task.FromResult<IReadOnlyList<Bestellung>>(_angelegteBestellungen);

    public async Task BewerteProdukt(string produktId, int wertung, string autor)
    {
        var antwort = await Http.PostAsJsonAsync($"api/products/{produktId}/ratings", new RatingCreateDto(autor, wertung));
        antwort.EnsureSuccessStatusCode();
    }

    private async Task<List<ProductListItemDto>> AlleSeitenLaden(string? sortierung)
    {
        var erste = await Http.GetFromJsonAsync<PagedResultDto>($"api/products?{BaueQuery(1, sortierung, null)}")
            ?? throw new InvalidOperationException("Leere Produktlisten-Antwort.");
        var alle = new List<ProductListItemDto>(erste.Items);
        for (var seite = 2; seite <= erste.TotalPages; seite++)
        {
            var naechste = await Http.GetFromJsonAsync<PagedResultDto>($"api/products?{BaueQuery(seite, sortierung, null)}")
                ?? throw new InvalidOperationException("Leere Produktlisten-Antwort.");
            alle.AddRange(naechste.Items);
        }
        return alle;
    }

    // 31.08.2026: JsonElement.ToString() liefert fuer boolesche Werte die
    // .NET-Konvention "True"/"False" (grossgeschrieben), nicht die JSON-
    // Konvention "true"/"false" -- ein reiner Adapter-Konvertierungsfehler,
    // der TF_B_F6_1 (Eigenschaftsfilter, leere Treffermenge) und TF_B_F7_1
    // (Detailansicht, "Kabellos" nicht in der Toleranzliste) verursacht
    // hatte. Kein SUT-Befund, siehe Uebergabedokument.
    private static string JsonElementAlsText(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.String => element.GetString() ?? "",
        JsonValueKind.Null => "",
        _ => element.GetRawText()
    };

    private static string BaueQuery(int seite, string? sortierung, string? suchtext)
    {
        var query = new List<string> { $"page={seite}" };
        if (suchtext is not null) query.Add($"search={Uri.EscapeDataString(suchtext)}");
        // 31.08.2026: urspruengliche Annahme ("<feld>"/"-<feld>") unverifiziert und falsch --
        // Server (ProductsController.ParseSort) erwartet feste Werte "price_asc"/"name_desc"/
        // "views_desc" (Quellcode direkt eingesehen), jeder unbekannte Wert faellt still auf
        // NameAsc zurueck. Echter Adapterfehler, kein SUT-Befund -- siehe Uebergabedokument.
        if (sortierung == Sortierungen.PreisAufsteigend) query.Add("sort=price_asc");
        else if (sortierung == Sortierungen.NameAbsteigend) query.Add("sort=name_desc");
        else if (sortierung == Sortierungen.AufrufzaehlerAbsteigend) query.Add("sort=views_desc");
        return string.Join("&", query);
    }

    private static ProduktUebersicht Uebersetze(ProductListItemDto p) => new(p.Id, p.Name, p.MinPreis ?? 0m, p.KategorieName);

    private static Bestellung UebersetzeBestellung(OrderResponseDto order) => new(
        order.Id.ToString(), order.Status,
        order.Positionen.Select(p => new Bestellposition(p.ProduktId, p.LieferantName, p.Preis, p.Menge)).ToList(),
        order.Gesamtsumme);
}
