# Feature-Katalog (Entwurf)

## Projektkontext

Die Anwendung unterstützt den Verkauf von Sitzplätzen für zeitlich geplante Vorstellungen an verschiedenen Veranstaltungsorten – etwa Vorführungen einer Veranstaltung in einem bestimmten Raum zu einem festen Termin. Besucherinnen und Besucher können verfügbare Vorstellungen durchsuchen, sich über Veranstaltung und Veranstaltungsort informieren, einzelne Sitzplätze in einer grafischen Sitzplandarstellung auswählen und darauf basierend eine Buchung abschließen. Registrierte Kundinnen und Kunden können ihre eigenen Buchungen einsehen, stornieren und ihre Kontaktdaten pflegen.

Auf der Verwaltungsseite steht ein umfangreiches Werkzeug zur Pflege der Stammdaten bereit: Veranstaltungsorte mit ihren Räumen und individuell konfigurierbaren Sitzplänen, Veranstaltungen, buchbare Vorstellungstermine sowie Preisstrategien, die unterschiedliche Preiskategorien pro Vorstellung definieren. Zusätzlich können Verwaltende bestehende Buchungen und Kundendatensätze einsehen und pflegen.

Fachlich zentral ist die Verknüpfung von Raum-Sitzplan, Vorstellungstermin und Preisstrategie: Ein Sitzplatz wird immer im Kontext einer konkreten Vorstellung gebucht, und der Preis richtet sich nach der zum Buchungszeitpunkt für diese Vorstellung gültigen Preiskategorie. Mehrbenutzerbetrieb (gleichzeitige Bearbeitung derselben Datensätze durch mehrere Verwaltende, gleichzeitige Buchungsversuche) wird durch Konflikterkennung bei der Aktualisierung von Datensätzen berücksichtigt.

## Katalog

| ID | Feature |
|----|---------|
| 1 | Besucher sehen eine aktuelle Übersicht verfügbarer Vorstellungen, die sie nach Datum, Veranstaltung und Veranstaltungsort filtern können. |
| 2 | Besucher rufen Detailinformationen zu einer Veranstaltung ab, einschließlich Beschreibung, Dauer und Altersfreigabe. |
| 3 | Besucher rufen Detailinformationen zu einem Veranstaltungsort ab, einschließlich Kontaktdaten, Standortangaben, Verweisen auf soziale Netzwerke und verfügbaren Einrichtungen wie Parkplätzen oder barrierefreiem Zugang. |
| 4 | Besucher wählen für eine Vorstellung einzelne Sitzplätze in einer grafischen Sitzplandarstellung aus, wobei bereits gebuchte Plätze erkennbar als belegt markiert sind. |
| 5 | Besucher ordnen jedem ausgewählten Sitzplatz eine Preiskategorie zu, deren Preis aus der für die jeweilige Vorstellung hinterlegten Preisstrategie stammt. |
| 6 | Besucher schließen eine Buchung ab, indem sie Kontakt- und Adressdaten angeben; registrierte Kundinnen und Kunden verwenden dafür automatisch ihre bereits hinterlegten Daten. |
| 7 | Das System lehnt eine Buchung ab, wenn eine der ausgewählten Preiskategorien beim endgültigen Abschluss nicht mehr Teil der aktuell gültigen Preisstrategie der Vorstellung ist. |
| 8 | Kundinnen und Kunden erhalten nach Abschluss einer Buchung eine Zusammenfassung mit den gebuchten Plätzen, den gewählten Preiskategorien und dem Gesamtpreis. |
| 9 | Registrierte Kundinnen und Kunden sehen die Liste ihrer eigenen Buchungen ein. |
| 10 | Registrierte Kundinnen und Kunden stornieren eine eigene Buchung, wodurch die zugehörigen Sitzplätze für die Vorstellung wieder freigegeben werden. |
| 11 | Registrierte Kundinnen und Kunden sehen ihre hinterlegten Kontakt- und Adressdaten ein und bearbeiten diese. |
| 12 | Verwaltende legen Veranstaltungsorte an, bearbeiten und löschen sie, einschließlich Kontakt-, Standort- und Einrichtungsangaben. |
| 13 | Verwaltende legen innerhalb eines Veranstaltungsorts Räume an und konfigurieren deren Sitzplan, einschließlich Anzahl der Sitzreihen und -spalten sowie der Position von Gängen zur optischen Gliederung der Sitzblöcke. |
| 14 | Verwaltende legen Veranstaltungen an, bearbeiten und löschen sie, einschließlich Beschreibung, Dauer und Altersfreigabe. |
| 15 | Verwaltende legen Vorstellungstermine an, die eine Veranstaltung, einen Raum, einen Zeitraum und eine Preisstrategie zu einem buchbaren Termin verknüpfen. |
| 16 | Verwaltende legen Preisstrategien mit mehreren benannten Preiskategorien und zugehörigen Preisen an und weisen sie einzelnen Vorstellungen zu. |
| 17 | Verwaltende sehen alle Buchungen aller Kundinnen und Kunden ein, bearbeiten und stornieren sie. |
| 18 | Verwaltende sehen Kundendatensätze ein, bearbeiten und löschen sie; beim Löschen eines Kundendatensatzes mit verknüpftem Zugang wird dieser Zugang mit entfernt. |
| 19 | Das System lehnt eine von einer verwaltenden Person vorgenommene Änderung an einem Datensatz (z. B. Buchung, Raum, Vorstellung, Veranstaltungsort) ab, wenn dieser Datensatz zwischenzeitlich von anderer Stelle geändert wurde, statt die fremde Änderung stillschweigend zu überschreiben. |

