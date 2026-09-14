
# SonarQube-Setup fuer A3/A5/A6/A7/A8

*Stand 07.09.2026 · Antwort auf den seit Wochen offenen Punkt "SonarQube-Instanz einrichten und einfrieren" aus `claude/Resterhebung_bis_Schreibbeginn.md`, Punkt 2 — vollstaendig abgeschlossen, inklusive Gegenpruefung der Feature-Datei-Zuordnung; eine Nacherhebung fuer eine einzelne Baseline-Variante steht noch aus.*

**Wichtiger Hinweis zur Herkunft dieses Dokuments.** Alles hier wurde in einer Sandbox ohne Docker, ohne .NET-SDK und mit durch eine Allowlist gesperrtem Netzwerk vorbereitet (verifiziert am 01.09.2026: `docker`/`dotnet` nicht installiert, `curl` gegen `sonarsource.com` mit `403 blocked-by-allowlist`). Die eigentliche Erhebung braucht Docker und das .NET-SDK, die laut `claude/Laufdurchfuehrung.md` Abschnitt 5 auf der Erhebungsmaschine bereits vorhanden sind (dort schon fuer MSSQL genutzt) — dieses Dokument, `docker-compose.sonarqube.yml`, `sonar-scan.sh`, `dependency-scan.sh` und `sonar_metriken_abziehen.py` wurden inzwischen alle real auf der Erhebungsmaschine ausgefuehrt und getestet (siehe Update-Hinweise unten).

**Update 01.09.2026, nachmittags: Dry-Run auf der Erhebungsmaschine erfolgreich.** `./sonar-scan.sh dry-run A bmad 01 backend` und `./sonar-scan.sh dry-run A bmad 01 frontend` liefen beide mit `ANALYSIS SUCCESSFUL` durch. Backend: 27 indizierte Dateien, 2 Module (`seed-a-backend.Api` + Projekt), 12 Build-Warnings (u.a. S3776 Cognitive Complexity in `SeedImporter.cs`/`BookingService.cs`, S6960 Controller mit mehreren Verantwortlichkeiten in `EventsController.cs`), 43 Dateien durch Exclude-Patterns ausgeschlossen. Frontend: 72 indizierte Dateien, 57 TS-Quelldateien analysiert, 19 Dateien durch Exclude-Patterns + 4 durch SCM-Ignore ausgeschlossen; einzige Auffaelligkeit eine harmlose Encoding-Warnung fuer `favicon.ico` (Binaerdatei, TextAndSecretsSensor) und der Hinweis "1 file is ignored because it is untracked by git or has been modified" (unkritisch fuer den Probe-Lauf, aber vor dem produktiven Lauf pruefen, ob im jeweiligen Run-Verzeichnis unversionierte Aenderungen vorliegen, da SCM-basierte Sensoren — Blame, "dirty files" — solche Dateien sonst stillschweigend anders behandeln). Damit ist die Werkzeugkette (Docker-Compose-Server, SonarScanner fuer .NET 11.2.1, SonarScanner CLI, Excludes) grundsaetzlich bestaetigt.

**Update 02.09.2026: Abschnitt 4 abgeschlossen, mit geaendertem Ergebnis gegenueber der urspruenglichen Planung.** Die urspruenglich vorgesehene manuelle Deaktivierung von "Deprecation-Regeln" wurde in der laufenden SonarQube-UI ausprobiert und wieder verworfen — Details und Begruendung siehe Abschnitt 4. Kurzfassung: Es gibt keine brauchbare Teilmenge von Regeln, die sich ueber Tags oder Titelsuche als "Versions-Deprecation-Regeln" identifizieren liesse; die beiden angelegten Profile bleiben deshalb **unveraenderte Kopien** von "Sonar way" und dienen nur noch dem Einfrieren des Regelsatzes gegen kuenftige SonarQube-Updates, nicht der Kuration. Das quantifizierte "Regel-Delta" aus `claude/Referenzprojekte_Auswahl.md` Abschnitt 7.4 entfaellt ersatzlos, dort ebenfalls aktualisiert (02.09.2026).

