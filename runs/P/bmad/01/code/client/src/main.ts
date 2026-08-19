import './style.css'
import { ApiError, borrowTool, fetchTools, returnTool, type ToolStatus, type ToolView } from './api.ts'

const statusLabels: Record<ToolView['status'], string> = {
  available: 'Verfügbar',
  borrowed: 'Ausgeliehen',
}

const filterOptions: { status: ToolStatus | undefined; label: string }[] = [
  { status: undefined, label: 'Alle' },
  { status: 'available', label: 'Verfügbar' },
  { status: 'borrowed', label: 'Ausgeliehen' },
]

let currentFilter: ToolStatus | undefined = undefined
let latestRequestId = 0

function escapeHtml(value: string): string {
  return value
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;')
}

function renderAction(tool: ToolView): string {
  if (tool.status === 'available') {
    return `
      <form class="loan-form" data-tool-id="${escapeHtml(tool.id)}">
        <input type="text" name="borrower" placeholder="Name" required />
        <button type="submit">Ausleihen</button>
        <p class="loan-error"></p>
      </form>`
  }
  return `
      <form class="return-form" data-tool-id="${escapeHtml(tool.id)}">
        <button type="submit">Zurückgeben</button>
        <p class="return-error"></p>
      </form>`
}

function renderTools(tools: ToolView[]): string {
  const rows = tools
    .map(
      (tool) => `
        <tr>
          <td>${escapeHtml(tool.name)}</td>
          <td>${escapeHtml(tool.id)}</td>
          <td>${escapeHtml(tool.condition)}</td>
          <td class="status status--${tool.status}">${statusLabels[tool.status]}</td>
          <td>${renderAction(tool)}</td>
        </tr>`,
    )
    .join('')

  return `
    <section id="tools">
      <h1>Werkzeugliste</h1>
      <table>
        <thead>
          <tr>
            <th>Bezeichnung</th>
            <th>Inventarnummer</th>
            <th>Zustand</th>
            <th>Status</th>
            <th>Aktion</th>
          </tr>
        </thead>
        <tbody>${rows}</tbody>
      </table>
    </section>
  `
}

function renderFilterBar(): string {
  const buttons = filterOptions
    .map(({ status, label }) => {
      const classAttr = status === currentFilter ? ' class="active"' : ''
      return `<button type="button" data-status="${status ?? ''}"${classAttr}>${label}</button>`
    })
    .join('')
  return `<div class="filter-bar">${buttons}</div>`
}

function attachFilterHandlers(app: HTMLDivElement) {
  app.querySelectorAll<HTMLButtonElement>('.filter-bar button').forEach((button) => {
    button.addEventListener('click', () => {
      const value = button.dataset.status
      currentFilter = value === undefined || value === '' ? undefined : (value as ToolStatus)
      loadAndRender(app)
    })
  })
}

function attachLoanFormHandlers(app: HTMLDivElement) {
  app.querySelectorAll<HTMLFormElement>('.loan-form').forEach((form) => {
    form.addEventListener('submit', (event) => {
      event.preventDefault()
      const toolId = form.dataset.toolId!
      const input = form.elements.namedItem('borrower') as HTMLInputElement
      const button = form.querySelector<HTMLButtonElement>('button[type="submit"]')!
      const errorEl = form.querySelector<HTMLParagraphElement>('.loan-error')!
      input.disabled = true
      button.disabled = true
      borrowTool(toolId, input.value)
        .then(() => loadAndRender(app))
        .catch((err) => {
          errorEl.textContent = err instanceof ApiError ? err.message : String(err)
          input.disabled = false
          button.disabled = false
        })
    })
  })
}

function attachReturnFormHandlers(app: HTMLDivElement) {
  app.querySelectorAll<HTMLFormElement>('.return-form').forEach((form) => {
    form.addEventListener('submit', (event) => {
      event.preventDefault()
      const toolId = form.dataset.toolId!
      const button = form.querySelector<HTMLButtonElement>('button[type="submit"]')!
      const errorEl = form.querySelector<HTMLParagraphElement>('.return-error')!
      button.disabled = true
      returnTool(toolId)
        .then(() => loadAndRender(app))
        .catch((err) => {
          errorEl.textContent = err instanceof ApiError ? err.message : String(err)
          button.disabled = false
        })
    })
  })
}

async function loadAndRender(app: HTMLDivElement) {
  const requestId = ++latestRequestId
  try {
    const tools = await fetchTools(currentFilter)
    if (requestId !== latestRequestId) return
    app.innerHTML = renderFilterBar() + renderTools(tools)
  } catch (err) {
    if (requestId !== latestRequestId) return
    console.error(err)
    app.innerHTML = renderFilterBar() + '<p>Werkzeugliste konnte nicht geladen werden.</p>'
  }
  attachFilterHandlers(app)
  attachLoanFormHandlers(app)
  attachReturnFormHandlers(app)
}

async function init() {
  const app = document.querySelector<HTMLDivElement>('#app')!
  await loadAndRender(app)
}

init()
