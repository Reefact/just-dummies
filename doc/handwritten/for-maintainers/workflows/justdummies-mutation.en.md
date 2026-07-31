# `justdummies-mutation` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](justdummies-mutation.fr.md)

> Maintainer documentation — part of the [workflow reference](README.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/justdummies-mutation.yml`](../../../../.github/workflows/justdummies-mutation.yml)

## What it is for

Coverage answers *"was this line executed by a test?"*. Mutation testing answers
the question that actually matters: *"would any test have noticed if this line
had been wrong?"*.

[Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction/) rewrites
the library one small change at a time — flip a comparison, drop a statement,
return the other constant, remove a block — rebuilds it, and re-runs the test
suite against each rewrite. A **mutant** the suite still passes on is a
**survivor**: a behaviour the code has and nothing asserts. A killed mutant is a
test doing its job.

This workflow makes that check automatic for the **three JustDummies
components** — `JustDummies`, its xUnit v3 adapter `JustDummies.Xunit`
([ADR-0018](../adr/0018-adapt-dummies-to-xunit-v3-through-a-companion-package.md)),
and the analyzers that ship inside the package
([ADR-0023](../adr/0023-ship-justdummies-analyzers.md)). On a pull request it
mutates only the files the pull request changed, for the adapter and the
analyzers; the generator is measured by the weekly sweep alone
([ADR-0028](../adr/0028-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.md)).
The score is reported **without blocking the merge** — advisory since
[ADR-0025](../adr/0025-make-the-per-pull-request-mutation-gate-advisory.md),
because Stryker's per-*file* `--since` selection makes the cost follow the size
of the file a change lands in, not the size of the change. The **weekly sweep**
is the enforced bar.

## Why it is a separate workflow

This split predates the repository. `JustDummies` is a standalone,
error-agnostic package that deliberately holds no reference to
`FirstClassErrors` ([ADR-0003](../adr/0003-host-dummies-as-a-standalone-package.md)),
and it was headed for a repository of its own. Splitting the mutation gate along
that future boundary *before* the move meant the move was a **file move rather
than an edit** — which is exactly how it played out: see *The move has happened*
below.

The split still earns its keep now that it has happened. It gives JustDummies its
own check, **`JustDummies mutation gate`**, and a bar that moves independently of
any other repository's. On pull requests it is **advisory**
([ADR-0025](../adr/0025-make-the-per-pull-request-mutation-gate-advisory.md));
the enforced bar is the weekly full sweep.

## When it runs

- On every **pull request targeting `main`** — diff-scoped and **advisory**: it
  reports the diff's score but never blocks the merge ([ADR-0025](../adr/0025-make-the-per-pull-request-mutation-gate-advisory.md)).
- **Weekly** on a schedule (Monday, 03:47 UTC) — the full sweep, the **enforced
  bar**.
- On demand via **`workflow_dispatch`** — the full sweep.

## How it runs

Each mutated component has its own Stryker configuration under
[`build/stryker/`](../../../../build/stryker/): the project to mutate, the test
projects that must kill its mutants, and the thresholds. Nothing about the run
policy lives only in the YAML, so `dotnet stryker --config-file
build/stryker/justdummies.json` on a maintainer's machine gates exactly like CI
does. The three are
[`justdummies.json`](../../../../build/stryker/justdummies.json),
[`justdummies-xunit.json`](../../../../build/stryker/justdummies-xunit.json) and
[`justdummies-analyzers.json`](../../../../build/stryker/justdummies-analyzers.json).

The engine itself is pinned in
[`.config/dotnet-tools.json`](../../../../.config/dotnet-tools.json) and restored
with `dotnet tool restore`. That pin is load-bearing: a newer Stryker invents new
mutants, which moves every score on its own.

### `changed` — the diff, on every pull request

One matrix leg per component in the per-PR scope. Each leg:

1. Checks out with **`fetch-depth: 0`** — Stryker's `--since` diffs against a
   commit, so the history has to be there.
2. Resolves the **fork point** (`git merge-base` of the pull request's base and
   `HEAD`), not the base branch tip: the tip may have moved on since the branch
   was cut, and every file changed on `main` in the meantime would otherwise be
   counted as "changed by this pull request".
3. Runs Stryker with `--since:<fork point>`, so only mutants **in files this pull
   request touched** are tested.
4. Renders the surviving mutants — status, file, line, kind of rewrite — into the
   run summary, so a failing leg can be diagnosed without leaving the run page.
5. Uploads the HTML and JSON reports as an artifact — `if: always()`, because the
   HTML view shows each survivor *in its source*, which the summary table cannot.

A leg whose project the pull request did not touch selects no mutant, reports
*"unable to calculate a mutation score"*, and exits 0. That is a pass — and it is
the common case.

### `gate` — the single advisory check

A matrix produces one check per leg. `gate` collapses them into one stable check
name — **`JustDummies mutation gate`** — so branch protection has a single entry
to point at rather than re-declaring leg names every time the matrix changes.

It is **advisory** ([ADR-0025](../adr/0025-make-the-per-pull-request-mutation-gate-advisory.md)):
it reports the aggregate of the diff legs but **never fails the pull request**. A
genuine leg failure is surfaced as a `::warning::` to investigate, and a run
cancelled by a superseding push is treated as noise, not a failure. It runs with
`if: always()` so it reports after a failed *or cancelled* leg rather than being
skipped. The enforced bar is the weekly `full` sweep, not this check.

### `full` — the weekly sweep

The same components with the `--since` filter removed, and the generator added
back: every mutant of every component. It is **advisory by construction** —
`--break-at 0` disables the threshold — because its job is to publish a trend,
not to turn `main` red on a Monday morning over code nobody changed. Read it from
the uploaded HTML report.

**The one place this workflow differs from a plain full-matrix gate: the per-PR
matrix is two legs, not three.** The generator is swept weekly but is **not**
mutated per pull request
([ADR-0028](../adr/0028-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.md)).
Because `--since` selects per changed **file** rather than per changed line, a
hundred-line diff touching one of the generator's larger sources pulls in that
whole file: measured at 844 mutants, still running after an hour, producing no
score at all. That is not a tuning gap — every lever Stryker exposes tops out
around −36 % where such a leg would need −95 %, sharding cannot go below one
file, and line-scoped `mutate` patterns select nothing. The adapter and the
analyzers are small, finish in about ninety seconds, and keep their leg.

## Two settings that are not tuning knobs

`build/stryker/*.json` carries two settings that look like performance tuning and
are not. Both were established by measurement; changing either silently breaks
the gate rather than making it slower.

### `"test-runner": "mtp"` — mandatory, not a preference

Stryker's **default VSTest runner does not work on this test bed at all.** Every
test project here is xUnit v3, and an xUnit v3 test project *is* an executable
that the VSTest adapter launches as a child process — out of reach of the
in-process hooks Stryker uses both to capture coverage and, crucially, to
**activate** the mutant. The run completes, reports a plausible test count, and
scores **0 %**: every mutant comes back "survived", including mutants that
demonstrably break the suite when the same edit is applied by hand. Upstream:
[stryker-net#3117](https://github.com/stryker-mutator/stryker-net/issues/3117).

The Microsoft Testing Platform runner launches the test executable itself, so the
mutant is activated and the score is real. Stryker flags it **preview** and says
so on every run; that warning is expected here, not a misconfiguration.

If a future Stryker upgrade makes every score collapse to zero, this is the first
thing to check.

### `"coverage-analysis": "off"` — accuracy, not speed

Stryker normally runs a coverage pass first so each mutant only re-runs the tests
that reach it. Under the MTP runner that selection is still incomplete
([stryker-net#3629](https://github.com/stryker-mutator/stryker-net/issues/3629)):
mutants the suite *does* kill get classified as uncovered and counted against the
score. Measured upstream on a comparable population, the same set scores 75 %
with selection on and 100 % with it off — and the 100 % is the true figure.

Turning it off costs little on the adapter and the analyzers, whose suites are
fast. It costs more on the generator, which is the largest component here. That
is a reason to keep the generator out of the per-PR critical path — which
[ADR-0028](../adr/0028-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.md)
does — not a reason to re-enable a selection that reports the wrong number.

## The cost model, and why the gate is diff-scoped

**One full run of the component's test suite per mutant**, plus roughly two
minutes of fixed cost per leg (solution analysis, build, initial test run, mutant
generation).

`JustDummies` is the largest component here — a few thousand mutants — so its
full sweep is the longest job this repository runs. That is the whole reason the
gate is diff-scoped rather than a full sweep per pull request.

It also explains two things that surprise people:

- **Selection is per changed *file*, not per changed *line*.** Stryker's `--since`
  has no line granularity. Adding one line to a large file selects **every**
  mutant in that file, so the gate reports the whole file's mutation score — not
  just the score of what was added. On the biggest files that is a longer job and
  a score that reflects pre-existing debt.
- **A pull request that only adds tests still selects mutants**, through the test
  files it changed.

## Where the thresholds come from

Each component carries its own `break` in `build/stryker/*.json`, and the values
differ on purpose. They are **not** an opinion about how good a component ought
to be: a bar is set from that component's measured full-sweep score at the time
the gate was introduced, rounded down, with a little room left for the odd
equivalent mutant.

That makes the gate a **ratchet**, not an aspiration. It says *do not go below
where this component already is* — a bar it clears on day one, so the gate never
starts red, and one that only ever moves up. Raising a value after the weekly
sweep shows real headroom is the intended way to use it; lowering one should feel
like a decision.

The consequence to keep in mind: a component sitting well below 100 % has a low
bar today, and a pull request touching one of its weaker files can still fall
under it. That is the gate working, not misfiring — the report says which
assertion is missing.

## `JustDummies` has no score threshold yet

Every other bar was set from a measured full sweep of the thing it gates (above).
`JustDummies` was not: it carries a few thousand mutants over a heavy suite, its
full sweep runs well past an hour, and **no score for it has been measured**.
Rather than invent a number,
[`justdummies.json`](../../../../build/stryker/justdummies.json) sets `break` to
**0** — the score gate for this one component is off.

That is deliberate and it is temporary. The leg still runs, still fails on a
broken build or a failing suite, and still lists its surviving mutants in the run
summary; what it does not yet do is refuse a pull request over a score. **The
first weekly sweep publishes the library-wide figure** — that is the run this
threshold is waiting on. Read it, and set `break` from it exactly as the other
bars were set.

`JustDummies.Xunit` needs no such caveat: it is small enough that its bar came
from a full sweep like the rest, and it gates normally.

The analyzers leg also ships with `break` at **0**, for a different reason: its
residual survivors are analyzer-infrastructure and descriptor-string mutants, so
it reports rather than blocks
([ADR-0023](../adr/0023-ship-justdummies-analyzers.md)).

## When the survivor is an equivalent mutant

Sometimes the honest answer is that the mutant cannot be killed: the rewrite does
not change observable behaviour, so no test could tell the difference. Writing a
test to chase it would be writing a test that asserts an implementation detail —
worse than the gap.

Stryker takes that answer in the source, next to the code, as a comment:

```csharp
// Stryker disable once Statement : the trace call has no observable effect
```

The form is `// Stryker disable [once] <mutator|all> [: reason]`, with
`// Stryker restore all` to end a non-`once` block. Prefer `once`, prefer naming
the mutator rather than `all`, and always give the reason — an undocumented
exclusion is indistinguishable from a missing test six months later. Reach for it
only after deciding the mutant really is equivalent; lowering a threshold to
silence one survivor hides every future survivor with it.

## Permissions & security

`contents: read` only. The workflow checks out, builds and runs tests; it stores
no secret and needs no write scope.

## The move has happened

JustDummies left `Reefact/first-class-errors` on 2026-07-31 and this repository is
where it landed. What the migration actually did, recorded because the runbook that
used to sit here described it in the future tense:

- this workflow kept its own name, `justdummies-mutation.yml`, rather than becoming
  `mutation.yml` — there is no second mutation workflow here to disambiguate it from,
  but renaming it would have broken the `JustDummies mutation gate` branch-protection
  entry for no gain;
- the three Stryker configurations came over unchanged except their `solution` field,
  which now names `JustDummies.sln`;
- [`.config/dotnet-tools.json`](../../../../.config/dotnet-tools.json), the Stryker
  pin, came over too — though not on the first pass, which is why both mutation legs
  failed at `dotnet tool restore` until it was restored;
- the shared sections of the upstream `mutation` page have now been folded into this
  one, so this page is self-contained. It no longer defers to a page this repository
  does not hold.

## Handle with care

- **`fetch-depth: 0` is required**, not a habit. A shallow clone leaves the fork
  point unreachable and `--since` cannot resolve it.
- **`--since` wants a branch, a tag or a real commit SHA — `HEAD` is rejected.**
  `--since:HEAD` fails the whole run with *"No branch or tag or commit found with
  given target"*, which is why the workflow resolves `git merge-base` to a SHA
  first rather than passing a rev expression through.
- **The CI warning ratchet does not need disabling here.** It is a fair worry —
  Stryker compiles *mutated* source, and a mutant routinely raises a warning the
  original never had — but measured, `GITHUB_ACTIONS=true` changes nothing:
  Stryker compiles the mutants through Roslyn with its own options and does not
  inherit `TreatWarningsAsErrors` from
  [`Directory.Build.props`](../../../../Directory.Build.props). The compile-error
  count is identical with the ratchet on and off. If a future Stryker started
  honouring it, mutants would silently turn into compile errors instead of being
  tested — the count in the run log is where that would show.
- **`if: always()` on `gate` is load-bearing.** Remove it and `gate` is skipped
  whenever a leg fails or is cancelled, so it never reports the aggregate — the
  advisory warning ([ADR-0025](../adr/0025-make-the-per-pull-request-mutation-gate-advisory.md))
  would be silently dropped exactly when there is something to say.
- **The Stryker version is pinned in the tool manifest.** Bumping it is a
  deliberate act: expect the scores to move, and re-read the thresholds.
- **The thresholds live in `build/stryker/*.json`, not in the YAML.** That is what
  keeps a local run and CI in agreement. `break` is the value that fails the
  build; `high`/`low` only colour the report.
- **A survivor is not automatically a bug**, and the answer to an equivalent one
  is a `// Stryker disable once` comment with a reason, never a lowered threshold
  — see *When the survivor is an equivalent mutant* above.

## Running it locally

```bash
dotnet tool restore
dotnet stryker --config-file build/stryker/justdummies.json
```

That is the full sweep of the largest component and it takes a while. To reproduce
what the gate does on a branch:

```bash
dotnet stryker --config-file build/stryker/justdummies.json --since:$(git merge-base origin/main HEAD)
```

Reports land in `StrykerOutput/` (git-ignored); open `reports/mutation-report.html`.

## Related

- [`justdummies`](../../../../.github/workflows/justdummies.yml) *(no reference
  page yet)* — the other JustDummies-scoped workflow: it proves the packaged
  `netstandard2.0` and `net8.0` assets behave on their own runtimes.
- [ADR-0022 — Gate pull requests on the mutation score of what they
  changed](../adr/0022-gate-pull-requests-on-the-mutation-score-of-the-diff.md)
  — the decision this workflow implements.
- [ADR-0025 — Make the per-pull-request mutation gate
  advisory](../adr/0025-make-the-per-pull-request-mutation-gate-advisory.md)
  — why the check reports instead of blocking.
- [`mutation`](https://github.com/Reefact/first-class-errors/blob/main/doc/handwritten/for-maintainers/workflows/mutation.en.md)
  in `Reefact/first-class-errors` — the same machine for that repository's
  libraries. Kept as a pointer only: this page no longer depends on it.