**Update 03.09.2026, vormittags: Vollerhebung durchgefuehrt.** Zwei reale Bugs in `sonar-scan.sh` wurden vorher noch gefunden und behoben (falsche `-D`/`/d:`-Syntax fuer die dotnet-Variante; scope-spezifische Exclusions ueberschrieben statt ergaenzten die allgemeinen Exclusions). Danach liefen `./sonar-scan.sh runs` (24 Analysen), `./sonar-scan.sh baselines` (8 Analysen) und `./dependency-scan.sh runs`/`baselines` vollstaendig durch, alle 32 erwarteten Projekt-Keys bestaetigt (Soll-Ist-Abgleich gegen `sonar-projekte.txt` per API: 0 fehlend). Die beiden Dry-Run-Probe-Projekte wurden ueber die API geloescht. Ausserdem lief die Metrikextraktion (`python3 sonar_metriken_abziehen.py --alle-aus-datei sonar-projekte.txt`) fehlerfrei fuer alle 32 Projekte durch (Abschnitt 6), sowie der unabhaengige lizard-Quercheck fuer A3 auf denselben seed-bereinigten Verzeichnissen (Median-CCN 1-2, Mittelwert 1,5-2,9 fuer alle zwoelf Laeufe und beide Baselines, plausibel). Details siehe `claude/Resterhebung_bis_Schreibbeginn.md`, Aenderungshinweise 03.09.2026.

**Update 03.09.2026, nachmittags: Freeze-Wirksamkeit per API verifiziert.** Auf Nachfrage geprueft, ob die beiden Profilkopien bei der Vollerhebung tatsaechlich verwendet wurden (nicht nur als Default gesetzt, sondern real angewendet): `GET /api/qualityprofiles/search?project=bmad-vgl-a-bmad-01-backend` bzw. `...-frontend` zeigt fuer `language: "cs"` das Profil `bmad-vergleichsstudie-cs` und fuer `language: "ts"` das Profil `bmad-vergleichsstudie-ts`, beide mit `"isDefault": true` **und** einem `"lastUsed"`-Zeitstempel, der exakt mit dem Zeitpunkt der jeweiligen Analyse uebereinstimmt (`2026-09-03T14:30:42`/`14:30:46`). Der Freeze-Mechanismus ist damit nicht nur eingerichtet, sondern nachweislich in der tatsaechlichen Messung wirksam gewesen — kein methodischer Vorbehalt mehr offen. Schritt 4 aus der Liste unten ("als Default setzen") war also korrekt durchgefuehrt; nur der optionale Schritt 5 (XML-Backup-Export der Profile als zusaetzliches Replikationsartefakt) ist noch nicht gemacht, blockiert aber nichts.

**Update 07.09.2026: Gegenpruefung der Feature-Datei-Zuordnung abgeschlossen — eine Nacherhebung noetig.** Die in Abschnitt 5 als offen markierte Pruefung der vier unsicheren Godsend-Frontend-Komponenten sowie der Doppelzuordnung von `supplier.model.ts` wurde durchgefuehrt (Volltext- und Grep-Pruefung gegen den echten Baseline-Checkout, Details in `claude/Feature_Datei_Zuordnung_Baseline.md` Abschnitt 2.3/2.4). Ergebnis: `store/home`/`store/consult` bestaetigt X, `store/pages` korrigiert auf P (traegt B-F1), `store/input-output` korrigiert auf Q (traegt u.a. B-F7), `supplier.model.ts` korrigiert auf P (traegt B-F7). `sonar/scope/B-frontend-modulscharf.properties` wurde entsprechend angepasst (Original als `.bak-vor-gegenpruefung-07092026` im selben Ordner gesichert) — die drei Dateien sind jetzt in den Inclusions, `supplier.model.ts` nicht mehr in den Exclusions. **Konsequenz:** Nur `bmad-vgl-baseline-b-modulscharf-frontend` muss neu analysiert werden (Backend unveraendert, `vollstaendig`-Variante unveraendert, alle 24 AI-Laeufe unveraendert, siehe Feature_Datei_Zuordnung_Baseline.md Abschnitt 5 fuer die vollstaendige Wirkungsanalyse). Da dies ein einzelner Docker/dotnet/Netzwerk-Schritt auf der Erhebungsmaschine ist, hier nicht automatisiert ausgefuehrt — Befehl siehe Abschnitt 5.

