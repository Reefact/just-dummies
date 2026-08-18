# AGENTS.md — JustDummies

Instructions for automated agents (OpenAI Codex and others) in this repository.
Two roles are covered: **writing code** and **reviewing pull requests**.

This file is self-sufficient: everything an agent needs is here or in the neutral
documentation it links to. Claude Code additionally packages these same rules under
`.claude/` — layered by when each is needed, per
[ADR-0073](doc/handwritten/for-maintainers/adr/0073-layer-the-agent-instructions-by-when-they-are-needed.md).
That packaging is a delivery mechanism, never a second source of truth: an agent
that cannot read `.claude/` loses nothing by reading this file instead.

## Project orientation (code changes)

- Generates **explicit, constrained, domain-respecting dummies** for .NET: constraints express the
  invariants a value must satisfy, never what the test asserts. Targets netstandard2.0 (the floor) and
  net8.0; the supported .NET Framework floor is 4.7.2. Ships 30 Roslyn analyzers (`JD001`-`JD030`)
  inside the package.
- **The name is the scope: *just* dummies** (ADR-0046). A dummy is arbitrary and valid for the
  constraints declared at the call site — not a statistically ideal draw, not a universal generator, not
  a constraint solver. Correctness is never what gets bounded: a returned value satisfies every
  constraint declared.
  The decision base repeats one move: bound the surface, bound the effort, refuse loudly at the edge —
  `Any.Combine` stops at arity eight (ADR-0005); `Any.StringMatching` covers the regular subset with the
  library's own parser and refuses the rest by name rather than taking a dependency (ADR-0008); distinct
  collections, string exclusions and regex matching use a bounded redraw that fails explicitly
  (ADR-0004, ADR-0012, ADR-0027); sizes are capped at a million (ADR-0076, superseding ADR-0029) and float draws stay within an
  ordinary magnitude (ADR-0031). Before making the generator cleverer, ask whether the honest answer is a
  clear refusal. A solver, a runtime dependency taken to widen coverage, or a silently widened bound
  needs a decision, not a patch.
- Build: `dotnet build JustDummies.sln`
- Test: `dotnet test JustDummies.sln` (analyzer tests: `dotnet test JustDummies.Analyzers.UnitTests`).
- Adding a `JustDummies` test? It belongs to exactly one of two suites:
  `JustDummies.PropertyTests` for invariants that hold for every legal constraint
  argument, `JustDummies.UnitTests` for specific named cases — message content,
  argument validation, structural conventions, dated regressions. Read
  [`doc/handwritten/for-maintainers/WritingJustDummiesTests.en.md`](doc/handwritten/for-maintainers/WritingJustDummiesTests.en.md)
  first; the decision behind it is ADR-0019.
- Repository language is **English** (code, comments, commits, PRs, issues, and
  review comments). French documentation is a translation kept in sync with the English page it mirrors:
  every maintainer page and every analyzer page comes as an `.en.md`/`.fr.md` pair. Change a page, change
  its twin.
- A type marked `[ValueObject]` (ADR-0043 — today `ConstraintClaim`, `ConstraintCall`, `Replay`) is
  **`class`, never `struct`**: a struct exposes a zero-initialized default that bypasses validating
  constructors. A reflection convention in `JustDummies.UnitTests/ValueObjectConventionTests.cs` holds
  every marked type to a full value identity. Enums are the only value-type exception.
- A declared constraint is carried as a value object, never as the text it renders to (ADR-0042).
- Keep changes small and focused. Treat renamed diagnostic IDs and public types as breaking changes.
- A `[SuppressMessage]` names its rule through the catalogue constants (`SonarRule`, `NetAnalyzersRule`,
  `JustDummiesRule` — ADR-0050), never string literals; a rule outside the referenced catalogues is
  reported to the maintainer, not suppressed by literal. A justification **duplicated** across sites (and
  only such a one) lives as a `const` in `SuppressionJustification.<RuleId>`, detailed reasoning in its
  `///<summary>`, crisp sentence as its value; a single-site justification stays inline, or may take the
  same value/summary split when its author wants the detail documented.
- A `[SuppressMessage]` is written with the **short name** (the file carries `using
  System.Diagnostics.CodeAnalysis;`) and its whole argument list on **one line**, however long — so two
  suppressions on one member read as two rules rather than one wrapped block. A justification long enough
  to make that unreadable moves to a `SuppressionJustification` constant; the attribute is not re-wrapped.
- **An image the maintainer supplies ships byte for byte.** Never resize, recolour, crop, composite or
  redraw one. If it does not work — over the size limit, wrong format, unreadable at the 128 px a
  nuget.org listing renders — say so and stop; replacing it is the maintainer's call. Check the file
  itself rather than describing it: format, dimensions, weight, transparency, canvas fill.
- Adding a new project? Also add its GUID to `JustDummies.sln`'s
  `GlobalSection(NestedProjects)`, nested under the `src` or `tests` solution
  folder like its siblings — a project left out of that section sits loose at
  the solution root instead of grouped with the rest. This has recurred
  several times; check it whenever a `.csproj` is added.

