import Database from 'better-sqlite3';
import { readFileSync, existsSync, unlinkSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';
import { seed } from './seed.js';

const here = dirname(fileURLToPath(import.meta.url));
const schemaSql = readFileSync(join(here, 'schema.sql'), 'utf8');

export function openDatabase(filePath) {
    const path = filePath ?? process.env.DB_PATH ?? join(here, 'data.sqlite');
    return openWithRecovery(path);
}

function openWithRecovery(path) {
    try {
        return initialize(new Database(path));
    } catch (err) {
        if (path === ':memory:' || !existsSync(path)) {
            throw err;
        }
        console.warn(`[db] failed to open ${path}: ${err.message}. Resetting file and retrying.`);
        unlinkSync(path);
        return initialize(new Database(path));
    }
}

function initialize(db) {
    db.pragma('journal_mode = WAL');
    db.pragma('foreign_keys = ON');
    db.exec(schemaSql);

    const deckCount = db.prepare('SELECT COUNT(*) AS n FROM decks').get().n;
    if (deckCount === 0) {
        seed(db);
    }
    return db;
}
