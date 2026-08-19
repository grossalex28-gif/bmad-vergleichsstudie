namespace seed_a_backend.Api.Controllers.Dtos;

/// <summary>Einheitliches API-Fehler-Envelope-Schema (AD-7): `{ code, message, details? }`.</summary>
public record ErrorEnvelopeDto(string Code, string Message, object? Details = null);
