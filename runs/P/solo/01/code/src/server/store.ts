import { readFile, writeFile } from 'node:fs/promises';
import type { Tool, ToolStatus } from '../shared/types.ts';
import {
  ToolAlreadyBorrowedError,
  ToolNotBorrowedError,
  ToolNotFoundError,
  ValidationError,
} from './errors.ts';

export class ToolStore {
  #filePath: string;

  constructor(filePath: string) {
    this.#filePath = filePath;
  }

  async #load(): Promise<Tool[]> {
    const raw = await readFile(this.#filePath, 'utf8');
    return JSON.parse(raw) as Tool[];
  }

  async #save(tools: Tool[]): Promise<void> {
    await writeFile(this.#filePath, JSON.stringify(tools, null, 2) + '\n', 'utf8');
  }

  async listTools(filter?: ToolStatus): Promise<Tool[]> {
    const tools = await this.#load();
    if (!filter) return tools;
    return tools.filter((tool) => tool.status === filter);
  }

  async borrowTool(id: string, borrowerName: string): Promise<Tool> {
    const name = borrowerName.trim();
    if (!name) {
      throw new ValidationError('Der Name der ausleihenden Person darf nicht leer sein.');
    }

    const tools = await this.#load();
    const tool = tools.find((t) => t.id === id);
    if (!tool) throw new ToolNotFoundError(id);
    if (tool.status === 'borrowed') throw new ToolAlreadyBorrowedError(id);

    tool.status = 'borrowed';
    tool.borrowedBy = name;
    tool.borrowedAt = new Date().toISOString();

    await this.#save(tools);
    return tool;
  }

  async returnTool(id: string): Promise<Tool> {
    const tools = await this.#load();
    const tool = tools.find((t) => t.id === id);
    if (!tool) throw new ToolNotFoundError(id);
    if (tool.status !== 'borrowed') throw new ToolNotBorrowedError(id);

    tool.status = 'available';
    tool.borrowedBy = null;
    tool.borrowedAt = null;

    await this.#save(tools);
    return tool;
  }
}
