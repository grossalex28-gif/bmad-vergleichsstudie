import express, { type Express } from "express";
import path from "node:path";
import type { Repository } from "../data/repository.ts";
import { filterToolsByStatus, toToolView } from "../domain/tool-view.ts";
import { borrowTool, returnTool, type BorrowResult, type ReturnResult } from "../domain/loan.ts";

export function createApp(clientDistPath: string, repository: Repository): Express {
  const app = express();
  app.use(express.json());

  const apiRouter = express.Router();
  apiRouter.get("/tools", (req, res) => {
    const state = repository.getState();
    const status = typeof req.query.status === "string" ? req.query.status : undefined;
    const tools = filterToolsByStatus(state.tools.map((tool) => toToolView(tool, state.loans)), status);
    res.json({ tools });
  });
  apiRouter.post("/tools/:id/loan", async (req, res) => {
    const borrower = typeof req.body?.borrower === "string" ? req.body.borrower : undefined;
    let result: BorrowResult;
    try {
      result = await repository.mutate((state) => borrowTool(state, req.params.id, borrower, new Date()));
    } catch {
      res.status(500).json({ error: { code: "INTERNAL_ERROR", message: "Ein interner Fehler ist aufgetreten." } });
      return;
    }
    if (result.ok) {
      res.status(201).json({ tool: result.tool });
      return;
    }
    const statusByCode = { VALIDATION_ERROR: 400, TOOL_NOT_FOUND: 404, ALREADY_BORROWED: 409 } as const;
    res.status(statusByCode[result.error.code]).json({ error: result.error });
  });
  apiRouter.post("/tools/:id/return", async (req, res) => {
    let result: ReturnResult;
    try {
      result = await repository.mutate((state) => returnTool(state, req.params.id));
    } catch {
      res.status(500).json({ error: { code: "INTERNAL_ERROR", message: "Ein interner Fehler ist aufgetreten." } });
      return;
    }
    if (result.ok) {
      res.status(200).json({ tool: result.tool });
      return;
    }
    const statusByCode = { TOOL_NOT_FOUND: 404, NOT_BORROWED: 409 } as const;
    res.status(statusByCode[result.error.code]).json({ error: result.error });
  });
  app.use("/api", apiRouter);

  app.use(express.static(clientDistPath));
  app.get("/*splat", (_req, res) => {
    res.sendFile(path.join(clientDistPath, "index.html"));
  });

  return app;
}
