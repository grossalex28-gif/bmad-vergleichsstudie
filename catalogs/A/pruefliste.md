# Pruefliste vor dem Einfrieren – Projekt A

Der Entwurf in `katalog-entwurf.md` war ein Vorschlag. Nach dieser Pruefung
ist er zum Katalog geworden: `katalog.md`, eingefroren am 2026-08-11,
SHA-256 in `katalog.meta.json`.

## 1. Anonymisierung

- [x] Kein Projekt- oder Repository-Name im Text
- [x] Keine charakteristischen Eigennamen aus dem Original
- [x] Kein Endpunktpfad, keine HTTP-Methode, kein Statuscode
- [x] Kein Klassen-, Datei- oder Ordnername
- [x] Kein Framework, keine Bibliothek, kein Datenbankschema

## 2. Zuschnitt

- [x] Jedes Feature geht vollstaendig durch alle Schichten
- [x] Kein Feature beschreibt einen technischen Teilschritt
- [x] Mindestens ein Fehler- oder Konfliktfall enthalten (A-F13)
- [x] Mindestens eine nichttriviale Datenstruktur gefordert (A-F5/A-F6)
- [x] Mindestens eine berechnende Fachregel enthalten (A-F8/A-F9)

## 3. Umfang

- [x] Featurezahl passt zum Zeitbudget (13 Features)
- [x] Ausschlussliste vollstaendig und je Eintrag begruendet

## 4. Abgleich

- [x] Entwurf gegen den bestehenden Katalog in
      `claude/Feature-Kataloge_Entwurf.md` gehalten
- [x] Abweichungen notiert (siehe `claude/Feature-Kataloge_final.md`, Abschnitt 4)
- [x] Endgueltige Fassung ist Entscheidung des Untersuchenden, nicht des Werkzeugs

## 5. Einfrieren

- [x] Katalog-Nummerierung und -Wortlaut eingefroren: `katalog.md`
      (2026-08-11), SHA-256 in `katalog.meta.json`. Ab hier keine Aenderung
      mehr - die Nummerierung ist Bezugsgroesse fuer A2, B1, B1b, B6 und B11.
- [ ] Katalog in die Eingabedatei Projekt A uebernommen
      (`claude/Eingabespezifikation.md`) - noch offen
- [ ] SHA-256 der fertigen Eingabedatei in der Laufkonfiguration hinterlegt
      - noch offen, folgt nach Erstellung der Eingabedatei
- [ ] Anfangsdatenbestand als Datei im Seed-Repository - noch offen,
      separater Punkt, beruehrt diese Nummerierung nicht
