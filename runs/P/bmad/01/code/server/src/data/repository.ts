import { mkdir, readFile, rename, rm, writeFile } from "node:fs/promises";
import path from "node:path";
import type { State } from "../domain/types.ts";

export interface Repository {
  mutate<R>(fn: (state: State) => { state: State; result: R }): Promise<R>;
  getState(): State;
}

function emptyState(): State {
  return { tools: [], loans: [] };
}

async function readExistingState(dataFilePath: string): Promise<State | null> {
  let raw: string;
  try {
    raw = await readFile(dataFilePath, "utf8");
  } catch (err) {
    if ((err as NodeJS.ErrnoException).code === "ENOENT") {
      return null;
    }
    throw err;
  }
  if (raw.trim().length === 0) {
    return null;
  }
  return JSON.parse(raw) as State;
}

async function writeStateAtomically(dataFilePath: string, state: State): Promise<void> {
  const dir = path.dirname(dataFilePath);
  await mkdir(dir, { recursive: true });
  const tempFilePath = path.join(dir, `.${path.basename(dataFilePath)}.${process.pid}.${Date.now()}.tmp`);
  await writeFile(tempFilePath, JSON.stringify(state, null, 2), "utf8");
  try {
    await rename(tempFilePath, dataFilePath);
  } catch (err) {
    await rm(tempFilePath, { force: true });
    throw err;
  }
}

export async function createRepository(dataFilePath: string): Promise<Repository> {
  const existingState = await readExistingState(dataFilePath);
  let state: State = existingState ?? emptyState();

  if (existingState === null) {
    await writeStateAtomically(dataFilePath, state);
  }

  // Serializes mutate() calls: each call is chained onto the previous one,
  // so read -> decide -> write per call runs atomically with respect to others (AD-3).
  let queue: Promise<unknown> = Promise.resolve();

  function mutate<R>(fn: (state: State) => { state: State; result: R }): Promise<R> {
    const task = queue.then(async () => {
      const { state: nextState, result } = fn(state);
      await writeStateAtomically(dataFilePath, nextState);
      state = nextState;
      return result;
    });
    queue = task.catch(() => {});
    return task;
  }

  function getState(): State {
    return state;
  }

  return { mutate, getState };
}
