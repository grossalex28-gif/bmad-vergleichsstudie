namespace seed_a_backend.Api.DTOs;

public record VeranstaltungListeDto(
    string Id,
    string Titel,
    string SpielstaetteId,
    string SpielstaetteName,
    DateTimeOffset Zeitpunkt);
