#!/usr/bin/env python3
"""
c_metriken_auswertung.py

Ebene C (Aufwand, FF2): parst alle 12 runs/[AB]/{bmad,solo}/{01,02,03}/logs/protokoll.csv
und berechnet daraus C1a, C1c, C4, C7, C8 sowie die drei "handhabbar"-Kennzahlen
(Kosten je Lauf, Kosten je A2-Feature, Faktor gegenueber Solo).

Reine Standardbibliothek, keine externen Abhaengigkeiten (analog statistische_auswertung.py).

Modellaufruf-Ereignisse sind event in {"step", "router"} -- beide haben belastbare
duration_s/token/cost-Werte, "router" tritt ausschliesslich im BMAD-Arm auf (Routing
zwischen Skills ist Teil des Frameworks). rate_limit_wait_end traegt ebenfalls einen
duration_s-Wert, das ist aber Wartezeit, keine Modellantwortzeit, und wird deshalb
explizit ausgeschlossen.

C1c (Wall-Clock) = letzter "terminated"-Zeitstempel minus erster Zeilen-Zeitstempel.
Bei mehrfach unterbrochenen Laeufen (reopened_by_operator) ist das der END-zu-END-Wert
ueber alle Segmente hinweg, inklusive Wartezeit -- so verlangt von der Definition
("inklusive ... Wartezeiten").

C4 (Blocker-Eingriffe) wird nach Ausloeser aufgeschluesselt: "blocker" (echter R6-Fall,
z.B. API-Fehler) vs. "T1" (Wiederaufnahme nach Zeitbudget-Terminierung, kein harter
Blocker im Sinn von R6). Beide werden gezaehlt, aber getrennt ausgewiesen.

network_fetch-Events werden nicht in die C-Metriken selbst eingerechnet (keine eigene
FF), aber als Nebenprodukt sauber klassifiziert: A-SOLO-1 enthaelt einen Fehlalarm
(curl gegen http://localhost:4200, ein Selbsttest der eigenen Anwendung, keine externe
Netzwerknutzung) -- protokoll.csv bleibt dabei unveraendert (append-only, siehe
Bachelorarbeit_Gesamtstand.md Abschnitt 12), die Reklassifikation erfolgt hier in der
Auswertung.
"""
import csv
import glob
import json
import statistics
from datetime import datetime

MODELLAUFRUF_EVENTS = {"step", "router"}

# A2 (streng, nur "vollstaendig") je Lauf, aus claude/A2_Feature_Abdeckung.md Abschnitt 1
A2_STRENG = {
    "A-BMAD-1": 13, "A-BMAD-2": 13, "A-BMAD-3": 13,
    "A-SOLO-1": 13, "A-SOLO-2": 13, "A-SOLO-3": 13,
    "B-BMAD-1": 14, "B-BMAD-2": 8, "B-BMAD-3": 10,
    "B-SOLO-1": 13, "B-SOLO-2": 13, "B-SOLO-3": 13,
}


def parse_ts(s):
    return datetime.strptime(s, "%Y-%m-%dT%H:%M:%S%z")


def lade_protokoll(pfad):
    with open(pfad, newline="", encoding="utf-8") as fh:
        return list(csv.DictReader(fh))


def werte_lauf_aus(pfad):
    rows = lade_protokoll(pfad)
    if not rows:
        return None
    run_id = rows[0]["run_id"]

    modell_rows = [r for r in rows if r["event"] in MODELLAUFRUF_EVENTS]
    step_rows = [r for r in modell_rows if r["event"] == "step"]
    router_rows = [r for r in modell_rows if r["event"] == "router"]

    c1a = sum(float(r["duration_s"]) for r in modell_rows if r["duration_s"])
    c7 = len(modell_rows)
    c7_step = len(step_rows)
    c7_router = len(router_rows)

    def summe(feld):
        return sum(float(r[feld]) for r in modell_rows if r[feld])

    in_tok = summe("in_tok")
    out_tok = summe("out_tok")
    cache_c_tok = summe("cache_c_tok")
    cache_r_tok = summe("cache_r_tok")
    cost_usd = summe("cost_usd")

    zeitstempel = [parse_ts(r["timestamp"]) for r in rows if r["timestamp"]]
    terminated_rows = [r for r in rows if r["event"] == "terminated"]
    if not zeitstempel or not terminated_rows:
        c1c = None
    else:
        start = min(zeitstempel)
        ende = max(parse_ts(r["timestamp"]) for r in terminated_rows)
        c1c = (ende - start).total_seconds()

    reopen_rows = [r for r in rows if r["event"] == "reopened_by_operator"]
    c4_blocker = sum(1 for r in reopen_rows if "blocker" in r["detail"])
    c4_t1 = sum(1 for r in reopen_rows if "T1" in r["detail"] and "blocker" not in r["detail"])
    c4_sonstige = len(reopen_rows) - c4_blocker - c4_t1

    netz_rows = [r for r in rows if r["event"] == "network_fetch"]
    netz_extern = [r for r in netz_rows if "localhost" not in r["detail"]]
    netz_fehlalarm = [r for r in netz_rows if "localhost" in r["detail"]]

    a2 = A2_STRENG.get(run_id)
    kosten_je_feature = cost_usd / a2 if a2 else None

    return {
        "run_id": run_id,
        "projekt": run_id.split("-")[0],
        "ansatz": run_id.split("-")[1].lower(),
        "c1a_modellzeit_s": round(c1a, 1),
        "c1c_wallclock_s": round(c1c, 1) if c1c is not None else None,
        "c1c_minus_c1a_s": round(c1c - c1a, 1) if c1c is not None else None,
        "c4_blocker_eingriffe": len(reopen_rows),
        "c4_davon_harter_blocker": c4_blocker,
        "c4_davon_t1_zeitbudget": c4_t1,
        "c4_davon_sonstige": c4_sonstige,
        "c7_iterationen": c7,
        "c7_davon_step": c7_step,
        "c7_davon_router": c7_router,
        "c8_in_tok": int(in_tok),
        "c8_out_tok": int(out_tok),
        "c8_cache_creation_tok": int(cache_c_tok),
        "c8_cache_read_tok": int(cache_r_tok),
        "c8_kosten_usd": round(cost_usd, 4),
        "a2_streng_anzahl": a2,
        "kosten_je_a2_feature_usd": round(kosten_je_feature, 4) if kosten_je_feature else None,
        "netz_externe_zugriffe": len(netz_extern),
        "netz_fehlalarme_lokal": len(netz_fehlalarm),
        "netz_fehlalarm_details": [r["detail"][:120] for r in netz_fehlalarm],
    }


