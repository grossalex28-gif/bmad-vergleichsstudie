import { createServer, type IncomingMessage, type ServerResponse } from 'node:http';
import { readFile } from 'node:fs/promises';
import { extname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { ToolStore } from './store.ts';
import {
  ToolAlreadyBorrowedError,
  ToolNotBorrowedError,
  ToolNotFoundError,
  ValidationError,
} from './errors.ts';
import type { ToolStatus } from '../shared/types.ts';

const projectRoot = fileURLToPath(new URL('../../', import.meta.url));
const publicDir = join(projectRoot, 'src', 'public');
const distPublicDir = join(projectRoot, 'dist', 'public');

const CONTENT_TYPES: Record<string, string> = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
};

function sendJson(res: ServerResponse, statusCode: number, body: unknown): void {
  const payload = JSON.stringify(body);
  res.writeHead(statusCode, { 'Content-Type': 'application/json; charset=utf-8' });
  res.end(payload);
}

async function readBody(req: IncomingMessage): Promise<string> {
  const chunks: Buffer[] = [];
  for await (const chunk of req) {
    chunks.push(chunk as Buffer);
  }
  return Buffer.concat(chunks).toString('utf8');
}

function isToolStatus(value: unknown): value is ToolStatus {
  return value === 'available' || value === 'borrowed';
}

async function serveStatic(res: ServerResponse, pathname: string): Promise<boolean> {
  const filePath = pathname === '/' ? 'index.html' : pathname.slice(1);
  const ext = extname(filePath);
  const contentType = CONTENT_TYPES[ext];
  if (!contentType) return false;

  const candidates = ext === '.js' ? [distPublicDir, publicDir] : [publicDir];
  for (const dir of candidates) {
    try {
      const data = await readFile(join(dir, filePath));
      res.writeHead(200, { 'Content-Type': contentType });
      res.end(data);
      return true;
    } catch {
      // try next candidate
    }
  }
  return false;
}

export function createApp(store: ToolStore) {
  return createServer((req, res) => {
    void handleRequest(req, res, store);
  });
}

async function handleRequest(
  req: IncomingMessage,
  res: ServerResponse,
  store: ToolStore,
): Promise<void> {
  const method = req.method ?? 'GET';
  const url = new URL(req.url ?? '/', 'http://localhost');
  const pathname = url.pathname;

  try {
    if (method === 'GET' && pathname === '/api/tools') {
      const statusParam = url.searchParams.get('status');
      if (statusParam !== null && !isToolStatus(statusParam)) {
        sendJson(res, 400, { error: 'Ungültiger Filter. Erlaubt: available, borrowed.' });
        return;
      }
      const tools = await store.listTools(statusParam ?? undefined);
      sendJson(res, 200, tools);
      return;
    }

    const borrowMatch = pathname.match(/^\/api\/tools\/([^/]+)\/borrow$/);
    if (method === 'POST' && borrowMatch) {
      const id = decodeURIComponent(borrowMatch[1]!);
      const bodyText = await readBody(req);
      let name = '';
      try {
        const parsed = bodyText ? JSON.parse(bodyText) : {};
        name = typeof parsed.name === 'string' ? parsed.name : '';
      } catch {
        sendJson(res, 400, { error: 'Ungültiger Anfragetext.' });
        return;
      }
      const tool = await store.borrowTool(id, name);
      sendJson(res, 200, tool);
      return;
    }

    const returnMatch = pathname.match(/^\/api\/tools\/([^/]+)\/return$/);
    if (method === 'POST' && returnMatch) {
      const id = decodeURIComponent(returnMatch[1]!);
      const tool = await store.returnTool(id);
      sendJson(res, 200, tool);
      return;
    }

    if (method === 'GET') {
      const served = await serveStatic(res, pathname);
      if (served) return;
    }

    sendJson(res, 404, { error: 'Nicht gefunden.' });
  } catch (error) {
    if (error instanceof ValidationError) {
      sendJson(res, 400, { error: error.message });
    } else if (error instanceof ToolNotFoundError) {
      sendJson(res, 404, { error: error.message });
    } else if (error instanceof ToolAlreadyBorrowedError || error instanceof ToolNotBorrowedError) {
      sendJson(res, 409, { error: error.message });
    } else {
      console.error(error);
      sendJson(res, 500, { error: 'Interner Serverfehler.' });
    }
  }
}
