using System.Net.Http.Json;
using A1Testsuite.Treiber;

namespace A1Testsuite.Implementierungen;

// Adapter fuer b_bmad_3, gebaut anhand von a1-testsuite/openapi/b_bmad_3.json
// UND zusaetzlich anhand von Live-Stichproben (a1-testsuite/openapi-samples/
// b_bmad_3_*.json), da die generierte OpenAPI-Beschreibung dort ausschliesslich
// die drei Request-DTOs dokumentiert (CreateOrderRequestDto, OrderLineRequestDto,
// RatingRequestDto) -- keine einzige Response-Form. Kategorien-, Produktlisten-
// und Produktdetail-Form sind ueber die Stichproben bestaetigt.
//
// Wichtiger Befund (kein Adapterfehler, sondern ein echter Server-Defekt):
// POST /api/orders wirft bei JEDEM Aufruf eine unbehandelte
// System.InvalidOperationException ("Record type 'CreateOrderRequestDto' has
// validation metadata defined on property 'Email' that will be ignored ...
// must be associated with the constructor parameter") -- ASP.NET Core lehnt
// die Validierungs-Metadaten des Record-Typs bereits beim Model-Binding ab.
// Das ist unabhaengig vom gesendeten Inhalt (auch ein vollstaendig gueltiger
// Testaufruf mit echter Produkt-/Lieferanten-ID schlaegt fehl, siehe
// a1-testsuite/openapi-samples/b_bmad_3_order_create_response.json). Alle
// Testfaelle, die eine erfolgreiche Bestellanlage voraussetzen (TF-B-F8 bis
// TF-B-F11), schlagen fuer b_bmad_3 deshalb erwartungsgemaess fehl -- eine
// echte Deckungsluecke der SUT (fehlerhafte Verwendung von Validierungs-
// Attributen auf einem Record-Primaerkonstruktor), keine Unsicherheit im
// Adapter. Die Response-Form von OrderDto ist daher unverifizierbar und
// orientiert sich am Feldschema des Request-DTOs (flache Adressfelder statt
// verschachteltem Delivery-Objekt).
//
// Kein serverseitiger Warenkorb-Endpunkt vorhanden -- LokalerWarenkorb.cs
// simuliert (siehe deren Kopfkommentar). HoleBestellungen: siehe
// Kopfkommentar in BBmad2Adapter.cs (kein Listing-Endpunkt).
//
// kategorieId (Unterkategorie-/Kategoriename) wird clientseitig gefiltert:
// die Produktlisten-Antwort liefert nur subcategoryName (kein categoryName),
// daher wird bei einem Treffer auf oberster Kategorieebene zusaetzlich ueber
// den Kategoriebaum aufgeloest, ob die jeweilige Unterkategorie darunter
// haengt. sortBy/sortDirection und der "attr"-Array-Parameter sind beide nur
// als "type: string" bzw. "array of string" ohne dokumentierte Kodierung
// vorhanden -- wie bei b_solo_3 deshalb clientseitige Sortierung/Filterung
// (echte SUT-Daten, reine Praesentationsreihenfolge) statt eines Rateversuchs
// an der Query-Codierung. ProductDetailDto hat kein Aufrufzaehler-Feld --
// HoleProdukt liefert dafuer fest 0, TF_B_F13_1_und_2 schlaegt damit
// erwartungsgemaess fehl (echte Deckungsluecke, kein Adapterfehler).
public sealed class BBmad3Adapter : HttpAdapterBasis, IProjektBTreiber
{
    public BBmad3Adapter(string basisUrl) : base(basisUrl) { }

    private readonly LokalerWarenkorb _warenkorb = new();
    private readonly List<Bestellung> _angelegteBestellungen = [];

