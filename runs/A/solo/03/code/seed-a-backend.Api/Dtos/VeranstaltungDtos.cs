namespace seed_a_backend.Api.Dtos;

public record SpielstaetteListItemDto(string Id, string Name);

public record VeranstaltungListItemDto(
    string Id,
    string Titel,
    string SpielstaetteId,
    string SpielstaetteName,
    DateTime Zeitpunkt);

public record RaumInfoDto(string Id, string Name);

public record PreiskategorieDto(string Id, string Name, decimal Preis);

public record VeranstaltungDetailDto(
    string Id,
    string Titel,
    string Beschreibung,
    DateTime Zeitpunkt,
    int DauerMinuten,
    int Altersfreigabe,
    SpielstaetteListItemDto Spielstaette,
    RaumInfoDto Raum,
    IReadOnlyList<PreiskategorieDto> Preiskategorien);

public enum SitzplatzTyp
{
    Sitzplatz,
    Gang
}

public enum SitzplatzStatus
{
    Frei,
    Belegt
}

public record SitzplatzDto(
    string Reihe,
    int Spalte,
    SitzplatzTyp Typ,
    SitzplatzStatus? Status);

public record RaumSitzplanDto(
    IReadOnlyList<string> Reihen,
    int Spalten,
    IReadOnlyList<int> GangSpalten,
    string? GangHinweis);

public record SitzplanDto(
    string VeranstaltungId,
    RaumSitzplanDto Raum,
    IReadOnlyList<SitzplatzDto> Sitzplaetze,
    IReadOnlyList<PreiskategorieDto> Preiskategorien);
