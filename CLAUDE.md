# JustDummies — guide for Claude Code

JustDummies generates **explicit, constrained, domain-respecting dummies** for .NET:
a fluent DSL where constraints express the invariants a value must satisfy, never
what the test asserts. Keep changes aligned with that. Two properties carry the
product and are worth protecting in every change: contradictory constraints fail
fast with a message naming both sides rather than looping or drawing something that
satisfies neither, and any sequential run replays from the seed it reports.

## Scope — *just* dummies

The name is the scope. A dummy is a value that is arbitrary and **valid for the
constraints declared at the call site** — not a statistically ideal draw, not a
universal generator, not a constraint solver. Being deliberate is not the same as
being exhaustive, and this library chooses deliberate.

This is recorded as a decision — [ADR-0046](doc/handwritten/for-maintainers/adr/0046-bound-the-generators-ambition-never-its-correctness.md),
which is where the reasoning, the alternatives and the consequences live. The base
had already made the same move seven times before naming it:
**bound the surface, bound the effort, and refuse loudly at the edge.**

* `Any.Combine` stops at arity eight and says so (ADR-0005).
* `Any.StringMatching` parses the **regular subset** with the library's own parser
  and refuses a non-regular construct with a named exception, rather than taking a
  regex-automaton dependency to widen coverage (ADR-0008).
* Distinct collections, string exclusions and regex matching all use a **bounded**
  redraw that fails explicitly and reproducibly instead of looping until it wins
  (ADR-0004, ADR-0012, ADR-0027).
* A size the generator must actually produce is refused above one million
  (ADR-0029), and floating-point draws stay within an ordinary magnitude of one
  million rather than roaming the type's full range (ADR-0031).

So, when a change would make the generator cleverer, ask first whether the honest
answer is a **clear refusal** instead. A first-class error naming what cannot be
honoured beats a value drawn by a mechanism nobody can reason about. Concretely,
treat these as needing a decision rather than a patch: adding a solver or
constraint-propagation pass, taking a runtime dependency to widen what can be
generated, silently widening an existing bound, or making a previously refused
case succeed by luck rather than by construction.

None of this licenses sloppiness — a drawn value must satisfy every constraint
declared, and the analyzers and property suite exist to hold that line. That is the
second half of ADR-0046 and it is not decoration: the boundary is about what the
library *attempts*, never about what it *guarantees* once it does. The point is
where the effort goes: into being **honest about the boundary**, not into pushing
it.

## Language

* The repository language is **English** by default:
  source code, code comments, commit messages, branch names,
  PR titles and descriptions, and issues.
* The English documentation is canonical.
* French documentation is an intentional translation and must stay in sync with the English page it
  mirrors. It is not one file: every maintainer page and every analyzer page comes as an `.en.md`/`.fr.md`
  pair (28 of them under `doc/handwritten/for-users/analyzers/`), plus `doc/handwritten/for-users/`'s
  `CONTRIBUTING.fr.md` and `SECURITY.fr.md`. Change a page, change its twin.
* You may reply to me in French in the chat, but never write repository content in French
  unless you are updating the French documentation.

## Build & test

* Target frameworks: **netstandard2.0** (the floor, widest reach) and **net8.0** (which additionally
  carries the generators for types absent downlevel: `DateOnly`, `TimeOnly`, `Int128`, `UInt128`,
  `Half`). The supported .NET Framework floor is 4.7.2 and CI runs the suites on it (ADR-0007).
* Build: `dotnet build JustDummies.sln`
* Test: `dotnet test JustDummies.sln`
* Run the analyzer tests when touching analyzers:
  `dotnet test JustDummies.Analyzers.UnitTests`
* `JustDummies` has two test suites, and a new test belongs to exactly one of them:
  `JustDummies.PropertyTests` owns invariants that hold for every legal constraint
  argument, `JustDummies.UnitTests` owns specific named cases (message content,
  argument validation, structural conventions, dated regressions). The rule and how
  to apply it are in
  [`doc/handwritten/for-maintainers/WritingJustDummiesTests.en.md`](doc/handwritten/for-maintainers/WritingJustDummiesTests.en.md)
  (decision: ADR-0019). Read it before adding a JustDummies test.
* Mutation testing measures every pull request on the files it changed, through a
  single check — `JustDummies mutation gate` — covering the library, the xUnit
  adapter and the analyzers (decisions: ADR-0022, and ADR-0025 which made the
  per-PR check **advisory** — it reports the diff's score but does not block the
  merge). The generator is swept weekly only, never per pull request (ADR-0028).
  **Nothing currently enforces a mutation score.** `justdummies.json` and
  `justdummies-analyzers.json` both set `break: 0`, and the weekly sweep passes
  `--break-at 0` by construction, so the only component with a real bar is
  `justdummies-xunit` (80) and even that one only reports. Treat the score as
  information, not as a gate, and do not claim a pull request "passed the mutation
  bar" — there is none to pass. A test that *executes* new code without
  *asserting* it will pass `dotnet test` and still be reported as a survivor.
  Reproduce it on a branch with
  `dotnet tool restore && dotnet stryker --config-file build/stryker/<project>.json --since:$(git merge-base origin/main HEAD)`;
  the configurations and the reasons behind them are in
  [`justdummies-mutation.en.md`](doc/handwritten/for-maintainers/workflows/justdummies-mutation.en.md).
