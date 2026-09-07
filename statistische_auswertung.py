#!/usr/bin/env python3
"""
statistische_auswertung.py

Fuehrt Resterhebung_bis_Schreibbeginn.md Punkt 6 aus: Median/IQR je Zelle
fuer alle A-Kern-Metriken (A3, A5, A6, A7, A8), ergaenzend Mann-Whitney-U mit
Cliff's Delta gepoolt ueber beide Projekte (BMAD n=6 gegen Solo n=6).

Kombinationsregel Backend+Frontend (Metrikkatalog.md Abschnitt 2, "Getrennte
Auswertung nach Schicht"): je Metrik ncloc-gewichteter Mittelwert -- fuer
Zaehlmetriken (A7, A8) ist das rechnerisch identisch mit "Summe der Rohzahlen
durch Summe ncloc", fuer A3 wird stattdessen nach Funktionsanzahl gepoolt
(mittlere Komplexitaet je Funktion = Gesamtkomplexitaet / Gesamtfunktionen),
weil A3 selbst eine Pro-Funktion-Groesse ist. A3-Kernzahl kommt aus SonarQube
(complexity/functions je Schicht, ncloc-analog nach Funktionsanzahl gepoolt),
siehe Entscheidung vom 03.09.2026 -- lizards Median/Anteil>10 bleibt als
unabhaengiger Anhang-Quercheck ohne Schichttrennung (separates Ergebnis,
siehe lizard-quercheck.sh / lizard-ergebnisse/).

Nur Standardbibliothek, keine externen Abhaengigkeiten (kein scipy/numpy noetig
-- Mann-Whitney-U wird exakt per Permutationstest ueber alle C(12,6)=924
Aufteilungen berechnet, das ist bei n=6 gegen 6 trivial schnell und robust
gegenueber Bindungen).

Aufruf:
  python3 statistische_auswertung.py
"""

import json
import re
import statistics
from itertools import combinations
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent
SONAR_DIR = REPO_ROOT / "sonar-ergebnisse"
DEP_DIR = SONAR_DIR / "abhaengigkeiten"
OUT_DIR = REPO_ROOT / "auswertung"
OUT_DIR.mkdir(exist_ok=True)

PROJEKTE = ["A", "B"]
ANSAETZE = ["bmad", "solo"]
NUMMERN = ["01", "02", "03"]
LAYERS = ["backend", "frontend"]
VARIANTEN = ["modulscharf", "vollstaendig"]

DOTNET_SEV_RE = re.compile(
    r"(Low|Moderate|High|Critical)\s+https://github\.com/advisories/\S+"
)


# ---------------------------------------------------------------------------
# Rohdaten laden
# ---------------------------------------------------------------------------

def lade_sonar(projekt_key: str) -> dict:
    pfad = SONAR_DIR / f"{projekt_key}.json"
    if not pfad.exists():
        raise FileNotFoundError(f"Fehlt: {pfad}")
    with open(pfad, encoding="utf-8") as f:
        return json.load(f)


def parse_dotnet_txt(pfad: Path) -> dict:
    """Zaehlt Sicherheitsbefunde je Schweregrad ueber alle Advisory-Zeilen,
    unabhaengig von Top-level/Transitive-Tabellenstruktur (robust gegen
    Fortsetzungszeilen ohne Paketnamen-Spalte)."""
    zaehler = {"Low": 0, "Moderate": 0, "High": 0, "Critical": 0}
    if not pfad.exists():
        return zaehler
    text = pfad.read_text(encoding="utf-8", errors="replace")
    for match in DOTNET_SEV_RE.finditer(text):
        zaehler[match.group(1)] += 1
    return zaehler


def parse_npm_json(pfad: Path) -> dict:
    zaehler = {"info": 0, "low": 0, "moderate": 0, "high": 0, "critical": 0}
    if not pfad.exists():
        return zaehler
    with open(pfad, encoding="utf-8") as f:
        d = json.load(f)
    meta = d.get("metadata", {}).get("vulnerabilities", {})
    for k in zaehler:
        zaehler[k] = int(meta.get(k, 0))
    return zaehler


