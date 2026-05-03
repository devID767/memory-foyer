import express from 'express';
import cors from 'cors';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';
import { openDatabase } from './db.js';
import { createDecksRouter } from './decks.js';
import { createSessionsRouter } from './sessions.js';

const here = dirname(fileURLToPath(import.meta.url));
const pkg = JSON.parse(readFileSync(join(here, 'package.json'), 'utf8'));

export function createApp({ db, now = () => new Date() } = {}) {
    if (!db) {
        throw new Error('createApp requires a db instance');
    }
    const app = express();
    app.use(cors());
    app.use(express.json());

    app.get('/health', (_req, res) => {
        res.json({ status: 'ok', version: pkg.version });
    });

    app.use('/decks', createDecksRouter({ db, now }));
    app.use('/sessions', createSessionsRouter({ db, now }));

    app.use((err, _req, res, _next) => {
        console.error('[server] unhandled', err.stack ?? err);
        res.status(500).json({ error: 'internal' });
    });

    return app;
}

const isMain = process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1];
if (isMain) {
    const db = openDatabase();
    const app = createApp({ db });
    const port = Number(process.env.PORT ?? 3000);
    const httpServer = app.listen(port, () => {
        console.log(`Memory Foyer server listening on http://localhost:${port}`);
    });

    const shutdown = (signal) => {
        console.log(`[server] received ${signal}, closing`);
        httpServer.close(() => {
            db.close();
            process.exit(0);
        });
    };
    process.on('SIGTERM', () => shutdown('SIGTERM'));
    process.on('SIGINT', () => shutdown('SIGINT'));
}
