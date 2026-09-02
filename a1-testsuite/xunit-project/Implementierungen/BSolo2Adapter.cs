using System.Net.Http.Json;
using A1Testsuite.Treiber;

namespace A1Testsuite.Implementierungen;

// Adapter fuer b_solo_2, gebaut ausschliesslich anhand von
// a1-testsuite/openapi/b_solo_2.json. Kein serverseitiger Warenkorb-
// Endpunkt -- LokalerWarenkorb.cs simuliert (siehe deren Kopfkommentar).
// IDs (Produkt/Kategorie/Lieferant) sind hier durchgehend int, nicht
// string. Kein Eigenschaftsfilter-Parameter auf GET /api/products
// vorhanden und die Listenantwort liefert auch keine Eigenschaften mit --
// TF-B-F6 laedt deshalb je Produkt der Kategorie zusaetzlich die
// Detailansicht und filtert clientseitig (reine Filterung echter
// SUT-Daten). sortBy/sortDir nur als "type: string" ohne dokumentierte
// Werte -- sortBy=minPrice/name/viewCount (JSON-Feldnamen) und
// sortDir=asc/desc angenommen. HoleBestellungen: siehe Kopfkommentar in
// BBmad2Adapter.cs.
public sealed class BSolo2Adapter : HttpAdapterBasis, IProjektBTreiber
{
    public BSolo2Adapter(string basisUrl) : base(basisUrl) { }

    private readonly LokalerWarenkorb _warenkorb = new();
    private readonly List<Bestellung> _angelegteBestellungen = [];

    private sealed record SubcategoryDto(int Id, string Name, List<string>? PropertyNames);
    private sealed record CategoryDto(int Id, string Name, List<SubcategoryDto> Subcategories);
    private sealed record ProductListItemDto(int Id, string Name, int SubcategoryId, string SubcategoryName, int CategoryId, string CategoryName, decimal? MinPrice, double? AverageRating, int RatingCount, long ViewCount);
    private sealed record PagedResultDto(List<ProductListItemDto> Items, int TotalCount, int Page, int PageSize);
    private sealed record ProductOfferDto(int SupplierId, string SupplierName, decimal Price);
    private sealed record ProductDetailDto(int Id, string Name, string Description, int SubcategoryId, string SubcategoryName, int CategoryId, string CategoryName, Dictionary<string, string> Properties, double? AverageRating, int RatingCount, long ViewCount, List<ProductOfferDto> Offers);
    private sealed record OrderContactDto(string RecipientName, string Street, string PostalCode, string City, string Email);
    private sealed record OrderItemCreateDto(int ProductId, int SupplierId, int Quantity);
    private sealed record OrderCreateDto(OrderContactDto Contact, List<OrderItemCreateDto> Items);
    private sealed record OrderItemResultDto(int ProductId, string ProductName, int SupplierId, string SupplierName, decimal UnitPrice, int Quantity, decimal LineTotal);
    private sealed record OrderDto(int Id, string Status, DateTime CreatedAt, OrderContactDto Contact, List<OrderItemResultDto> Items, decimal Total);

    public Task<string> NeueWarenkorbSitzung() => Task.FromResult(_warenkorb.NeueSitzung());

    public async Task<ProduktlisteErgebnis> ListeProdukte(
        int seite, string? kategorieId = null, string? sortierung = null, IReadOnlyDictionary<string, string>? eigenschaftsfilter = null)
    {
        // kategorieId traegt den fachlichen (Unter-)Kategorienamen, keine
        // technische ID -- die Listenantwort liefert Kategorienamen bereits
        // mit, deshalb clientseitige Filterung statt Aufloesung ueber den
        // Kategoriebaum. Fuer den Eigenschaftsfilter fehlt ein Query-
        // Parameter komplett; dafuer wird je Treffer zusaetzlich die
        // Detailansicht geladen (dort stehen die Eigenschaften).
        if (kategorieId is not null || (eigenschaftsfilter is not null && eigenschaftsfilter.Count > 0))
        {
            var alle = await AlleSeitenLaden(sortierung);
            var kategoriegefiltert = alle
                .Where(p => kategorieId is null || p.SubcategoryName == kategorieId || p.CategoryName == kategorieId)
                .ToList();

            List<ProductListItemDto> passend;
            if (eigenschaftsfilter is not null && eigenschaftsfilter.Count > 0)
            {
                passend = [];
                foreach (var p in kategoriegefiltert)
                {
                    var detail = await Http.GetFromJsonAsync<ProductDetailDto>($"api/products/{p.Id}")
                        ?? throw new InvalidOperationException($"Produkt '{p.Id}' nicht gefunden.");
                    if (eigenschaftsfilter.All(f => detail.Properties.TryGetValue(f.Key, out var wert) && wert == f.Value))
                        passend.Add(p);
                }
            }
            else
            {
                passend = kategoriegefiltert;
            }

            var produkteF = passend.Select(Uebersetze).ToList();
            return new ProduktlisteErgebnis(produkteF, 1, 1, produkteF.Count);
        }

        var antwort = await Http.GetFromJsonAsync<PagedResultDto>($"api/products?{BaueQuery(seite, sortierung, null)}")
            ?? throw new InvalidOperationException("Leere Produktlisten-Antwort.");
        var seitenAnzahl = antwort.PageSize > 0 ? (int)Math.Ceiling(antwort.TotalCount / (double)antwort.PageSize) : 1;
        return new ProduktlisteErgebnis(antwort.Items.Select(Uebersetze).ToList(), antwort.Page, seitenAnzahl, antwort.TotalCount);
    }

