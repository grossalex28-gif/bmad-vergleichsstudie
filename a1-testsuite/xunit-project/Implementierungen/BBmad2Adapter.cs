using System.Net.Http.Json;
using A1Testsuite.Treiber;

namespace A1Testsuite.Implementierungen;

// Adapter fuer b_bmad_2, gebaut ausschliesslich anhand von
// a1-testsuite/openapi/b_bmad_2.json. Einzige der sechs Projekt-B-
// Implementierungen mit echtem serverseitigem Warenkorb (/api/cart/...,
// per X-Cart-Id-Header identifiziert) -- hier NICHT die
// LokalerWarenkorb-Simulation noetig, siehe deren Kopfkommentar.
//
// Die Produktlisten-Antwort (ProductSummaryDto) enthaelt nur id/name, kein
// Preis- oder Kategoriefeld -- ProduktUebersicht.AbPreis/KategorieName
// bleiben deshalb hier 0/"" (kein Testfall prueft diese beiden Felder auf
// der Liste, nur Name wird gebraucht). sortBy/sortDirection und der
// properties-Objektfilter sind in der OpenAPI nur als "type: string" bzw.
// "type: object" dokumentiert, ohne zulaessige Werte -- uebliche
// ASP.NET-Core-Konvention angenommen (sortBy=price/name,
// sortDirection=asc/desc, properties[Name]=Wert); bei Abweichung schlaegt
// das in einem klar erkennbaren, isolierten Testfall fehl.
// ProductDetailDto hat kein Aufrufzaehler-Feld -- HoleProdukt liefert dafuer
// fest 0, TF_B_F13_1_und_2 (Aufrufzaehler steigt) schlaegt damit fuer diese
// Implementierung erwartungsgemaess fehl (echte Deckungsluecke der SUT, kein
// Adapterfehler; siehe Uebergabedokument).
public sealed class BBmad2Adapter : HttpAdapterBasis, IProjektBTreiber
{
    public BBmad2Adapter(string basisUrl) : base(basisUrl) { }

    // Es gibt keinen Endpunkt, um Bestellungen nach Kontakt zu listen (nur
    // GET /api/orders/{id} fuer eine einzelne bekannte ID) -- betrifft alle
    // sechs Projekt-B-Implementierungen gleichermassen, siehe
    // Uebergabedokument. HoleBestellungen gibt deshalb die in dieser
    // Adapter-Instanz waehrend LegeBestellungAn tatsaechlich erfolgreich
    // angelegten Bestellungen zurueck (echte SUT-Antworten, nur lokal
    // zwischengespeichert -- jede Testmethode erzeugt eine frische
    // Adapter-Instanz, siehe README.md).
    private readonly List<Bestellung> _angelegteBestellungen = [];

    private sealed record SubcategoryDto(string Id, string Name, List<string>? Properties);
    private sealed record CategoryDto(string Id, string Name, List<SubcategoryDto> Subcategories);
    private sealed record ProductSummaryDto(string Id, string Name);
    private sealed record ProductListResponse(List<ProductSummaryDto> Items, int Page, int PageSize, int TotalCount);
    private sealed record CategoryRefDto(string Id, string Name);
    private sealed record ProductPropertyDto(string Name, string Value);
    private sealed record SupplierOfferDto(string SupplierId, string SupplierName, decimal Price);
    private sealed record ProductDetailDto(string Id, string Name, string Description, CategoryRefDto Category, CategoryRefDto Subcategory, List<ProductPropertyDto> Properties, double? AverageRating, int RatingCount, List<SupplierOfferDto> Offers);
    private sealed record CartItemDto(string ProductId, string ProductName, string SupplierId, string SupplierName, int Quantity, decimal UnitPrice, decimal LineTotal);
    private sealed record CartDto(string CartId, List<CartItemDto> Items, decimal TotalPrice);
    private sealed record AddCartItemRequest(string ProductId, string SupplierId, int Quantity);
    private sealed record UpdateCartItemQuantityRequest(int Quantity);
    private sealed record OrderItemDto(string ProductId, string ProductName, string SupplierId, string SupplierName, int Quantity, decimal UnitPrice, decimal LineTotal);
    private sealed record OrderDto(string OrderId, string Status, List<OrderItemDto> Items, decimal TotalPrice);
    private sealed record PlaceOrderRequest(string CustomerName, string DeliveryAddress, string Email);
    private sealed record RatingSubmissionResponseDto(double AverageRating, int RatingCount);

