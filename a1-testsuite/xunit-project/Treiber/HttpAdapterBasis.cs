using System.Net.Http;
using System.Text.Json;

namespace A1Testsuite.Treiber;

/// <summary>
/// Gemeinsame HTTP-Grundausstattung fuer alle zwoelf Adapter. Reine Technik
/// (HttpClient, JSON-Optionen) -- keine Fachlogik. Fachlogik, Wiederholungs-
/// versuche oder Fehlerkompensation sind im Adapter ausdruecklich untersagt,
/// siehe Abschnitt 6 der Akzeptanztest-Architektur.
/// </summary>
public abstract class HttpAdapterBasis
{
    protected HttpClient Http { get; }

    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected HttpAdapterBasis(string basisUrl)
    {
        Http = new HttpClient { BaseAddress = new Uri(basisUrl) };
    }

    /// <summary>
    /// Zerlegt eine Sitzplatzbezeichnung in der Notation "Reihe+Spalte"
    /// (z. B. "A1", "B12") in Reihe und Spalte. Reine Syntax, keine
    /// Fachlogik -- wird von den Projekt-A-Adaptern zur Uebersetzung
    /// zwischen der fachlichen Bezeichnung (Treiber-Interface) und den
    /// technischen Reihe/Spalte- bzw. Sitzplatz-ID-Feldern der jeweiligen
    /// Implementierung verwendet.
    /// </summary>
    protected static (string Reihe, int Spalte) ZerlegeSitzplatzBezeichnung(string bezeichnung)
    {
        var i = 0;
        while (i < bezeichnung.Length && !char.IsDigit(bezeichnung[i])) i++;
        return (bezeichnung[..i], int.Parse(bezeichnung[i..]));
    }

    /// <summary>
    /// Deutet einen als String kodierten Sitzplatz-Status. Mehrere
    /// Implementierungen liefern diesen Status nur als "type: string" ohne
    /// dokumentierte Werte (kein "enum" in der OpenAPI-Beschreibung) --
    /// diese robuste Teilstring-Pruefung deckt sowohl deutsche als auch
    /// englische Bezeichner ab, statt sich auf eine einzelne geratene
    /// Zeichenkette festzulegen. Reine Uebersetzung eines vom Server
    /// gelieferten Werts, keine Fachlogik.
    /// </summary>
    protected static bool IstBelegtStatus(string status) =>
        status.Contains("beleg", StringComparison.OrdinalIgnoreCase) ||
        status.Contains("occup", StringComparison.OrdinalIgnoreCase) ||
        status.Contains("taken", StringComparison.OrdinalIgnoreCase) ||
        status.Contains("reserv", StringComparison.OrdinalIgnoreCase) ||
        status.Contains("book", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Analog zu <see cref="IstBelegtStatus"/>, aber fuer einen als String
    /// kodierten Positionstyp (Sitz vs. Gang).
    /// </summary>
    protected static bool IstGangTyp(string typ) =>
        typ.Contains("gang", StringComparison.OrdinalIgnoreCase) ||
        typ.Contains("aisle", StringComparison.OrdinalIgnoreCase) ||
        typ.Contains("corridor", StringComparison.OrdinalIgnoreCase);
}
