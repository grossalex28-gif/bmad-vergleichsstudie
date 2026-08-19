export interface Tool {
  id: string;
  name: string;
  condition: string;
}

export interface Loan {
  toolId: string;
  borrower: string;
  borrowedAt: string;
}

export interface State {
  tools: Tool[];
  loans: Loan[];
}

export type ToolStatus = "available" | "borrowed";

export interface ToolView {
  id: string;
  name: string;
  condition: string;
  status: ToolStatus;
  loan: { borrower: string; borrowedAt: string } | null;
}