## Bewusst ausgeschlossen

| Ausgeschlossen | Begründung |
|-----------------|------------|
| Registrierung, Anmeldung, Abmeldung und Passwortverwaltung für Nutzerkonten | Authentifizierung und Benutzerverwaltung sind Querschnittsfunktionen, die für nahezu jede Anwendung mit Nutzerkonten gleichartig anfallen und keine für dieses Fachgebiet spezifische Aussage liefern. |
| Versand von Bestätigungs- und Informations-E-Mails (z. B. Registrierungsbestätigung, Buchungsbestätigung) | E-Mail-/Benachrichtigungsversand ist ein technischer Begleitprozess zu anderen Features (z. B. Buchungsabschluss) und stellt keine eigenständige fachliche Anforderung dar. |
| Allgemeine Navigations- und Übersichtsseiten des Verwaltungsbereichs ohne eigene fachliche Funktion | Reine Administrationsoberflächen-Bestandteile ohne fachlichen Eigenwert wurden nicht als separate Features gezählt, da sie lediglich Zugriffspunkte zu den bereits erfassten Verwaltungsfunktionen darstellen. |
| Pflege von Hervorhebungs-/Marketinginhalten für die Startseite | Dieser Bereich bildet einen von der Buchungsabwicklung unabhängigen, eigenständigen Fachbereich (Inhaltspflege statt Buchungsgeschäft) und wurde bewusst nicht in den Kernkatalog aufgenommen. |

## Beobachtungen

- Unklar bleibt, ob das System eine gleichzeitige Buchung desselben Sitzplatzes durch zwei Kundinnen/Kunden für dieselbe Vorstellung aktiv verhindert oder ob die Belegungsanzeige lediglich informativ ist und der Konflikt erst nachträglich auffällt. Dies wäre ein weiterer möglicher Fehlerfall, ließ sich aus der Analyse aber nicht eindeutig bestätigen.
- Der genaue fachliche Umfang der Preiskategorien (z. B. ob sie venue-, veranstaltungs- oder betreiberweit wiederverwendet werden oder je Preisstrategie individuell sind) war nicht in jedem Detail eindeutig abzuleiten.
- Es blieb offen, ob Verwaltende beim Bearbeiten einer bestehenden Buchung auch die Sitzplatzzuordnung ändern können oder nur Rahmendaten, da die zugrunde liegende Aktualisierungslogik keine fachliche Unterscheidung erkennen lässt.
- Die Reichweite der auf Veranstaltungsorten hinterlegten Einrichtungsmerkmale (z. B. ob sie für Besucher lediglich informativ sind oder auch als Filterkriterium bei der Suche dienen) konnte nicht abschließend geklärt werden.
