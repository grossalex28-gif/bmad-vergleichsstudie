namespace seed_b_backend.Api.Dtos;

public record LieferdatenDto(string Name, string Strasse, string Plz, string Ort, string Land);

public record KontaktdatenDto(string Email, string Telefon);

public record PositionCreateDto(string ProduktId, string LieferantId, int Menge);

public record OrderCreateDto(LieferdatenDto Lieferdaten, KontaktdatenDto Kontakt, List<PositionCreateDto> Positionen);

public record OrderItemResponseDto(
    string ProduktId,
    string ProduktName,
    string LieferantId,
    string LieferantName,
    decimal Preis,
    int Menge,
    decimal Zwischensumme);

public record OrderResponseDto(
    int Id,
    string Status,
    DateTime ErstelltAm,
    LieferdatenDto Lieferdaten,
    KontaktdatenDto Kontakt,
    List<OrderItemResponseDto> Positionen,
    decimal Gesamtsumme);
