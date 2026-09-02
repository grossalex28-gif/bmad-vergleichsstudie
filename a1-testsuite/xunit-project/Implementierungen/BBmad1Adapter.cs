using System.Net.Http.Json;
using A1Testsuite.Treiber;

namespace A1Testsuite.Implementierungen;

// Adapter fuer b_bmad_1, gebaut ausschliesslich anhand von
// a1-testsuite/openapi/b_bmad_1.json. Kein serverseitiger Warenkorb-
// Endpunkt vorhanden -- LokalerWarenkorb.cs simuliert Warenkorb-Operationen,
// nur LegeBestellungAn ruft echt POST /api/orders auf (siehe deren
// Kopfkommentar). sortBy/sortDir sind nur als "type: string" dokumentiert;
// sortBy=lowestPrice/name (entspricht dem tatsaechlichen JSON-Feldnamen in
// ProductListItemDto) und sortDir=asc/desc angenommen. Der "property"-
// Array-Filter fuer Eigenschaften ist nur als "array of string" ohne
// Kodierung dokumentiert -- "Name:Wert" je Eintrag angenommen (unverifiziert,
// siehe Uebergabedokument). HoleBestellungen: siehe Kopfkommentar in
// BBmad2Adapter.cs (kein Listing-Endpunkt, lokal zwischengespeichert).
// ProductDetailDto hat kein Aufrufzaehler-Feld -- HoleProdukt liefert dafuer
// fest 0, TF_B_F13_1_und_2 (Aufrufzaehler steigt) schlaegt damit fuer diese
// Implementierung erwartungsgemaess fehl (echte Deckungsluecke der SUT, kein
// Adapterfehler; siehe Uebergabedokument).
public sealed class BBmad1Adapter : HttpAdapterBasis, IProjektBTreiber
{
    public BBmad1Adapter(string basisUrl) : base(basisUrl) { }

    private readonly LokalerWarenkorb _warenkorb = new();
    private readonly List<Bestellung> _angelegteBestellungen = [];

    private sealed record SubcategoryDto(string Id, string Name);
    private sealed record CategoryDto(string Id, string Name, List<SubcategoryDto> Subcategories);
    private sealed record ProductListItemDto(string Id, string Name, decimal? LowestPrice);
    private sealed record PagedResultDto(List<ProductListItemDto> Items, int TotalCount, int PageCount, int Page, int PageSize);
    private sealed record ProductPropertyDto(string Name, string Value);
    private sealed record ProductOfferDto(string SupplierId, string SupplierName, decimal Price);
    private sealed record ProductDetailDto(string Id, string Name, string Description, string CategoryId, string CategoryName, string SubcategoryId, string SubcategoryName, List<ProductPropertyDto> Properties, List<ProductOfferDto> Offers, double? AverageRating, int RatingCount);
    private sealed record DeliveryDto(string Name, string Street, string PostalCode, string City, string Country, string Email, string? Phone);
    private sealed record OrderItemCreateDto(string ProductId, string SupplierId, int Quantity);
    private sealed record OrderCreateDto(List<OrderItemCreateDto> Items, DeliveryDto Delivery);
    private sealed record OrderItemResponseDto(string ProductId, string ProductName, string SupplierId, string SupplierName, decimal UnitPrice, int Quantity, decimal LineTotal);
    private sealed record OrderDto(string PublicId, string Status, DeliveryDto Delivery, List<OrderItemResponseDto> Items, decimal TotalAmount, DateTime CreatedAt);
    private sealed record RatingCreateDto(string AuthorName, int Value);

    public Task<string> NeueWarenkorbSitzung() => Task.FromResult(_warenkorb.NeueSitzung());

    public async Task<ProduktlisteErgebnis> ListeProdukte(
        int seite, string? kategorieId = null, string? sortierung = null, IReadOnlyDictionary<string, string>? eigenschaftsfilter = null)
    {
        var query = new List<string> { $"page={seite}" };
        // kategorieId traegt hier den fachlichen Namen einer (Unter-)Kategorie
        // (Abschnitt 7 der Architektur, wie bei Preiskategorien/Lieferanten),
        // keine technische ID -- erst ueber den Kategoriebaum aufloesen.
        if (kategorieId is not null)
        {
            var (paramName, paramWert) = await LoeseKategorieAuf(kategorieId);
            query.Add($"{paramName}={Uri.EscapeDataString(paramWert)}");
        }
        if (sortierung == Sortierungen.PreisAufsteigend) { query.Add("sortBy=price"); query.Add("sortDir=asc"); }
        else if (sortierung == Sortierungen.NameAbsteigend) { query.Add("sortBy=name"); query.Add("sortDir=desc"); }
        else if (sortierung == Sortierungen.AufrufzaehlerAbsteigend) { query.Add("sortBy=viewCount"); query.Add("sortDir=desc"); }
        if (eigenschaftsfilter is not null)
            foreach (var (k, v) in eigenschaftsfilter)
                query.Add($"property={Uri.EscapeDataString($"{k}:{v}")}");

        var antwort = await Http.GetFromJsonAsync<PagedResultDto>($"api/products?{string.Join("&", query)}")
            ?? throw new InvalidOperationException("Leere Produktlisten-Antwort.");
        var produkte = antwort.Items.Select(p => new ProduktUebersicht(p.Id, p.Name, p.LowestPrice ?? 0m, "")).ToList();
        return new ProduktlisteErgebnis(produkte, antwort.Page, antwort.PageCount, antwort.TotalCount);
    }

