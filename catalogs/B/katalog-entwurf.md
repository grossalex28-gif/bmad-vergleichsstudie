# Feature-Katalog (Entwurf)

## Projektkontext

Die Anwendung ist eine webbasierte Handelsplattform, über die Besuchende ein Sortiment aus Produkten durchsuchen, filtern, vergleichen und im Detail einsehen können. Produkte sind einer beliebig tief verschachtelbaren Kategorie zugeordnet und tragen je nach Kategorie unterschiedliche, frei definierbare Merkmale, nach denen sich das Angebot eingrenzen und sortieren lässt. Neben den Produkten führt die Plattform ein eigenständiges Verzeichnis von Lieferanten, die dieselben Produkte zu unterschiedlichen Preisen anbieten, sodass ein Produkt mehreren Lieferanten zugeordnet sein kann.

Angemeldete Nutzer sammeln Produkte in einem Warenkorb, wählen dabei je Position einen Lieferanten aus und schließen den Einkauf zu einer Bestellung ab, deren Bearbeitungsstatus anschließend von befugtem Personal bis zum Versand gepflegt wird. Zusätzlich können Produkte, Lieferanten und redaktionelle Artikel bewertet und kommentiert werden, wobei Kommentare beliebig tief als Antworten verschachtelt werden können und nachträglich nur von ihren Verfassern oder von befugtem Personal verändert werden dürfen.

Ergänzend pflegt befugtes Fachpersonal den Produkt-, Lieferanten- und Artikelbestand direkt in der Anwendung, während eine Volltextsuche über Produkte und Lieferanten sowie mitgezählte Aufrufe zur Beliebtheitsermittlung das Angebot für Besuchende zusätzlich erschließen.

## Katalog

| ID | Feature |
| --- | --- |
| F01 | Besuchende blättern seitenweise durch die Liste aller Produkte des Sortiments. |
| F02 | Besuchende navigieren durch eine beliebig tief verschachtelte Kategoriehierarchie, um das Sortiment einzugrenzen. |
| F03 | Besuchende filtern die Produkte einer Kategorie nach deren kategoriespezifischen Merkmalen (Zahlbereiche und Textwerte) in beliebiger Kombination. |
| F04 | Besuchende sortieren eine Produktliste nach Name, Bewertung, Aufrufzahl oder einem beliebigen kategoriespezifischen Merkmal, wahlweise auf- oder absteigend. |
| F05 | Besuchende wählen mehrere Produkte derselben Kategorie aus und lassen sich deren Merkmale nebeneinander vergleichen. |
| F06 | Besuchende rufen die Detailseite eines Produkts auf und sehen dort Beschreibung, Bildergalerie, Bewertung, Kommentare sowie alle anbietenden Lieferanten mit jeweiligem Preis. |
| F07 | Besuchende bewerten ein Produkt, einen Lieferanten oder einen Artikel mit einer Punktzahl; eine bereits vorhandene eigene Bewertung wird dabei überschrieben und der Durchschnitt neu berechnet. |
| F08 | Besuchende verfassen einen Kommentar zu einem Produkt, Lieferanten oder Artikel oder antworten auf einen bestehenden Kommentar in beliebiger Verschachtelungstiefe. |
| F09 | Verfassende bearbeiten oder löschen ihren eigenen Kommentar nachträglich. |
| F10 | Der Versuch, einen fremden Kommentar zu bearbeiten oder zu löschen, wird abgelehnt, wenn die anfragende Person weder Verfasser des Kommentars noch zur Moderation berechtigt ist. |
| F11 | Käufer legen ein Produkt bei einem selbst gewählten Lieferanten mit einer Menge in ihren Warenkorb und passen Menge oder Auswahl jederzeit vor dem Kauf an. |
| F12 | Käufer schließen den Warenkorb zu einer Bestellung ab, wobei jede Position mit dem zuvor gewählten Lieferanten und dessen zu diesem Zeitpunkt gültigem Preis übernommen wird. |
| F13 | Der Bestellabschluss wird als Ganzes abgelehnt und keine Bestellung angelegt, sobald eine Warenkorbposition keinem gültigen Lieferanten zugeordnet ist. |
| F14 | Käufer rufen die Liste ihrer eigenen Bestellungen mit dem jeweils aktuellen Bearbeitungsstatus ab. |
| F15 | Befugtes Personal ändert den Bearbeitungsstatus einer Bestellung entlang eines festen Ablaufs (in Bearbeitung, bereit, versendet, storniert), wobei beim Wechsel auf versendet zusätzlich ein Abschlusszeitpunkt festgehalten wird. |
| F16 | Besuchende durchsuchen Produkte und Lieferanten gemeinsam oder getrennt per Freitextsuche über deren Namen. |
| F17 | Besuchende blättern durch die Liste aller Lieferanten und rufen deren Detailseite mit den dort angebotenen Produkten und Preisen auf. |
| F18 | Lesende rufen redaktionelle Artikel mit Schlagworten und Autorenangabe in einer Liste sowie einzeln in der Detailansicht ab. |
| F19 | Befugte Redakteure verfassen einen neuen Artikel oder bearbeiten Inhalt und Schlagworte eines bestehenden Artikels. |
| F20 | Befugtes Fachpersonal pflegt das Sortiment, indem es Produkte samt Kategoriezuordnung, kategoriespezifischen Merkmalen und anbietenden Lieferanten mit Preis anlegt oder bearbeitet. |
| F21 | Befugtes Fachpersonal legt neue Lieferanten an oder bearbeitet bestehende Lieferanteneinträge. |
| F22 | Befugtes Personal entfernt ein Produkt, einen Lieferanten oder einen Artikel unwiderruflich aus dem Bestand. |
| F23 | Jeder Aufruf der Detailseite eines Produkts, Lieferanten oder Artikels erhöht einen dauerhaft gespeicherten Zähler, der für die Sortierung nach Beliebtheit herangezogen wird. |

