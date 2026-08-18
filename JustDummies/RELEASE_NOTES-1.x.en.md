# Release notes — JustDummies, 1.x

What changed for you, release by release, in the `lib` train. For the full technical record — every constraint, every edge case, every ADR — see [CHANGELOG.md](https://github.com/Reefact/just-dummies/blob/main/JustDummies/CHANGELOG.md). Earlier: [0.x](https://github.com/Reefact/just-dummies/blob/main/JustDummies/RELEASE_NOTES-0.x.en.md).

## 1.0.0-preview.1 — August 7, 2026

_Not a bigger surface than 0.1.0 — the same one, offered to an outside consumer for the first time, with one new promise attached: your seed._

### ✨ New

- **A seed now replays across patch and minor versions.** Pin one in a test, and it keeps drawing the same values through every upgrade within `1.x` ([ADR-0049](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0049-replay-a-seed-across-patch-and-minor-versions.md)).

### 🙌 Improvements

- The package now carries an icon, shared across every package this repository publishes.
- The packaged readme's links point at this repository instead of the one JustDummies was extracted from.
