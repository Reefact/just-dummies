# Release notes — JustDummies, 1.x

What changed for you, release by release, in the `lib` train. For the full technical record — every constraint, every edge case, every ADR — see [CHANGELOG.md](https://github.com/Reefact/just-dummies/blob/main/JustDummies/CHANGELOG.md). Earlier: [0.x](https://github.com/Reefact/just-dummies/blob/main/JustDummies/RELEASE_NOTES-0.x.en.md).

## 1.0.0-preview.4 — August 24, 2026

_A rename that reads better, and a size ceiling analyzer and library now agree on._

### ⚠️ Breaking changes

- `AnyChar` and `AnyString` rename `LowerCase()`/`UpperCase()` to `InLowerCase()`/`InUpperCase()` — the bare names read like a state change, not a quality of the drawn value. No behaviour changes; only the two names do.

### 🐛 Bug Fixes

- JD014 now reports a size ceiling above the producible cap: `WithMaxLength` and `WithMaxCount` were declared uncapped while the library caps them, so a call the analyzer blessed could be refused at run time with nothing between the two to say so.

## 1.0.0-preview.3 — August 21, 2026

_A fixed-prefix, restricted-alphabet format — `ORD-` followed by alphanumerics — is finally one chain instead of a workaround._

### ✨ New

- A character family, a subtraction or a casing now governs only the characters the generator draws — never a prefix, suffix or contained value you wrote. `Any.String().StartingWith("ORD-").AlphaNumeric()` no longer throws at declaration, and yields `ORD-` followed by alphanumerics only ([ADR-0079](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0079-constrain-what-a-dummy-draws-never-the-literals-it-was-given.md)). No chain that worked before stops working, and no generated value changes shape.
- New rule JD033 — names an anchored literal the declared characters cannot draw, at the call site, without refusing the chain. Information only, on by default.
- New rule JD031 — points a chain declaring both inclusive bounds separately (`WithMinLength(8).WithMaxLength(20)`) at the range form the same generator exposes (`WithLengthBetween(8, 20)`). Information only.
- New rule JD032 — warns when a bound is declared twice and the looser call silently loses, whichever order the two were written in.

### 🙌 Improvements

- JD015 now reports, as a warning, a value set every declared constraint empties — until now this surfaced only as two information-level JD029 notes.
- JD024 and JD015 narrow to stay in step with the changes above: JD024 no longer reports a bound JD032 now owns, and JD015 keeps only its length-budget check, so neither refuses at build time what the library now honours at run time.

## 1.0.0-preview.2 — August 18, 2026

_Two breaking defaults, both in service of the same idea: an unconstrained dummy should certify something, not just look harmless._

### ⚠️ Breaking changes

- `Any.String()` and `Any.Char()` now draw from the whole of ASCII, control characters included, and an unconstrained string spans 0 to 1024 characters — narrow it with `NonEmpty()`, `WithMaxLength(n)` or `Printable()` ([ADR-0075](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0075-draw-characters-from-the-whole-of-ascii.md)).
- A declared maximum — `WithMaxLength`, `WithLengthBetween`, `WithMaxCount` — now steers the draw instead of composing with the old narrow spread, and a maximum above 1,000,000 is refused ([ADR-0076](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0076-let-a-declared-maximum-steer-the-size-draw.md)).

### ✨ New

- Five new character families — `Punctuation()`, `Printable()`, `NonPrintable()`, `Whitespaces()` and `Hexadecimal()` — plus `WithoutAlpha()`/`WithoutNumeric()` to subtract rather than pin, on both `Any.String()` and `Any.Char()`.
- **`IPoolInspection<T>` reports what your own constraints left of a pool you supplied** — `GetSurvivors()`, `GetRejections()` — on every generator that takes a value set ([ADR-0067](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0067-report-a-filtered-pool-through-an-explicit-interface.md), [ADR-0068](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0068-carry-the-pool-inspection-wherever-a-caller-supplies-the-values.md)).
- JD029 flags, at build time, a value written into a pool that your own constraints can never draw.
- JD030 flags an `Any.String()` chain that settles no length, as information.

### 🙌 Improvements

- The packaged readme now documents how to draw a `NaN` on purpose, since `Any.Double()`, `Any.Single()` and `Any.Half()` refuse one as an argument too.
- JD015 now validates against the new character families, and every value the library prints is escaped against control characters.

### 🐛 Bug Fixes

- A decimal exclusion declared twice no longer empties a satisfiable grid.
- A conflict on `Any.Enum<T>()` or `Any.Guid()` now names the exclusion that caused it.
- A `DateTimeOffset` pool holding two clocks for the same instant now reaches one verdict regardless of declaration order.
- JD023 and JD024 now read `UInt16`/`UInt32`/`UInt64` constants regardless of the literal's suffix.
- JD015, JD023, JD024 and JD029 now recognise a seeded chain written in one expression.

## 1.0.0-preview.1 — August 7, 2026

_Not a bigger surface than 0.1.0 — the same one, offered to an outside consumer for the first time, with one new promise attached: your seed._

### ✨ New

- **A seed now replays across patch and minor versions.** Pin one in a test, and it keeps drawing the same values through every upgrade within `1.x` ([ADR-0049](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0049-replay-a-seed-across-patch-and-minor-versions.md)).

### 🙌 Improvements

- The package now carries an icon, shared across every package this repository publishes.
- The packaged readme's links point at this repository instead of the one JustDummies was extracted from.
