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

Two things a local report will not tell you. **A `Timeout` is not a survivor and not a kill
either** — Stryker scores it as killed, and on 2026-09-01 the library's first completed sweep
came back at 100 % with 2505 of its 4575 judged mutants ending there. And **a local report can
disagree with CI on the same commit**: seventeen mutants of `Guards.cs` that the runner called
killed survive in a container, cause unknown. Where a verdict matters, the arbiter is neither
report — apply the mutation to the source by hand and run the suite.

The configurations and the reasons behind them are in
[`justdummies-mutation.en.md`](../../doc/handwritten/for-maintainers/workflows/justdummies-mutation.en.md).

## Asserting a refusal

A refusal is stated with NFluent, in one expression:

```csharp
Check.ThatCode(() => Any.Int32().Positive().Negative())
     .Throws<ConflictingAnyConstraintException>()
     .WhichMember(conflict => conflict.Message).Contains("Negative()", "Positive()");
```

`.WithMessage(...)` pins the whole sentence, `.WhichMember(e => e.Message)` opens it to
`Contains` and `Not.Contains`, and `.WithProperty(e => e.Seed, 1234).And` reaches another member
first. The lambda binds to `Func<T>`, so the generator is never converted to `object` — the
recipe-as-value shape `JD011` exists to catch, and has to exempt when `Assert.Throws<T>` takes it
through its `Func<object>` overload.

`Assert.Throws` stays where the exception **instance** is the subject, and nowhere else:

* a **base** exception type is named — `ArgumentException`, `InvalidOperationException`. xUnit
  matches the exact type there and that exactness is the assertion; NFluent's `Throws<T>` admits a
  derived type and would weaken it.
* **two** thrown exceptions are compared to one another.
* the throw sits inside a scope the assertions sit outside of, or one test asserts two unrelated
  members — a message *and* an `InnerException`. A chain ends at one member.

## Conventions that are already enforced

`*Tests/**.cs` declines five analyzer rules in `.editorconfig` (`CA1861`, `S1244`, `S107`,
`S2326`, `S108`), each with its reason, and shipping code keeps them. Do not suppress those
locally — the scope already handles it.
