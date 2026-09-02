# SonarQube-Setup fuer A3/A5/A6/A7/A8

*Stand 01.09.2026 · Antwort auf den seit Wochen offenen Punkt "SonarQube-Instanz einrichten und einfrieren" aus `claude/Resterhebung_bis_Schreibbeginn.md`, Punkt 2.*

**Wichtiger Hinweis zur Herkunft dieses Dokuments.** Alles hier wurde in einer Sandbox ohne Docker, ohne .NET-SDK und mit durch eine Allowlist gesperrtem Netzwerk vorbereitet (verifiziert am 01.09.2026: `docker`/`dotnet` nicht installiert, `curl` gegen `sonarsource.com` mit `403 blocked-by-allowlist`). Die eigentliche Erhebung braucht Docker und das .NET-SDK, die laut `claude/Laufdurchfuehrung.md` Abschnitt 5 auf der Erhebungsmaschine bereits vorhanden sind (dort schon fuer MSSQL genutzt) — dieses Dokument, `docker-compose.sonarqube.yml`, `sonar-scan.sh`, `dependency-scan.sh` und `sonar_metriken_abziehen.py` sind also vorbereitet, aber **noch nicht real ausgefuehrt oder gegen eine laufende Instanz getestet**. Vor dem produktiven Einsatz erst `sonar-scan.sh check` und `sonar-scan.sh dry-run` an einem einzelnen Lauf pruefen (Abschnitt 5).

## 1. Versions-Freeze

Recherchiert am 01.09.2026 (siehe Quellenliste am Ende):

| Komponente | Version | Quelle/Pinning |
|---|---|---|
| SonarQube | **Community Build 26.8.0.126808** | Docker-Tag `sonarqube:26.8.0.126808-community`, exakt in `docker-compose.sonarqube.yml` gepinnt (nicht `:community`/`:latest`, die rollen) |
| Datenbank | PostgreSQL 16 | `docker-compose.sonarqube.yml`; SonarQube unterstuetzt offiziell nur PostgreSQL, die eingebettete H2-Variante ist laut Sonar-Dokumentation nur fuer Kurz-Evaluierung gedacht |
| SonarScanner fuer .NET (Backend) | **11.2.1** | `dotnet tool install --global dotnet-sonarscanner --version 11.2.1` |
| SonarScanner CLI (Frontend) | **12.1.0.3233** (mit gebuendeltem JRE 8.0.1) | Docker-Image-Tag `sonarsource/sonar-scanner-cli:12.1.0.3233_8.0.1`, alternativ als natives CLI-Paket gleicher Versionsnummer |
| C#-/TypeScript-Analyzer | in der jeweiligen SonarQube-Version bzw. im SonarScanner-fuer-.NET-Paket gebuendelt, keine separate Versionsnummer | in `config.messlauf.json`/Analyse-Metadaten den SonarQube- und Scanner-Stand dieser Tabelle vermerken |
| Qualitaetsprofil | Kopie von "Sonar way" fuer C# und TypeScript, siehe Abschnitt 4 | Export nach dem Einrichten unter Administration > Quality Profiles sichern und mit ins Replikationspaket legen |

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

## 4. Qualitaetsprofil einrichten (Regelsatz einfrieren)

Grundlage: `claude/Referenzprojekte_Auswahl.md` Abschnitt 7.4, Punkt 2 — Regelkategorien deaktivieren, die **ausschliesslich Versionsalter** kennzeichnen (Deprecation-/Migrationsregeln), damit A7 nicht durch den Sprung von Angular 6/ASP.NET Core 2.1 (Original) auf aktuelle Versionen (KI-Laeufe) verzerrt wird.

**Vorgehen (in der UI, da die exakten Regel-Keys erst gegen die konkret installierte Version geprueft werden koennen — hier keine Regel-Keys raten, siehe Warnhinweis unten):**

1. *Quality Profiles > C# ("Sonar way") > Copy* → Name `bmad-vergleichsstudie-cs`.
2. *Quality Profiles > TypeScript ("Sonar way") > Copy* → Name `bmad-vergleichsstudie-ts`.
3. In beiden Kopien nach Regeln mit Tag `deprecated` bzw. Titeln wie "should not be used", "Deprecated ... should not be used" filtern und deaktivieren. **Vor dem Deaktivieren jede betroffene Regel-ID in einer eigenen Tabelle (z.B. `docs/SonarQube_Regel_Deltas.md`) protokollieren** — das ist die Grundlage fuer das in Abschnitt 7.4, Punkt 2 der Referenzprojekte-Auswahl geforderte **Regel-Delta** ("Anteil der Baseline-Befunde, die aus Regeln stammen, die zum Entstehungszeitpunkt des Originals noch nicht existierten").
4. Beide Profile unter *Administration > Projects* als Default fuer neue C#- bzw. TypeScript-Projekte setzen, ODER je Projekt-Key manuell zuweisen (robuster gegen versehentliche spaetere Default-Aenderungen).
5. Profil-Export (*Quality Profiles > … > Backup*) als XML sichern und ins Replikationspaket legen — das ist der eigentliche "Freeze"-Nachweis, nicht nur die Versionsnummer.

