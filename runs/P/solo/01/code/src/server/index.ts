import { fileURLToPath } from 'node:url';
import { join } from 'node:path';
import { createApp } from './app.ts';
import { ToolStore } from './store.ts';

const projectRoot = fileURLToPath(new URL('../../', import.meta.url));
const dataFile = process.env.TOOLS_DATA_FILE ?? join(projectRoot, 'data', 'tools.json');
const port = Number(process.env.PORT ?? 3000);

const store = new ToolStore(dataFile);
const server = createApp(store);

server.listen(port, () => {
  console.log(`Werkzeugausgabe läuft auf http://localhost:${port}`);
});