## Architecture decisions (code changes)

Before finalizing a pull request, check it against the ADR base under
`doc/handwritten/for-maintainers/adr/` (format and conventions: `doc/handwritten/for-maintainers/adr/README.md`). An ADR
records a **significant, lasting decision** — one a future maintainer would ask
"why did they do it this way?" about — not every change. Apply the README's test:
*if the implementation changed but the decision stood, the ADR should not need
editing.* Most pull requests embark no such decision; the **check** is mandatory,
the **ADR** is not.

The check has three outcomes — state the result in the pull request description:

- **Create** — the pull request embarks a new lasting decision (a public API
  contract, a cross-cutting invariant, a supported-platform floor, a dependency or
  security/compatibility policy, and the like). Draft one ADR per decision from
  `template.md` with **`Status: Proposed`**, add it to the index in `README.md`,
  and link it from the pull request.
- **Supersede** — the decision replaces one already recorded. Never edit the
  existing ADR in place or change its status yourself: name it in the pull request,
  draft the successor as `Proposed`, and leave the status flip to the maintainer.
  Accepted ADRs are immutable historical records.
- **Alert** — the pull request contradicts an accepted ADR. Do not proceed
  silently: flag it in the description — `⚠️ Conflicts with ADR-NNNN (<title>)` —
  with the precise conflict, and let the maintainer decide (accept it as a
  supersession, or change the code).

An agent **drafts and proposes**; it never accepts, supersedes, or deprecates an
ADR on its own authority — that is the maintainer's call, exactly as no agent
merges a pull request. When it is genuinely unclear whether a change is
significant enough, or whether it supersedes an existing ADR, say so in the pull
request and let `@reefact` judge rather than guessing.

## Tidying history before a pull request (acting agent)

This governs the agent that *prepares* a branch for review, not the reviewer.
This repository lands pull requests by **rebase** ([ADR-0051](doc/handwritten/for-maintainers/adr/0051-land-pull-requests-by-rebase.md)),
so every commit a branch carries is replayed onto `main` — a messy branch is not
squashed away on merge, and no merge commit brackets it either: its commits
arrive one by one on the line, indistinguishable from the rest. It pollutes
protected history for good. `CONTRIBUTING.md` already
fixes the endpoint (autosquash placeholders squashed before merge, a conforming
header on every commit, one intention per commit); this section makes the agent
*reach* it **on its own initiative**, the way it runs the ADR check without
being asked.

At two moments, read the branch against a freshly fetched `origin/main`:
**before opening a pull request**, and **after pushing further commits to an
already-open one**.

```
git fetch origin
git log --oneline origin/main..HEAD
```

Judge whether the history reads clean. Treat these as **messy**, worth proposing
a cleanup for:

- autosquash placeholders still pending — `fixup!`, `squash!`, `amend!` (CI
  rejects them);
- a commit that only fixes, rewords, or reverts an earlier commit of the *same*
  branch — "wip", "typo", "address review", a commit and its own revert;
- a header that fails the convention — run each through the repository's own
  linter, `git log -1 --format=%B <sha> | tools/commit-lint/lint-commit-message.sh --ci -`;
- one logical change scattered across commits that do not each stand alone, or
  two unrelated intentions folded into one commit (CONTRIBUTING.md, "Commit
  messages");
- a commit whose state no reader will ever reach — one superseded by a later
  commit of the *same* branch, so the intermediate state never lands on `main`.
  Each such commit can be well-formed and still be redundant, which is why the
  four signals above miss it. Recording a decision as `Proposed` and accepting
  it in the next commit is the recurring case, and it is one intention, not two.
  It is genuinely two only when the acceptance came later and on its own, as
  ADR-0051's did seven commits after its record. The hook flags that ADR
  instance; every other shape of it is your judgement.

When it reads clean, say so in one line and proceed. When it is messy,
**propose** a concrete plan — which commits to squash, reword, drop, or reorder,
and the resulting `git log --oneline` shape — and rewrite only after an explicit
go-ahead. The endpoint is the maintainer's to approve: no agent rewrites a
branch on its own authority any more than it merges one.

Hard constraints on the rewrite itself (CONTRIBUTING.md, "Branches"):

- Rewrite history **only while the branch is yours alone**. Once anyone may have
  based work on it, a force-push discards that work — leave the history and say
  why.
- Publish with `git push --force-with-lease`, never a bare `--force`: the lease
  refuses the push if the remote moved under you.
- Never touch a commit already on `main`; `origin/main..HEAD` is the only range
  you may rewrite.
- This tidies history, not code. The diff against `origin/main` MUST be identical
  before and after — prove it with `git range-diff origin/main <old-head> HEAD`
  (only messages and grouping move, never the tree).

For Claude Code the mechanics are packaged: the `tidy-history` skill runs the
assessment and, on approval, the rewrite; a hook (`.claude/`, on pull-request
creation and after each committing or pushing git command) flags the CI-fatal
signals so the check is never skipped. Other agents apply the rule by hand.
Either way the judgement — *is this messy?* — and the decision to rewrite stay
here.

## Review guidelines (pull request reviews)

READ THIS BEFORE REVIEWING. This section is the whole contract — the rules below
are mandatory. Review the pull request's delta first (`git diff origin/main...HEAD`,
the changed files, any earlier findings on the branch); widen to a repository-wide
scan only when the change's nature demands it.

