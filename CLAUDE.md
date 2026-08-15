# JustDummies — guide for Claude Code

<!-- This file is loaded at the start of EVERY session, before the task is known.
     An entry earns its place here only if it is reasonable to pay for it while
     fixing an unrelated test. Everything else lives one layer down: .claude/rules/
     (loaded by path), .claude/skills/ (loaded on demand), or a tool that enforces
     it without prose. The layering is recorded in ADR-0073. Target: under 200 lines.
     Block HTML comments like this one are stripped before injection — they cost
     nothing in context. -->

JustDummies generates **explicit, constrained, domain-respecting dummies** for .NET: a
fluent DSL where constraints express the invariants a value must satisfy, never what the
test asserts. It ships 29 Roslyn analyzers (`JD001`–`JD029`) inside its own package.

## The name is the scope — *just* dummies

A dummy is a value that is arbitrary and **valid for the constraints declared at the call
site** — not a statistically ideal draw, not a universal generator, not a constraint solver.
Being deliberate is not the same as being exhaustive, and this library chooses deliberate.

That is [ADR-0046](doc/handwritten/for-maintainers/adr/0046-bound-the-generators-ambition-never-its-correctness.md),
and the base had made the same move seven times before naming it: **bound the surface, bound
the effort, and refuse loudly at the edge** — arity capped at eight (ADR-0005), the regular
subset parsed by the library's own parser rather than taking a regex dependency (ADR-0008),
bounded redraws that fail explicitly instead of looping (ADR-0004, ADR-0012, ADR-0027), sizes
capped at a million (ADR-0029), floating-point draws within an ordinary magnitude (ADR-0031).

**So when a change would make the generator cleverer, ask first whether the honest answer is
a clear refusal.** A first-class error naming what cannot be honoured beats a value drawn by
a mechanism nobody can reason about. Treat these as needing a decision rather than a patch:
adding a solver or constraint-propagation pass, taking a runtime dependency to widen what can
be generated, silently widening an existing bound, or making a previously refused case
succeed by luck rather than by construction.

**Correctness is never what gets bounded.** A drawn value satisfies every constraint
declared, contradictory constraints fail fast with a message naming both sides, and any
sequential run replays from the seed it reports. The analyzers and the property suite exist
to hold that line. The boundary is about what the library *attempts*, never about what it
*guarantees* once it does.

## Build & test

```
dotnet build JustDummies.sln
dotnet test  JustDummies.sln
dotnet test  JustDummies.Analyzers.UnitTests     # when touching analyzers
```

Target frameworks: **netstandard2.0** (the floor, widest reach) and **net8.0** (which
additionally carries the generators for types absent downlevel). The supported .NET Framework
floor is 4.7.2 and CI runs the suites on it (ADR-0007).

**Only report a test or command as passing if you actually ran it.** If you did not run a
relevant command, say so explicitly.

## Repository map

| | |
|---|---|
| `JustDummies` | the library (+ `.UnitTests` for named cases, `.PropertyTests` for invariants) |
| `JustDummies.Analyzers` | Roslyn analyzers, bundled inside the library package (+ `.UnitTests`) |
| `JustDummies.DiagnosticCatalog` | the constants a `[SuppressMessage]` names its rule with; its own `catalog` train |
| `JustDummies.Xunit` | xUnit v3 adapter (+ `.UnitTests`) |
| `JustDummies.GenAny` | the `dum` scaffolding engine, pinned to the Roslyn floor (+ `.UnitTests`) |
| `JustDummies.Cli` | the `dum` tool itself, the shell around that engine (+ `.UnitTests`) |
| `JustDummies.Documentation.UnitTests` | compiles the documentation's C# samples, checks link and EN/FR parity |
| `build/`, `tools/` | shared MSBuild props, the Sonar profile, packaging and lint scripts |
| `doc/handwritten/` | `for-users/` and `for-maintainers/` (ADRs, specifications, workflow pages) |

`tools/justdummies-check` and `tools/floor-check` are deliberately **outside** the solution:
they consume the packed package, not the projects.

## Always-on rules

