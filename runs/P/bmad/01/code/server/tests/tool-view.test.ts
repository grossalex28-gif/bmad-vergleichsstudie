import { test } from "node:test";
import assert from "node:assert/strict";
import { filterToolsByStatus, toToolView } from "../src/domain/tool-view.ts";
import type { Loan, Tool, ToolView } from "../src/domain/types.ts";

test("toToolView() returns status available and loan null when no loan matches the tool", () => {
  const tool: Tool = { id: "1", name: "Hammer", condition: "gut" };
  const loans: Loan[] = [];

  const view = toToolView(tool, loans);

  assert.deepEqual(view, { id: "1", name: "Hammer", condition: "gut", status: "available", loan: null });
});

test("toToolView() returns status borrowed with the matching loan's borrower and borrowedAt", () => {
  const tool: Tool = { id: "1", name: "Hammer", condition: "gut" };
  const loans: Loan[] = [{ toolId: "1", borrower: "Alex", borrowedAt: "2026-08-08T10:00:00.000Z" }];

  const view = toToolView(tool, loans);

  assert.deepEqual(view, {
    id: "1",
    name: "Hammer",
    condition: "gut",
    status: "borrowed",
    loan: { borrower: "Alex", borrowedAt: "2026-08-08T10:00:00.000Z" },
  });
});

test("toToolView() picks only the loan matching the tool's id among multiple loans in state", () => {
  const tool: Tool = { id: "2", name: "Säge", condition: "gut" };
  const loans: Loan[] = [
    { toolId: "1", borrower: "Alex", borrowedAt: "2026-08-08T10:00:00.000Z" },
    { toolId: "2", borrower: "Sam", borrowedAt: "2026-08-08T11:00:00.000Z" },
  ];

  const view = toToolView(tool, loans);

  assert.deepEqual(view, {
    id: "2",
    name: "Säge",
    condition: "gut",
    status: "borrowed",
    loan: { borrower: "Sam", borrowedAt: "2026-08-08T11:00:00.000Z" },
  });
});

const sampleViews: ToolView[] = [
  { id: "1", name: "Hammer", condition: "gut", status: "available", loan: null },
  {
    id: "2",
    name: "Säge",
    condition: "neu",
    status: "borrowed",
    loan: { borrower: "Alex", borrowedAt: "2026-08-08T10:00:00.000Z" },
  },
];

test("filterToolsByStatus() returns only available tools when status is 'available'", () => {
  const result = filterToolsByStatus(sampleViews, "available");
  assert.deepEqual(result, [sampleViews[0]]);
});

test("filterToolsByStatus() returns only borrowed tools when status is 'borrowed'", () => {
  const result = filterToolsByStatus(sampleViews, "borrowed");
  assert.deepEqual(result, [sampleViews[1]]);
});

test("filterToolsByStatus() returns the full list unchanged when status is undefined", () => {
  const result = filterToolsByStatus(sampleViews, undefined);
  assert.deepEqual(result, sampleViews);
});

test("filterToolsByStatus() returns the full list unchanged when status is an empty string", () => {
  const result = filterToolsByStatus(sampleViews, "");
  assert.deepEqual(result, sampleViews);
});

test("filterToolsByStatus() returns the full list unchanged when status is an unknown value", () => {
  const result = filterToolsByStatus(sampleViews, "foo");
  assert.deepEqual(result, sampleViews);
});
