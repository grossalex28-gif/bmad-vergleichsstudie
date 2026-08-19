import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import {
  ToolAlreadyBorrowedError,
  ToolNotBorrowedError,
  ToolNotFoundError,
  ValidationError,
} from '../src/server/errors.ts';
import { AVAILABLE_TOOL_ID, BORROWED_TOOL_ID, createTempStore } from './fixtures.ts';
import type { Tool } from '../src/shared/types.ts';

test('listTools() liefert alle Werkzeuge ohne Filter', async () => {
  const { store, cleanup } = await createTempStore();
  try {
    const tools = await store.listTools();
    assert.equal(tools.length, 2);
  } finally {
    await cleanup();
  }
});

test('listTools("available") liefert nur verfügbare Werkzeuge', async () => {
  const { store, cleanup } = await createTempStore();
  try {
    const tools = await store.listTools('available');
    assert.equal(tools.length, 1);
    assert.equal(tools[0]!.id, AVAILABLE_TOOL_ID);
  } finally {
    await cleanup();
  }
});

test('listTools("borrowed") liefert nur ausgeliehene Werkzeuge', async () => {
  const { store, cleanup } = await createTempStore();
  try {
    const tools = await store.listTools('borrowed');
    assert.equal(tools.length, 1);
    assert.equal(tools[0]!.id, BORROWED_TOOL_ID);
  } finally {
    await cleanup();
  }
});

test('borrowTool markiert ein verfügbares Werkzeug als ausgeliehen', async () => {
  const { store, filePath, cleanup } = await createTempStore();
  try {
    const tool = await store.borrowTool(AVAILABLE_TOOL_ID, 'Max Mustermann');
    assert.equal(tool.status, 'borrowed');
    assert.equal(tool.borrowedBy, 'Max Mustermann');
    assert.ok(tool.borrowedAt);

    const persisted = JSON.parse(await readFile(filePath, 'utf8')) as Tool[];
    const persistedTool = persisted.find((t) => t.id === AVAILABLE_TOOL_ID)!;
    assert.equal(persistedTool.status, 'borrowed');
    assert.equal(persistedTool.borrowedBy, 'Max Mustermann');
  } finally {
    await cleanup();
  }
});

test('borrowTool lehnt ein bereits ausgeliehenes Werkzeug ab', async () => {
  const { store, cleanup } = await createTempStore();
  try {
    await assert.rejects(
      () => store.borrowTool(BORROWED_TOOL_ID, 'Jemand anderes'),
      ToolAlreadyBorrowedError,
    );
  } finally {
    await cleanup();
  }
});

test('borrowTool lehnt einen leeren Namen ab', async () => {
  const { store, cleanup } = await createTempStore();
  try {
    await assert.rejects(() => store.borrowTool(AVAILABLE_TOOL_ID, '   '), ValidationError);
  } finally {
    await cleanup();
  }
});

test('borrowTool wirft bei unbekannter id', async () => {
  const { store, cleanup } = await createTempStore();
  try {
    await assert.rejects(() => store.borrowTool('unbekannt', 'Name'), ToolNotFoundError);
  } finally {
    await cleanup();
  }
});

test('returnTool markiert ein ausgeliehenes Werkzeug wieder als verfügbar', async () => {
  const { store, cleanup } = await createTempStore();
  try {
    const tool = await store.returnTool(BORROWED_TOOL_ID);
    assert.equal(tool.status, 'available');
    assert.equal(tool.borrowedBy, null);
    assert.equal(tool.borrowedAt, null);
  } finally {
    await cleanup();
  }
});

test('returnTool lehnt ein bereits verfügbares Werkzeug ab', async () => {
  const { store, cleanup } = await createTempStore();
  try {
    await assert.rejects(() => store.returnTool(AVAILABLE_TOOL_ID), ToolNotBorrowedError);
  } finally {
    await cleanup();
  }
});

test('returnTool wirft bei unbekannter id', async () => {
  const { store, cleanup } = await createTempStore();
  try {
    await assert.rejects(() => store.returnTool('unbekannt'), ToolNotFoundError);
  } finally {
    await cleanup();
  }
});
