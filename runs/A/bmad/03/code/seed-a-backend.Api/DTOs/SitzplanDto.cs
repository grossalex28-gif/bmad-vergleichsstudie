namespace seed_a_backend.Api.DTOs;

public record SitzplanDto(string VeranstaltungId, string RaumId, string RaumName, IReadOnlyList<SitzplanReiheDto> Reihen);

public record SitzplanReiheDto(string Reihe, IReadOnlyList<SitzplanPositionDto> Positionen);

public record SitzplanPositionDto(int Spalte, string Typ, string? Code, string? Status);
