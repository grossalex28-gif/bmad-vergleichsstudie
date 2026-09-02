using System.Net.Http.Json;
using A1Testsuite.Treiber;

namespace A1Testsuite.Implementierungen;

// Adapter fuer b_solo_3, gebaut ausschliesslich anhand von
// a1-testsuite/openapi/b_solo_3.json. Kein serverseitiger Warenkorb-
// Endpunkt -- LokalerWarenkorb.cs simuliert (siehe deren Kopfkommentar).
// kategorieId (Unterkategorie-/Kategoriename) wird clientseitig gefiltert,
// da die Liste die Namen bereits mitliefert (wie bei b_solo_1/b_solo_2).
// Der "Sort"-Parameter ist an das Enum ProductSort gebunden, das in der
// OpenAPI-Beschreibung nur als "type: integer" ohne Bezeichner-Zuordnung
// dokumentiert ist -- ein Rateversuch waere hier nicht einmal
// selbstdiagnostizierend (ein falscher Wert liefert vermutlich einfach die
// Standardsortierung zurueck, statt eindeutig sichtbar zu scheitern).
// Deshalb wird "Sort" gar nicht erst gesendet, stattdessen clientseitig
// nach minPrice/name/viewCount sortiert -- echte SUT-Daten, reine
// PrAesentationsreihenfolge, keine Fachlogik.
public sealed class BSolo3Adapter : HttpAdapterBasis, IProjektBTreiber
{
    public BSolo3Adapter(string basisUrl) : base(basisUrl) { }

    private readonly LokalerWarenkorb _warenkorb = new();
    private readonly List<Bestellung> _angelegteBestellungen = [];

    private sealed record SubCategoryDto(string Id, string Name, List<string>? PropertyNames);
    private sealed record CategoryDto(string Id, string Name, List<SubCategoryDto> SubCategories);
    private sealed record ProductListItemDto(string Id, string Name, string SubCategoryId, string SubCategoryName, string CategoryId, string CategoryName, decimal? MinPrice, long ViewCount, double? AverageRating, int RatingCount);
    private sealed record ProductListResponseDto(List<ProductListItemDto> Items, int Page, int PageSize, int TotalCount, int TotalPages);
    private sealed record OfferDto(string SupplierId, string SupplierName, decimal Price);
    private sealed record ProductDetailDto(string Id, string Name, string Description, string SubCategoryId, string SubCategoryName, string CategoryId, string CategoryName, Dictionary<string, string> Properties, double? AverageRating, int RatingCount, long ViewCount, List<OfferDto> Offers);
    private sealed record CreateOrderItemRequest(string ProductId, string SupplierId, int Quantity);
    private sealed record CreateOrderRequest(string ContactName, string Email, string Street, string PostalCode, string City, List<CreateOrderItemRequest> Items);
    private sealed record OrderItemDto(string ProductId, string ProductName, string SupplierId, string SupplierName, decimal UnitPrice, int Quantity, decimal LineTotal);
    private sealed record OrderDto(int Id, string Status, DateTime CreatedAt, string ContactName, string Email, string Street, string PostalCode, string City, List<OrderItemDto> Items, decimal Total);
    private sealed record CreateRatingRequest(string AuthorName, int Stars);

    public Task<string> NeueWarenkorbSitzung() => Task.FromResult(_warenkorb.NeueSitzung());

