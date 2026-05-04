import { Router } from 'express';
import { rowToCardScheduleDto } from './mappers.js';
import { selectDeckScheduleRows } from './scheduleQuery.js';

// 'YYYY-MM-DD' in UTC — sortable lexically, matches SQLite default BINARY collation
// for equality and ordering against the released_on column.
function utcDateString(date) {
    return date.toISOString().slice(0, 10);
}

export function createDecksRouter({ db, now }) {
    const router = Router();

    router.get('/', (_req, res) => {
        const nowDate = now();
        const nowIso = nowDate.toISOString();
        const today = utcDateString(nowDate);

        const decks = db.prepare(
            'SELECT deck_id, display_name, new_cards_per_day FROM decks ORDER BY deck_id'
        ).all();
        const totalStmt = db.prepare('SELECT COUNT(*) AS n FROM cards WHERE deck_id = ?');
        const releasedNewStmt = db.prepare(
            `SELECT COUNT(*) AS n FROM card_schedules cs
             JOIN cards c ON c.card_id = cs.card_id
             WHERE c.deck_id = ? AND cs.stage = 'new' AND cs.released_on IS NOT NULL`
        );
        const unreleasedNewStmt = db.prepare(
            `SELECT COUNT(*) AS n FROM card_schedules cs
             JOIN cards c ON c.card_id = cs.card_id
             WHERE c.deck_id = ? AND cs.stage = 'new' AND cs.released_on IS NULL`
        );
        const releasedTodayStmt = db.prepare(
            `SELECT COUNT(*) AS n FROM card_schedules cs
             JOIN cards c ON c.card_id = cs.card_id
             WHERE c.deck_id = ? AND cs.released_on = ?`
        );
        const dueLearningReviewStmt = db.prepare(
            `SELECT COUNT(*) AS n FROM card_schedules cs
             JOIN cards c ON c.card_id = cs.card_id
             WHERE c.deck_id = ? AND cs.stage IN ('learning','review','relearning') AND cs.due_at <= ?`
        );

        const result = decks.map((d) => {
            const totalCount = totalStmt.get(d.deck_id).n;
            const releasedNew = releasedNewStmt.get(d.deck_id).n;
            const unreleasedNew = unreleasedNewStmt.get(d.deck_id).n;
            const releasedToday = releasedTodayStmt.get(d.deck_id, today).n;
            // Project what the next /schedule fetch would release without mutating state —
            // /decks must remain side-effect-free.
            const projectedFresh = Math.min(
                unreleasedNew,
                Math.max(0, d.new_cards_per_day - releasedToday)
            );
            const newCount = releasedNew + projectedFresh;
            const dueOther = dueLearningReviewStmt.get(d.deck_id, nowIso).n;
            return {
                deckId: d.deck_id,
                displayName: d.display_name,
                dueCount: newCount + dueOther,
                newCount,
                totalCount,
            };
        });

        res.json(result);
    });

    router.get('/:id/schedule', (req, res) => {
        const deckId = req.params.id;
        const deck = db.prepare('SELECT new_cards_per_day FROM decks WHERE deck_id = ?').get(deckId);
        if (!deck) {
            return res.status(404).json({ error: 'unknown-deck', deckId });
        }
        const today = utcDateString(now());

        // Mutating GET: the read-quota + UPDATE are wrapped in a transaction to guarantee
        // the daily cap holds even if a future async refactor allows interleaved handlers.
        // Idempotent on retry — released_on stays set, so a repeat fetch is a no-op.
        const releaseFresh = db.transaction(() => {
            const releasedToday = db.prepare(
                `SELECT COUNT(*) AS n FROM card_schedules cs
                 JOIN cards c ON c.card_id = cs.card_id
                 WHERE c.deck_id = ? AND cs.released_on = ?`
            ).get(deckId, today).n;

            const quota = Math.max(0, deck.new_cards_per_day - releasedToday);
            if (quota === 0) {
                return;
            }
            const freshIds = db.prepare(
                `SELECT cs.card_id FROM card_schedules cs
                 JOIN cards c ON c.card_id = cs.card_id
                 WHERE c.deck_id = ? AND cs.stage = 'new' AND cs.released_on IS NULL
                 ORDER BY c.ord ASC
                 LIMIT ?`
            ).all(deckId, quota).map((r) => r.card_id);
            if (freshIds.length === 0) {
                return;
            }
            const placeholders = freshIds.map(() => '?').join(',');
            db.prepare(
                `UPDATE card_schedules SET released_on = ? WHERE card_id IN (${placeholders})`
            ).run(today, ...freshIds);
        });
        releaseFresh();

        const rows = selectDeckScheduleRows(db, deckId);

        res.json({ deckId, cards: rows.map(rowToCardScheduleDto) });
    });

    return router;
}
