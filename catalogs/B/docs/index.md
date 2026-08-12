# Project Documentation Index

## Project Overview

- **Type:** multi-part (backend API + shared domain library + Angular web client + thin mobile/Telegram alternate clients)
- **Primary Language:** C# (backend/domain), TypeScript (web client)
- **Architecture:** Layered REST API (generic entity base controller/repository pattern) with a componentized Angular SPA frontend; real-time push via WebSocket-based notifications.

## Scan Scope

This was a **targeted deep scan** aimed at extracting the business/domain model (entities, controllers, business rules, user-facing capabilities) to support deriving an anonymized feature catalog, rather than a full architecture/deployment documentation pass. See `project-scan-report.json` for the structured findings.

## Generated Documentation

- [Project Scan Report](./project-scan-report.json) — structured findings: entities, business rules, user-facing capabilities
- [Architecture](./architecture.md) _(To be generated)_
- [Component Inventory](./component-inventory.md) _(To be generated)_
- [Development Guide](./development-guide.md) _(To be generated)_
- [API Contracts](./api-contracts.md) _(To be generated)_
- [Data Models](./data-models.md) _(To be generated)_

## Existing Documentation

- [README](../repo/README.md) — original project readme with feature screenshots

## Getting Started

The condensed findings in `project-scan-report.json` were used directly to produce `../katalog-entwurf.md`, the anonymized feature catalog. For a full architecture/deployment documentation pass, re-run the document-project workflow in `full_rescan` mode with `scan_level = deep` or `exhaustive`.
