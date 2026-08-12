# Zeitregime der automatisierten Läufe

*Präzisierung zu Abschnitt 6 „Automatisierte Durchführung", Unterabschnitt Terminierung · Stand 07.08.2026*

Dieses Dokument **ändert** keine Festlegung des Gesamtstands, sondern füllt die dort offen gelassenen Zahlenwerte („großzügig gesetztes Token- und Zeitbudget", offener Punkt in Abschnitt 16). Bei Widersprüchen gilt weiterhin der Gesamtstand.

**Gewählte Rahmung:** umfangsfixiertes Design. Der Zeitdeckel ist eine Notbremse und soll im Regelfall nicht greifen. A2 bleibt damit interpretierbar als „wie vollständig wurde der Auftrag umgesetzt" und nicht als „wie viel schafft das Werkzeug in X Stunden".

---

## 1. Vier Zeitgrößen, vier Zwecke

Ein einzelner Zeitwert reicht nicht, weil Wartezeiten aus dem Abo-Rate-Limit sonst dasselbe Budget verzehren wie tatsächliche Arbeit. Deshalb vier getrennte Größen:

| ID | Größe | Wert | Wirkung beim Erreichen |
|---|---|---|---|
| **T1** | Aktivzeit je Lauf | **8 h** | Terminierung des Laufs (regulär, Befund) |
| **T2** | Kalenderdeckel je Lauf | **36 h** ab Laufbeginn | Terminierung, aber als *Umgebungs-*Befund getrennt berichtet |
| **T3** | Deckel je Einzelschritt | **90 min** | Blocker nach H7, keine Terminierung |
| **T4** | Kampagnendeckel | **21 Kalendertage** | Entscheidungspunkt, siehe Abschnitt 4 |

**T1 – Aktivzeit.** Definiert als Wall-Clock des Laufs abzüglich protokollierter Rate-Limit-Pausen und abzüglich Operator-Wartezeit nach einem Blocker. Das ist die Größe, die dem Werkzeug zurechenbar ist, und damit die einzige, die als Behandlungsbudget taugen kann. Sie ist in beiden Bedingungen identisch. Wird sie erreicht, terminiert der Lauf nach dem gerade laufenden Schritt; es findet keine Nachbesserung statt.

**T2 – Kalenderdeckel.** Schützt den Gesamtplan. Ein Lauf, der acht Stunden Arbeit auf drei Kalendertage verteilt, weil er ständig in Limitpausen fällt, sprengt die Kampagne, obwohl T1 nie greift. T2 beendet ihn. Wichtig für die Auswertung: Ein durch T2 beendeter Lauf ist **kein** Befund über BMAD oder Claude, sondern über die Abo-Bedingungen. Solche Läufe werden gesondert ausgewiesen und sind Kandidaten für eine Wiederholung, wenn der Zeitplan sie hergibt.

**T3 – Schrittdeckel.** Dient der Stall-Erkennung, nicht der Terminierung. Wenn ein einzelner Schritt — eine Story, eine Planungsphase — 90 Minuten überschreitet, hängt mit hoher Wahrscheinlichkeit etwas (Endlosschleife im Build, wartender Prozess). Das ist ein Blockertatbestand nach H7 und gehört in C4.

**T4 – Kampagnendeckel.** Der äußere Rahmen von rund drei Wochen inklusive Wochenenden.

---

## 2. Der Deckel wird dem Modell nicht mitgeteilt

Das ist keine Formalie, sondern eine Bedingung der Validität. Ein in die Eingabedatei geschriebener Hinweis wie „du hast acht Stunden" wäre eine Prozessanweisung und zugleich ein Verhaltensappell — genau die Kategorie, die die Eingabespezifikation ausschließt. Er würde beide Bedingungen behandeln und insbesondere die Solo-Bedingung zu einem anderen Vorgehen verleiten.

Der Deckel wird deshalb **ausschließlich harnessseitig** durchgesetzt: Das Skript misst mit, beendet den Lauf an der nächsten Schrittgrenze und schreibt das Ergebnis ins Protokoll. Weder BMAD noch Claude erfahren von seiner Existenz. Das ist im Methodikkapitel ausdrücklich festzuhalten.

---

## 3. Vorab festgelegte Rahmungsregel

Damit die Wahl „umfangsfixiert" nicht nachträglich unehrlich wird, gilt eine vor der Erhebung fixierte Regel:

> Greift T1 in **mehr als einem** der drei BMAD-Läufe eines Projekts, gilt der Deckel als bindend. Das Design ist dann für dieses Projekt nachträglich als **zeitfixiert** zu deklarieren, A2 wird zur primären Ergebnisgröße, und alle übrigen A-Metriken werden ausschließlich auf A2 normiert berichtet.

Der Wert dieser Regel liegt darin, dass sie vorher feststeht. Sie nimmt der Kritik den Angriffspunkt, die Rahmung sei an die Ergebnisse angepasst worden. Sie gehört als solche ins Methodikkapitel, nicht in die Diskussion.

Ergänzend gilt dasselbe für das Tokenbudget: Auch dort ist vorab zu fixieren, ab welcher Häufigkeit ein Greifen als bindend zu werten ist.

---

## 4. Kampagnenplanung

**Rechnung.** Zwölf Läufe zu höchstens acht Stunden Aktivzeit ergeben 96 Stunden Obergrenze. Der Erwartungswert liegt deutlich darunter — bei einem vollständigen BMAD-Durchlauf realistisch bei vier bis sechs Stunden, in der Solo-Bedingung eher darunter —, also grob 50 bis 70 Stunden Aktivzeit über die gesamte Kampagne. Bei einem Lauf je Kalendertag sind das zwölf Tage reine Durchführung. In das Fenster von 14 bis 21 Tagen passt das, aber der Puffer für Wiederholungen ist schmal.

**Zwei Bedingungen, ohne die die Rechnung nicht aufgeht:**

Erstens muss der Harness **unbeaufsichtigt über Nacht** laufen können, einschließlich selbsttätiger Fortsetzung nach einer Limitpause. Wenn jeder Lauf deine Anwesenheit braucht, halbiert sich die nutzbare Zeit. Das ist derselbe Mechanismus wie das gewünschte Pausieren und Fortsetzen; er wird ohnehin gebaut.

Zweitens laufen die Läufe **strikt sequenziell**. Parallele Läufe teilen sich dasselbe Rate-Limit, treiben sich gegenseitig in Wartezeiten und verfälschen dadurch C1c in beiden. Der scheinbare Zeitgewinn ist keiner.

**Reihenfolge.** Die Bedingungen werden abwechselnd gefahren, nicht blockweise:

```
A-BMAD-1 · A-Solo-1 · A-BMAD-2 · A-Solo-2 · A-BMAD-3 · A-Solo-3
B-BMAD-1 · B-Solo-1 · B-BMAD-2 · B-Solo-2 · B-BMAD-3 · B-Solo-3
```

Das hat zwei Gründe. Erstens sind bei jedem vorzeitigen Ende vollständige, balancierte Paare vorhanden statt einer fertigen und einer leeren Bedingung. Zweitens verteilt es eine mögliche Modellaktualisierung oder Anbieterlastschwankung während der Erhebung gleichmäßig über beide Bedingungen, statt sie einem zuzuschlagen — eine Zeitreihen-Konfundierung, die bei blockweiser Abarbeitung schwer auszuräumen wäre.

**Entscheidungspunkt nach Lauf 6.** Ist Projekt A abgeschlossen und sind bis dahin mehr als zwölf Kalendertage verbraucht, entfällt Projekt B. Das entspricht der Prioritätsregel des Gesamtstands: drei Wiederholungen je Zelle sind unverhandelbar, das zweite Projekt ist nachrangig. Sechs saubere Läufe an einem Projekt tragen die Arbeit; zwölf halbfertige tun es nicht.

---

## 5. Was daraus für das Harness-Skript folgt

- Drei Timer je Lauf (T1, T2, T3), wobei T1 bei erkannter Limitpause und bei Blocker-Wartezeit angehalten wird.
- Jede Pause wird mit Beginn, Ende und Ursache in `protokoll.csv` geschrieben, damit C1c nachträglich um Wartezeiten bereinigt werden kann. Ohne diese Einträge ist C1c nicht auswertbar.
- Die Prüfung aller Deckel erfolgt **zwischen** zwei Schritten, nie mitten in einem laufenden Prompt — identisch zur manuellen Stopp-Anforderung.
- `config.yaml` je Lauf hält T1, T2, T3 und das Tokenbudget fest, damit belegbar ist, dass die Werte nicht zwischen den Läufen verändert wurden.
- Die Terminierungsursache wird als eigenes Feld protokolliert: regulärer Abschluss · T1 · T2 · Tokenbudget · Iterationsobergrenze · Blocker ohne Auflösung. Diese Verteilung ist selbst ein Ergebnis und gehört in die Ergebnisdarstellung zu FF2.

---

## 6. Verbleibende offene Werte

- **Tokenbudget je Lauf** — noch offen, aus den Vorstudien-Läufen zu kalibrieren. Es sollte so gesetzt sein, dass es typischerweise *nach* T1 greift, sonst ist T1 wirkungslos.
- **Iterationsobergrenze in der Solo-Bedingung** — ebenfalls aus der Vorstudie.
- **Phasenintervall in der Solo-Bedingung**, damit T3 dort überhaupt eine definierte Bezugsgröße hat.
- **Schrittgranularität in der BMAD-Bedingung** — voraussichtlich eine Story je Schritt; ist zu bestätigen, sobald die Workflow-Definitionen der v6-Installation eingesehen sind.
