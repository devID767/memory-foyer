import express from 'express';
import cors from 'cors';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const pkg = JSON.parse(readFileSync(join(dirname(fileURLToPath(import.meta.url)), 'package.json'), 'utf8'));

const app = express();
app.use(cors());
app.use(express.json());

// In-memory store. Replaced by SQLite in Phase 3.5.
const store = {
};

app.get('/health', (_req, res) => {
    res.json({ status: 'ok', version: pkg.version });
});

const PORT = process.env.PORT ?? 3000;
app.listen(PORT, () => {
    console.log(`Mock server listening on http://localhost:${PORT}`);
});
