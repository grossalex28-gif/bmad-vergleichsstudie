namespace seed_a_backend.Api.Application;

/// <summary>Projektion für die Veranstaltungsdetails, DTO-Mapping auf die API-Form bleibt im Controller (AD-5).</summary>
public record EventDetail(
    int Id,
    string Titel,
    string Beschreibung,
    int DauerMinuten,
    int Altersfreigabe,
    string Spielstaette,
    string Raum,
    DateTime Zeitpunkt,
    IReadOnlyList<PriceCategorySummary> Preiskategorien);
