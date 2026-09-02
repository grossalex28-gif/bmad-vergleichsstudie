namespace A1Testsuite.Treiber;

// Treiberschnittstelle Projekt B (E-Commerce-Plattform), Schicht 2 der
// Akzeptanztest-Architektur v2.0, Abschnitt 5 (angepasst an die
// Lieferantenbeziehung). Rein fachlich, keine technischen Details.

public sealed record Unterkategorie(string Name);

public sealed record Oberkategorie(string Name, IReadOnlyList<Unterkategorie> Unterkategorien);

public sealed record ProduktUebersicht(string Id, string Name, decimal AbPreis, string KategorieName);

public sealed record ProduktlisteErgebnis(
    IReadOnlyList<ProduktUebersicht> Produkte,
    int Seite,
    int SeitenAnzahl,
    int GesamtAnzahl);

public sealed record LieferantenAngebot(string LieferantName, decimal Preis);

public sealed record ProduktDetail(
    string Id,
    string Name,
    string Beschreibung,
    string KategorieName,
    IReadOnlyDictionary<string, string> Eigenschaften,
    IReadOnlyList<LieferantenAngebot> Lieferanten,
    double? DurchschnittsBewertung,
    int AnzahlBewertungen,
    int Aufrufzaehler);

public sealed record Warenkorbposition(string PositionId, string ProduktId, string LieferantName, decimal Einzelpreis, int Menge)
{
    public decimal Zwischensumme => Einzelpreis * Menge;
}

public sealed record Warenkorb(IReadOnlyList<Warenkorbposition> Positionen)
{
    public decimal Gesamtsumme => Positionen.Sum(p => p.Zwischensumme);
}

public sealed record Lieferdaten(string EmpfaengerName, string StrasseHausnummer, string Postleitzahl, string Ort);

public sealed record Kontaktdaten(string Name, string Email);

public sealed record Bestellposition(string ProduktId, string LieferantName, decimal FestgeschriebenerPreis, int Menge);

public sealed record Bestellung(string Kennung, string Status, IReadOnlyList<Bestellposition> Positionen, decimal Gesamtsumme);

/// <summary>
/// Wird von <see cref="IProjektBTreiber.LegeInWarenkorb"/> geworfen, wenn das
/// Produkt bei diesem Lieferanten nicht mehr angeboten wird oder die Menge
/// unzulaessig ist (B-F14-2, implementierungsabhaengig).
/// </summary>
public sealed class WarenkorbAbgelehntException(string? grund = null)
    : Exception(grund ?? "Warenkorb-Operation abgelehnt");

/// <summary>
/// Wird von <see cref="IProjektBTreiber.LegeBestellungAn"/> geworfen, wenn die
/// Bestellung abgelehnt wird (B-F14: leerer Warenkorb oder unzulaessige
/// Menge). Geprueft wird nach Abschnitt 7 nur, dass die Exception geworfen
/// wird, nicht der Wortlaut einer Begruendung.
/// </summary>
public sealed class BestellungAbgelehntException(string? grund = null)
    : Exception(grund ?? "Bestellung abgelehnt");

public interface IProjektBTreiber
{
    Task<ProduktlisteErgebnis> ListeProdukte(
        int seite,
        string? kategorieId = null,
        string? sortierung = null,
        IReadOnlyDictionary<string, string>? eigenschaftsfilter = null);

    Task<IReadOnlyList<Oberkategorie>> HoleKategoriebaum();

    Task<IReadOnlyList<ProduktUebersicht>> SucheProdukte(string suchtext);

    Task<ProduktDetail> HoleProdukt(string id);

    Task<string> NeueWarenkorbSitzung();

    /// <exception cref="WarenkorbAbgelehntException">
    /// Produkt bei diesem Lieferanten nicht mehr angeboten, oder Menge unzulaessig (B-F14-2)
    /// </exception>
    Task LegeInWarenkorb(string sitzung, string produktId, string lieferantId, int menge);

    Task<Warenkorb> HoleWarenkorb(string sitzung);

    Task AendereMenge(string sitzung, string positionId, int menge);

    Task EntferneAusWarenkorb(string sitzung, string positionId);

    /// <exception cref="BestellungAbgelehntException">
    /// Warenkorb leer, oder eine Position enthaelt eine unzulaessige Menge (B-F14)
    /// </exception>
    Task<string> LegeBestellungAn(string sitzung, Lieferdaten lieferdaten, Kontaktdaten kontaktdaten);

    Task<IReadOnlyList<Bestellung>> HoleBestellungen(Kontaktdaten kontakt);

    Task BewerteProdukt(string produktId, int wertung, string autor);
}

/// <summary>
/// Exakte Sortierschluessel-Literale, wie sie in den Testfaellen verwendet
/// werden (Abschnitt "Konventionen" in claude/Akzeptanztestfaelle_ProjektB.md).
/// Adapter uebersetzen diese auf den tatsaechlichen Sortierparameter der
/// jeweiligen Implementierung.
/// </summary>
public static class Sortierungen
{
    public const string PreisAufsteigend = "Preis aufsteigend";
    public const string NameAbsteigend = "Name absteigend";
    public const string AufrufzaehlerAbsteigend = "Aufrufz\u00e4hler absteigend";
}