    public async Task<ProduktlisteErgebnis> ListeProdukte(
        int seite, string? kategorieId = null, string? sortierung = null, IReadOnlyDictionary<string, string>? eigenschaftsfilter = null)
    {
        var query = new List<string> { $"Page={seite}" };
        if (eigenschaftsfilter is not null)
            foreach (var (k, v) in eigenschaftsfilter)
                query.Add($"Properties[{Uri.EscapeDataString(k)}]={Uri.EscapeDataString(v)}");

        if (kategorieId is not null || sortierung is not null)
        {
            // Kategoriename und Sortierung laufen beide clientseitig (siehe
            // Kopfkommentar), deshalb hier alle Seiten unfiltriert laden.
            var alle = await AlleSeitenLaden(eigenschaftsfilter);
            IEnumerable<ProductListItemDto> gefiltert = alle
                .Where(p => kategorieId is null || p.SubCategoryName == kategorieId || p.CategoryName == kategorieId);
            gefiltert = sortierung switch
            {
                _ when sortierung == Sortierungen.PreisAufsteigend => gefiltert.OrderBy(p => p.MinPrice ?? 0m),
                _ when sortierung == Sortierungen.NameAbsteigend => gefiltert.OrderByDescending(p => p.Name, StringComparer.Ordinal),
                _ when sortierung == Sortierungen.AufrufzaehlerAbsteigend => gefiltert.OrderByDescending(p => p.ViewCount),
                _ => gefiltert,
            };
            var liste = gefiltert.ToList();
            return new ProduktlisteErgebnis(liste.Select(Uebersetze).ToList(), 1, 1, liste.Count);
        }

        var antwort = await Http.GetFromJsonAsync<ProductListResponseDto>($"api/products?{string.Join("&", query)}")
            ?? throw new InvalidOperationException("Leere Produktlisten-Antwort.");
        return new ProduktlisteErgebnis(antwort.Items.Select(Uebersetze).ToList(), antwort.Page, antwort.TotalPages, antwort.TotalCount);
    }

    public async Task<IReadOnlyList<Oberkategorie>> HoleKategoriebaum()
    {
        var kategorien = await Http.GetFromJsonAsync<List<CategoryDto>>("api/categories") ?? [];
        return kategorien.Select(k => new Oberkategorie(k.Name, k.SubCategories.Select(u => new Unterkategorie(u.Name)).ToList())).ToList();
    }

    public async Task<IReadOnlyList<ProduktUebersicht>> SucheProdukte(string suchtext)
    {
        var antwort = await Http.GetFromJsonAsync<ProductListResponseDto>($"api/products?Search={Uri.EscapeDataString(suchtext)}")
            ?? throw new InvalidOperationException("Leere Produktlisten-Antwort.");
        return antwort.Items.Select(Uebersetze).ToList();
    }

    public async Task<ProduktDetail> HoleProdukt(string id)
    {
        var p = await Http.GetFromJsonAsync<ProductDetailDto>($"api/products/{id}")
            ?? throw new InvalidOperationException($"Produkt '{id}' nicht gefunden.");
        return new ProduktDetail(
            p.Id, p.Name, p.Description, p.SubCategoryName, p.Properties,
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

        var request = new CreateOrderRequest(
            kontaktdaten.Name, kontaktdaten.Email, lieferdaten.StrasseHausnummer, lieferdaten.Postleitzahl, lieferdaten.Ort,
            positionen.Select(p => new CreateOrderItemRequest(p.ProduktId, p.LieferantTechnischeId, p.Menge)).ToList());

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
        var antwort = await Http.PostAsJsonAsync($"api/products/{produktId}/ratings", new CreateRatingRequest(autor, wertung));
        antwort.EnsureSuccessStatusCode();
    }

    private async Task<List<ProductListItemDto>> AlleSeitenLaden(IReadOnlyDictionary<string, string>? eigenschaftsfilter)
    {
        string Query(int seite)
        {
            var q = new List<string> { $"Page={seite}" };
            if (eigenschaftsfilter is not null)
                foreach (var (k, v) in eigenschaftsfilter)
                    q.Add($"Properties[{Uri.EscapeDataString(k)}]={Uri.EscapeDataString(v)}");
            return string.Join("&", q);
        }

        var erste = await Http.GetFromJsonAsync<ProductListResponseDto>($"api/products?{Query(1)}")
            ?? throw new InvalidOperationException("Leere Produktlisten-Antwort.");
        var alle = new List<ProductListItemDto>(erste.Items);
        for (var seite = 2; seite <= erste.TotalPages; seite++)
        {
            var naechste = await Http.GetFromJsonAsync<ProductListResponseDto>($"api/products?{Query(seite)}")
                ?? throw new InvalidOperationException("Leere Produktlisten-Antwort.");
            alle.AddRange(naechste.Items);
        }
        return alle;
    }

    private static ProduktUebersicht Uebersetze(ProductListItemDto p) => new(p.Id, p.Name, p.MinPrice ?? 0m, p.CategoryName);

    private static Bestellung UebersetzeBestellung(OrderDto order) => new(
        order.Id.ToString(), order.Status,
        order.Items.Select(i => new Bestellposition(i.ProductId, i.SupplierName, i.UnitPrice, i.Quantity)).ToList(),
        order.Total);
}