* Only report tests as passing if you actually ran the corresponding command.
* If you did not run a relevant command, say so explicitly.

## Project layout

* `JustDummies`             — the library (+ `.UnitTests`, `.PropertyTests`)
* `JustDummies.Analyzers`   — Roslyn analyzers, bundled inside the library package (+ `.UnitTests`)
* `JustDummies.Xunit`       — xUnit v3 adapter (+ `.UnitTests`)
* `tools/justdummies-check` — packaged-asset compatibility check, deliberately outside the solution
* `doc/`                    — documentation: `handwritten/` (`for-users`, `for-maintainers`)

The `dum` scaffolder is specified in
`doc/handwritten/for-maintainers/specifications/justdummies-tool.md` but not built yet.

When adding a new project to the solution, also add its GUID to
`JustDummies.sln`'s `GlobalSection(NestedProjects)`, nested under the
`src` or `tests` solution folder like its siblings — a project missing from
that section shows up loose at the solution root in Visual Studio/Rider
instead of grouped with the rest. This has been missed and fixed after the
fact several times; check it every time a `.csproj` is added.

## Change guidelines

* Keep changes small, focused, and aligned with the requested task.
* **Never alter an image I supply.** An icon I give you ships byte for byte — not resized, not
  recoloured, not cropped, not composited with anything, and not redrawn from what you can see of it.
  If it does not work — over the format's size limit, wrong format, unreadable at the 128 px a
  nuget.org listing renders, wrong aspect — **say so and stop**. Changing it is my call, not yours.
  Check the file rather than describing it: format, dimensions, weight, transparency, and how much of
  the canvas it fills. This is written down because it was got wrong: three variants were composited
  out of one supplied mark, and every one was worse than the file it started from.
* Do not introduce new dependencies without a clear reason.
* Do not make public API changes unless they are required by the task.
* Treat renamed error codes, diagnostic IDs, and public types as breaking changes unless explicitly stated otherwise.
* **Value objects are reference types (`class`), never structs.** A type whose instances are values
  declares itself with `[ValueObject]` (ADR-0043) — today `ConstraintClaim`, `ConstraintCall` and
  `Replay` — and a reflection convention in `JustDummies.UnitTests/ValueObjectConventionTests.cs` holds
  every marked type to a full value identity and to rendering itself for a reader. Such a type must be a
  `class`: a `struct` always exposes an unsuppressable default constructor (`default(T)`, `new T[]`,
  uninitialized fields) yielding a zero-initialized instance that bypasses every validating constructor,
  and nullable reference types only warn at compile time. Do not convert one to `struct`/`readonly
  struct` for allocation reasons: these sit on the constraint-declaration path, not in a hot loop, and
  invariant correctness takes precedence. (Enums are the legitimate value-type case — they carry no
  invariant to bypass.)
* **A declared constraint is carried as a value object, never as the text it renders to** (ADR-0042).
* Preserve compatibility with the **netstandard2.0** floor: a net8.0-only API belongs behind the
  existing `#if NET8_0_OR_GREATER` additive branch, never in the common surface.

## Coding rules

Rules you must apply to code you write. They are written out here, rather than
delegated to `JustDummies.sln.DotSettings`, because that file is a
ReSharper/Rider artifact: Rider reads it and nothing else can — no compiler, no CI
job, and no agent. Pointing at it read like an instruction without being one, and
the explicit-type rule below drifted to 203 violations under that arrangement
(decision: ADR-0035). This list is the extensible home for such rules; each one
states how it is checked, so none of them rests on attention alone.

* **Write the type; never `var`.** The only exception is a declaration C# gives no
  other spelling, which in practice means an anonymous type (`new { ... }`). This
  is checked twice: `.claude/hooks/coding-rules.sh` reports it on the edit itself,
  and the build reports it as `IDE0008`, which CI turns into an error (ADR-0034).
  A pull request carrying one does not merge.

* **Do not reformat code you did not change.** The repository's layout — the
  column alignment of consecutive declarations, the file layout patterns, the
  region conventions — comes from the `.DotSettings`, and no tool available to you
  can reproduce it. Reformatting therefore does not converge on the repository's
  style; it drifts away from it while burying the real change. Touch the lines the
  task requires and leave their neighbours alone, even when the surrounding
  alignment already looks stale.

## Diagnostic and documentation conventions

