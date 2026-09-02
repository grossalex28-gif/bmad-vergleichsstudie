# A1-Testsuite (xUnit-Projekt)

Setzt `claude/Akzeptanztests_Architektur.md` (v2.0) und die beiden
Akzeptanztestfaelle-Dokumente (`claude/Akzeptanztestfaelle_ProjektA.md`,
`_ProjektB.md`) technisch um. Aufgerufen wird dieses Projekt von
`run-a1.sh` im Repository-Wurzelverzeichnis:

```
dotnet test a1-testsuite/xunit-project --filter "FullyQualifiedName~<db_name>_Tests" ...
```

## Struktur

- `Treiber/` — `IProjektATreiber`, `IProjektBTreiber` (Schicht 2 der
  Architektur, Abschnitt 5), die zugehoerigen Datentypen, die
  Ablehnungs-Exceptions (Abschnitt 7) und `HttpAdapterBasis` als
  gemeinsame, rein technische Grundausstattung fuer die Adapter (HttpClient,
  JSON-Optionen — keine Fachlogik).
- `Basisklassen/` — `ProjektATestsBase`, `ProjektBTestsBase` (Schicht 1):
  je ein `[Fact]` pro Testfall aus den beiden Testfaelle-Dokumenten, formuliert
  ausschliesslich gegen die Treiber-Interfaces.
- `Implementierungen/` — **noch leer.** Hier fehlen noch:
  - zwoelf Adapter-Klassen (Schicht 3), je eine pro Implementierung, gebaut
    ausschliesslich anhand der jeweiligen generierten OpenAPI-Beschreibung
    (`http://localhost:5001/openapi/v1.json` bzw. `/swagger/v1/swagger.json`
    waehrend die jeweilige Implementierung laeuft), nach den Regeln aus
    Abschnitt 6 der Architektur (reine Uebersetzung, Richtwert 150 Zeilen,
    duerfen von einer separaten, nicht gemessenen Modellsitzung erzeugt,
    muessen aber manuell geprueft werden).
  - zwoelf kleine konkrete Testklassen, eine je Implementierung, z. B.:

    ```csharp
    namespace A1Testsuite.Implementierungen;

    public sealed class a_bmad_1_Tests : ProjektATestsBase
    {
        public a_bmad_1_Tests() : base(new ABmad1Adapter("http://localhost:5001")) { }
    }
    ```

    Der Klassenname muss `<db_name>_Tests` lauten (z. B. `a_bmad_1_Tests`),
    weil `run-a1.sh` genau danach filtert.

## Design-Entscheidung: Wie werden Veranstaltungen/Produkte adressiert?

Die Architektur legt fest, dass Kategorien und Lieferanten im Treiber-Interface
ueber ihren Namen adressiert werden (Abschnitt 7), nicht ueber eine technische
ID. Fuer Veranstaltungen ("E1"–"E6") und Produkte ("P1"–"P14") ist das in den
Dokumenten nicht explizit geregelt — "E1"/"P1" sind reine Bezeichnungen aus
`claude/Anfangsdatenbestand_Spezifikation.md` fuer die Spezifikation, keine
echten IDs, die in einer laufenden Implementierung so vorkommen.

Deshalb loesen die Basisklassen die tatsaechliche technische ID zur Laufzeit
selbst auf: `FindeVeranstaltungId(titel)` ruft `ListeVeranstaltungen()` auf und
sucht den Eintrag mit passendem `Titel`; `FindeProduktId(name)` ruft
`SucheProdukte(name)` auf und sucht den Treffer mit passendem `Name`. Das ist
robuster als eine implizite Nummern-Zuordnung im Adapter und erfordert von den
zwoelf Adaptern keine Sonderbehandlung ueber die normale Uebersetzung von
`ListeVeranstaltungen`/`SucheProdukte` hinaus. Diese Entscheidung ist bisher
nicht gesondert im Architektur-Dokument festgehalten — bei Bedarf dort
nachtragen.

## Noch offen

Adapter und konkrete Testklassen fuer alle zwoelf Implementierungen (siehe
oben). Dafuer wird je Implementierung kurz das Backend gestartet und die
OpenAPI-Beschreibung geholt — praktischerweise laesst sich das mit demselben
`start_backend`-Mechanismus wie in `run-a1.sh` erledigen.
