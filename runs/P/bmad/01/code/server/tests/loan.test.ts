import { test } from "node:test";
import assert from "node:assert/strict";
import { borrowTool, returnTool } from "../src/domain/loan.ts";
import type { State } from "../src/domain/types.ts";

const now = new Date("2026-08-08T12:00:00.000Z");

function stateWithTools(): State {
  return {
    tools: [
      { id: "1", name: "Hammer", condition: "gut" },
      { id: "2", name: "Säge", condition: "neu" },
    ],
    loans: [],
  };
}

test("borrowTool erstellt einen neuen Loan-Eintrag und liefert eine ToolView mit status borrowed", () => {
  const state = stateWithTools();
  const { state: nextState, result } = borrowTool(state, "1", "Ben", now);

  assert.equal(result.ok, true);
  assert.deepEqual(result.ok ? result.tool : undefined, {
    id: "1",
    name: "Hammer",
    condition: "gut",
    status: "borrowed",
    loan: { borrower: "Ben", borrowedAt: "2026-08-08T12:00:00.000Z" },
  });
  assert.deepEqual(nextState.loans, [{ toolId: "1", borrower: "Ben", borrowedAt: "2026-08-08T12:00:00.000Z" }]);
});

test("borrowTool trimmt führende und nachgestellte Leerzeichen im borrower vor dem Speichern", () => {
  const state = stateWithTools();
  const { state: nextState, result } = borrowTool(state, "1", "  Ben  ", now);

  assert.equal(result.ok, true);
  assert.deepEqual(result.ok ? result.tool.loan : undefined, {
    borrower: "Ben",
    borrowedAt: "2026-08-08T12:00:00.000Z",
  });
  assert.deepEqual(nextState.loans, [{ toolId: "1", borrower: "Ben", borrowedAt: "2026-08-08T12:00:00.000Z" }]);
});

test("borrowTool liefert VALIDATION_ERROR bei fehlendem borrower und lässt den State unverändert", () => {
  const state = stateWithTools();
  const { state: nextState, result } = borrowTool(state, "1", undefined, now);

  assert.deepEqual(result, {
    ok: false,
    error: { code: "VALIDATION_ERROR", message: "Bitte einen Namen angeben." },
  });
  assert.deepEqual(nextState, state);
  assert.equal(nextState.loans.length, 0);
});

test("borrowTool liefert VALIDATION_ERROR bei leerem borrower", () => {
  const state = stateWithTools();
  const { state: nextState, result } = borrowTool(state, "1", "", now);

  assert.deepEqual(result, {
    ok: false,
    error: { code: "VALIDATION_ERROR", message: "Bitte einen Namen angeben." },
  });
  assert.deepEqual(nextState, state);
});

test("borrowTool liefert VALIDATION_ERROR bei nur aus Leerzeichen bestehendem borrower", () => {
  const state = stateWithTools();
  const { state: nextState, result } = borrowTool(state, "1", "   ", now);

  assert.deepEqual(result, {
    ok: false,
    error: { code: "VALIDATION_ERROR", message: "Bitte einen Namen angeben." },
  });
  assert.deepEqual(nextState, state);
});

test("borrowTool liefert TOOL_NOT_FOUND bei unbekannter id", () => {
  const state = stateWithTools();
  const { state: nextState, result } = borrowTool(state, "unbekannt", "Ben", now);

  assert.deepEqual(result, {
    ok: false,
    error: { code: "TOOL_NOT_FOUND", message: "Werkzeug nicht gefunden." },
  });
  assert.deepEqual(nextState, state);
});

test("borrowTool liefert ALREADY_BORROWED bei bereits ausgeliehenem Werkzeug und lässt den bestehenden Loan unverändert", () => {
  const existingLoan = { toolId: "2", borrower: "Alex", borrowedAt: "2026-08-08T10:00:00.000Z" };
  const state: State = { ...stateWithTools(), loans: [existingLoan] };
  const { state: nextState, result } = borrowTool(state, "2", "Ben", now);

  assert.deepEqual(result, {
    ok: false,
    error: { code: "ALREADY_BORROWED", message: "Dieses Werkzeug ist bereits ausgeliehen." },
  });
  assert.deepEqual(nextState.loans, [existingLoan]);
  assert.equal(nextState.loans.length, 1);
});

test("returnTool löscht den Loan-Eintrag und liefert eine ToolView mit status available", () => {
  const loan = { toolId: "2", borrower: "Alex", borrowedAt: "2026-08-08T10:00:00.000Z" };
  const state: State = { ...stateWithTools(), loans: [loan] };
  const { state: nextState, result } = returnTool(state, "2");

  assert.equal(result.ok, true);
  assert.deepEqual(result.ok ? result.tool : undefined, {
    id: "2",
    name: "Säge",
    condition: "neu",
    status: "available",
    loan: null,
  });
  assert.deepEqual(nextState.loans, []);
});

test("returnTool entfernt nur den Loan des zurückgegebenen Werkzeugs, andere Loans bleiben erhalten", () => {
  const loanA = { toolId: "1", borrower: "Alex", borrowedAt: "2026-08-08T09:00:00.000Z" };
  const loanB = { toolId: "2", borrower: "Ben", borrowedAt: "2026-08-08T10:00:00.000Z" };
  const state: State = { ...stateWithTools(), loans: [loanA, loanB] };
  const { state: nextState, result } = returnTool(state, "2");

  assert.equal(result.ok, true);
  assert.deepEqual(nextState.loans, [loanA]);
});

test("returnTool liefert NOT_BORROWED bei Werkzeug ohne offenen Loan und lässt den State unverändert", () => {
  const state = stateWithTools();
  const { state: nextState, result } = returnTool(state, "1");

  assert.deepEqual(result, {
    ok: false,
    error: { code: "NOT_BORROWED", message: "Dieses Werkzeug ist nicht ausgeliehen." },
  });
  assert.deepEqual(nextState, state);
});

test("returnTool liefert TOOL_NOT_FOUND bei unbekannter id und lässt den State unverändert", () => {
  const state = stateWithTools();
  const { state: nextState, result } = returnTool(state, "unbekannt");

  assert.deepEqual(result, {
    ok: false,
    error: { code: "TOOL_NOT_FOUND", message: "Werkzeug nicht gefunden." },
  });
  assert.deepEqual(nextState, state);
});
