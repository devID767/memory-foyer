CREATE TABLE IF NOT EXISTS decks (
  deck_id           TEXT PRIMARY KEY,
  display_name      TEXT NOT NULL,
  description       TEXT NOT NULL DEFAULT '',
  new_cards_per_day INTEGER NOT NULL DEFAULT 10
);

CREATE TABLE IF NOT EXISTS cards (
  card_id   TEXT PRIMARY KEY,
  deck_id   TEXT NOT NULL REFERENCES decks(deck_id) ON DELETE CASCADE,
  ord       INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_cards_deck ON cards(deck_id);

CREATE TABLE IF NOT EXISTS card_schedules (
  card_id        TEXT PRIMARY KEY REFERENCES cards(card_id) ON DELETE CASCADE,
  reps           INTEGER NOT NULL DEFAULT 0,
  ease_factor    REAL    NOT NULL DEFAULT 2.5,
  interval_days  INTEGER NOT NULL DEFAULT 0,
  due_at         TEXT    NOT NULL,
  stage          TEXT    NOT NULL DEFAULT 'new', -- one of: 'new' | 'learning' | 'review' | 'relearning'
  learning_step  INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS processed_sessions (
  session_id    TEXT PRIMARY KEY,
  deck_id       TEXT NOT NULL,
  payload_hash  TEXT NOT NULL,
  processed_at  TEXT NOT NULL,
  snapshot_json TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS review_log (
  id            INTEGER PRIMARY KEY AUTOINCREMENT,
  session_id    TEXT NOT NULL,
  card_id       TEXT NOT NULL,
  grade         INTEGER NOT NULL,
  reviewed_at   TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_review_log_session ON review_log(session_id);
