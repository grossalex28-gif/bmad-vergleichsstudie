import { test } from "node:test";
import assert from "node:assert/strict";
import path from "node:path";
import type { AddressInfo } from "node:net";
import { createApp } from "../src/http/app.ts";
import { createRepository, type Repository } from "../src/data/repository.ts";
import { withTempDir } from "./test-helpers.ts";

async function withRunningApp(
  dir: string,
  run: (baseUrl: string, repository: Repository) => Promise<void>,
): Promise<void> {
  const dataFilePath = path.join(dir, "data.json");
  const repository = await createRepository(dataFilePath);
  const app = createApp(path.join(dir, "client-dist"), repository);
  const server = app.listen(0);
  try {
    await new Promise<void>((resolve) => server.once("listening", resolve));
    const { port } = server.address() as AddressInfo;
    await run(`http://127.0.0.1:${port}`, repository);
  } finally {
    await new Promise<void>((resolve) => server.close(() => resolve()));
  }
}

test("GET /api/tools returns an empty list for an empty inventory", async () => {
  await withTempDir(async (dir) => {
    await withRunningApp(dir, async (baseUrl) => {
      const res = await fetch(`${baseUrl}/api/tools`);
      assert.equal(res.status, 200);
      const body = await res.json();
      assert.deepEqual(body, { tools: [] });
    });
  });
});

async function withMixedInventory(
  dir: string,
  run: (baseUrl: string) => Promise<void>,
): Promise<void> {
  await withRunningApp(dir, async (baseUrl, repository) => {
    await repository.mutate((state) => ({
      state: {
        tools: [
          { id: "1", name: "Hammer", condition: "gut" },
          { id: "2", name: "Säge", condition: "neu" },
        ],
        loans: [{ toolId: "2", borrower: "Alex", borrowedAt: "2026-08-08T10:00:00.000Z" }],
      },
      result: undefined,
    }));
    await run(baseUrl);
  });
}

test("GET /api/tools returns available and borrowed tools with derived status and loan info", async () => {
  await withTempDir(async (dir) => {
    await withMixedInventory(dir, async (baseUrl) => {
      const res = await fetch(`${baseUrl}/api/tools`);
      assert.equal(res.status, 200);
      const body = await res.json();
      assert.deepEqual(body, {
        tools: [
          { id: "1", name: "Hammer", condition: "gut", status: "available", loan: null },
          {
            id: "2",
            name: "Säge",
            condition: "neu",
            status: "borrowed",
            loan: { borrower: "Alex", borrowedAt: "2026-08-08T10:00:00.000Z" },
          },
        ],
      });
      assert.deepEqual(Object.keys(body), ["tools"]);
    });
  });
});

test("GET /api/tools?status=available returns only the available tool", async () => {
  await withTempDir(async (dir) => {
    await withMixedInventory(dir, async (baseUrl) => {
      const res = await fetch(`${baseUrl}/api/tools?status=available`);
      assert.equal(res.status, 200);
      const body = await res.json();
      assert.deepEqual(body, {
        tools: [{ id: "1", name: "Hammer", condition: "gut", status: "available", loan: null }],
      });
    });
  });
});

test("GET /api/tools?status=borrowed returns only the borrowed tool including loan info", async () => {
  await withTempDir(async (dir) => {
    await withMixedInventory(dir, async (baseUrl) => {
      const res = await fetch(`${baseUrl}/api/tools?status=borrowed`);
      assert.equal(res.status, 200);
      const body = await res.json();
      assert.deepEqual(body, {
        tools: [
          {
            id: "2",
            name: "Säge",
            condition: "neu",
            status: "borrowed",
            loan: { borrower: "Alex", borrowedAt: "2026-08-08T10:00:00.000Z" },
          },
        ],
      });
    });
  });
});

test("GET /api/tools?status=sonstwas returns the full unfiltered list", async () => {
  await withTempDir(async (dir) => {
    await withMixedInventory(dir, async (baseUrl) => {
      const res = await fetch(`${baseUrl}/api/tools?status=sonstwas`);
      assert.equal(res.status, 200);
      const body = await res.json();
      assert.equal(body.tools.length, 2);
    });
  });
});

