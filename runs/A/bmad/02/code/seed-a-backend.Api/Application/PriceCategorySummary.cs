namespace seed_a_backend.Api.Application;

/// <summary>Projektion einer Preiskategorie für die Veranstaltungsdetails, DTO-Mapping bleibt im Controller (AD-5).</summary>
public record PriceCategorySummary(int Id, string Name, decimal Preis);
