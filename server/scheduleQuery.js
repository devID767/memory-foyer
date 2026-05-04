// Shared schedule filter for the deck snapshot returned by both
// GET /decks/:id/schedule and POST /sessions updatedSchedule.
// Keeping the SQL in one place is what prevents the two endpoints from
// drifting apart again (T-3 fix).
export function selectDeckScheduleRows(db, deckId) {
    return db.prepare(
        `SELECT cs.* FROM card_schedules cs
         JOIN cards c ON c.card_id = cs.card_id
         WHERE c.deck_id = ?
           AND (cs.stage IN ('learning','review','relearning')
                OR (cs.stage = 'new' AND cs.released_on IS NOT NULL))
         ORDER BY c.ord ASC`
    ).all(deckId);
}