def lade_abhaengigkeiten(dep_key: str) -> dict:
    dotnet = parse_dotnet_txt(DEP_DIR / f"{dep_key}-dotnet.txt")
    npm = parse_npm_json(DEP_DIR / f"{dep_key}-npm.json")
    total = sum(dotnet.values()) + sum(npm.values())
    return {"dotnet": dotnet, "npm": npm, "gesamt": total}


# ---------------------------------------------------------------------------
# Kombination Backend + Frontend
# ---------------------------------------------------------------------------

def kombiniere(sonar_be: dict, sonar_fe: dict, dep: dict) -> dict:
    m_be, m_fe = sonar_be["measures"], sonar_fe["measures"]

    ncloc_be, ncloc_fe = int(m_be["ncloc"]), int(m_fe["ncloc"])
    ncloc_total = ncloc_be + ncloc_fe

    funcs_be, funcs_fe = int(m_be["functions"]), int(m_fe["functions"])
    funcs_total = funcs_be + funcs_fe
    komplexitaet_be, komplexitaet_fe = int(m_be["complexity"]), int(m_fe["complexity"])

    a3 = (komplexitaet_be + komplexitaet_fe) / funcs_total if funcs_total else None

    def ncloc_gewichtet(feld):
        v_be, v_fe = float(m_be[feld]), float(m_fe[feld])
        return (v_be * ncloc_be + v_fe * ncloc_fe) / ncloc_total if ncloc_total else None

    a5 = ncloc_gewichtet("sqale_debt_ratio")
    a6 = ncloc_gewichtet("duplicated_lines_density")

    code_smells_total = int(m_be["code_smells"]) + int(m_fe["code_smells"])
    a7 = code_smells_total / ncloc_total * 1000 if ncloc_total else None

    vuln_be = sonar_be.get("vulnerabilities_nach_schweregrad", {})
    vuln_fe = sonar_fe.get("vulnerabilities_nach_schweregrad", {})
    vuln_kombiniert = {
        sev: vuln_be.get(sev, 0) + vuln_fe.get(sev, 0)
        for sev in set(vuln_be) | set(vuln_fe)
    }
    vuln_total = sum(vuln_kombiniert.values())
    a8_eigen = vuln_total / ncloc_total * 1000 if ncloc_total else None

    a8_abhaengig = dep["gesamt"] / ncloc_total * 1000 if ncloc_total else None

    return {
        "ncloc_backend": ncloc_be,
        "ncloc_frontend": ncloc_fe,
        "ncloc_gesamt": ncloc_total,
        "A3_mittlere_komplexitaet_je_funktion": round(a3, 4) if a3 is not None else None,
        "A5_sqale_debt_ratio_kombiniert": round(a5, 4) if a5 is not None else None,
        "A6_duplicated_lines_density_kombiniert": round(a6, 4) if a6 is not None else None,
        "A7_code_smells_je_1000_ncloc": round(a7, 4) if a7 is not None else None,
        "A8_eigen_je_1000_ncloc": round(a8_eigen, 4) if a8_eigen is not None else None,
        "A8_eigen_schweregrade": vuln_kombiniert,
        "A8_abhaengig_je_1000_ncloc": round(a8_abhaengig, 4) if a8_abhaengig is not None else None,
        "A8_abhaengig_schweregrade": {"dotnet": dep["dotnet"], "npm": dep["npm"]},
    }


# ---------------------------------------------------------------------------
# Zellen (12 Laeufe) und Baselines einlesen
# ---------------------------------------------------------------------------

