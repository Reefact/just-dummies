# ADR-0097 | Rename the Any facade to Dummy

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0097-rename-the-any-facade-to-dummy.fr.md)

**Status:** Proposed
**Proposed:** 2026-09-03
**Decision Makers:** Reefact

## Context

* Since the project's first commit, `JustDummies`' entry point has been a static class named
  `Any`, and the whole generator surface followed that word: the `Any{Type}` builder family
  (`AnyString`, `AnyGuid`, …), the seeded-context mirror `AnyContext`, the interface
  `IAny<T>` every generator implements, and `ConflictingAnyConstraintException`.
* The package, the repository and the library's own documentation are named `JustDummies`
  and describe every value the library draws as a "dummy" throughout — the definition in
  `CLAUDE.md`, the user guides, the README. The facade a consumer actually writes,
  `Any.Int32()`, has named the identical concept with a different word since the beginning.
* `JustDummies.GenAny`, the engine behind the `dum` scaffolder, and the `JustDummies.Cli`
  tool built on it carry the same word forward: a scaffolded generator is emitted named
  `Any{Type}` (e.g. `AnyOrder`), and `dum generate --entry-point any` asks for an entry
  point reaching the library's own façade.
* `JustDummies` has published preview versions to nuget.org since 2026-07-31, most recently
  `1.0.0-preview.6` on 2026-09-02. `JustDummies.Cli` has published beta versions since
  `cli-v1.0.0-beta.1`, most recently `1.1.0-beta.6` on 2026-09-03; per
  `.claude/rules/cli-and-scaffolder.md`, what a `cli` version commits to is the command
  line itself, and every option gained since `1.0.0-beta.1` — `--entry-point` included —
  has so far been additive, never a rename of an existing one.
* The maintainer directed this rename directly, in two steps: first the facade and its
  whole generator family, library-wide, including the `JustDummies.GenAny` engine; then,
  on review of the consequence that `JustDummies.Cli`'s `--entry-point any` value was left
  unchanged, confirmed it should follow the same rename.

## Decision

Every occurrence of `Any` naming this concept — the facade and its generator family, the
`IAny<T>` interface, `ConflictingAnyConstraintException`, the `JustDummies.GenAny` engine and
the `dum` CLI's `--entry-point any` value — is renamed to `Dummy`, with no exception and no
compatibility alias.

## Rationale

* The package, the repository and the documentation already call every drawn value a
  "dummy"; a facade named `Any` was a second word for the identical concept that every new
  reader had to learn was not a distinction. Naming the facade `Dummy` closes that gap
  instead of asking each reader to bridge it.
* The scaffolder emits code that calls the library's own surface, and the CLI's
  `--entry-point` flag exists to name what that emitted call reaches. Renaming the facade
  without the scaffolder and the flag would leave `--entry-point any` reaching a call
  spelled `Dummy.Order()` — the flag's word and the surface it names would then disagree,
  which is the exact mismatch the rename exists to remove, reintroduced at the tool
  boundary instead of the library one.
* No compatibility alias, because a second name for the facade reproduces the very
  ambiguity this decision removes — a consumer reading `Any.Int32()` beside `Dummy.Int32()`
  in the same codebase would again have two words to reconcile, indefinitely, rather than
  once at upgrade time.
* Paying the migration cost now rather than later is a deliberate trade: `JustDummies` and
  `JustDummies.Cli` already have real, if early, preview and beta consumers, so the number
  of consumers who must migrate only grows the longer the rename waits.

## Alternatives Considered

### Keep `Any`, and introduce `Dummy` as an alias

Considered because it lets an already-published consumer keep compiling unchanged.

Rejected because two names for one facade double the discoverable surface and leave the
naming mismatch this decision exists to close only partially resolved — a codebase mixing
`Any.Int32()` and `Dummy.Int32()` still has two words to reconcile, this time inside the
library's own public surface rather than only between the library and its package name.

### Rename the facade only, and leave `JustDummies.GenAny` and `--entry-point any` unchanged

Considered because the scaffolder and the CLI ship as a separate package with no shared
public-API baseline, so nothing forces either to track the library's own names.

Rejected because the scaffolder's whole purpose is emitting calls against the library's
surface: leaving `--entry-point any` in place would have it reach a call already spelled
`Dummy.Order()`, so the flag's own word would stop naming anything in the surface it
reaches.

### Defer the rename to the first stable release, when nothing yet depends on the current names

Considered because it is normally the cheapest moment to rename a public surface, as
Context notes ADR-0010 did for the one pre-1.0 factory it renamed.

Rejected because that moment has already passed: both packages have published preview and
beta versions with real, if early, consumers. Waiting for a stable release does not remove
the migration cost, it only grows the set of consumers who pay it.

## Consequences

### Positive

* One name, `Dummy`, now names the concept everywhere a consumer meets it: the package, the
  repository, the documentation, the facade, the scaffolder's emitted code and the CLI's own
  flag.
* The scaffolder's emitted call and the CLI flag that asks for it agree again:
  `--entry-point dummy` reaches `Dummy.Order()`.

### Negative

* Every consumer of an already-published `JustDummies` or `JustDummies.Cli` preview or beta
  must migrate by hand: `Any` to `Dummy`, `IAny<T>` to `IDummy<T>`, `Any{Type}` to
  `Dummy{Type}`, `--entry-point any` to `--entry-point dummy`.
* The rename touches the public surface of both packages at once — every generator type,
  the scaffolder's emitted names, the CLI's own command line, the committed PublicAPI
  baselines and the paired English/French documentation — a wide, one-time mechanical cost.

### Risks

* A consumer who upgrades without reading the changelog meets a compile break with no soft
  transition, since no alias was kept. Mitigated by the changelog entries this rename adds
  to both trains, and by SemVer: a preview or beta version does not promise compatibility
  across releases.

## Follow-up Actions

* None — the rename is complete: the facade and its generator family, `JustDummies.GenAny`
  to `JustDummies.GenDummy`, and `--entry-point any` to `--entry-point dummy` *(done in the
  changes this record documents)*.

## References

* [ADR-0010](0010-name-any-factories-after-their-clr-type.md) — the CLR-type
  factory-naming decision on the same scalar surface; unaffected by this rename, which
  renames the surface's own prefix, not its factory names.
* [`doc/handwritten/for-maintainers/specifications/justdummies-tool.md`](../specifications/justdummies-tool.md),
  §4.5 — the entry-point mechanics this decision's CLI half touches.
* `CONTRIBUTING.md`, "Public API baseline" — the mechanism that turned this rename into a
  reviewed diff on both packages' committed surfaces.
