using seed_a_backend.Api.DTOs;

namespace seed_a_backend.Api.Services;

public interface IBuchungenService
{
    Task<BuchungErgebnis> BuchungAnlegenAsync(string veranstaltungId, BuchungAnlegenRequestDto request);

    Task<BuchungDetailDto?> BuchungAbrufenAsync(string referenz);

    Task<StornierungErgebnis> BuchungStornierenAsync(string referenz);
}