## 1. Versions-Freeze

Recherchiert am 01.09.2026 (siehe Quellenliste am Ende):

| Komponente | Version | Quelle/Pinning |
|---|---|---|
| SonarQube | **Community Build 26.8.0.126808** | Docker-Tag `sonarqube:26.8.0.126808-community`, exakt in `docker-compose.sonarqube.yml` gepinnt (nicht `:community`/`:latest`, die rollen) |
| Datenbank | PostgreSQL 16 | `docker-compose.sonarqube.yml`; SonarQube unterstuetzt offiziell nur PostgreSQL, die eingebettete H2-Variante ist laut Sonar-Dokumentation nur fuer Kurz-Evaluierung gedacht |
| SonarScanner fuer .NET (Backend) | **11.2.1** | `dotnet tool install --global dotnet-sonarscanner --version 11.2.1` |
| SonarScanner CLI (Frontend) | **12.1.0.3233** (mit gebuendeltem JRE 8.0.1) | Docker-Image-Tag `sonarsource/sonar-scanner-cli:12.1.0.3233_8.0.1`, alternativ als natives CLI-Paket gleicher Versionsnummer |
| C#-/TypeScript-Analyzer | in der jeweiligen SonarQube-Version bzw. im SonarScanner-fuer-.NET-Paket gebuendelt, keine separate Versionsnummer | in `config.messlauf.json`/Analyse-Metadaten den SonarQube- und Scanner-Stand dieser Tabelle vermerken |
| Qualitaetsprofil | Unveraenderte Kopie von "Sonar way" fuer C# und TypeScript, siehe Abschnitt 4 — **Wirksamkeit per API verifiziert (03.09.2026)** | Export nach dem Einrichten unter Administration > Quality Profiles sichern und mit ins Replikationspaket legen (noch offen, siehe Abschnitt 4) |

**Warum Community Build statt SonarQube Server (LTA).** Die kostenpflichtigen "SonarQube Server"-Editionen (z.B. 2026.1 LTA) bieten zusaetzlich Portfolio-/Branch-Features, die hier nicht gebraucht werden; Community Build deckt C#- und TypeScript-Analyse (Complexity, SQALE-Schuldenquote, Duplikation, Code Smells, Security Hotspots/Vulnerabilities) vollstaendig kostenfrei ab, siehe `docs.sonarsource.com/sonarqube-community-build`.

## 2. Voraussetzungen auf der Erhebungsmaschine

Vor dem ersten Start, **einmalig auf dem Host**, nicht im Container (sonst startet der eingebettete Elasticsearch-Prozess nicht):

```bash
sudo sysctl -w vm.max_map_count=524288
sudo sysctl -w fs.file-max=131072
# dauerhaft: in /etc/sysctl.d/99-sonarqube.conf eintragen
```

Danach:

**Wichtig zu `--env-file`:** Docker Compose laedt nur eine Datei, die exakt `.env` heisst, automatisch. Da die Datei hier bewusst `.env.sonarqube` heisst (damit sie sich klar von anderen `.env`-Dateien im Repo unterscheidet), muss `--env-file .env.sonarqube` bei **jedem** `docker compose`-Aufruf wiederholt werden, nicht nur beim ersten `up` — sonst bricht auch `logs`, `ps`, `down` etc. mit `required variable ... is missing a value` ab, weil Compose beim Interpolieren der Variablen in `docker-compose.sonarqube.yml` sonst keinen Wert findet. Alternative: die Datei in `.env` umbenennen (dann `.gitignore`-Eintrag von `.env.sonarqube` auf `.env` anpassen) und `--env-file` ueberall weglassen — hier ist durchgaengig die explizite `--env-file`-Variante dokumentiert, weil sie unabhaengig vom Dateinamen funktioniert.

