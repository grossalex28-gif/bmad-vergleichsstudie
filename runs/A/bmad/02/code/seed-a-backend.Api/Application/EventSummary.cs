namespace seed_a_backend.Api.Application;

/// <summary>Projektion für die Programmübersicht, DTO-Mapping auf die API-Form bleibt im Controller (AD-5).</summary>
public record EventSummary(int Id, string Titel, string Spielstaette, DateTime Zeitpunkt);
