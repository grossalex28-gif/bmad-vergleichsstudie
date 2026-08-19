import type { Loan, Tool, ToolView } from "./types.ts";

export function toToolView(tool: Tool, loans: Loan[]): ToolView {
  const loan = loans.find((candidate) => candidate.toolId === tool.id);
  if (loan === undefined) {
    return { id: tool.id, name: tool.name, condition: tool.condition, status: "available", loan: null };
  }
  return {
    id: tool.id,
    name: tool.name,
    condition: tool.condition,
    status: "borrowed",
    loan: { borrower: loan.borrower, borrowedAt: loan.borrowedAt },
  };
}

export function filterToolsByStatus(tools: ToolView[], status: string | undefined): ToolView[] {
  if (status === "available" || status === "borrowed") {
    return tools.filter((tool) => tool.status === status);
  }
  return tools;
}
