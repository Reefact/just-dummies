# Release notes — dum (JustDummies.Cli), 1.x

What changed for you, release by release, in the `cli` train. For the full technical record — every constraint, every edge case, every ADR — see [CHANGELOG.md](https://github.com/Reefact/just-dummies/blob/main/JustDummies.Cli/CHANGELOG.md).

## 1.1.0-beta.3 — August 24, 2026

_Guard reading gets wider and stricter at once — two named guard libraries and a factory-built type are read now, while three shapes where the tool was confidently wrong about a guard's reach are refused instead of guessed._

### ⚠️ Breaking changes

- **A guard the tool cannot place above every write to its parameter, or cannot prove runs on every construction, is now marked `unread guards`** — so a scaffold that used to compile can block your build until you confirm the generator. It used to emit a constraint the real constructor does not hold, which is the worse of the two failures: the file compiled, the recap reported nothing worth looking at, and the draw threw inside the constructor much later.

### ✨ New

- **The guard helpers of Ardalis.GuardClauses and CommunityToolkit.Diagnostics are read, in both their spellings** — `Name = Guard.Against.NullOrWhiteSpace(name);` no longer ends the scan before anything is read, so a constructor guarded in that style stops scaffolding neutral generators under a recap showing no doubt anywhere ([ADR-0086](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0086-read-the-guard-helpers-of-named-libraries.md)). A recognised library's method outside the measured rows earns `unread guards` rather than silence.
- **A type with no accessible constructor now scaffolds through its own factory** — the canonical validating value object, a private constructor behind a public `Create`, has `Generate()` call the factory and its guards read like a constructor's, with the recap's signature line naming the call the emitted file makes (`factory Email.Create(string)`). An abstract type with a factory scaffolds too.

### 🐛 Bug Fixes

- **A guard is no longer read as a bound on a value the constructor had already replaced, or never reached** — a write to the parameter itself (`percent = 100 - percent`), a `: this(…)` or `: base(…)` initializer that runs entire before the body, a loop, `switch`, `using` or `finally` whose order the tool does not read, and a `return` or `goto` above the guard. Each is now placed correctly or refused.
- **A sign guard on an unsigned parameter no longer loses its constraint** — `if (size <= 0)` on a `byte` or a `uint` read as `.Positive()`, a member the unsigned generators do not carry, so it was dropped silently and `Any.Byte()` still drew `0`; it now reads as `.NonZero()`, which is the same constraint rather than a looser one.
- **The arithmetic `ArgumentOutOfRangeException` throw helpers are read as guards instead of blocking the build** — `ThrowIfNegative`, `ThrowIfNegativeOrZero`, `ThrowIfZero`, `ThrowIfLessThan`, `ThrowIfGreaterThan`, `ThrowIfLessThanOrEqual` and `ThrowIfGreaterThanOrEqual` now map to the same numeric rows a comparison already builds.
- **A guard followed by an `else`, or an `else if` chain that throws throughout, is read rather than passed over** — reading stops at the first branch that does not throw unconditionally, and that branch, with everything after it, is marked `unread guards`.
- **An enum exclusion guard is read as `AnyEnum<T>.DifferentFrom`** — `if (status == Status.None) { throw … }`, the commonest enum guard there is, used to read as `.NonZero()`, a member `AnyEnum<T>` does not carry, and was dropped silently.
- **The recap no longer prints `guard` for a factory whose guards tightened nothing** — the word is computed from the constraints that reach the emitted chain, on the factory path as on every other.

## 1.1.0-beta.2 — August 22, 2026

_Guard reading gets substantially more thorough — a helper-delegated or modern-spelled guard, and a guard that throws in a shape the tool couldn't parse before, are both read now — and a guard the tool still can't vouch for blocks compilation instead of compiling silently._

### ⚠️ Breaking changes

- **Each parameter's recipe now lives in its own private factory method**, not inlined in the constructor's initializer — every emitted file's shape changes, though `Generate()`, the fields and the `With…` methods do not.
- **A guard the tool cannot vouch for now blocks compilation**, the same way an unresolved parameter already did, instead of quietly keeping a neutral generator that could draw a value the real constructor rejects ([ADR-0083](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.md)). The recap counts it separately — `1 TODO, 1 to verify` — since a generator *was* inferred there.

### 🙌 Improvements

- **A guard delegated to a helper, or spelled with a modern throw-helper**, is now read the same as its `if`-based equivalent — `Ensure.NotBlank(name)` and `ArgumentNullException.ThrowIfNull(name)` no longer pass over in silence or block a build that was already correct.
- **A throwing guard in a shape the tool couldn't parse before** — an `else if` chain, a block that logs before it throws — is now marked `unread guards` instead of reporting nothing at all.
- **`--format json`'s `openParameters` no longer counts a parameter that only needs verifying** — it keeps its published meaning, and the new `summary.parametersToVerify` carries the other count.
- **The recap no longer disagrees with itself about one parameter** — a row reading `to verify` is now counted as `to verify` in the footer too, not `TODO`.

### 🐛 Bug Fixes

- **Several arithmetic, size and cross-parameter guards that were misread, silently dropped, or crashed the run** are now read correctly, composed, or refused explicitly: a condition on a value derived from the parameter, an odd parameter name (`@event`, `_id`), a size past what the library can produce, a guard constant past `decimal`, a non-`int` size bound, an `Enum.IsDefined` guard, a guard spanning two parameters, and a type the emitted file could not construct.

## 1.1.0-beta.1 — August 13, 2026

_A minor release, additive throughout: three new options, and not one existing behaviour changed. `dum generate Order` still writes exactly what it wrote in 1.0.0-beta.1, byte for byte._

### ✨ New

- **`--entry-point`** — a scaffold can now also write an entry point, so a generator is reached the way the library's own are. `any` emits a C# 14 extension member, giving you `Any.Order()` beside `Any.Int32()`; `static:<Name>` emits a `partial` root you own, giving you `Dummies.Order()`, with no language-version requirement at all. Defaults to `none` ([ADR-0070](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0070-emit-an-entry-point-on-request-as-a-file-of-its-own.md)).
- **`--entry-point-namespace`** — puts the entry-point file in a namespace of its own, apart from the generator.
- **`--format json`** — a run reports itself as one JSON document on stdout instead of the console recap, for a caller that is a script rather than a reader. Carries what the exit code cannot — `summary.openParameters`, and a row per parameter with its expression and provenance. The exit codes themselves do not move ([ADR-0071](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0071-report-a-run-as-data-without-moving-the-exit-codes.md)).
- **`dum.json`** — an optional file beside the project supplies defaults for `output`, `namespace`, `entryPoint`, `entryPointNamespace` and `format`. The command line always wins ([ADR-0072](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0072-read-project-defaults-from-a-file-the-command-line-overrides.md)).

### 🙌 Improvements

- Where a scaffold writes two files, it now writes both or neither — an existing `Any{Type}.Entry.cs` refuses the whole scaffold, and `--force` covers both.
- The console recap now names the entry-point call it opened.

### 🐛 Bug Fixes

- **`--namespace ""` and its four siblings no longer point at stale advice** now that `dum.json` can set the same option — the refusal points at the file instead.
- **A parameter type outside any namespace no longer emits a `using` that fails to parse.** Hit most often by a parameter whose type failed to resolve.

## 1.0.0-beta.1 — August 10, 2026

_First published version — `dum` reaches nuget.org for the first time, implementing the scaffolder specification in full. A **beta**, not a preview: a tool carries no public-API baseline, its surface being the command line rather than a set of types, and that surface has not yet been exercised by a project outside this repository._

### ✨ New

- **`dum generate <Type>`** — writes the dummy generator for a type, once, as ordinary code you then own.
- **Resolution.** A constructor parameter becomes a generator through the base table, then the constructor's own guard clauses (`quantity <= 0` → `.Positive()`), then composition through a factory or an already-scaffolded `Any{Type}`.
- **An open parameter is left open, loudly** — emitted as an identifier that does not exist, so your own build reports it at the line, with the type in hand ([ADR-0060](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0060-seed-generators-from-constructor-guards.md)).
- **A console recap** saying where each expression came from — base table, guard, factory, a reused generator, or nothing.
- **`--project`, `--output`, `--namespace`, `--force`, `--dry-run`**, and nothing else.
- **Package hardening** — embedded SPDX SBOM, SourceLink, symbol package, deterministic build, and a build-provenance attestation on the release artifact.

### 🙌 Improvements

- Requires the `JustDummies` package in the analyzed project. No dependency on it is declared in either direction — every library symbol is resolved by name against your compilation, exactly as the analyzers do ([ADR-0063](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0063-give-the-scaffolder-no-dependency-on-the-package.md)), so tool and library versions can never skew.