* When you change user-facing behavior, keep the English page and its French twin in sync.
* When you change analyzers, update or add analyzer tests.
* When you add, change or retire a rule, keep all five in step: the `JDxxx` id, its message, its
  `AnalyzerReleases.*.md` entry, its `doc/handwritten/for-users/analyzers/JDxxx.{en,fr}.md` pages, and the
  table in `doc/handwritten/for-users/analyzers/README.md`. The release-tracking analyzer (RS2003) checks
  the second of those and nothing checks the rest.
* A generated value's relationship to its seed is **not** a versioned contract while the library is
  below 1.0 — changing a draw sequence is allowed. Say so in the changelog when you do.

## Architecture decisions (ADRs)

Before finalizing a pull request, check the change against the ADR base under
`doc/handwritten/for-maintainers/adr/`. This is **advisory**: produce a recommendation, never a
blocker. Full procedure in [`AGENTS.md`](AGENTS.md) ("Architecture decisions");
format and conventions in [`doc/handwritten/for-maintainers/adr/README.md`](doc/handwritten/for-maintainers/adr/README.md).
The essentials, inlined so they hold even if `AGENTS.md` is not read:

* An ADR records a **significant, lasting decision** — one a future maintainer
  would question. Test: *if the implementation changed but the decision stood, the
  ADR should not need editing.* Most pull requests need none; the **check** is the
  habit, the **ADR** is the exception.
* **Create** — a new lasting decision (public API contract, cross-cutting invariant,
  supported-platform floor, dependency or security/compatibility policy): copy
  [`doc/handwritten/for-maintainers/adr/template.md`](doc/handwritten/for-maintainers/adr/template.md),
  draft one ADR per decision as `Status: Proposed`, index it in that folder's `README.md`, and link it
  from the PR.
* **Supersede** — the change replaces a recorded decision: draft the successor as
  `Proposed`; never edit an accepted ADR in place or flip its status yourself.
* **Alert** — the change contradicts an accepted ADR: flag it in the PR description
  (`⚠️ Conflicts with ADR-NNNN`); do not proceed silently.
* You **draft and propose**; you never accept, supersede, or deprecate an ADR — the
  maintainer decides, exactly as no agent merges a pull request. When unsure whether
  a change is significant enough, say so and let `@reefact` judge.

## Git and pull requests

* Follow `.github/pull_request_template.md` for every pull request.
* Do not open a pull request unless I explicitly ask for one.
* PR titles, descriptions, commits, and branch names must be written in English.
* Write every commit message per [`CONTRIBUTING.md`](CONTRIBUTING.md): Conventional Commits, a closed type list, the scopes `core, analyzers, xunit, cli, catalog`, an imperative header within 72 characters, and `Refs: #NN` in a footer when a GitHub issue exists (issue-closing keywords belong in the PR description, not the commit).
* Write every pull request title per [`CONTRIBUTING.md`](CONTRIBUTING.md): name the whole change in English; a single-intention PR mirrors its commit header (`type(scope): description`), a multi-intention PR uses a short descriptive title, and issue references stay in the description, not the title.
* Enable the local commit-message hook once per clone with `git config core.hooksPath .githooks`; the same check runs in CI on every pull request.
* Before opening a pull request — and after pushing more commits to an open one — read the branch against a fresh `origin/main` and, if the history is messy (pending `fixup!`/`squash!`, wip/typo/"address review" commits, headers the lint rejects, one change split across non-standalone commits or two folded into one), **propose** a cleanup and rewrite only after I approve — while the branch is yours alone, with `git push --force-with-lease`, leaving the diff against `origin/main` unchanged. This repository lands pull requests by rebase ([ADR-0051](doc/handwritten/for-maintainers/adr/0051-land-pull-requests-by-rebase.md)), so every commit of a messy branch reaches `main` on its own, with no merge commit bracketing it. Full rule in [`AGENTS.md`](AGENTS.md) ("Tidying history before a pull request"); the `/tidy-history` command runs it.
* In PR descriptions, do not invent testing results. Only check items that were actually run.

## Responding to pull request review feedback

When you act on review feedback on a pull request (for example a Codex review),
follow the escalation rules in [`AGENTS.md`](AGENTS.md) ("Responding to review
feedback"). The essentials, inlined so they hold even if `AGENTS.md` is not read:

* If you agree and the fix is clear and local, implement it, push, and reply
  `Resolved in <sha>`.
* If you believe a finding is wrong, reply with the concrete technical reason and
  mention `@reefact` to arbitrate — do not argue with the reviewer bot.
* If a finding needs a human judgement (architecture, a trade-off, an ambiguous
  requirement, a security or compatibility policy), mention `@reefact` and wait.
* Never mention both the reviewer bot and `@reefact` on the same thread; cap at
  two fix/re-review cycles, then escalate to `@reefact`.
* No agent merges a pull request or enables auto-merge on it — the human
  maintainer merges.
