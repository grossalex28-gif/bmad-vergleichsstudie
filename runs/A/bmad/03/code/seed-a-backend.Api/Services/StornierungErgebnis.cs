using seed_a_backend.Api.DTOs;

namespace seed_a_backend.Api.Services;

public enum StornierungErgebnisTyp
{
    Erfolgreich,
    NichtGefunden,
    BereitsStorniert,
}

public class StornierungErgebnis
{
    public required StornierungErgebnisTyp Typ { get; init; }

    public BuchungDetailDto? Buchung { get; init; }

    public static StornierungErgebnis Erfolg(BuchungDetailDto dto) =>
        new() { Typ = StornierungErgebnisTyp.Erfolgreich, Buchung = dto };

    public static StornierungErgebnis NichtGefunden() =>
        new() { Typ = StornierungErgebnisTyp.NichtGefunden };

    public static StornierungErgebnis BereitsStorniert() =>
        new() { Typ = StornierungErgebnisTyp.BereitsStorniert };
}