Falls `docker compose` (mit Leerzeichen, v2-Plugin) auf der Erhebungsmaschine nicht installiert ist (`docker compose version` meldet `unknown shorthand flag` o.ae.), das aeltere `docker-compose` (mit Bindestrich, gleiche Argument-Syntax) verwenden — pruefen mit `docker-compose version`; auf Arch Linux z.B. via `sudo pacman -S docker-compose` nachinstallierbar. Im Folgenden `docker compose`/`docker-compose` je nach dem, was auf der Maschine vorhanden ist, gleichbedeutend lesen.

```bash
cd ~/dev/Bachelor/bmad-vergleichsstudie
cp .env.sonarqube.example .env.sonarqube   # Passwort eintragen, Datei ist in .gitignore
docker compose -f docker-compose.sonarqube.yml --env-file .env.sonarqube up -d
# Hochfahren dauert erfahrungsgemaess 1-2 Minuten; Fortschritt:
docker compose -f docker-compose.sonarqube.yml --env-file .env.sonarqube logs -f sonarqube
# Status pruefen, ohne staendig die Logs zu verfolgen:
docker compose -f docker-compose.sonarqube.yml --env-file .env.sonarqube ps
```

Server ist bereit, sobald `http://localhost:9000/api/system/status` `{"status":"UP"}` liefert (per `curl -s http://localhost:9000/api/system/status` pruefbar, sobald der Container laeuft).

## 3. Erstanmeldung und Token

1. `http://localhost:9000` oeffnen, mit `admin`/`admin` anmelden, Passwort aendern (Pflicht beim ersten Login).
2. Unter *My Account > Security* einen **User Token** erzeugen (Typ "User Token", kein Ablaufdatum fuer die Dauer der Erhebung oder ein Ablaufdatum weit genug in der Zukunft). Token **nicht committen** — als `SONAR_TOKEN` exportieren:
   ```bash
   export SONAR_TOKEN="<token>"
   export SONAR_HOST_URL="http://localhost:9000"   # Default in den Skripten, nur bei Abweichung noetig
   ```
   Falls der Token verloren geht (z.B. Terminal geschlossen, Wert nur im Shell-Verlauf): SonarQube zeigt Token-Werte nur einmalig beim Erzeugen an und kann sie nicht erneut anzeigen. In diesem Fall den alten Token unter *My Account > Security* widerrufen und einen neuen erzeugen — es gibt keine Wiederherstellung. Umgesetzt (01.09.2026): Token liegt in einer eigenen, gitignorten Datei `.env.sonar-token` (`export SONAR_TOKEN=...`) und wird vor jeder Sitzung per `source .env.sonar-token` eingelesen.

## 4. Qualitaetsprofil einfrieren (keine Regelkuration)

Grundlage (urspruenglich): `claude/Referenzprojekte_Auswahl.md` Abschnitt 7.4, Punkt 2 — Regelkategorien deaktivieren, die **ausschliesslich Versionsalter** kennzeichnen (Deprecation-/Migrationsregeln), damit A7 nicht durch den Sprung von Angular 6/ASP.NET Core 2.1 (Original) auf aktuelle Versionen (KI-Laeufe) verzerrt wird.

**Status 02.09.2026: abgeschlossen — mit geaendertem Ergebnis.** Der urspruenglich vorgesehene Schritt "Regeln mit Tag `deprecated` bzw. Titeln wie 'should not be used' suchen und deaktivieren" wurde in der laufenden SonarQube-UI (Community Build 26.8.0.126808) tatsaechlich ausprobiert und dabei verworfen:

