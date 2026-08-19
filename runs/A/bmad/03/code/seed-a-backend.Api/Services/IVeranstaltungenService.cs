using seed_a_backend.Api.DTOs;

namespace seed_a_backend.Api.Services;

public interface IVeranstaltungenService
{
    Task<IReadOnlyList<VeranstaltungListeDto>> GetVeranstaltungenAsync(DateOnly? von = null, DateOnly? bis = null, string? spielstaetteId = null);

    Task<VeranstaltungDetailDto?> GetVeranstaltungAsync(string id);

    Task<SitzplanDto?> GetSitzplanAsync(string veranstaltungId);
}