* **Write the type; never `var`.** The only exception is a declaration C# gives no other
  spelling — in practice an anonymous type. Checked twice: an edit-time hook, and `IDE0008`
  which CI turns into an error (ADR-0034).
* **Do not reformat code you did not change.** The repository's layout comes from
  `JustDummies.sln.DotSettings`, which no tool available to you can reproduce; reformatting
  drifts away from the style while burying the real change. Touch the lines the task requires
  and leave their neighbours alone, even when the surrounding alignment looks stale.
* **An image the maintainer supplies ships byte for byte.** Never resize, recolour, crop,
  composite or redraw one. If it does not work, say so and stop — replacing it is the
  maintainer's call. (Reasoning: `.claude/rules/assets.md`.)
* Keep changes small, focused and aligned with the requested task. Do not introduce a new
  dependency without a clear reason. Do not change the public API unless the task requires it.
* Treat renamed error codes, diagnostic IDs and public types as **breaking changes** unless
  explicitly stated otherwise.
* Preserve the **netstandard2.0** floor: a net8.0-only API belongs behind the existing
  `#if NET8_0_OR_GREATER` additive branch, never in the common surface.
* **Nothing enforces a mutation score** (ADR-0025). The per-pull-request check reports and
  does not block — never claim a pull request "passed the mutation bar".
* Repository language is **English**: source, comments, commit messages, branch names,
  pull-request titles and descriptions, issues. The English documentation is canonical and
  every page has a **French twin that changes with it**. You may reply to me in French in the
  chat; never write repository content in French unless updating the French documentation.
* Commits follow `CONTRIBUTING.md`: Conventional Commits, a closed type list, the scopes
  `core, analyzers, xunit, cli, catalog` (**required on `feat` and `fix`**), an imperative
  header within 72 characters, `Refs: #NN` in a footer when an issue exists. Enable the local
  hook once per clone: `git config core.hooksPath .githooks`.
* **Do not open a pull request unless I explicitly ask for one**, and never merge one or
  enable auto-merge on it.

## Where the rest lives

Loaded automatically when you touch matching files — `.claude/rules/`:

| Rule | Covers |
|---|---|
| `csharp.md` | value objects are `class` (ADR-0043); property vs method; the shape of a `[SuppressMessage]`; what analyzers already enforce |
| `tests.md` | which of the two `JustDummies` suites a test belongs to (ADR-0019); what mutation testing does and does not gate |
| `analyzers.md` | the five things a `JDxxx` change keeps in step; the Roslyn floor; the catalogue |
| `documentation.md` | EN/FR twins, the two naming conventions, where each kind of page lives |
| `cli-and-scaffolder.md` | the `dum` specification, golden files, the engine's constraints, the `cli` train |
| `build-and-ci.md` | the solution's `NestedProjects` section, the single sources of truth, the warning ratchet |
| `assets.md` | why an image is never modified |
| `session-economics.md` | always loaded: choosing context, model and effort proportionately |

Invoke on demand — `.claude/skills/`:

| Skill | When |
|---|---|
| `adr-check` | before finalizing a pull request, or when a change touches a contract, an invariant, a floor or a policy. **Selects** the few relevant records from the index — never reads all 73 |
| `open-pr` | preparing a pull request: title, template, honest testing claims |
| `review-pr` | reviewing one: the Conventional Comments contract |
| `review-feedback` | acting on review findings: the three routes, and when to escalate |
| `tidy-history` | before opening a pull request, and after pushing more commits to an open one |
| `release-train` | cutting, tagging or preparing a release; drafting a changelog |

Canonical documentation, shared with other agents and with humans:

* [`doc/handwritten/for-maintainers/README.md`](doc/handwritten/for-maintainers/README.md) — the hub: architecture, conventions, records.
* [`adr/README.md`](doc/handwritten/for-maintainers/adr/README.md) — the 73 decisions and their index.
* [`specifications/`](doc/handwritten/for-maintainers/specifications/) — the `dum` specification; what each accepted decision enforces and where.
* [`CONTRIBUTING.md`](CONTRIBUTING.md) — branches, commit messages, pull-request titles, the public API baseline.
* [`AGENTS.md`](AGENTS.md) — the same rules for agents that do not read `.claude/`.
