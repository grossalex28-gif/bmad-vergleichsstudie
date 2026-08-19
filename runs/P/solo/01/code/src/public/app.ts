import type { Tool, ToolStatus } from '../shared/types.ts';

const tableBody = document.getElementById('tools-body') as HTMLTableSectionElement;
const messageEl = document.getElementById('message') as HTMLParagraphElement;
const filterButtons = Array.from(document.querySelectorAll<HTMLButtonElement>('.filters button'));

let currentFilter: ToolStatus | '' = '';

function showMessage(text: string): void {
  messageEl.textContent = text;
}

async function fetchTools(): Promise<Tool[]> {
  const query = currentFilter ? `?status=${currentFilter}` : '';
  const response = await fetch(`/api/tools${query}`);
  if (!response.ok) {
    throw new Error('Werkzeugliste konnte nicht geladen werden.');
  }
  return (await response.json()) as Tool[];
}

async function borrowTool(id: string, name: string): Promise<void> {
  const response = await fetch(`/api/tools/${encodeURIComponent(id)}/borrow`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name }),
  });
  const body = (await response.json()) as Tool | { error: string };
  if (!response.ok) {
    throw new Error('error' in body ? body.error : 'Ausleihe fehlgeschlagen.');
  }
}

async function returnTool(id: string): Promise<void> {
  const response = await fetch(`/api/tools/${encodeURIComponent(id)}/return`, {
    method: 'POST',
  });
  const body = (await response.json()) as Tool | { error: string };
  if (!response.ok) {
    throw new Error('error' in body ? body.error : 'Rückgabe fehlgeschlagen.');
  }
}

function renderActionCell(tool: Tool): HTMLTableCellElement {
  const cell = document.createElement('td');
  cell.className = 'action-cell';

  if (tool.status === 'available') {
    const form = document.createElement('form');
    const input = document.createElement('input');
    input.type = 'text';
    input.placeholder = 'Name';
    input.required = true;
    input.setAttribute('aria-label', 'Name der ausleihenden Person');

    const button = document.createElement('button');
    button.type = 'submit';
    button.textContent = 'Ausleihen';

    form.append(input, button);
    form.addEventListener('submit', (event) => {
      event.preventDefault();
      void handleBorrow(tool.id, input.value);
    });

    cell.appendChild(form);
  } else {
    const button = document.createElement('button');
    button.type = 'button';
    button.textContent = 'Zurückgeben';
    button.addEventListener('click', () => {
      void handleReturn(tool.id);
    });
    cell.appendChild(button);
  }

  return cell;
}

function renderRow(tool: Tool): HTMLTableRowElement {
  const row = document.createElement('tr');

  const name = document.createElement('td');
  name.textContent = tool.name;

  const inventoryNumber = document.createElement('td');
  inventoryNumber.textContent = tool.inventoryNumber;

  const condition = document.createElement('td');
  condition.textContent = tool.condition;

  const status = document.createElement('td');
  status.textContent = tool.status === 'available' ? 'verfügbar' : `ausgeliehen (${tool.borrowedBy})`;
  status.className = tool.status === 'available' ? 'status-available' : 'status-borrowed';

  row.append(name, inventoryNumber, condition, status, renderActionCell(tool));
  return row;
}

async function refresh(): Promise<void> {
  try {
    const tools = await fetchTools();
    tableBody.innerHTML = '';
    for (const tool of tools) {
      tableBody.appendChild(renderRow(tool));
    }
  } catch (error) {
    showMessage(error instanceof Error ? error.message : String(error));
  }
}

async function handleBorrow(id: string, name: string): Promise<void> {
  showMessage('');
  try {
    await borrowTool(id, name);
    await refresh();
  } catch (error) {
    showMessage(error instanceof Error ? error.message : String(error));
  }
}

async function handleReturn(id: string): Promise<void> {
  showMessage('');
  try {
    await returnTool(id);
    await refresh();
  } catch (error) {
    showMessage(error instanceof Error ? error.message : String(error));
  }
}

for (const button of filterButtons) {
  button.addEventListener('click', () => {
    currentFilter = (button.dataset.filter ?? '') as ToolStatus | '';
    for (const btn of filterButtons) {
      btn.classList.toggle('active', btn === button);
    }
    void refresh();
  });
}

void refresh();
