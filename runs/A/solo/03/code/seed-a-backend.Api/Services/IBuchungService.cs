using seed_a_backend.Api.Dtos;

namespace seed_a_backend.Api.Services;

public interface IBuchungService
{
    Task<BuchungDto> CreateBuchungAsync(CreateBuchungRequestDto request, CancellationToken cancellationToken = default);

    Task<BuchungDto> GetByReferenzAsync(string referenz, CancellationToken cancellationToken = default);

    Task<BuchungDto> StornierenAsync(string referenz, CancellationToken cancellationToken = default);
}
