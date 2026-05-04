import { Router } from 'express';
import { createHash } from 'node:crypto';
import { sessionResultSchema, sessionIdParamSchema } from './schemas.js';
import { schedule as sm2Schedule } from './sm2.js';
import { rowToCardScheduleDto, rowToReviewDto, rowToSm2State, sm2StateToRowValues } from './mappers.js';
import { selectDeckScheduleRows } from './scheduleQuery.js';

function canonicalReviewsHash(reviews) {
    const canonical = JSON.stringify(
        reviews.map((r) => ({ cardId: r.cardId, grade: r.grade, reviewedAt: r.reviewedAt }))
    );
    return createHash('sha256').update(canonical).digest('hex');
}

// Mirrors GET /decks/:id/schedule — see scheduleQuery.js for the shared filter.
function buildDeckSchedule(db, deckId) {
    const rows = selectDeckScheduleRows(db, deckId);
    return { deckId, cards: rows.map(rowToCardScheduleDto) };
}

export function createSessionsRouter({ db, now }) {
    const router = Router();

    router.post('/', (req, res, next) => {
        const parsed = sessionResultSchema.safeParse(req.body);
        if (!parsed.success) {
            return res.status(400).json({ error: 'validation', details: parsed.error.issues });
        }
        const { sessionId, deckId, reviews } = parsed.data;
        const payloadHash = canonicalReviewsHash(reviews);
        const processedAt = now().toISOString();

        const insertSession = db.prepare(
            `INSERT OR IGNORE INTO processed_sessions
             (session_id, deck_id, payload_hash, processed_at, snapshot_json)
             VALUES (?, ?, ?, ?, '')`
        );
        const selectSession = db.prepare(
            'SELECT payload_hash, snapshot_json FROM processed_sessions WHERE session_id = ?'
        );
        const selectDeckCardIds = db.prepare('SELECT card_id FROM cards WHERE deck_id = ?');
        const selectSchedule = db.prepare('SELECT * FROM card_schedules WHERE card_id = ?');
        const updateSchedule = db.prepare(
            `UPDATE card_schedules
             SET reps = @reps, ease_factor = @ease_factor, interval_days = @interval_days,
                 due_at = @due_at, stage = @stage, learning_step = @learning_step
             WHERE card_id = @card_id`
        );
        const insertLog = db.prepare(
            'INSERT INTO review_log (session_id, card_id, grade, reviewed_at) VALUES (?, ?, ?, ?)'
        );
        const updateSnapshot = db.prepare(
            'UPDATE processed_sessions SET snapshot_json = ? WHERE session_id = ?'
        );

        try {
            // Wrap in IMMEDIATE transaction; better-sqlite3 sync API + single-connection
            // makes concurrent POSTs serialize, but IMMEDIATE is belt-and-braces.
            const tx = db.transaction(() => {
                const inserted = insertSession.run(sessionId, deckId, payloadHash, processedAt);
                if (inserted.changes === 0) {
                    const existing = selectSession.get(sessionId);
                    if (existing.payload_hash === payloadHash) {
                        return { dedup: true, snapshot: JSON.parse(existing.snapshot_json) };
                    }
                    const err = new Error('payload-mismatch');
                    err.code = 'payload-mismatch';
                    throw err;
                }

                const cardRows = selectDeckCardIds.all(deckId);
                const validIds = new Set(cardRows.map((r) => r.card_id));
                for (const review of reviews) {
                    if (!validIds.has(review.cardId)) {
                        const err = new Error('unknown-card');
                        err.code = 'unknown-card';
                        err.cardId = review.cardId;
                        throw err;
                    }
                }

                for (const review of reviews) {
                    const row = selectSchedule.get(review.cardId);
                    const state = rowToSm2State(row);
                    const next = sm2Schedule(state, review.grade, new Date(review.reviewedAt));
                    updateSchedule.run({ ...sm2StateToRowValues(next), card_id: review.cardId });
                    insertLog.run(sessionId, review.cardId, review.grade, new Date(review.reviewedAt).toISOString());
                }

                const snapshot = buildDeckSchedule(db, deckId);
                updateSnapshot.run(JSON.stringify(snapshot), sessionId);
                return { dedup: false, snapshot };
            });

            const result = tx();
            const body = { ok: true, updatedSchedule: result.snapshot };
            if (result.dedup) body.dedup = true;
            res.json(body);
        } catch (err) {
            if (err.code === 'payload-mismatch') {
                return res.status(409).json({ error: 'session-payload-mismatch', sessionId });
            }
            if (err.code === 'unknown-card') {
                return res.status(400).json({ error: 'unknown-card', cardId: err.cardId });
            }
            return next(err);
        }
    });

    router.get('/:id', (req, res) => {
        const parsed = sessionIdParamSchema.safeParse(req.params);
        if (!parsed.success) {
            return res.status(400).json({ error: 'validation', details: parsed.error.issues });
        }
        const sessionId = parsed.data.id;
        const sessionRow = db.prepare(
            'SELECT deck_id, processed_at FROM processed_sessions WHERE session_id = ?'
        ).get(sessionId);
        if (!sessionRow) {
            return res.status(404).json({ error: 'unknown-session', sessionId });
        }
        const reviews = db.prepare(
            'SELECT card_id, grade, reviewed_at FROM review_log WHERE session_id = ? ORDER BY id ASC'
        ).all(sessionId).map(rowToReviewDto);

        res.json({
            sessionId,
            deckId: sessionRow.deck_id,
            processedAt: new Date(sessionRow.processed_at).toISOString(),
            reviews,
        });
    });

    return router;
}