def main():
    dateien = sorted(glob.glob("runs/[AB]/*/*/logs/protokoll.csv"))
    laeufe = [werte_lauf_aus(f) for f in dateien]
    laeufe = [l for l in laeufe if l is not None]
    laeufe.sort(key=lambda l: l["run_id"])

    felder = list(laeufe[0].keys())
    felder.remove("netz_fehlalarm_details")
    with open("auswertung/interactions.csv", "w", newline="", encoding="utf-8") as fh:
        w = csv.DictWriter(fh, fieldnames=felder)
        w.writeheader()
        for l in laeufe:
            w.writerow({k: l[k] for k in felder})

    def gruppe(ansatz):
        return [l for l in laeufe if l["ansatz"] == ansatz]

    bmad = gruppe("bmad")
    solo = gruppe("solo")

    def mittel(liste, feld):
        werte = [l[feld] for l in liste if l[feld] is not None]
        return statistics.mean(werte) if werte else None

    handhabbar = {
        "kosten_je_lauf_usd": {
            "bmad_median": statistics.median([l["c8_kosten_usd"] for l in bmad]),
            "bmad_mittel": mittel(bmad, "c8_kosten_usd"),
            "solo_median": statistics.median([l["c8_kosten_usd"] for l in solo]),
            "solo_mittel": mittel(solo, "c8_kosten_usd"),
        },
        "kosten_je_a2_feature_usd": {
            "bmad_median": statistics.median([l["kosten_je_a2_feature_usd"] for l in bmad]),
            "bmad_mittel": mittel(bmad, "kosten_je_a2_feature_usd"),
            "solo_median": statistics.median([l["kosten_je_a2_feature_usd"] for l in solo]),
            "solo_mittel": mittel(solo, "kosten_je_a2_feature_usd"),
        },
    }
    handhabbar["faktor_kosten_je_lauf_bmad_vs_solo"] = round(
        handhabbar["kosten_je_lauf_usd"]["bmad_mittel"] / handhabbar["kosten_je_lauf_usd"]["solo_mittel"], 2
    )
    handhabbar["faktor_kosten_je_feature_bmad_vs_solo"] = round(
        handhabbar["kosten_je_a2_feature_usd"]["bmad_mittel"] / handhabbar["kosten_je_a2_feature_usd"]["solo_mittel"], 2
    )

    je_projekt = {}
    for projekt in ("A", "B"):
        pb = [l for l in bmad if l["projekt"] == projekt]
        ps = [l for l in solo if l["projekt"] == projekt]
        je_projekt[projekt] = {
            "bmad_kosten_median_usd": statistics.median([l["c8_kosten_usd"] for l in pb]),
            "solo_kosten_median_usd": statistics.median([l["c8_kosten_usd"] for l in ps]),
            "faktor": round(statistics.median([l["c8_kosten_usd"] for l in pb]) /
                             statistics.median([l["c8_kosten_usd"] for l in ps]), 2),
        }

    netz_zusammenfassung = {
        "echte_externe_zugriffe_gesamt": sum(l["netz_externe_zugriffe"] for l in laeufe),
        "davon_je_lauf": {l["run_id"]: l["netz_externe_zugriffe"] for l in laeufe if l["netz_externe_zugriffe"] > 0},
        "fehlalarme_gesamt": sum(l["netz_fehlalarme_lokal"] for l in laeufe),
        "fehlalarm_details": {l["run_id"]: l["netz_fehlalarm_details"] for l in laeufe if l["netz_fehlalarme_lokal"] > 0},
    }

    ausgabe = {
        "laeufe": laeufe,
        "handhabbar_kosten": handhabbar,
        "handhabbar_kosten_je_projekt": je_projekt,
        "netzwerkklassifikation": netz_zusammenfassung,
    }
    with open("auswertung/c_metriken.json", "w", encoding="utf-8") as fh:
        json.dump(ausgabe, fh, indent=2, ensure_ascii=False)

    print(json.dumps(ausgabe, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
