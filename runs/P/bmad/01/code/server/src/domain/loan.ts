import type { State, ToolView } from "./types.ts";
import { toToolView } from "./tool-view.ts";

export type BorrowResult =
  | { ok: true; tool: ToolView }
  | {
      ok: false;
      error: { code: "VALIDATION_ERROR" | "TOOL_NOT_FOUND" | "ALREADY_BORROWED"; message: string };
    };

export function borrowTool(
  state: State,
  toolId: string,
  borrower: string | undefined,
  now: Date,
): { state: State; result: BorrowResult } {
  const trimmedBorrower = typeof borrower === "string" ? borrower.trim() : "";
  if (trimmedBorrower.length === 0) {
    return {
      state,
      result: {
        ok: false,
        error: { code: "VALIDATION_ERROR", message: "Bitte einen Namen angeben." },
      },
    };
  }

  const tool = state.tools.find((candidate) => candidate.id === toolId);
  if (tool === undefined) {
    return {
      state,
      result: {
        ok: false,
        error: { code: "TOOL_NOT_FOUND", message: "Werkzeug nicht gefunden." },
      },
    };
  }

  const existingLoan = state.loans.find((candidate) => candidate.toolId === toolId);
  if (existingLoan !== undefined) {
    return {
      state,
      result: {
        ok: false,
        error: { code: "ALREADY_BORROWED", message: "Dieses Werkzeug ist bereits ausgeliehen." },
      },
    };
  }

  const newLoan = { toolId, borrower: trimmedBorrower, borrowedAt: now.toISOString() };
  const nextState: State = { tools: state.tools, loans: [...state.loans, newLoan] };
  return { state: nextState, result: { ok: true, tool: toToolView(tool, nextState.loans) } };
}

export type ReturnResult =
  | { ok: true; tool: ToolView }
  | { ok: false; error: { code: "TOOL_NOT_FOUND" | "NOT_BORROWED"; message: string } };

export function returnTool(state: State, toolId: string): { state: State; result: ReturnResult } {
  const tool = state.tools.find((candidate) => candidate.id === toolId);
  if (tool === undefined) {
    return {
      state,
      result: {
        ok: false,
        error: { code: "TOOL_NOT_FOUND", message: "Werkzeug nicht gefunden." },
      },
    };
  }

  const existingLoan = state.loans.find((candidate) => candidate.toolId === toolId);
  if (existingLoan === undefined) {
    return {
      state,
      result: {
        ok: false,
        error: { code: "NOT_BORROWED", message: "Dieses Werkzeug ist nicht ausgeliehen." },
      },
    };
  }

  const nextState: State = { tools: state.tools, loans: state.loans.filter((loan) => loan.toolId !== toolId) };
  return { state: nextState, result: { ok: true, tool: toToolView(tool, nextState.loans) } };
}
