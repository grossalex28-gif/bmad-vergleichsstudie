using seed_a_backend.Api.Dtos;

namespace seed_a_backend.Api.Services;

public interface IVeranstaltungService
{
    Task<IReadOnlyList<SpielstaetteListItemDto>> GetSpielstaettenAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VeranstaltungListItemDto>> GetVeranstaltungenAsync(
        DateTime? von,
        DateTime? bis,
        string? spielstaetteId,
        CancellationToken cancellationToken = default);

    Task<VeranstaltungDetailDto> GetDetailAsync(string veranstaltungId, CancellationToken cancellationToken = default);

    Task<SitzplanDto> GetSitzplanAsync(string veranstaltungId, CancellationToken cancellationToken = default);
}