test("GET /api/tools without a status query parameter returns the full unfiltered list", async () => {
  await withTempDir(async (dir) => {
    await withMixedInventory(dir, async (baseUrl) => {
      const res = await fetch(`${baseUrl}/api/tools`);
      assert.equal(res.status, 200);
      const body = await res.json();
      assert.equal(body.tools.length, 2);
    });
  });
});

test("POST /api/tools/:id/loan borrows an available tool and returns 201 with the updated ToolView", async () => {
  await withTempDir(async (dir) => {
    await withMixedInventory(dir, async (baseUrl) => {
      const res = await fetch(`${baseUrl}/api/tools/1/loan`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ borrower: "Ben" }),
      });
      assert.equal(res.status, 201);
      const body = await res.json();
      assert.equal(body.tool.status, "borrowed");
      assert.equal(body.tool.loan.borrower, "Ben");
      assert.equal(typeof body.tool.loan.borrowedAt, "string");
    });
  });
});

test("POST /api/tools/:id/loan returns 400 for an empty borrower", async () => {
  await withTempDir(async (dir) => {
    await withMixedInventory(dir, async (baseUrl) => {
      const res = await fetch(`${baseUrl}/api/tools/1/loan`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ borrower: "" }),
      });
      assert.equal(res.status, 400);
      const body = await res.json();
      assert.deepEqual(Object.keys(body), ["error"]);
      assert.equal(body.error.code, "VALIDATION_ERROR");
    });
  });
});

test("POST /api/tools/:id/loan returns 400 for a missing borrower", async () => {
  await withTempDir(async (dir) => {
    await withMixedInventory(dir, async (baseUrl) => {
      const res = await fetch(`${baseUrl}/api/tools/1/loan`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({}),
      });
      assert.equal(res.status, 400);
      const body = await res.json();
      assert.deepEqual(Object.keys(body), ["error"]);
      assert.equal(body.error.code, "VALIDATION_ERROR");
    });
  });
});

test("POST /api/tools/:id/loan returns 404 for an unknown tool id", async () => {
  await withTempDir(async (dir) => {
    await withMixedInventory(dir, async (baseUrl) => {
      const res = await fetch(`${baseUrl}/api/tools/unbekannt/loan`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ borrower: "Ben" }),
      });
      assert.equal(res.status, 404);
      const body = await res.json();
      assert.equal(body.error.code, "TOOL_NOT_FOUND");
    });
  });
});

test("POST /api/tools/:id/loan verarbeitet zwei nahezu gleichzeitige Anfragen für dasselbe Werkzeug serialisiert (genau ein 201, ein 409)", async () => {
  // Deferred aus der Story-2.1-Code-Review (deferred-work.md): Nebenläufigkeitstest für die
  // ALREADY_BORROWED/AD-2-Invariante ("höchstens ein Eintrag pro Werkzeug-Id") unter echter Concurrency,
  // nicht nur sequenziell wie im 409-Test oben.
  await withTempDir(async (dir) => {
    await withMixedInventory(dir, async (baseUrl) => {
      const postLoan = (borrower: string) =>
        fetch(`${baseUrl}/api/tools/1/loan`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ borrower }),
        });

      const [resA, resB] = await Promise.all([postLoan("Ben"), postLoan("Alex")]);

      const statuses = [resA.status, resB.status].sort();
      assert.deepEqual(statuses, [201, 409]);

      const [bodyA, bodyB] = await Promise.all([resA.json(), resB.json()]);
      const winnerBody = resA.status === 201 ? bodyA : bodyB;
      const loserBody = resA.status === 201 ? bodyB : bodyA;
      assert.equal(loserBody.error.code, "ALREADY_BORROWED");

      const listRes = await fetch(`${baseUrl}/api/tools`);
      assert.equal(listRes.status, 200);
      const listBody = await listRes.json();
      const tool1 = listBody.tools.find((t: { id: string }) => t.id === "1");
      assert.ok(tool1, "tool 1 missing from GET /api/tools response");
      assert.equal(tool1.loan.borrower, winnerBody.tool.loan.borrower);

      // Note: the view returned by GET /api/tools always has exactly one row per tool id
      // (toToolView() picks the first matching loan), so this cannot by itself detect a
      // duplicate loan record in server-side state. The AD-2 "at most one loan per tool"
      // invariant under concurrency is proven by the [201, 409] split above: since
      // repository.mutate() serializes borrowTool()'s check-then-write, both requests
      // cannot see the tool as unborrowed at once, so a second loan record can never be
      // created. This check only confirms the winner's borrower is reflected consistently.
      const loansForTool1 = listBody.tools.filter((t: { id: string; loan: unknown }) => t.id === "1" && t.loan !== null);
      assert.equal(loansForTool1.length, 1);
    });
  });
});

