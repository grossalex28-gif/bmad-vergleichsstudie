import path from "node:path";
import { createApp } from "./src/http/app.ts";
import { createRepository } from "./src/data/repository.ts";

const DEFAULT_PORT = 3000;
const DEFAULT_DATA_FILE = "./data/werkzeugausgabe.json";

const parsedPort = Number(process.env.PORT);
const port = process.env.PORT && Number.isInteger(parsedPort) && parsedPort > 0 ? parsedPort : DEFAULT_PORT;

const dataFilePath = path.resolve(
  import.meta.dirname,
  process.env.DATA_FILE || DEFAULT_DATA_FILE,
);
const clientDistPath = path.resolve(import.meta.dirname, "../client/dist");

const repository = await createRepository(dataFilePath);
const app = createApp(clientDistPath, repository);

app.listen(port, () => {
  console.log(`Server listening on port ${port}`);
});
