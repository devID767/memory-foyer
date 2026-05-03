import Database from 'better-sqlite3';
import { readFileSync, existsSync, unlinkSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';
import { loadDeckRegistry, applySeed, findOrphans } from './registry.js';

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
    migrate(db);

    const decks = loadDeckRegistry();
    applySeed(db, decks);

    const orphans = findOrphans(db, decks);
    if (orphans.length > 0) {
        console.warn(
            `[db] ${orphans.length} orphan card_ids in DB not present in decks.json: ${orphans.join(', ')}`
        );
    }

    return db;
}

export function migrate(db) {
    const version = db.pragma('user_version', { simple: true });
    if (version < 1) {
        // Pre-relearning rows used a 'reps > 0' heuristic to encode collapsed Relearning.
        // Retag them so the four-stage model is consistent.
        db.exec(`UPDATE card_schedules SET stage='relearning' WHERE stage='learning' AND reps > 0;`);
        db.pragma('user_version = 1');
    }
    if (version < 2) {
        // Card content moved to DeckAsset SOs on the client. Server stores ids and
        // ordering only. Drop columns if they exist (legacy DBs); fresh DBs already
        // have the new schema and skip this branch.
        const cols = db.prepare('PRAGMA table_info(cards)').all();
        if (cols.some((c) => c.name === 'front')) {
            db.exec(`ALTER TABLE cards DROP COLUMN front;`);
        }
        if (cols.some((c) => c.name === 'back')) {
            db.exec(`ALTER TABLE cards DROP COLUMN back;`);
        }
        db.pragma('user_version = 2');
    }
}