    // sitzung == X-Cart-Id. Server generiert bei fehlendem Header eine neue
    // Cart-Id, wir setzen ihn hier bewusst selbst, um eine stabile Sitzung
    // ueber mehrere Aufrufe hinweg zu garantieren.
    public Task<string> NeueWarenkorbSitzung() => Task.FromResult(Guid.NewGuid().ToString("N"));

    public async Task<ProduktlisteErgebnis> ListeProdukte(
        int seite, string? kategorieId = null, string? sortierung = null, IReadOnlyDictionary<string, string>? eigenschaftsfilter = null)
    {
        var query = new List<string> { $"page={seite}" };
        // kategorieId traegt den fachlichen (Unter-)Kategorienamen, keine
        // technische ID -- erst ueber den Kategoriebaum aufloesen.
        if (kategorieId is not null)
        {
            var (paramName, paramWert) = await LoeseKategorieAuf(kategorieId);
            query.Add($"{paramName}={Uri.EscapeDataString(paramWert)}");
        }
        if (sortierung == Sortierungen.PreisAufsteigend) { query.Add("sortBy=price"); query.Add("sortDirection=asc"); }
        else if (sortierung == Sortierungen.NameAbsteigend) { query.Add("sortBy=name"); query.Add("sortDirection=desc"); }
        else if (sortierung == Sortierungen.AufrufzaehlerAbsteigend) { query.Add("sortBy=views"); query.Add("sortDirection=desc"); }
        if (eigenschaftsfilter is not null)
            foreach (var (k, v) in eigenschaftsfilter)
                query.Add($"properties[{Uri.EscapeDataString(k)}]={Uri.EscapeDataString(v)}");

        var antwort = await Http.GetFromJsonAsync<ProductListResponse>($"api/products?{string.Join("&", query)}")
            ?? throw new InvalidOperationException("Leere Produktlisten-Antwort.");
        var seitenAnzahl = antwort.PageSize > 0 ? (int)Math.Ceiling(antwort.TotalCount / (double)antwort.PageSize) : 1;
        var produkte = antwort.Items.Select(p => new ProduktUebersicht(p.Id, p.Name, 0m, "")).ToList();
        return new ProduktlisteErgebnis(produkte, antwort.Page, seitenAnzahl, antwort.TotalCount);
    }

    public async Task<IReadOnlyList<Oberkategorie>> HoleKategoriebaum()
    {
        var kategorien = await LadeKategorien();
        return kategorien.Select(k => new Oberkategorie(k.Name, k.Subcategories.Select(u => new Unterkategorie(u.Name)).ToList())).ToList();
    }

