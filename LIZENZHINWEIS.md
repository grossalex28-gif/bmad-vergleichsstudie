# Lizenzen

**Code** (`harness.py`, `audit.py`, `database.py`, `prompts.py`, `selftest.py`,
`derive_catalog.py`, `tools/`) steht unter der MIT-Lizenz, siehe `LICENSE`.

**Daten und Dokumente** (`inputs/`, `catalogs/`, `anfangsdatenbestand/`, `docs/`)
stehen unter CC BY 4.0. Bei Weiterverwendung ist die Urheberschaft zu nennen.

**Nicht enthalten** ist die BMAD-Installation, die als Behandlung untersucht
wird. Sie ist Fremdcode und wird über die gepinnte Version reproduziert:

```bash
npx bmad-method@6.10.0 install
```

Auszuwählen ist ausschließlich das Modul „BMad Method". Der Hash des
Installationsmanifests wird in jeder `config.json` je Lauf mitgeschrieben und
belegt, welcher Stand tatsächlich verwendet wurde.

**Die beiden Referenzprojekte**, aus denen die Feature-Kataloge abgeleitet
wurden, stehen unter der MIT-Lizenz ihrer jeweiligen Urheber. Die Herkunft ist
in der Arbeit angegeben. In den Auftragstexten unter `inputs/` ist sie bewusst
nicht genannt, um Datenleckage aus den Trainingsdaten zu erschweren.
