namespace A1Testsuite.Treiber;

/// <summary>
/// Simuliert einen Warenkorb rein im Adapter-Speicher, fuer die fuenf
/// Projekt-B-Implementierungen ohne serverseitigen Warenkorb-Endpunkt
/// (b_bmad_1, b_bmad_3, b_solo_1, b_solo_2, b_solo_3 -- nur b_bmad_2 hat
/// echte /api/cart-Endpunkte). Entscheidung vom 31.08.2026, siehe
/// claude/Uebergabe_A1_Messinstrument.md, Abschnitt "Warenkorb-Luecke":
/// LegeInWarenkorb/HoleWarenkorb/AendereMenge/EntferneAusWarenkorb laufen
/// bei diesen fuenf Implementierungen rein lokal, nur LegeBestellungAn ruft
/// tatsaechlich POST /api/orders mit der gesammelten Positionsliste auf.
///
/// Bewusste methodische Konsequenz: TF-B-F10/F11 (Bestellung anlegen und
/// abrufen) pruefen bei diesen fuenf Implementierungen weiterhin echt gegen
/// die SUT, TF-B-F8/F9 (Warenkorbinhalt/-summen vor dem Bestellen) pruefen
/// dann nur noch diese Klasse selbst, nicht mehr die SUT -- im
/// Methodik-Kapitel als Limitation zu dokumentieren.
/// </summary>
public sealed class LokalerWarenkorb
{
    // LieferantTechnischeId wird zusaetzlich zu LieferantName gefuehrt, weil
    // das Treiber-Interface Lieferanten nach Namen adressiert (Abschnitt 7
    // der Architektur), die tatsaechliche Bestellerstellung beim Server
    // aber die technische Lieferanten-ID braucht (aus dem Angebot in
    // HoleProdukt aufgeloest, siehe jeweiliger Adapter).
    private sealed record Position(string PositionId, string ProduktId, string LieferantName, string LieferantTechnischeId, decimal Einzelpreis, int Menge);

    private readonly Dictionary<string, List<Position>> _sitzungen = new();
    private int _naechstePositionId = 1;

    public string NeueSitzung()
    {
        var id = Guid.NewGuid().ToString("N");
        _sitzungen[id] = [];
        return id;
    }

    public void Hinzufuegen(string sitzung, string produktId, string lieferantName, string lieferantTechnischeId, decimal einzelpreis, int menge)
        => Liste(sitzung).Add(new Position((_naechstePositionId++).ToString(), produktId, lieferantName, lieferantTechnischeId, einzelpreis, menge));

    public Warenkorb Hole(string sitzung)
        => new(Liste(sitzung).Select(p => new Warenkorbposition(p.PositionId, p.ProduktId, p.LieferantName, p.Einzelpreis, p.Menge)).ToList());

    public void AendereMenge(string sitzung, string positionId, int menge)
    {
        var liste = Liste(sitzung);
        var idx = liste.FindIndex(p => p.PositionId == positionId);
        if (idx < 0) throw new InvalidOperationException($"Warenkorbposition '{positionId}' nicht gefunden.");
        liste[idx] = liste[idx] with { Menge = menge };
    }

    public void Entfernen(string sitzung, string positionId)
        => Liste(sitzung).RemoveAll(p => p.PositionId == positionId);

    /// <summary>Liest den aktuellen Inhalt aus und leert die Sitzung (nach erfolgreicher Bestellung).</summary>
    public IReadOnlyList<(string ProduktId, string LieferantTechnischeId, int Menge)> Leeren(string sitzung)
    {
        var inhalt = Liste(sitzung).Select(p => (p.ProduktId, p.LieferantTechnischeId, p.Menge)).ToList();
        Liste(sitzung).Clear();
        return inhalt;
    }

    private List<Position> Liste(string sitzung)
        => _sitzungen.TryGetValue(sitzung, out var liste)
            ? liste
            : throw new InvalidOperationException($"Warenkorbsitzung '{sitzung}' nicht gefunden.");
}
