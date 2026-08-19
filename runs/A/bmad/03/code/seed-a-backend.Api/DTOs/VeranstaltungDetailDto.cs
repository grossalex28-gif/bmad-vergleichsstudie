namespace seed_a_backend.Api.DTOs;

public record VeranstaltungDetailDto(
    string Id,
    string Titel,
    string Beschreibung,
    int DauerMinuten,
    int Altersfreigabe,
    string SpielstaetteName,
    string RaumName,
    DateTimeOffset Zeitpunkt,
    IReadOnlyList<PreiskategorieDto> Preiskategorien);