- Suche nach `deprecated` im globalen Regel-Browser (`Rules`, Filter Language = C#): **genau 1 Treffer**, `csharpsquid:S1133` "Deprecated code should be removed" (Tag `obsolete`, Severity Info). Diese Regel markiert im analysierten Code selbst mit `[Obsolete]` annotierte Elemente — sie hat nichts mit dem Alter des SonarQube-Regelsatzes gegenueber Angular 6/ASP.NET Core 2.1 zu tun und waere fuer die Baseline-Verzerrung irrelevant.
- Suche nach `should not be used`: **128 Treffer**, weit ueberwiegend allgemeine Code-Smell- und Namenskonventionsregeln ohne jeden Versionsbezug (z.B. `"async" and "await" should not be used as identifiers`, `"==" should not be used when "Equals" is overridden`, `"[ExpectedException]" should not be used`). Eine manuelle Durchsicht und Einzelbewertung aller 128 Regeln stuende in keinem vertretbaren Verhaeltnis zum Nutzen und waere zudem eine subjektive Einzelfallentscheidung ohne belastbares Auswahlkriterium — genau das, was Abschnitt 4 der urspruenglichen Fassung durch eine UI-Prozedur statt geratener Regel-Keys vermeiden wollte.

**Befund:** In der tatsaechlich installierten SonarQube-Version gibt es keine verlaessliche, nicht-willkuerliche Moeglichkeit, "Regeln, die 2018 (Entstehungszeitpunkt der Originale) noch nicht existierten" ueber Tags oder Titelsuche zu identifizieren. Rule-Metadaten mit "Since-Version" sind in der Community-Build-UI nicht durchsuchbar exponiert.

**Entscheidung (02.09.2026):** Keine manuelle Regel-Deaktivierung, kein quantifiziertes Regel-Delta. Die Mitigation der Baseline-Verzerrung stuetzt sich stattdessen ausschliesslich auf die bereits bestehende Gegenmassnahme aus `claude/Referenzprojekte_Auswahl.md` Abschnitt 7.3/7.4 (dort am 02.09.2026 aktualisiert): FF3 stuetzt sich primaer auf **A3** (zyklomatische Komplexitaet) und **A6** (Duplikation), die generationsrobust sind; A5, A7 und A8 werden mitberichtet, aber explizit als versionskonfundiert gekennzeichnet, zusaetzlich gedaempft durch Normalisierung auf Dichten je kLOC statt Absolutwerte.

**Warum die beiden Profil-Kopien trotzdem angelegt werden — anderer Grund als urspruenglich geplant.** Ein kopiertes Profil erhaelt laut SonarQube-UI-Hinweis *keine* automatischen Updates mehr, wenn SonarSource kuenftig neue Regeln in das eingebaute "Sonar way" aufnimmt ("This quality profile does not inherit from a built-in profile. It will not benefit from automatic updates when new rules are introduced"). Ohne eigene Kopie koennte sich der wirksame Regelsatz zwischen einem fruehen und einem spaeten Lauf der Erhebung unterscheiden, falls der SonarQube-Server zwischenzeitlich aktualisiert wird — genau das soll der in Abschnitt 1 dokumentierte Versions-Freeze verhindern. Die Kopie ist also weiterhin der Freeze-Mechanismus, nur nicht mehr das Kurationswerkzeug.

**Tatsaechliches Vorgehen:**

1. *Quality Profiles > C# ("Sonar way") > Copy* → Name `bmad-vergleichsstudie-cs`. **Erledigt.**
2. *Quality Profiles > TypeScript ("Sonar way") > Copy* → Name `bmad-vergleichsstudie-ts`. **Erledigt.**
3. ~~Deprecation-Regeln suchen und deaktivieren, Regel-Delta protokollieren~~ — **entfaellt, siehe Entscheidung oben.**
4. Beide Profile unter *Administration > Projects* als Default fuer neue C#- bzw. TypeScript-Projekte setzen. **Erledigt und per API verifiziert (03.09.2026):** `GET /api/qualityprofiles/search?project=<key>` zeigt fuer alle gepruefte Projekte `bmad-vergleichsstudie-cs`/`-ts` mit `isDefault: true` und einem `lastUsed`-Zeitstempel, der exakt mit dem tatsaechlichen Analysezeitpunkt uebereinstimmt — der Freeze war bei der echten Messung nachweislich wirksam.
5. Profil-Export (*Quality Profiles > … > Backup*) als XML sichern, z.B. unter `sonar/quality-profiles/bmad-vergleichsstudie-cs.xml` und `...-ts.xml`, und ins Replikationspaket legen — **noch offen**, aber rein additiv (Replikationsartefakt), die Wirksamkeit des Freeze ist bereits anderweitig (Punkt 4) belegt und blockiert nichts.

## 5. Projekte anlegen und Analysen fahren

Projekte muessen vor der ersten Analyse nicht manuell angelegt werden — `dotnet-sonarscanner`/`sonar-scanner` legen einen Projekt-Key beim ersten Lauf automatisch an, sofern der Token ausreichende Rechte hat (Standard bei einem User-Token des Admin-Kontos).

```bash
cd ~/dev/Bachelor/bmad-vergleichsstudie
./sonar-scan.sh check
./sonar-scan.sh dry-run A bmad 01 backend
./sonar-scan.sh dry-run A bmad 01 frontend
```

**Status 03.09.2026: vollstaendig abgeschlossen.** `check`/`dry-run` erfolgreich (01.09.), Qualitaetsprofil-Freeze eingerichtet und verifiziert (02./03.09.), beide Dry-Run-Probe-Projekte ueber die API geloescht. Danach liefen erfolgreich:

```bash
./sonar-scan.sh runs        # alle 12 Laeufe, 24 Analysen -- ERLEDIGT
./sonar-scan.sh baselines   # beide Baselines, je modulscharf+vollstaendig, 8 Analysen -- ERLEDIGT

./dependency-scan.sh runs      # ERLEDIGT
./dependency-scan.sh baselines # ERLEDIGT
```

Soll-Ist-Abgleich der 32 erwarteten Projekt-Keys aus `sonar-projekte.txt` gegen die SonarQube-API: 32/32 vollstaendig, 0 fehlend (nach Nachholen von sechs zunaechst fehlenden Baseline-Analysen).

Die modulscharfe/vollstaendige Abgrenzung der Baselines nutzt die Dateien unter `sonar/scope/*.properties`, die direkt aus `claude/Feature_Datei_Zuordnung_Baseline.md` abgeleitet sind. **Erledigt (07.09.2026):** die vier dort als unsicher markierten Godsend-Frontend-Komponenten sowie die doppelte Einordnung von `supplier.model.ts` wurden gegengeprueft (siehe Update oben und `Feature_Datei_Zuordnung_Baseline.md` Abschnitt 2.3/2.4). `sonar/scope/B-frontend-modulscharf.properties` ist bereits korrigiert (Original gesichert unter `sonar/scope/B-frontend-modulscharf.properties.bak-vor-gegenpruefung-07092026`).

**Noch auszufuehren — einmalige Nacherhebung der Baseline B/modulscharf** (`scan_baseline` in `sonar-scan.sh` fuehrt Backend und Frontend immer gemeinsam aus; einen reinen Frontend-Einzelaufruf gibt es im Skript nicht — der Backend-Teil wird dabei unveraendert erneut hochgeladen, das ist harmlos):

```bash
cd ~/dev/Bachelor/bmad-vergleichsstudie
source .env.sonar-token
./sonar-scan.sh baseline B modulscharf
python3 sonar_metriken_abziehen.py --alle-aus-datei sonar-projekte.txt
```

Der zweite Befehl aktualisiert `sonar-ergebnisse/bmad-vgl-baseline-b-modulscharf-{backend,frontend}.json` (bzw. laesst sich bei Bedarf auf die zwei betroffenen Keys eingrenzen). Nach dem Lauf `claude/Statistische_Auswertung.md` Abschnitt 4 (deskriptive Baseline-Tabelle) mit dem neuen Frontend-Wert aktualisieren.

## 6. Kennzahlen abziehen

```bash
python3 sonar_metriken_abziehen.py --alle-aus-datei sonar-projekte.txt
```

**Status 03.09.2026: erledigt.** Lief fehlerfrei fuer alle 32 Projekte durch. Ergebnis: `sonar-ergebnisse/<projekt-key>.json` je der 32 Analysen (Ablage analog `cloc-ergebnisse/`; enthaelt u.a. `complexity`, `sqale_debt_ratio`, `duplicated_lines_density`, `code_smells`, `vulnerabilities`/`vulnerabilities_nach_schweregrad`, `ncloc`, `functions`), zusaetzlich `sonar-ergebnisse/abhaengigkeiten/*-dotnet.txt` und `*-npm.json` fuer die A8-Abhaengigkeitsbefunde. `A3`-Median/Anteil-ueber-10 kommt zusaetzlich aus dem lizard-Quercheck (`lizard-quercheck.sh`, ebenfalls erledigt 03.09.2026, siehe `claude/Resterhebung_bis_Schreibbeginn.md`): Median-CCN 1-2, Mittelwert 1,5-2,9, unter 3 % der Funktionen mit CCN > 10 fuer alle zwoelf Laeufe und beide Baselines — plausibel, kein Hinweis auf eine verzerrte A3-Messung. **Hinweis (07.09.2026):** `bmad-vgl-baseline-b-modulscharf-frontend` muss nach der Scope-Korrektur (Abschnitt 5) neu erhoben werden, bevor dieser eine Wert im Ergebniskapitel zitiert wird — alle anderen 31 Werte sind unveraendert gueltig.

## 7. Offene Punkte nach diesem Setup

- ~~Reale Erstausfuehrung auf der Erhebungsmaschine~~ **Erledigt (01.09.2026)**, Dry-Run erfolgreich.
- ~~Qualitaetsprofil einrichten und einfrieren~~ **Erledigt (02.09.2026), Wirksamkeit per API verifiziert (03.09.2026)**, siehe Abschnitt 4 — als unveraenderte Profilkopien, ohne Regelkuration, nachweislich bei der echten Messung angewendet.
- ~~Regel-Deltas dokumentieren / `docs/SonarQube_Regel_Deltas.md` anlegen~~ **entfaellt** (Entscheidung 02.09.2026, siehe Abschnitt 4).
- ~~Volle Ausfuehrung `./sonar-scan.sh runs`/`baselines` sowie `dependency-scan.sh`~~ **Erledigt (03.09.2026)**, siehe Abschnitt 5.
- ~~Kennzahlen abziehen (`sonar_metriken_abziehen.py`) und lizard-Quercheck~~ **Erledigt (03.09.2026)**, siehe Abschnitt 6.
- ~~Vier unsichere Godsend-Frontend-Komponenten gegenpruefen~~ **Erledigt (07.09.2026)**, siehe Update oben und `Feature_Datei_Zuordnung_Baseline.md` Abschnitt 2.3/2.4.
- **Neu, einziger noch offener Punkt:** `./sonar-scan.sh baseline B modulscharf` erneut ausfuehren und `sonar_metriken_abziehen.py` erneut laufen lassen (Abschnitt 5/6) — braucht Docker/Netzwerk auf der Erhebungsmaschine, deshalb bewusst nicht ferngesteuert ausgefuehrt, sondern hier als Befehl hinterlegt.
- Optional, nicht blockierend: Profil-XML-Backup als Replikationsartefakt sichern (Abschnitt 4, Schritt 5).
- Statistische Auswertung (Median/IQR je Zelle, Mann-Whitney-U/Cliff's Delta) auf Basis der `sonar-ergebnisse/*.json` — **erledigt (03.09.2026)**, siehe `claude/Statistische_Auswertung.md`. Die dortige rein deskriptive Baseline-Zeile fuer `baseline-b-modulscharf` muss nach der Nacherhebung oben einmalig aktualisiert werden; die BMAD-vs-Solo-Kernergebnisse sind davon nicht betroffen.

## Quellen (Web-Recherche 01.09.2026)

- SonarQube Docker-Image-Releases: https://github.com/SonarSource/docker-sonarqube/releases
- SonarQube Community Build Datenbankanforderungen: https://docs.sonarsource.com/sonarqube-community-build/setup-and-upgrade/installation-requirements/database-requirements/
- SonarScanner fuer .NET: https://docs.sonarsource.com/sonarqube-community-build/analyzing-source-code/scanners/dotnet/introduction/
- dotnet-sonarscanner NuGet-Paket: https://www.nuget.org/packages/dotnet-sonarscanner
- sonar-scanner-cli Docker-Tags: https://hub.docker.com/r/sonarsource/sonar-scanner-cli/tags