    private sealed record SubcategoryDto(string Id, string Name);
    private sealed record CategoryDto(string Id, string Name, List<SubcategoryDto> Subcategories);
    private sealed record ProductListItemDto(string Id, string Name, string SubcategoryName, decimal? MinPrice);
    private sealed record PagedResultDto(List<ProductListItemDto> Items, int Page, int PageSize, int TotalCount, int TotalPages);
    private sealed record ProductAttributeDto(string Name, string Value);
    private sealed record ProductOfferDto(string SupplierId, string SupplierName, decimal Price);
    private sealed record ProductDetailDto(string Id, string Name, string Description, string CategoryId, string CategoryName, string SubcategoryId, string SubcategoryName, List<ProductAttributeDto> Attributes, double? AverageRating, int RatingCount, List<ProductOfferDto> Offers);
    private sealed record OrderLineRequestDto(string ProductId, string SupplierId, int Quantity);
    private sealed record CreateOrderRequestDto(string Name, string Street, string PostalCode, string City, string Country, string Email, List<OrderLineRequestDto> Items);
    private sealed record OrderItemResponseDto(string ProductId, string ProductName, string SupplierId, string SupplierName, decimal UnitPrice, int Quantity, decimal LineTotal);
    // Unverifizierbar (siehe Kopfkommentar) -- Feldschema an CreateOrderRequestDto angelehnt.
    private sealed record OrderDto(string Id, string Status, DateTime CreatedAt, string Name, string Street, string PostalCode, string City, string Country, string Email, List<OrderItemResponseDto> Items, decimal Total);
    private sealed record RatingRequestDto(string AuthorName, int Value);

    public Task<string> NeueWarenkorbSitzung() => Task.FromResult(_warenkorb.NeueSitzung());

    public async Task<ProduktlisteErgebnis> ListeProdukte(
        int seite, string? kategorieId = null, string? sortierung = null, IReadOnlyDictionary<string, string>? eigenschaftsfilter = null)
    {
        if (kategorieId is not null || sortierung is not null || (eigenschaftsfilter is not null && eigenschaftsfilter.Count > 0))
        {
            var alle = await AlleSeitenLaden();
            IEnumerable<ProductListItemDto> gefiltert = alle;
            if (kategorieId is not null)
            {
                var kategorien = await LadeKategorien();
                gefiltert = gefiltert.Where(p => GehoertZuKategorie(p.SubcategoryName, kategorieId, kategorien));
            }
            if (eigenschaftsfilter is not null && eigenschaftsfilter.Count > 0)
            {
                var passend = new List<ProductListItemDto>();
                foreach (var p in gefiltert)
                {
                    var detail = await Http.GetFromJsonAsync<ProductDetailDto>($"api/products/{p.Id}")
                        ?? throw new InvalidOperationException($"Produkt '{p.Id}' nicht gefunden.");
                    var eigenschaften = detail.Attributes.ToDictionary(a => a.Name, a => a.Value);
                    if (eigenschaftsfilter.All(f => eigenschaften.TryGetValue(f.Key, out var wert) && wert == f.Value))
                        passend.Add(p);
                }
                gefiltert = passend;
            }
            gefiltert = sortierung switch
            {
                _ when sortierung == Sortierungen.PreisAufsteigend => gefiltert.OrderBy(p => p.MinPrice ?? 0m),
                _ when sortierung == Sortierungen.NameAbsteigend => gefiltert.OrderByDescending(p => p.Name, StringComparer.Ordinal),
                // Aufrufzaehler existiert bei b_bmad_3 nicht (siehe Kopfkommentar) --
                // Reihenfolge bleibt unveraendert, TF_B_F13 prueft ohnehin nicht diese Methode.
                _ => gefiltert,
            };
            var liste = gefiltert.ToList();
            return new ProduktlisteErgebnis(liste.Select(Uebersetze).ToList(), 1, 1, liste.Count);
        }

        var antwort = await Http.GetFromJsonAsync<PagedResultDto>($"api/products?page={seite}")
            ?? throw new InvalidOperationException("Leere Produktlisten-Antwort.");
        return new ProduktlisteErgebnis(antwort.Items.Select(Uebersetze).ToList(), antwort.Page, antwort.TotalPages, antwort.TotalCount);
    }

