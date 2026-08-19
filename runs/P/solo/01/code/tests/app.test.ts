import test from 'node:test';
import assert from 'node:assert/strict';
import type { AddressInfo } from 'node:net';
import { createApp } from '../src/server/app.ts';
import { AVAILABLE_TOOL_ID, BORROWED_TOOL_ID, createTempStore } from './fixtures.ts';
import type { Tool } from '../src/shared/types.ts';

async function withServer(
  fn: (baseUrl: string) => Promise<void>,
): Promise<void> {
  const { store, cleanup } = await createTempStore();
  const server = createApp(store);
  try {
    await new Promise<void>((resolve) => server.listen(0, resolve));
    const { port } = server.address() as AddressInfo;
    await fn(`http://127.0.0.1:${port}`);
  } finally {
    await new Promise((resolve) => server.close(resolve));
    await cleanup();
  }
}

test('GET /api/tools liefert alle Werkzeuge', async () => {
  await withServer(async (baseUrl) => {
    const response = await fetch(`${baseUrl}/api/tools`);
    assert.equal(response.status, 200);
    const tools = (await response.json()) as Tool[];
    assert.equal(tools.length, 2);
  });
});

test('GET /api/tools?status=available filtert die Liste', async () => {
  await withServer(async (baseUrl) => {
    const response = await fetch(`${baseUrl}/api/tools?status=available`);
    assert.equal(response.status, 200);
    const tools = (await response.json()) as Tool[];
    assert.deepEqual(tools.map((t) => t.id), [AVAILABLE_TOOL_ID]);
  });
});

test('GET /api/tools?status=ungueltig liefert 400', async () => {
  await withServer(async (baseUrl) => {
    const response = await fetch(`${baseUrl}/api/tools?status=ungueltig`);
    assert.equal(response.status, 400);
  });
});

test('Ausleihen und Zurückgeben über die API', async () => {
  await withServer(async (baseUrl) => {
    const borrowResponse = await fetch(`${baseUrl}/api/tools/${AVAILABLE_TOOL_ID}/borrow`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name: 'Erika Mustermann' }),
    });
    assert.equal(borrowResponse.status, 200);
    const borrowed = (await borrowResponse.json()) as Tool;
    assert.equal(borrowed.status, 'borrowed');
    assert.equal(borrowed.borrowedBy, 'Erika Mustermann');

    const returnResponse = await fetch(`${baseUrl}/api/tools/${AVAILABLE_TOOL_ID}/return`, {
      method: 'POST',
    });
    assert.equal(returnResponse.status, 200);
    const returned = (await returnResponse.json()) as Tool;
    assert.equal(returned.status, 'available');
  });
});

test('Ausleihen eines bereits ausgeliehenen Werkzeugs wird mit 409 abgelehnt', async () => {
  await withServer(async (baseUrl) => {
    const response = await fetch(`${baseUrl}/api/tools/${BORROWED_TOOL_ID}/borrow`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name: 'Jemand anderes' }),
    });
    assert.equal(response.status, 409);
  });
});

test('GET / liefert die Startseite aus', async () => {
  await withServer(async (baseUrl) => {
    const response = await fetch(`${baseUrl}/`);
    assert.equal(response.status, 200);
    assert.match(response.headers.get('content-type') ?? '', /text\/html/);
  });
});