test("POST /api/tools/:id/loan returns 409 for an already borrowed tool and leaves the existing loan unchanged", async () => {
  await withTempDir(async (dir) => {
    await withMixedInventory(dir, async (baseUrl) => {
      const res = await fetch(`${baseUrl}/api/tools/2/loan`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ borrower: "Ben" }),
      });
      assert.equal(res.status, 409);
      const body = await res.json();
      assert.equal(body.error.code, "ALREADY_BORROWED");

      const listRes = await fetch(`${baseUrl}/api/tools`);
      const listBody = await listRes.json();
      const tool2 = listBody.tools.find((t: { id: string }) => t.id === "2");
      assert.deepEqual(tool2.loan, { borrower: "Alex", borrowedAt: "2026-08-08T10:00:00.000Z" });
    });
  });
});

test("POST /api/tools/:id/return returns a borrowed tool and deletes the loan from server state", async () => {
  await withTempDir(async (dir) => {
    await withMixedInventory(dir, async (baseUrl) => {
      const res = await fetch(`${baseUrl}/api/tools/2/return`, { method: "POST" });
      assert.equal(res.status, 200);
      const body = await res.json();
      assert.equal(body.tool.status, "available");
      assert.equal(body.tool.loan, null);

      const listRes = await fetch(`${baseUrl}/api/tools`);
      const listBody = await listRes.json();
      const tool2 = listBody.tools.find((t: { id: string }) => t.id === "2");
      assert.equal(tool2.status, "available");
      assert.equal(tool2.loan, null);
    });
  });
});

test("POST /api/tools/:id/return returns 409 for a tool without an open loan", async () => {
  await withTempDir(async (dir) => {
    await withMixedInventory(dir, async (baseUrl) => {
      const res = await fetch(`${baseUrl}/api/tools/1/return`, { method: "POST" });
      assert.equal(res.status, 409);
      const body = await res.json();
      assert.equal(body.error.code, "NOT_BORROWED");
    });
  });
});

test("POST /api/tools/:id/return returns 404 for an unknown tool id", async () => {
  await withTempDir(async (dir) => {
    await withMixedInventory(dir, async (baseUrl) => {
      const res = await fetch(`${baseUrl}/api/tools/unbekannt/return`, { method: "POST" });
      assert.equal(res.status, 404);
      const body = await res.json();
      assert.equal(body.error.code, "TOOL_NOT_FOUND");
    });
  });
});

test("POST /api/tools/:id/return gefolgt von POST /api/tools/:id/loan ist ohne Sperre sofort erfolgreich (AC #4, NFR4)", async () => {
  await withTempDir(async (dir) => {
    await withMixedInventory(dir, async (baseUrl) => {
      const returnRes = await fetch(`${baseUrl}/api/tools/2/return`, { method: "POST" });
      assert.equal(returnRes.status, 200);

      const loanRes = await fetch(`${baseUrl}/api/tools/2/loan`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ borrower: "Ben" }),
      });
      assert.equal(loanRes.status, 201);
      const loanBody = await loanRes.json();
      assert.equal(loanBody.tool.loan.borrower, "Ben");
    });
  });
});
