---
paths:
  - "JustDummies.UnitTests/**/*.cs"
  - "JustDummies.PropertyTests/**/*.cs"
  - "JustDummies.Analyzers.UnitTests/**/*.cs"
  - "JustDummies.Xunit.UnitTests/**/*.cs"
  - "JustDummies.Cli.UnitTests/**/*.cs"
  - "JustDummies.GenAny.UnitTests/**/*.cs"
  - "JustDummies.Documentation.UnitTests/**/*.cs"
  - "build/stryker/*.json"
---

# Tests

## `JustDummies` has two suites, and a new test belongs to exactly one

* **`JustDummies.PropertyTests`** — invariants that hold for **every** legal constraint
  argument.
* **`JustDummies.UnitTests`** — specific named cases: message content, argument validation,
  structural conventions, dated regressions.

The answer is never "either". Read
[`doc/handwritten/for-maintainers/WritingJustDummiesTests.en.md`](../../doc/handwritten/for-maintainers/WritingJustDummiesTests.en.md)
before adding one — it carries the question to ask and the worked cases. The decision behind
the split is ADR-0019.

The other projects have one suite each and no such question.

## Running them

```
dotnet test JustDummies.sln
dotnet test JustDummies.Analyzers.UnitTests
```

Report a suite as passing only if you actually ran it. If you did not run a relevant
command, say so.

## Mutation testing measures; it does not gate

Every pull request is measured on the files it changed, through a single check —
`JustDummies mutation gate` — covering the library, the xUnit adapter and the analyzers
(ADR-0022, and ADR-0025 which made the per-pull-request check **advisory**). The generator
is swept weekly only, never per pull request (ADR-0028).

**Nothing currently enforces a mutation score.** `justdummies.json` and
`justdummies-analyzers.json` both set `break: 0`, and the weekly sweep passes `--break-at 0`
by construction, so the only component with a real bar is `justdummies-xunit` (80) and even
that one only reports. Treat the score as information, and never claim a pull request
"passed the mutation bar" — there is none to pass.

The consequence for writing tests: a test that **executes** new code without **asserting**
it passes `dotnet test` and is still reported as a survivor. Reproduce a report on a branch
with

```
dotnet tool restore
cd <the mutated project> && dotnet stryker --config-file ../build/stryker/<project>.json --since:$(git merge-base origin/main HEAD)
```

**From the mutated project's own directory, never the repository root** (ADR-0092): a Stryker
configuration that names a solution has its `test-projects` list discovered away, and every suite
referencing the assembly judges the mutants instead of the declared one.

The configurations and the reasons behind them are in
[`justdummies-mutation.en.md`](../../doc/handwritten/for-maintainers/workflows/justdummies-mutation.en.md).

## Conventions that are already enforced

`*Tests/**.cs` declines five analyzer rules in `.editorconfig` (`CA1861`, `S1244`, `S107`,
`S2326`, `S108`), each with its reason, and shipping code keeps them. Do not suppress those
locally — the scope already handles it.
