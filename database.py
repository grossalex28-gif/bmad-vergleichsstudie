"""
Datenbanksteuerung fuer die Messlaeufe.

Warum eigenstaendig: Der Probelauf war dateibasiert und brauchte keine Datenbank.
Die zwoelf Messlaeufe laufen gegen SQL Server, und `claude/Seed-Repository_Anleitung.md`
Abschnitt 5 verlangt genau zwei Dinge, die der Harness bisher nicht leistet:

1. Je Lauf eine eigene, leere Datenbank, damit sich der Datenstand eines Laufs
   im Nachhinein isoliert betrachten laesst (Entsprechung zu H1 auf der
   Persistenzseite).
2. Die Verbindungszeichenfolge als Laufzeit-Injektion ueber eine
   Umgebungsvariable statt als Datei im Seed-Commit, weil der Seed-Commit ueber
   alle sechs Laeufe eines Projekts byteidentisch bleiben muss, der
   Datenbankname aber je Lauf wechselt.

Das Schema legt der jeweilige Arm selbst an. Der Harness erzeugt ausschliesslich
eine leere Datenbank und reicht die Verbindung weiter. Alles darueber hinaus
waere eine Vorgabe zur Datenmodellierung und damit Teil dessen, was gemessen
werden soll.

Nur Standardbibliothek.
"""

from __future__ import annotations

import os
import subprocess
from typing import Any, Optional

REDACTED = "***"


def db_config(raw: dict) -> Optional[dict]:
    """Liefert den Datenbankabschnitt, oder None wenn abgeschaltet.

    Fehlt der Abschnitt ganz, gilt er als abgeschaltet. Damit bleibt die
    Probelauf-Konfiguration unveraendert lauffaehig.
    """
    d = raw.get("database")
    if not d or not d.get("enabled"):
        return None
    return d


def database_name(dbcfg: dict, project: str, condition: str, repetition: int) -> str:
    tmpl = dbcfg.get("name_template", "{project}_{condition}_{repetition}")
    name = tmpl.format(project=project.lower(), condition=condition.lower(),
                       repetition=repetition)
    if not name.replace("_", "").isalnum():
        raise SystemExit(f"Unzulaessiger Datenbankname '{name}'. "
                         "Erlaubt sind ausschliesslich Buchstaben, Ziffern und Unterstrich.")
    return name


def password(dbcfg: dict) -> str:
    var = dbcfg.get("password_env", "MSSQL_SA_PASSWORD")
    pw = os.environ.get(var)
    if not pw:
        raise SystemExit(
            f"Umgebungsvariable {var} ist nicht gesetzt. Das Kennwort der "
            "SQL-Server-Instanz steht bewusst nicht in der Konfiguration, damit "
            "config.messlauf.json unveraendert ins Replikationspaket kann.")
    return pw


def connection_string(dbcfg: dict, name: str, redact: bool = False) -> str:
    tmpl = dbcfg.get(
        "connection_template",
        "Server={server};Database={database};User Id={user};Password={password};"
        "TrustServerCertificate=True;Encrypt=True")
    return tmpl.format(server=dbcfg.get("server", "localhost,1433"),
                       database=name,
                       user=dbcfg.get("user", "sa"),
                       password=REDACTED if redact else password(dbcfg))


def env_var(dbcfg: dict) -> str:
    return dbcfg.get("env_var", "ConnectionStrings__Default")


def _argv(dbcfg: dict, sql: str, database: Optional[str] = None) -> list[str]:
    sqlcmd = dbcfg.get("sqlcmd", "/opt/mssql-tools18/bin/sqlcmd")
    base: list[str] = []
    if dbcfg.get("mode", "docker") == "docker":
        container = dbcfg.get("container")
        if not container:
            raise SystemExit("database.mode ist 'docker', aber database.container fehlt.")
        binary = dbcfg.get("docker_bin", "docker")
        # Auch als Liste zulaessig, damit ["sudo", "docker"] moeglich ist, ohne
        # den Aufruf durch eine Shell zu schicken.
        base = (list(binary) if isinstance(binary, list) else [binary]) + \
            ["exec", container]
    argv = base + [sqlcmd,
                   "-S", dbcfg.get("server", "localhost,1433"),
                   "-U", dbcfg.get("user", "sa"),
                   "-P", password(dbcfg),
                   "-b"]
    if dbcfg.get("trust_server_certificate", True):
        argv.append("-C")
    if database:
        argv += ["-d", database]
    argv += ["-Q", sql]
    return argv


def run_sql(dbcfg: dict, sql: str, database: Optional[str] = None,
            timeout_s: float = 120.0) -> str:
    """Fuehrt eine Anweisung aus und liefert die Ausgabe. Bricht bei Fehler ab.

    Das Kennwort steht im Argumentvektor und darf deshalb in keiner
    Fehlermeldung auftauchen, weshalb bei einem Fehlschlag nur stdout und
    stderr wiedergegeben werden, nicht der Aufruf selbst.
    """
    try:
        proc = subprocess.run(_argv(dbcfg, sql, database), capture_output=True,
                              text=True, timeout=timeout_s)
    except FileNotFoundError as exc:
        raise SystemExit(f"Datenbankwerkzeug nicht gefunden: {exc}") from exc
    except subprocess.TimeoutExpired as exc:
        raise SystemExit(f"Datenbankbefehl nach {timeout_s:.0f} s ohne Antwort.") from exc
    if proc.returncode != 0:
        raise SystemExit("Datenbankbefehl fehlgeschlagen.\n"
                         f"  stdout: {proc.stdout.strip()[:500]}\n"
                         f"  stderr: {proc.stderr.strip()[:500]}")
    return proc.stdout


def check(dbcfg: dict) -> str:
    """Erreichbarkeitspruefung. Liefert die Versionszeile der Instanz."""
    out = run_sql(dbcfg, "SET NOCOUNT ON; SELECT @@VERSION;")
    for line in out.splitlines():
        line = line.strip()
        if line and not line.startswith("-"):
            return line
    return out.strip()


def reset_database(dbcfg: dict, name: str) -> None:
    """Leere Datenbank je Lauf. Vorhandene Verbindungen werden getrennt.

    SINGLE_USER WITH ROLLBACK IMMEDIATE ist noetig, weil ein abgebrochener
    Vorlauf oder ein noch offener Verbindungspool das DROP sonst blockiert.
    """
    sql = (
        f"IF DB_ID('{name}') IS NOT NULL "
        f"BEGIN "
        f"  ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; "
        f"  DROP DATABASE [{name}]; "
        f"END; "
        f"CREATE DATABASE [{name}];"
    )
    run_sql(dbcfg, sql)


def engine_fingerprint(dbcfg: dict) -> dict[str, Any]:
    """Angaben zur Engine fuer config.json je Lauf.

    Der Image-Digest steht in seed.meta.json und ist damit ueber den Seed
    belegt. Hier interessiert die tatsaechlich antwortende Instanz.
    """
    return {
        "mode": dbcfg.get("mode", "docker"),
        "container": dbcfg.get("container"),
        "server": dbcfg.get("server", "localhost,1433"),
        "version": check(dbcfg),
    }
