import { mkdtemp, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { ToolStore } from '../src/server/store.ts';
import type { Tool } from '../src/shared/types.ts';

export const AVAILABLE_TOOL_ID = 'a';
export const BORROWED_TOOL_ID = 'b';

const seedTools: Tool[] = [
  {
    id: AVAILABLE_TOOL_ID,
    name: 'Hammer',
    inventoryNumber: 'INV-1',
    condition: 'gut',
    status: 'available',
    borrowedBy: null,
    borrowedAt: null,
  },
  {
    id: BORROWED_TOOL_ID,
    name: 'Säge',
    inventoryNumber: 'INV-2',
    condition: 'gut',
    status: 'borrowed',
    borrowedBy: 'Test Person',
    borrowedAt: '2026-01-01T00:00:00.000Z',
  },
];

export interface TempStoreHandle {
  store: ToolStore;
  filePath: string;
  cleanup: () => Promise<void>;
}

export async function createTempStore(): Promise<TempStoreHandle> {
  const dir = await mkdtemp(join(tmpdir(), 'werkzeugausgabe-'));
  const filePath = join(dir, 'tools.json');
  await writeFile(filePath, JSON.stringify(seedTools, null, 2), 'utf8');

  return {
    store: new ToolStore(filePath),
    filePath,
    cleanup: () => rm(dir, { recursive: true, force: true }),
  };
}
