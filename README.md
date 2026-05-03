# Memory Foyer

A spaced-repetition trainer with a calm 3D foyer for deck selection. Built as a Unity 6 portfolio project to demonstrate strict layered architecture (Domain / Application / Infrastructure / Presentation / Composition) with VContainer.

> **Status:** work in progress — see [docs/Roadmap.md](docs/Roadmap.md) for the current phase.

## Highlights

_Filled in during Phase 8 (README, GIFs, polish)._

## Demo

_GIF and screenshots land in Phase 8._

## Tech stack

- Unity 6000.4.1f1 (URP, Linear color space)
- VContainer — DI
- UniTask — async
- MessagePipe — in-process pub/sub (integrated with VContainer)
- DOTween — tweening
- Cinemachine — camera
- Unity Test Framework (NUnit) — EditMode tests
- Node.js + Express + SQLite — local authoritative backend

## Run

### Unity client

1. Install Unity `6000.4.1f1` via Unity Hub.
2. Open this folder as a project; URP will import on first launch.
3. Open `Assets/Scenes/Foyer.unity` and press Play.

### Backend

```bash
cd server
npm install
npm start
# http://localhost:3000/health
```

See [server/README.md](server/README.md).

## Architecture

Layered, with `Domain` compiled without `UnityEngine` (`noEngineReferences: true` in the asmdef). Time flows through `IClock` so the SM-2 algorithm is unit-testable in pure C#.

Full reference: [docs/architecture.md](docs/architecture.md).

## Documentation

- [docs/GDD.md](docs/GDD.md) — game design, mechanics, SM-2 rules
- [docs/architecture.md](docs/architecture.md) — layers, dependency rules, asmdefs
- [docs/Roadmap.md](docs/Roadmap.md) — project phases and next steps

## Trade-offs and what's next

_Filled in during Phase 8._

## License

[MIT](LICENSE).