## Bewusst ausgeschlossen

| Ausgeschlossen | Begründung |
| --- | --- |
| Authentifizierung und Benutzerverwaltung (Registrierung, Anmeldung inkl. Drittanbieter-Anmeldung, Profilbearbeitung, Rollenzuweisung) | Technischer Querschnittsmechanismus, der als Voraussetzung für die übrigen Funktionen dient, selbst aber keine eigenständige fachliche Anforderung des Handelsgeschäfts darstellt. |
| Echtzeit-Rückmeldungen und Benachrichtigungsversand zu ausgeführten Aktionen | Reine technische Zustellmechanik von Erfolgs- oder Fehlermeldungen ohne eigenen fachlichen Mehrwert über die jeweils zugrundeliegende Aktion hinaus. |
| Datei-Upload (Bilder zu Produkten und Artikeln) | Rein technischer Übertragungsvorgang; der fachliche Mehrwert („Produkt besitzt eine Bildergalerie“) ist bereits in der Produktdetail-Anforderung enthalten. |
| Mehrsprachigkeit der Benutzeroberfläche | Reine Darstellungs- und Lokalisierungsfrage ohne Einfluss auf fachliche Logik oder Datenbestand. |
| Administrationsoberfläche zur Rollenvergabe und für Direktnachrichten an einzelne Nutzer | Betrifft die Verwaltung des Systems und seiner Nutzer selbst statt des eigentlichen Handelsgeschäfts und richtet sich an eine reine Betriebsrolle statt an Kundschaft oder Fachpersonal. |
| Alternative Zugangskanäle für die Produktsuche (z. B. eine mobile Anwendung oder ein Chat-Assistent) | Bieten lediglich einen eingeschränkten Ausschnitt der bereits im Katalog erfassten Produktsuche über einen weiteren Kanal an und begründen keine eigenständige fachliche Anforderung. |

## Beobachtungen

- Im untersuchten Code wird an keiner Stelle ein Lagerbestand geführt oder bei einer Bestellung geprüft; es blieb unklar, ob dies eine bewusste Vereinfachung oder eine offene, noch nicht umgesetzte Anforderung des Originalprojekts ist. Eine Bestandsprüfung wurde deshalb nicht in den Katalog aufgenommen.
- Einige Oberflächenbereiche (u. a. ein Statistik-Bereich und ein Beratungs-/Kontaktbereich) sind im Frontend als leere Platzhalter angelegt und serverseitig existieren Funktionsstellen für „eigenen Kommentar bearbeiten“ und „eigenen Kommentar löschen“, die absichtlich einen Laufzeitfehler auslösen statt eine Aktion auszuführen. Diese Bereiche wurden als nicht funktionsfähig eingestuft und nicht in den Katalog übernommen.
- Es existiert eine eigene Berechtigungsstufe für „Lieferanten-Nutzer“, ohne dass im untersuchten Code ein darauf zugeschnittener eigener Arbeitsablauf erkennbar ist. Sie wurde daher unter „befugtes Personal“ subsumiert statt als eigene Anforderung ausgewiesen.
- Bestellpositionen unterscheiden im Code konzeptionell zwischen stückbasierten und gewichtsbasierten Posten; der gewichtsbasierte Weg ist im Frontend vollständig auskommentiert und wurde als inaktiv eingestuft, weshalb nur der stückbasierte Bestellweg in den Katalog aufgenommen wurde.
- Der Preis einer Bestellposition wird zum Zeitpunkt des Bestellabschlusses aus der Lieferantenzuordnung übernommen; ob eine zwischenzeitliche Preisänderung beim Lieferanten vor Abschluss der Bestellung erneut geprüft wird, ließ sich aus dem untersuchten Code nicht eindeutig klären.

