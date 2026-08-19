namespace seed_a_backend.Api.Controllers.Dtos;

public record EventDetailDto(
    int Id,
    string Titel,
    string Beschreibung,
    int DauerMinuten,
    int Altersfreigabe,
    string Spielstaette,
    string Raum,
    DateTime Zeitpunkt,
    IReadOnlyList<PriceCategoryDto> Preiskategorien);
