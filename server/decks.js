import { Router } from 'express';
import { rowToCardScheduleDto } from './mappers.js';

export function createDecksRouter({ db, now }) {
    const router = Router();

    router.get('/', (_req, res) => {
        const nowIso = now().toISOString();
        const decks = db.prepare('SELECT deck_id, display_name, new_cards_per_day FROM decks ORDER BY deck_id').all();
        const totalStmt = db.prepare('SELECT COUNT(*) AS n FROM cards WHERE deck_id = ?');
        const newCountStmt = db.prepare(
            `SELECT COUNT(*) AS n FROM card_schedules cs
             JOIN cards c ON c.card_id = cs.card_id
             WHERE c.deck_id = ? AND cs.stage = 'new'`
        );
        const dueLearningReviewStmt = db.prepare(
            `SELECT COUNT(*) AS n FROM card_schedules cs
             JOIN cards c ON c.card_id = cs.card_id
             WHERE c.deck_id = ? AND cs.stage IN ('learning','review','relearning') AND cs.due_at <= ?`
        );

        const result = decks.map((d) => {
            const totalCount = totalStmt.get(d.deck_id).n;
            const newPool = newCountStmt.get(d.deck_id).n;
            const newCount = Math.min(d.new_cards_per_day, newPool);
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

        const learningReview = db.prepare(
            `SELECT cs.* FROM card_schedules cs
             JOIN cards c ON c.card_id = cs.card_id
             WHERE c.deck_id = ? AND cs.stage IN ('learning','review','relearning')
             ORDER BY c.ord ASC`
        ).all(deckId);

        const newCards = db.prepare(
            `SELECT cs.* FROM card_schedules cs
             JOIN cards c ON c.card_id = cs.card_id
             WHERE c.deck_id = ? AND cs.stage = 'new'
             ORDER BY c.ord ASC
             LIMIT ?`
        ).all(deckId, deck.new_cards_per_day);

        const cards = [...learningReview, ...newCards].map(rowToCardScheduleDto);
        res.json({ deckId, cards });
    });

    return router;
}
