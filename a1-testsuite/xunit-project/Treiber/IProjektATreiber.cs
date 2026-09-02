namespace A1Testsuite.Treiber;

// Treiberschnittstelle Projekt A (Buchungssystem), Schicht 2 der
// Akzeptanztest-Architektur v2.0, Abschnitt 5. Rein fachlich, keine
// technischen Details -- die Uebersetzung auf die tatsaechlichen Endpunkte
// einer Implementierung ist Aufgabe des jeweiligen Adapters (Abschnitt 6).

public sealed record VeranstaltungUebersicht(
    string Id,
    string Titel,
    string Spielstaette,
    string Raum,
    DateTime Zeitpunkt);

public sealed record VeranstaltungDetail(
    string Id,
    string Beschreibung,
    int DauerMinuten,
    int Altersfreigabe,
    string Spielstaette,
    string Raum,
    DateTime Zeitpunkt);

public enum PositionsTyp { Sitz, Gang }

public enum PositionsStatus { Frei, Belegt }

/// <summary>
/// Ein Platz im Sitzplan. <see cref="Bezeichnung"/> folgt der in den
/// Testfaellen verwendeten Notation "Reihe+Spalte", z. B. "A1" = Reihe A,
/// Spalte 1. <see cref="Status"/> ist nur fuer Positionen vom Typ
/// <see cref="PositionsTyp.Sitz"/> gesetzt, fuer Gaenge null.
/// </summary>
public sealed record SitzplanPosition(
    string Reihe,
    int Spalte,
    PositionsTyp Typ,
    PositionsStatus? Status)
{
    public string Bezeichnung => $"{Reihe}{Spalte}";
}

public sealed record Sitzplan(
    int AnzahlReihen,
    int AnzahlSpalten,
    IReadOnlyList<SitzplanPosition> Positionen);

public sealed record Preiskategorie(string Id, string Bezeichnung, decimal Preis);

/// <summary>
/// Ein gewaehlter Sitzplatz mit Kategoriewahl. <see cref="SitzplatzBezeichnung"/>
/// in derselben "Reihe+Spalte"-Notation wie <see cref="SitzplanPosition.Bezeichnung"/>,
/// <see cref="KategorieBezeichnung"/> als Name aus <see cref="Preiskategorie.Bezeichnung"/>
/// (Adressierung ueber den Namen, nicht die technische ID, Abschnitt 7).
/// </summary>
public sealed record SitzplatzAuswahl(string SitzplatzBezeichnung, string KategorieBezeichnung);

public sealed record Buchungsposition(string SitzplatzBezeichnung, string KategorieBezeichnung, decimal Einzelpreis);

public sealed record Buchung(string Referenz, IReadOnlyList<Buchungsposition> Positionen, decimal Gesamtpreis, string Status);

/// <summary>
/// Wird von <see cref="IProjektATreiber.LegeBuchungAn"/> geworfen, wenn die
/// Buchung abgelehnt wird (A-F13). Geprueft wird nach Abschnitt 7 nur, dass
/// die Exception geworfen wird, nicht der Wortlaut einer Begruendung.
/// </summary>
public sealed class BuchungAbgelehntException(string? grund = null)
    : Exception(grund ?? "Buchung abgelehnt");

public interface IProjektATreiber
{
    Task<IReadOnlyList<VeranstaltungUebersicht>> ListeVeranstaltungen(
        DateTime? vonDatum = null, DateTime? bisDatum = null, string? spielstaette = null);

    Task<VeranstaltungDetail> HoleVeranstaltung(string id);

    Task<Sitzplan> HoleSitzplan(string veranstaltungId);

    Task<IReadOnlyList<Preiskategorie>> HolePreiskategorien(string veranstaltungId);

    Task<decimal> BerechnePreis(string veranstaltungId, IReadOnlyList<SitzplatzAuswahl> auswahl);

    /// <exception cref="BuchungAbgelehntException">
    /// mindestens ein gewaehlter Sitzplatz ist zwischenzeitlich belegt (A-F13)
    /// </exception>
    Task<string> LegeBuchungAn(string veranstaltungId, IReadOnlyList<SitzplatzAuswahl> auswahl, string name, string email);

    Task<Buchung> HoleBuchung(string referenz);

    Task StorniereBuchung(string referenz);
}