    public async Task<IReadOnlyList<Oberkategorie>> HoleKategoriebaum()
    {
        var kategorien = await LadeKategorien();
        return kategorien.Select(k => new Oberkategorie(k.Name, k.Subcategories.Select(u => new Unterkategorie(u.Name)).ToList())).ToList();
    }

    private async Task<List<CategoryDto>> LadeKategorien()
        => await Http.GetFromJsonAsync<List<CategoryDto>>("api/categories") ?? [];

    /// <summary>
    /// Loest einen fachlichen (Unter-)Kategorienamen ueber den Kategoriebaum
    /// in den vom Server erwarteten Query-Parameter auf (Unterkategorie hat
    /// Vorrang, da die Testfaelle nur mit Unterkategorienamen filtern).
    /// </summary>
    private async Task<(string ParamName, string Wert)> LoeseKategorieAuf(string kategorieName)
    {
        var kategorien = await LadeKategorien();
        foreach (var k in kategorien)
        {
            var u = k.Subcategories.SingleOrDefault(u => u.Name == kategorieName);
            if (u is not null) return ("subcategoryId", u.Id);
        }
        var top = kategorien.SingleOrDefault(k => k.Name == kategorieName);
        if (top is not null) return ("categoryId", top.Id);
        throw new InvalidOperationException($"Kategorie '{kategorieName}' nicht im Kategoriebaum gefunden.");
    }

    public async Task<IReadOnlyList<ProduktUebersicht>> SucheProdukte(string suchtext)
    {
        var antwort = await Http.GetFromJsonAsync<PagedResultDto>($"api/products?q={Uri.EscapeDataString(suchtext)}")
            ?? throw new InvalidOperationException("Leere Produktlisten-Antwort.");
        return antwort.Items.Select(p => new ProduktUebersicht(p.Id, p.Name, p.LowestPrice ?? 0m, "")).ToList();
    }

    public async Task<ProduktDetail> HoleProdukt(string id)
    {
        var p = await Http.GetFromJsonAsync<ProductDetailDto>($"api/products/{id}")
            ?? throw new InvalidOperationException($"Produkt '{id}' nicht gefunden.");
        return new ProduktDetail(
            p.Id, p.Name, p.Description, p.SubcategoryName,
            p.Properties.ToDictionary(e => e.Name, e => e.Value),
            p.Offers.Select(o => new LieferantenAngebot(o.SupplierName, o.Price)).ToList(),
            p.AverageRating, p.RatingCount, 0);
    }

    public async Task LegeInWarenkorb(string sitzung, string produktId, string lieferantId, int menge)
    {
        var detail = await Http.GetFromJsonAsync<ProductDetailDto>($"api/products/{produktId}")
            ?? throw new InvalidOperationException($"Produkt '{produktId}' nicht gefunden.");
        var angebot = detail.Offers.SingleOrDefault(o => o.SupplierName == lieferantId)
            ?? throw new WarenkorbAbgelehntException($"Lieferant '{lieferantId}' bietet Produkt '{produktId}' nicht an.");
        if (menge <= 0) throw new WarenkorbAbgelehntException("Menge muss positiv sein.");
        _warenkorb.Hinzufuegen(sitzung, produktId, lieferantId, angebot.SupplierId, angebot.Price, menge);
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

        var items = positionen.Select(p => new OrderItemCreateDto(p.ProduktId, p.LieferantTechnischeId, p.Menge)).ToList();
        var delivery = new DeliveryDto(lieferdaten.EmpfaengerName, lieferdaten.StrasseHausnummer, lieferdaten.Postleitzahl, lieferdaten.Ort, "Deutschland", kontaktdaten.Email, null);

        var antwort = await Http.PostAsJsonAsync("api/orders", new OrderCreateDto(items, delivery));
        if (!antwort.IsSuccessStatusCode) throw new BestellungAbgelehntException();

        var order = await antwort.Content.ReadFromJsonAsync<OrderDto>() ?? throw new InvalidOperationException("Leere Bestell-Antwort.");
        _angelegteBestellungen.Add(UebersetzeBestellung(order));
        return order.PublicId;
    }

    public Task<IReadOnlyList<Bestellung>> HoleBestellungen(Kontaktdaten kontakt)
        => Task.FromResult<IReadOnlyList<Bestellung>>(_angelegteBestellungen);

    public async Task BewerteProdukt(string produktId, int wertung, string autor)
    {
        var antwort = await Http.PostAsJsonAsync($"api/products/{produktId}/ratings", new RatingCreateDto(autor, wertung));
        antwort.EnsureSuccessStatusCode();
    }

    private static Bestellung UebersetzeBestellung(OrderDto order) => new(
        order.PublicId, order.Status,
        order.Items.Select(i => new Bestellposition(i.ProductId, i.SupplierName, i.UnitPrice, i.Quantity)).ToList(),
        order.TotalAmount);
}
