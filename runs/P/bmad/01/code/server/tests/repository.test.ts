import { test } from "node:test";
import assert from "node:assert/strict";
import { readdir, readFile, writeFile } from "node:fs/promises";
import path from "node:path";
import { createRepository } from "../src/data/repository.ts";
import type { State } from "../src/domain/types.ts";
import { withTempDir } from "./test-helpers.ts";

test("mutate() serializes concurrent calls without interleaving", async () => {
  await withTempDir(async (dir) => {
    const dataFilePath = path.join(dir, "data.json");
    const repository = await createRepository(dataFilePath);

    let activeCalls = 0;
    const log: string[] = [];

    function runMutation(label: string): Promise<string> {
      return repository.mutate((state) => {
        activeCalls++;
        assert.strictEqual(activeCalls, 1, "mutate() must not run two fn() concurrently");
        log.push(`${label}-start`);
        const nextTools = [...state.tools, { id: label, name: label, condition: "ok" }];
        log.push(`${label}-end`);
        activeCalls--;
        return { state: { ...state, tools: nextTools }, result: label };
      });
    }

    const results = await Promise.all([runMutation("a"), runMutation("b")]);

    assert.deepEqual(new Set(results), new Set(["a", "b"]));
    // Each pair of log entries for a label must be adjacent - proves no interleaving.
    assert.equal(log.length, 4);
    assert.equal(log[0]!.split("-")[0], log[1]!.split("-")[0]);
    assert.equal(log[2]!.split("-")[0], log[3]!.split("-")[0]);

    const persisted = JSON.parse(await readFile(dataFilePath, "utf8")) as State;
    assert.equal(persisted.tools.length, 2);
    assert.deepEqual(
      new Set(persisted.tools.map((tool) => tool.id)),
      new Set(["a", "b"]),
    );
  });
});

test("mutate() writes atomically via temp file + rename, leaving no temp file behind", async () => {
  await withTempDir(async (dir) => {
    const dataFilePath = path.join(dir, "data.json");
    const repository = await createRepository(dataFilePath);

    await repository.mutate((state) => ({
      state: { ...state, tools: [{ id: "1", name: "Hammer", condition: "gut" }] },
      result: undefined,
    }));

    const persisted = JSON.parse(await readFile(dataFilePath, "utf8")) as State;
    assert.deepEqual(persisted.tools, [{ id: "1", name: "Hammer", condition: "gut" }]);

    const entries = await readdir(dir);
    assert.deepEqual(entries, ["data.json"]);
  });
});

test("createRepository initializes an empty data file when none exists", async () => {
  await withTempDir(async (dir) => {
    const dataFilePath = path.join(dir, "data.json");

    await createRepository(dataFilePath);

    const persisted = JSON.parse(await readFile(dataFilePath, "utf8")) as State;
    assert.deepEqual(persisted, { tools: [], loans: [] });
  });
});

test("createRepository initializes an empty data file when the existing file is empty", async () => {
  await withTempDir(async (dir) => {
    const dataFilePath = path.join(dir, "data.json");
    await writeFile(dataFilePath, "", "utf8");

    await createRepository(dataFilePath);

    const persisted = JSON.parse(await readFile(dataFilePath, "utf8")) as State;
    assert.deepEqual(persisted, { tools: [], loans: [] });
  });
});

test("createRepository loads an existing populated data file unchanged", async () => {
  await withTempDir(async (dir) => {
    const dataFilePath = path.join(dir, "data.json");
    const initialState: State = {
      tools: [{ id: "1", name: "Bohrmaschine", condition: "gut" }],
      loans: [{ toolId: "1", borrower: "Alex", borrowedAt: "2026-08-08T10:00:00.000Z" }],
    };
    const initialContent = JSON.stringify(initialState, null, 2);
    await writeFile(dataFilePath, initialContent, "utf8");

    await createRepository(dataFilePath);

    const contentAfterInit = await readFile(dataFilePath, "utf8");
    assert.equal(contentAfterInit, initialContent);
  });
});

test("createRepository creates a missing target directory instead of failing with ENOENT", async () => {
  await withTempDir(async (dir) => {
    const dataFilePath = path.join(dir, "nested", "subdir", "data.json");

    await createRepository(dataFilePath);

    const persisted = JSON.parse(await readFile(dataFilePath, "utf8")) as State;
    assert.deepEqual(persisted, { tools: [], loans: [] });
  });
});

test("mutate() rejects and writes nothing when fn() throws", async () => {
  await withTempDir(async (dir) => {
    const dataFilePath = path.join(dir, "data.json");
    const repository = await createRepository(dataFilePath);
    const contentBefore = await readFile(dataFilePath, "utf8");

    await assert.rejects(
      repository.mutate(() => {
        throw new Error("boom");
      }),
      /boom/,
    );

    const contentAfter = await readFile(dataFilePath, "utf8");
    assert.equal(contentAfter, contentBefore);

    const entries = await readdir(dir);
    assert.deepEqual(entries, ["data.json"]);
  });
});

test("mutate() after a rejected fn() still processes subsequent calls", async () => {
  await withTempDir(async (dir) => {
    const dataFilePath = path.join(dir, "data.json");
    const repository = await createRepository(dataFilePath);

    await assert.rejects(
      repository.mutate(() => {
        throw new Error("boom");
      }),
    );

    const result = await repository.mutate((state) => ({
      state: { ...state, tools: [{ id: "1", name: "Säge", condition: "gut" }] },
      result: "ok",
    }));

    assert.equal(result, "ok");
    const persisted = JSON.parse(await readFile(dataFilePath, "utf8")) as State;
    assert.deepEqual(persisted.tools, [{ id: "1", name: "Säge", condition: "gut" }]);
  });
});