### Output format — mandatory

Every inline comment MUST use exactly this shape, with nothing around it:

```text
<label> [(decorations)]: <subject on one line>

<optional discussion>
```

In this shape, `< >` marks a placeholder to replace and `[ ]` marks an optional
part — write neither the angle brackets nor the square brackets literally.
Decorations, when present, go in parentheses (for example `(security)`).

- The entire comment is written in **English** — label, decorations, subject and
  discussion. Code identifiers, API names and exception messages are quoted verbatim.
- Never publish an unlabelled comment.
- Exactly **one label** and **one independent finding** per comment. At most two decorations.
- Do **NOT** add a severity/priority prefix — no `P0`, `P1`, `P2`, `P3`,
  `critical`, `major`, `minor`, anywhere in the comment. Blocking status is
  carried only by the label and the `(blocking)` / `(non-blocking)` decoration.
- No introduction or conclusion around the comment. Place it on the smallest
  relevant code range. Do not repeat the same finding on multiple lines.

Canonical example:

```text
issue (correctness): The redraw loop can exit without satisfying the declared exclusion.

`AnyString.Excluding` redraws while the candidate is excluded, but the bounded-redraw guard
returns the last candidate when the budget runs out instead of throwing. A generator that
cannot honour its constraint must fail loudly (ADR-0012), not hand back a value that violates
the invariant the caller declared.

Raise `AnyGenerationException` when the budget is exhausted, as the collection path does.
```

### Labels (one per comment)

- `issue:` confirmed defect that must be addressed — *blocking*.
- `todo:` small, obvious, local, non-debatable required change — *blocking*.
- `chore:` mandatory process step before merge; name the command/file — *blocking*.
- `question:` code looks suspicious but evidence is insufficient to assert a defect — *non-blocking*.
- `suggestion:` concrete optional improvement (never for incorrect code — use `issue:`) — *non-blocking*.
- `nitpick:` purely subjective, optional preference; should be rare — *non-blocking*.
- `note:` relevant information, no change expected — *non-blocking*.
- `thought:` design/architecture observation out of scope; must state no change is required here — *non-blocking*.
- `praise:` genuinely good and worth preserving; explain what and why — *non-blocking*.

Override a default only when the finding genuinely differs, e.g.
`suggestion (blocking):` or `issue (non-blocking):`. Never restate a default
(`issue (blocking):`, `nitpick (non-blocking):`).

Allowed decorations: `(blocking)`, `(non-blocking)`, `(if-minor)`, `(security)`,
`(perf)`, `(test)`, `(archi)`. One normally, never more than two.

### What to report (priority order)

Correctness → security → data integrity → regressions → public API / compatibility
→ concurrency / reliability → significant performance → missing tests for a
demonstrated risk → violations of an explicit repository rule (e.g. a value object
converted to `struct`).

Do NOT report: formatter-enforced style, issues already flagged by a `JDxxx`
analyzer or by the Sonar profile, naming already enforced by tooling, speculative problems with
no execution path, broad refactors unrelated to the PR, personal style presented as
a requirement, or pre-existing issues the PR does not materially affect.

If there is no relevant finding, approve without manufacturing comments.

### Final summary

Keep it concise. Report only: the number of blocking findings, the number of
non-blocking findings, and the main risk areas. Do not restate every inline
comment. If nothing was found, state clearly that no blocking issue was found.
The summary is not a Conventional Comment and needs no label.

## Responding to review feedback (acting agent)

This section governs the agent that *fixes* a pull request in response to a
review (for example `@claude`), not the reviewer. The human maintainer `@reefact`
is the only authority that merges a pull request; no agent merges, and no agent
enables auto-merge on its own pull request.

For each review finding, take exactly one route:

- **You agree and the fix is clear and local** — implement it, push, and reply on
  the thread with `Resolved in <sha>`. You MAY ask the reviewer (`@codex`) for a
  single confirming re-review; never open a back-and-forth.
- **You believe the finding is wrong** — reply on the thread with the concrete
  technical reason and mention `@reefact` to arbitrate. Do not ping `@codex` to
  argue: a peer reviewer has no authority to settle the disagreement.
- **The finding needs a human judgement** — architecture, a product trade-off, an
  ambiguous requirement, a security or compatibility policy — mention `@reefact`
  and wait. Do not decide unilaterally.

Rules:

- Never mention both `@codex` and `@reefact` on the same thread: a bot round-trip
  or a human decision, never both.
- At most two fix / re-review cycles per finding. If it is still open after that,
  stop and mention `@reefact` instead of continuing.
- Keep replies short and factual; the diff is the record.