    public async Task<IReadOnlyList<Oberkategorie>> HoleKategoriebaum()
    {
        var kategorien = await Http.GetFromJsonAsync<List<CategoryDto>>("api/categories") ?? [];
        return kategorien.Select(k => new Oberkategorie(k.Name, k.Subcategories.Select(u => new Unterkategorie(u.Name)).ToList())).ToList();
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
            p.Id.ToString(), p.Name, p.Description, p.SubcategoryName,
            p.Properties,
            p.Offers.Select(o => new LieferantenAngebot(o.SupplierName, o.Price)).ToList(),
            p.AverageRating, p.RatingCount, (int)p.ViewCount);
    }

    public async Task LegeInWarenkorb(string sitzung, string produktId, string lieferantId, int menge)
    {
        var p = await Http.GetFromJsonAsync<ProductDetailDto>($"api/products/{produktId}")
            ?? throw new InvalidOperationException($"Produkt '{produktId}' nicht gefunden.");
        var angebot = p.Offers.SingleOrDefault(o => o.SupplierName == lieferantId)
            ?? throw new WarenkorbAbgelehntException($"Lieferant '{lieferantId}' bietet Produkt '{produktId}' nicht an.");
        if (menge <= 0) throw new WarenkorbAbgelehntException("Menge muss positiv sein.");
        _warenkorb.Hinzufuegen(sitzung, produktId, lieferantId, angebot.SupplierId.ToString(), angebot.Price, menge);
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
            new OrderContactDto(kontaktdaten.Name, lieferdaten.StrasseHausnummer, lieferdaten.Postleitzahl, lieferdaten.Ort, kontaktdaten.Email),
            positionen.Select(p => new OrderItemCreateDto(int.Parse(p.ProduktId), int.Parse(p.LieferantTechnischeId), p.Menge)).ToList());

        var antwort = await Http.PostAsJsonAsync("api/orders", request);
        if (!antwort.IsSuccessStatusCode) throw new BestellungAbgelehntException();

        var order = await antwort.Content.ReadFromJsonAsync<OrderDto>() ?? throw new InvalidOperationException("Leere Bestell-Antwort.");
        _angelegteBestellungen.Add(UebersetzeBestellung(order));
        return order.Id.ToString();
    }

    public Task<IReadOnlyList<Bestellung>> HoleBestellungen(Kontaktdaten kontakt)
        => Task.FromResult<IReadOnlyList<Bestellung>>(_angelegteBestellungen);

    public async Task BewerteProdukt(string produktId, int wertung, string autor)
    {
        var antwort = await Http.PostAsJsonAsync($"api/products/{produktId}/reviews", new { authorName = autor, rating = wertung });
        antwort.EnsureSuccessStatusCode();
    }

    private async Task<List<ProductListItemDto>> AlleSeitenLaden(string? sortierung)
    {
        var erste = await Http.GetFromJsonAsync<PagedResultDto>($"api/products?{BaueQuery(1, sortierung, null)}")
            ?? throw new InvalidOperationException("Leere Produktlisten-Antwort.");
        var seitenAnzahl = erste.PageSize > 0 ? (int)Math.Ceiling(erste.TotalCount / (double)erste.PageSize) : 1;
        var alle = new List<ProductListItemDto>(erste.Items);
        for (var seite = 2; seite <= seitenAnzahl; seite++)
        {
            var naechste = await Http.GetFromJsonAsync<PagedResultDto>($"api/products?{BaueQuery(seite, sortierung, null)}")
                ?? throw new InvalidOperationException("Leere Produktlisten-Antwort.");
            alle.AddRange(naechste.Items);
        }
        return alle;
    }

    private static string BaueQuery(int seite, string? sortierung, string? suchtext)
    {
        var query = new List<string> { $"page={seite}" };
        if (suchtext is not null) query.Add($"search={Uri.EscapeDataString(suchtext)}");
        // 31.08.2026: urspruengliche Annahme (JSON-Feldnamen minPrice/viewCount)
        // unverifiziert und falsch -- Server (ProductsController.GetProducts)
        // erwartet feste Werte "price"/"popularity"/"name" (Quellcode direkt
        // eingesehen), jeder unbekannte sortBy-Wert faellt still auf Name
        // aufsteigend zurueck. "name" traf zufaellig bereits zu, "minPrice"/
        // "viewCount" nicht. Echter Adapterfehler, kein SUT-Befund -- siehe
        // Uebergabedokument.
        if (sortierung == Sortierungen.PreisAufsteigend) { query.Add("sortBy=price"); query.Add("sortDir=asc"); }
        else if (sortierung == Sortierungen.NameAbsteigend) { query.Add("sortBy=name"); query.Add("sortDir=desc"); }
        else if (sortierung == Sortierungen.AufrufzaehlerAbsteigend) { query.Add("sortBy=popularity"); query.Add("sortDir=desc"); }
        return string.Join("&", query);
    }

    private static ProduktUebersicht Uebersetze(ProductListItemDto p) => new(p.Id.ToString(), p.Name, p.MinPrice ?? 0m, p.CategoryName);

    private static Bestellung UebersetzeBestellung(OrderDto order) => new(
        order.Id.ToString(), order.Status,
        order.Items.Select(i => new Bestellposition(i.ProductId.ToString(), i.SupplierName, i.UnitPrice, i.Quantity)).ToList(),
        order.Total);
}