**Warnhinweis.** Die konkreten Regel-Keys fuer "Deprecation" haengen von der tatsaechlich installierten Version 26.8.0.126808 ab und wurden hier **nicht** erraten oder aus einer anderen Version uebernommen — das haette in einer Bachelorarbeit falsche Regel-IDs zur Folge haben koennen. Schritt 3 ist deshalb als UI-Prozedur beschrieben, nicht als fertige Kommandozeile.

## 5. Projekte anlegen und Analysen fahren

Projekte muessen vor der ersten Analyse nicht manuell angelegt werden — `dotnet-sonarscanner`/`sonar-scanner` legen einen Projekt-Key beim ersten Lauf automatisch an, sofern der Token ausreichende Rechte hat (Standard bei einem User-Token des Admin-Kontos). Empfehlung dennoch: **zuerst `check`, dann `dry-run` an einem einzelnen Lauf**, bevor alle 32 Analysen laufen:

```bash
cd ~/dev/Bachelor/bmad-vergleichsstudie
./sonar-scan.sh check
./sonar-scan.sh dry-run A bmad 01 backend
./sonar-scan.sh dry-run A bmad 01 frontend
# beide Ergebnisse in der SonarQube-UI unter dem Projekt "bmad-vgl-probe-*" pruefen,
# danach den Probe-Projekt-Key in der UI loeschen (Administration > Projects)

./sonar-scan.sh runs        # alle 12 Laeufe, 24 Analysen
./sonar-scan.sh baselines   # beide Baselines, je modulscharf+vollstaendig, 8 Analysen

./dependency-scan.sh runs
./dependency-scan.sh baselines
```

Die modulscharfe/vollstaendige Abgrenzung der Baselines nutzt die Dateien unter `sonar/scope/*.properties`, die direkt aus `claude/Feature_Datei_Zuordnung_Baseline.md` abgeleitet sind. **Vor dem produktiven Lauf** die vier dort als unsicher markierten Godsend-Frontend-Komponenten (`store/home`, `store/consult`, `store/pages`, `store/input-output`) sowie die doppelte Einordnung von `supplier.model.ts` kurz gegenpruefen (siehe dortiger Abschnitt 5 "Offene Punkte") und `sonar/scope/B-frontend-*.properties` bei Bedarf anpassen.

## 6. Kennzahlen abziehen

```bash
python3 sonar_metriken_abziehen.py --alle-aus-datei sonar-projekte.txt
```

Ergebnis: `sonar-ergebnisse/<projekt-key>.json` je der 32 Analysen (Ablage analog `cloc-ergebnisse/`), zusaetzlich `sonar-ergebnisse/abhaengigkeiten/*-dotnet.txt` und `*-npm.json` fuer die A8-Abhaengigkeitsbefunde. `A3`-Median/Anteil-ueber-10 kommt wie im Metrikkatalog vorgesehen zusaetzlich aus dem lizard-Quercheck (separates, in diesem Dokument nicht behandeltes Werkzeug — `pip install lizard`, direkt gegen dieselben seed-bereinigten Verzeichnisse laufen lassen, die `sonar-scan.sh` auch fuer den Backend-Scan verwendet).

## 7. Offene Punkte nach diesem Setup

- Reale Erstausfuehrung auf der Erhebungsmaschine (Docker/dotnet/npm vorhanden) — alles hier ist ungetestet vorbereitet.
- Regel-Deltas dokumentieren (Abschnitt 4, Schritt 3).
- `docs/SonarQube_Regel_Deltas.md` anlegen, sobald das Quality Profile in der UI feststeht.
- Vier unsichere Godsend-Frontend-Komponenten gegenpruefen (Abschnitt 5).
- Statistische Auswertung (Median/IQR je Zelle, Mann-Whitney-U/Cliff's Delta) auf Basis der `sonar-ergebnisse/*.json` — das ist `claude/Resterhebung_bis_Schreibbeginn.md`, Punkt 6, und setzt diesen Punkt vollstaendig voraus.

## Quellen (Web-Recherche 01.09.2026)

- SonarQube Docker-Image-Releases: https://github.com/SonarSource/docker-sonarqube/releases
- SonarQube Community Build Datenbankanforderungen: https://docs.sonarsource.com/sonarqube-community-build/setup-and-upgrade/installation-requirements/database-requirements/
- SonarScanner fuer .NET: https://docs.sonarsource.com/sonarqube-community-build/analyzing-source-code/scanners/dotnet/introduction/
- dotnet-sonarscanner NuGet-Paket: https://www.nuget.org/packages/dotnet-sonarscanner
- sonar-scanner-cli Docker-Tags: https://hub.docker.com/r/sonarsource/sonar-scanner-cli/tags