zellen = {}  # (projekt, ansatz, nn) -> kombinierte Werte
for projekt in PROJEKTE:
    for ansatz in ANSAETZE:
        for nn in NUMMERN:
            pk = projekt.lower()
            key_be = f"bmad-vgl-{pk}-{ansatz}-{nn}-backend"
            key_fe = f"bmad-vgl-{pk}-{ansatz}-{nn}-frontend"
            dep_key = f"{projekt}-{ansatz}-{nn}"
            sonar_be = lade_sonar(key_be)
            sonar_fe = lade_sonar(key_fe)
            dep = lade_abhaengigkeiten(dep_key)
            zellen[(projekt, ansatz, nn)] = kombiniere(sonar_be, sonar_fe, dep)

baselines = {}  # (projekt, variante) -> kombinierte Werte
for projekt in PROJEKTE:
    dep = lade_abhaengigkeiten(f"baseline-{projekt}")
    for variante in VARIANTEN:
        pk = projekt.lower()
        key_be = f"bmad-vgl-baseline-{pk}-{variante}-backend"
        key_fe = f"bmad-vgl-baseline-{pk}-{variante}-frontend"
        sonar_be = lade_sonar(key_be)
        sonar_fe = lade_sonar(key_fe)
        baselines[(projekt, variante)] = kombiniere(sonar_be, sonar_fe, dep)


# ---------------------------------------------------------------------------
# Median/IQR je Zelle (Projekt x Ansatz, ueber die drei Laeufe 01-03)
# ---------------------------------------------------------------------------

METRIKEN = [
    "A3_mittlere_komplexitaet_je_funktion",
    "A5_sqale_debt_ratio_kombiniert",
    "A6_duplicated_lines_density_kombiniert",
    "A7_code_smells_je_1000_ncloc",
    "A8_eigen_je_1000_ncloc",
    "A8_abhaengig_je_1000_ncloc",
]


def median_iqr(werte):
    werte = sorted(werte)
    med = statistics.median(werte)
    if len(werte) >= 2:
        q1, _, q3 = statistics.quantiles(werte, n=4, method="inclusive")
    else:
        q1 = q3 = med
    return {"median": round(med, 4), "q1": round(q1, 4), "q3": round(q3, 4), "iqr": round(q3 - q1, 4), "n": len(werte)}


zellen_stats = {}
for projekt in PROJEKTE:
    for ansatz in ANSAETZE:
        gruppe = f"{projekt}-{ansatz}"
        zellen_stats[gruppe] = {}
        for metrik in METRIKEN:
            werte = [zellen[(projekt, ansatz, nn)][metrik] for nn in NUMMERN]
            zellen_stats[gruppe][metrik] = median_iqr(werte)


# ---------------------------------------------------------------------------
# Mann-Whitney-U (exakt, Permutationstest) + Cliff's Delta,
# gepoolt ueber beide Projekte: BMAD (n=6) gegen Solo (n=6)
# ---------------------------------------------------------------------------

def cliffs_delta(x, y):
    n, m = len(x), len(y)
    mehr = sum(1 for xi in x for yj in y if xi > yj)
    weniger = sum(1 for xi in x for yj in y if xi < yj)
    return (mehr - weniger) / (n * m)


def u_statistik(gruppe1, gruppe2):
    n1, n2 = len(gruppe1), len(gruppe2)
    kombiniert = sorted(
        [(w, 1) for w in gruppe1] + [(w, 2) for w in gruppe2], key=lambda t: t[0]
    )
    # Raenge mit Bindungsdurchschnitt
    raenge = [0.0] * len(kombiniert)
    i = 0
    while i < len(kombiniert):
        j = i
        while j + 1 < len(kombiniert) and kombiniert[j + 1][0] == kombiniert[i][0]:
            j += 1
        rang_avg = (i + 1 + j + 1) / 2.0
        for k in range(i, j + 1):
            raenge[k] = rang_avg
        i = j + 1
    r1 = sum(r for r, (w, g) in zip(raenge, kombiniert) if g == 1)
    u1 = r1 - n1 * (n1 + 1) / 2
    return u1


