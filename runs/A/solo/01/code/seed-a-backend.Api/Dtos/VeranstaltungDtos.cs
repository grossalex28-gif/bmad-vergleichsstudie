namespace seed_a_backend.Api.Dtos;

public record VeranstaltungListItemDto(
    string Id,
    string Titel,
    string SpielstaetteId,
    string SpielstaetteName,
    DateTime Zeitpunkt
);

public record PreiskategorieDto(string Id, string Name, decimal Preis);

public record VeranstaltungDetailDto(
    string Id,
    string Titel,
    string Beschreibung,
    int DauerMinuten,
    int Altersfreigabe,
    string SpielstaetteId,
    string SpielstaetteName,
    string RaumId,
    string RaumName,
    DateTime Zeitpunkt,
    IReadOnlyList<PreiskategorieDto> Preiskategorien
);

public record SitzplatzPositionDto(string Reihe, int Spalte);

public record SitzplanDto(
    string RaumName,
    IReadOnlyList<string> Reihen,
    int Spalten,
    IReadOnlyList<int> GangSpalten,
    string? GangHinweis,
    IReadOnlyList<SitzplatzPositionDto> BelegtePlaetze
);