    private async Task<List<CategoryDto>> LadeKategorien()
        => await Http.GetFromJsonAsync<List<CategoryDto>>("api/categories") ?? [];

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
        var antwort = await Http.GetFromJsonAsync<ProductListResponse>($"api/products?search={Uri.EscapeDataString(suchtext)}")
            ?? throw new InvalidOperationException("Leere Produktlisten-Antwort.");
        return antwort.Items.Select(p => new ProduktUebersicht(p.Id, p.Name, 0m, "")).ToList();
    }

    public async Task<ProduktDetail> HoleProdukt(string id)
    {
        var p = await Http.GetFromJsonAsync<ProductDetailDto>($"api/products/{id}")
            ?? throw new InvalidOperationException($"Produkt '{id}' nicht gefunden.");
        return new ProduktDetail(
            p.Id, p.Name, p.Description, p.Subcategory.Name,
            p.Properties.ToDictionary(e => e.Name, e => e.Value),
            p.Offers.Select(o => new LieferantenAngebot(o.SupplierName, o.Price)).ToList(),
            p.AverageRating, p.RatingCount, 0);
    }

    public async Task LegeInWarenkorb(string sitzung, string produktId, string lieferantId, int menge)
    {
        // lieferantId traegt den Lieferantennamen (Abschnitt 7 der
        // Architektur), der Server erwartet aber die technische Lieferanten-ID.
        var detail = await Http.GetFromJsonAsync<ProductDetailDto>($"api/products/{produktId}")
            ?? throw new InvalidOperationException($"Produkt '{produktId}' nicht gefunden.");
        var angebot = detail.Offers.SingleOrDefault(o => o.SupplierName == lieferantId)
            ?? throw new WarenkorbAbgelehntException($"Lieferant '{lieferantId}' bietet Produkt '{produktId}' nicht an.");

        var antwort = await SendeMitCartId(HttpMethod.Post, "api/cart/items", sitzung, new AddCartItemRequest(produktId, angebot.SupplierId, menge));
        if (!antwort.IsSuccessStatusCode) throw new WarenkorbAbgelehntException();
    }

    public async Task<Warenkorb> HoleWarenkorb(string sitzung)
    {
        var antwort = await SendeMitCartId(HttpMethod.Get, "api/cart", sitzung, null);
        antwort.EnsureSuccessStatusCode();
        var cart = await antwort.Content.ReadFromJsonAsync<CartDto>() ?? throw new InvalidOperationException("Leere Warenkorb-Antwort.");
        return UebersetzeWarenkorb(cart);
    }

    public async Task AendereMenge(string sitzung, string positionId, int menge)
    {
        var (produktId, lieferantId) = ZerlegePositionId(positionId);
        var antwort = await SendeMitCartId(HttpMethod.Patch, $"api/cart/items/{produktId}/{lieferantId}", sitzung, new UpdateCartItemQuantityRequest(menge));
        antwort.EnsureSuccessStatusCode();
    }

    public async Task EntferneAusWarenkorb(string sitzung, string positionId)
    {
        var (produktId, lieferantId) = ZerlegePositionId(positionId);
        var antwort = await SendeMitCartId(HttpMethod.Delete, $"api/cart/items/{produktId}/{lieferantId}", sitzung, null);
        antwort.EnsureSuccessStatusCode();
    }

    public async Task<string> LegeBestellungAn(string sitzung, Lieferdaten lieferdaten, Kontaktdaten kontaktdaten)
    {
        var adresse = $"{lieferdaten.StrasseHausnummer}, {lieferdaten.Postleitzahl} {lieferdaten.Ort}";
        var antwort = await SendeMitCartId(HttpMethod.Post, "api/orders", sitzung,
            new PlaceOrderRequest(kontaktdaten.Name, adresse, kontaktdaten.Email));
        if (!antwort.IsSuccessStatusCode) throw new BestellungAbgelehntException();

        var order = await antwort.Content.ReadFromJsonAsync<OrderDto>() ?? throw new InvalidOperationException("Leere Bestell-Antwort.");
        _angelegteBestellungen.Add(UebersetzeBestellung(order));
        return order.OrderId;
    }

    public Task<IReadOnlyList<Bestellung>> HoleBestellungen(Kontaktdaten kontakt)
        => Task.FromResult<IReadOnlyList<Bestellung>>(_angelegteBestellungen);

    public async Task BewerteProdukt(string produktId, int wertung, string autor)
    {
        var antwort = await Http.PostAsJsonAsync($"api/products/{produktId}/ratings", new { authorName = autor, score = wertung });
        antwort.EnsureSuccessStatusCode();
    }

    private async Task<HttpResponseMessage> SendeMitCartId(HttpMethod methode, string pfad, string sitzung, object? body)
    {
        using var anfrage = new HttpRequestMessage(methode, pfad);
        anfrage.Headers.Add("X-Cart-Id", sitzung);
        if (body is not null) anfrage.Content = JsonContent.Create(body);
        return await Http.SendAsync(anfrage);
    }

    private static (string ProduktId, string LieferantId) ZerlegePositionId(string positionId)
    {
        var teile = positionId.Split('|', 2);
        return (teile[0], teile[1]);
    }

    private static Warenkorb UebersetzeWarenkorb(CartDto cart) => new(
        cart.Items.Select(i => new Warenkorbposition($"{i.ProductId}|{i.SupplierId}", i.ProductId, i.SupplierName, i.UnitPrice, i.Quantity)).ToList());

    private static Bestellung UebersetzeBestellung(OrderDto order) => new(
        order.OrderId, order.Status,
        order.Items.Select(i => new Bestellposition(i.ProductId, i.SupplierName, i.UnitPrice, i.Quantity)).ToList(),
        order.TotalPrice);
}
