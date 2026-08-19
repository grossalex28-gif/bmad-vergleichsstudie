using System.Text.Json;

namespace seed_b_backend.Api.Dtos;

public record ProductListItemDto(
    string Id,
    string Name,
    string Beschreibung,
    string KategorieId,
    string KategorieName,
    string UnterkategorieId,
    string UnterkategorieName,
    Dictionary<string, JsonElement> Eigenschaften,
    decimal? MinPreis,
    double? DurchschnittsBewertung,
    int AnzahlBewertungen,
    int Aufrufe);

public record OfferDto(string LieferantId, string LieferantName, decimal Preis);

public record ProductDetailDto(
    string Id,
    string Name,
    string Beschreibung,
    string KategorieId,
    string KategorieName,
    string UnterkategorieId,
    string UnterkategorieName,
    Dictionary<string, JsonElement> Eigenschaften,
    double? DurchschnittsBewertung,
    int AnzahlBewertungen,
    int Aufrufe,
    List<OfferDto> Angebote);

public record PagedResult<T>(List<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public record RatingCreateDto(string AutorName, int Wert);

public record RatingResponseDto(double DurchschnittsBewertung, int AnzahlBewertungen);