def mann_whitney_exakt(gruppe1, gruppe2):
    n1, n2 = len(gruppe1), len(gruppe2)
    alle = list(gruppe1) + list(gruppe2)
    u_beob = u_statistik(gruppe1, gruppe2)
    u_min_beob = min(u_beob, n1 * n2 - u_beob)

    indizes = range(len(alle))
    anzahl_kombis = 0
    anzahl_extrem = 0
    for auswahl in combinations(indizes, n1):
        anzahl_kombis += 1
        g1 = [alle[i] for i in auswahl]
        rest = set(indizes) - set(auswahl)
        g2 = [alle[i] for i in rest]
        u = u_statistik(g1, g2)
        u_min = min(u, n1 * n2 - u)
        if u_min <= u_min_beob + 1e-9:
            anzahl_extrem += 1
    p_wert = anzahl_extrem / anzahl_kombis
    return {"U": round(u_beob, 3), "U_min": round(u_min_beob, 3), "p_exakt_zweiseitig": round(p_wert, 5), "kombinationen_geprueft": anzahl_kombis}


mwu_ergebnisse = {}
for metrik in METRIKEN:
    bmad_werte = [
        zellen[(projekt, "bmad", nn)][metrik] for projekt in PROJEKTE for nn in NUMMERN
    ]
    solo_werte = [
        zellen[(projekt, "solo", nn)][metrik] for projekt in PROJEKTE for nn in NUMMERN
    ]
    mwu = mann_whitney_exakt(bmad_werte, solo_werte)
    delta = cliffs_delta(bmad_werte, solo_werte)
    mwu_ergebnisse[metrik] = {
        **mwu,
        "cliffs_delta": round(delta, 4),
        "bmad_n": len(bmad_werte),
        "solo_n": len(solo_werte),
        "bmad_werte": bmad_werte,
        "solo_werte": solo_werte,
    }


# ---------------------------------------------------------------------------
# Ausgabe
# ---------------------------------------------------------------------------

def jsonify_keys(d):
    return {f"{k[0]}-{k[1]}" if isinstance(k, tuple) and len(k) == 2 else "-".join(k): v for k, v in d.items()}


ergebnis = {
    "zellen_rohwerte": {f"{p}-{a}-{nn}": v for (p, a, nn), v in zellen.items()},
    "baseline_rohwerte": {f"{p}-{v}": val for (p, v), val in baselines.items()},
    "zellen_median_iqr": zellen_stats,
    "mann_whitney_cliffs_delta_bmad_vs_solo": mwu_ergebnisse,
}

out_pfad = OUT_DIR / "statistische_auswertung.json"
with open(out_pfad, "w", encoding="utf-8") as f:
    json.dump(ergebnis, f, indent=2, ensure_ascii=False)

print(f"Gespeichert: {out_pfad}\n")

print("=== Median/IQR je Zelle (Projekt-Ansatz, n=3 Laeufe) ===")
for gruppe, metriken in zellen_stats.items():
    print(f"\n{gruppe}:")
    for metrik, stat in metriken.items():
        print(f"  {metrik}: median={stat['median']}, IQR=[{stat['q1']}, {stat['q3']}] (Breite {stat['iqr']})")

print("\n=== Mann-Whitney-U + Cliff's Delta, BMAD (n=6) vs. Solo (n=6), gepoolt A+B ===")
for metrik, r in mwu_ergebnisse.items():
    signifikant = "signifikant (p<0.05)" if r["p_exakt_zweiseitig"] < 0.05 else "nicht signifikant"
    print(f"\n{metrik}:")
    print(f"  BMAD: {r['bmad_werte']}")
    print(f"  Solo: {r['solo_werte']}")
    print(f"  U={r['U']}, p={r['p_exakt_zweiseitig']} ({signifikant}), Cliff's delta={r['cliffs_delta']}")

print("\n=== Baseline-Werte (deskriptiv, A5/A7/A8 versionskonfundiert) ===")
for (p, v), val in baselines.items():
    print(f"\n{p}-{v}:")
    for metrik in METRIKEN:
        print(f"  {metrik}: {val[metrik]}")
