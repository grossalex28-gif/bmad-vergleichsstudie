using seed_a_backend.Api.DTOs;

namespace seed_a_backend.Api.Services;

public enum BuchungErgebnisTyp
{
    Erfolgreich,
    VeranstaltungNichtGefunden,
    PreiskategorieUngueltig,
    SitzplatzDuplikat,
    SitzplatzKonflikt,
    ReferenzErzeugungFehlgeschlagen,
}

public class BuchungErgebnis
{
    public required BuchungErgebnisTyp Typ { get; init; }

    public BuchungDto? Buchung { get; init; }

    public IReadOnlyList<string>? BetroffeneSitzplaetze { get; init; }

    public string? UngueltigePreiskategorieId { get; init; }

    public static BuchungErgebnis Erfolg(BuchungDto dto) =>
        new() { Typ = BuchungErgebnisTyp.Erfolgreich, Buchung = dto };

    public static BuchungErgebnis VeranstaltungFehlt() =>
        new() { Typ = BuchungErgebnisTyp.VeranstaltungNichtGefunden };

    public static BuchungErgebnis PreiskategorieFehlt(string preiskategorieId) =>
        new() { Typ = BuchungErgebnisTyp.PreiskategorieUngueltig, UngueltigePreiskategorieId = preiskategorieId };

    public static BuchungErgebnis SitzplatzDuplikat() =>
        new() { Typ = BuchungErgebnisTyp.SitzplatzDuplikat };

    public static BuchungErgebnis Konflikt(IReadOnlyList<string> betroffeneSitzplaetze) =>
        new() { Typ = BuchungErgebnisTyp.SitzplatzKonflikt, BetroffeneSitzplaetze = betroffeneSitzplaetze };

    public static BuchungErgebnis ReferenzFehlgeschlagen() =>
        new() { Typ = BuchungErgebnisTyp.ReferenzErzeugungFehlgeschlagen };
}