    public async Task<IReadOnlyList<Oberkategorie>> HoleKategoriebaum()
    {
        var kategorien = await LadeKategorien();
        return kategorien.Select(k => new Oberkategorie(k.Name, k.Subcategories.Select(u => new Unterkategorie(u.Name)).ToList())).ToList();
    }

    private async Task<List<CategoryDto>> LadeKategorien()
        => await Http.GetFromJsonAsync<List<CategoryDto>>("api/categories") ?? [];

    private static bool GehoertZuKategorie(string subcategoryName, string kategorieName, List<CategoryDto> kategorien)
    {
        if (subcategoryName == kategorieName) return true;
        var top = kategorien.SingleOrDefault(k => k.Name == kategorieName);
        return top is not null && top.Subcategories.Any(u => u.Name == subcategoryName);
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
            p.Id, p.Name, p.Description, p.SubcategoryName,
            p.Attributes.ToDictionary(a => a.Name, a => a.Value),
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

        var items = positionen.Select(p => new OrderLineRequestDto(p.ProduktId, p.LieferantTechnischeId, p.Menge)).ToList();
        // Country ist bei CreateOrderRequestDto Pflichtfeld, Lieferdaten hat aber kein
        // Land-Feld -- wie bei allen anderen B-Adaptern fest "Deutschland" (Studienkontext).
        var request = new CreateOrderRequestDto(
            lieferdaten.EmpfaengerName, lieferdaten.StrasseHausnummer, lieferdaten.Postleitzahl, lieferdaten.Ort, "Deutschland", kontaktdaten.Email, items);

        // Siehe Kopfkommentar: dieser Aufruf schlaegt bei b_bmad_3 aktuell
        // durchgehend mit HTTP 500 fehl (Server-Defekt, kein Adapterfehler).
        var antwort = await Http.PostAsJsonAsync("api/orders", request);
        if (!antwort.IsSuccessStatusCode) throw new BestellungAbgelehntException();

        var order = await antwort.Content.ReadFromJsonAsync<OrderDto>() ?? throw new InvalidOperationException("Leere Bestell-Antwort.");
        _angelegteBestellungen.Add(UebersetzeBestellung(order));
        return order.Id;
    }

    public Task<IReadOnlyList<Bestellung>> HoleBestellungen(Kontaktdaten kontakt)
        => Task.FromResult<IReadOnlyList<Bestellung>>(_angelegteBestellungen);

    public async Task BewerteProdukt(string produktId, int wertung, string autor)
    {
        var antwort = await Http.PostAsJsonAsync($"api/products/{produktId}/ratings", new RatingRequestDto(autor, wertung));
        antwort.EnsureSuccessStatusCode();
    }

    private async Task<List<ProductListItemDto>> AlleSeitenLaden()
    {
        var erste = await Http.GetFromJsonAsync<PagedResultDto>("api/products?page=1")
            ?? throw new InvalidOperationException("Leere Produktlisten-Antwort.");
        var alle = new List<ProductListItemDto>(erste.Items);
        for (var seite = 2; seite <= erste.TotalPages; seite++)
        {
            var naechste = await Http.GetFromJsonAsync<PagedResultDto>($"api/products?page={seite}")
                ?? throw new InvalidOperationException("Leere Produktlisten-Antwort.");
            alle.AddRange(naechste.Items);
        }
        return alle;
    }

    private static ProduktUebersicht Uebersetze(ProductListItemDto p) => new(p.Id, p.Name, p.MinPrice ?? 0m, p.SubcategoryName);

    private static Bestellung UebersetzeBestellung(OrderDto order) => new(
        order.Id, order.Status,
        order.Items.Select(i => new Bestellposition(i.ProductId, i.SupplierName, i.UnitPrice, i.Quantity)).ToList(),
        order.Total);
}
