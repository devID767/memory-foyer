# Mock backend

> **Status: skeleton.** Currently exposes only `GET /health` with an in-memory store.
> Full backend (SQLite, SM-2, five endpoints, validation, idempotency, OpenAPI) lands in Roadmap Phase 3.5 — see [docs/Roadmap.md](../docs/Roadmap.md) and [docs/GDD.md §8](../docs/GDD.md). The target contract is what [docs/GDD.md §8](../docs/GDD.md) describes; this file will be rewritten when Phase 3.5 closes.

## Run

```bash
npm install
npm start
# http://localhost:3000/health
```

`/health` responds with `{ status: 'ok', version }` (version comes from `package.json`).

## Local-only by design

The server is meant to run on the developer machine alongside the Unity client. There is no cloud deployment, no auth, and no multi-device sync. SQLite (once introduced in Phase 3.5) lives in `server/data.sqlite`, ignored by git.
