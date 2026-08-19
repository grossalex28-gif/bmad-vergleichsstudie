export type ToolStatus = "available" | "borrowed";

export interface ToolView {
  id: string;
  name: string;
  condition: string;
  status: ToolStatus;
  loan: { borrower: string; borrowedAt: string } | null;
}

export async function fetchTools(status?: ToolStatus): Promise<ToolView[]> {
  const url = status === undefined ? "/api/tools" : `/api/tools?status=${status}`;
  const res = await fetch(url);
  if (!res.ok) {
    throw new Error(`GET /api/tools failed with status ${res.status}`);
  }
  const body = (await res.json()) as { tools: ToolView[] };
  return body.tools;
}

export class ApiError extends Error {
  code: string;
  constructor(code: string, message: string) {
    super(message);
    this.code = code;
  }
}

export async function borrowTool(id: string, borrower: string): Promise<ToolView> {
  const res = await fetch(`/api/tools/${encodeURIComponent(id)}/loan`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ borrower }),
  });
  let body: { tool: ToolView } | { error: { code: string; message: string } };
  try {
    body = (await res.json()) as { tool: ToolView } | { error: { code: string; message: string } };
  } catch {
    throw new ApiError("UNKNOWN_ERROR", "Die Antwort des Servers konnte nicht gelesen werden.");
  }
  if (!res.ok) {
    const { error } = body as { error: { code: string; message: string } };
    throw new ApiError(error.code, error.message);
  }
  return (body as { tool: ToolView }).tool;
}

export async function returnTool(id: string): Promise<ToolView> {
  const res = await fetch(`/api/tools/${encodeURIComponent(id)}/return`, {
    method: "POST",
  });
  let body: { tool: ToolView } | { error: { code: string; message: string } };
  try {
    body = (await res.json()) as { tool: ToolView } | { error: { code: string; message: string } };
  } catch {
    throw new ApiError("UNKNOWN_ERROR", "Die Antwort des Servers konnte nicht gelesen werden.");
  }
  if (!res.ok) {
    const { error } = body as { error: { code: string; message: string } };
    throw new ApiError(error.code, error.message);
  }
  return (body as { tool: ToolView }).tool;
}
