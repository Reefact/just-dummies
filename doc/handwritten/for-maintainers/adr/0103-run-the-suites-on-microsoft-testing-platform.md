# ADR-0103 | Run the test suites on Microsoft.Testing.Platform

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0103-run-the-suites-on-microsoft-testing-platform.fr.md)

**Status:** Proposed
**Proposed:** 2026-08-31
**Decision Makers:** Reefact

## Context

Until now every suite in this repository ran through **VSTest**: `dotnet test` invoked the SDK's
`VSTest` target, `xunit.runner.visualstudio` adapted xUnit to it, and `coverlet.collector` — a VSTest
*data collector* — produced the OpenCover report the Sonar gate reads.

xUnit.net has since moved to **Microsoft.Testing.Platform** (MTP), a runner in which a test project is
an executable that hosts its own extensions rather than a library a separate runner loads. The two
platforms coexisted while MTP 1.x still shipped a bridge back to the `VSTest` target. **MTP 2.x removed
that bridge on the .NET 10 SDK**, which is the SDK `global.json` pins: the target now stops with a
first-class error telling the caller to opt into the new `dotnet test` experience.

`xunit.v3` 4.x reached the repository anyway, ahead of this decision. Its core package installs the
MTP 2.x variant and defaults `IsTestingPlatformApplication` to true, which is what stopped `dotnet test`
on that error; the bump landed by defaulting the property back to false, putting every test project back
on the `VSTest` target. So an intermediate state does exist — it is the one `main` is in today: xUnit
pinned at 4.0.1, the suites running through VSTest, coverage still produced by `coverlet.collector`.

Three facts describe what holding that state costs.

* It holds against the direction of the ecosystem rather than against a transient defect. xUnit targets
  MTP, the bridge is gone on the pinned SDK, and every subsequent xUnit release is on the far side of it.
  The default the shim overrides is one upstream sets deliberately.
* The repository already runs its suites on both platforms at once. The four Stryker configurations
  drive their legs with `"test-runner": "mtp"`, and Stryker launches the test application directly
  instead of going through the `VSTest` target, so the shim never reaches it: a mutation leg exercises
  these suites on MTP while `dotnet test` exercises the same suites on VSTest.
* `coverlet.collector` is a VSTest *data collector*. It keeps working only for as long as the suites stay
  on the platform it plugs into.

One fact used to make waiting the better option, and no longer does. Stryker's `mtp` runner was unusable
here until 5.0.0: 4.16.0 counted every test in the solution during a leg's initial run instead of the
project's own, tripping its own more-than-half-failing guard (upstream
`stryker-mutator/stryker-net#3117`). That left one mutation leg permanently red on the first attempt at
this migration. The repository pins 5.0.0, where a leg's initial run counts its own suite.

`JustDummies.Xunit` compiles against `xunit.v3.extensibility.core` and declares it as a **published**
dependency, so its compatibility floor followed the pin when the bump landed: `main` already declares
4.0.1. Raising that floor is therefore not part of this decision, and nothing here changes a published
dependency.

## Decision

The repository's test suites run on Microsoft.Testing.Platform, opted into for every caller through
`global.json`, with coverage produced by `coverlet.MTP` and configured by a settings file copied beside
each test application.

## Rationale

**The shim was a reprieve, and a reprieve is only worth keeping while it still buys something.** It
bought the xUnit bump: CI was red across every suite, and defaulting one property back cleared that
without touching the runner, the collector or four CI invocations at the same time. What it buys from
here is a repository held on a platform its own test framework has left, by overriding a default upstream
sets on purpose — a cost that recurs at every xUnit release with nothing accruing against it. Migrating
while the reprieve still holds is the difference between choosing the moment and being forced into it.

**One repository should not run its suites on two platforms.** The mutation legs already drive MTP and
`dotnet test` drives VSTest, so the same tests execute under two runners depending on which tool asked.
A discovery or execution difference between the two then surfaces as a mutation result that cannot be
reconciled with a green suite — and the first attempt at this migration produced exactly that, which is
how long it took to find the upstream defect behind it. The divergence was tolerable while it was nobody's
decision; it is not something to keep on purpose.

**Opting in through `global.json` puts the choice where the SDK already looks.** The runner is a
property of *this repository's toolchain*, not of any one project or command line, and `global.json`
is where this repository already states which SDK it is built by. A per-project MSBuild property would
have had to be repeated seven times and would have left a bare `dotnet test` in a contributor's shell
behaving differently from the same command in CI — the divergence the pinned SDK exists to prevent.

**Keeping OpenCover keeps the quality gate honest.** The alternative collector on this platform emits
Microsoft's own coverage format, which Sonar reads through a *different* importer. Changing the report
format at the same time as the runner would have moved two variables under a gate whose thresholds were
calibrated against the first, and any drift in the numbers would have been unattributable. `coverlet.MTP`
is the same tool as before, by the same authors, emitting the same format — so the gate keeps measuring
what it measured, and the migration is falsifiable by comparison.

**Configuring the collector in a file, not on a command line, preserves an existing decision.** The
settings that used to sit in `coverage.runsettings` were kept in a file precisely so a local run and a
CI run could not measure different things. The platform's settings file serves the same purpose, so the
property survives the change of mechanism; only the file's name and format moved. The command line keeps
just the switch that *enables* collection, exactly as it did before.

**Bounding the collector to the modern leg beats discovering its limits on the floor.** The support floor
(ADR-0007) runs the netstandard2.0 assets on the real .NET Framework CLR, and that leg collects no
coverage — the numbers come from the modern leg. Since the collector documents .NET Core 8.0 as its
supported runtime, wiring it into a leg that neither needs it nor is promised to run it would buy
nothing and risk a start-up failure in the one job whose whole purpose is to prove the floor still runs.

**The runner is a toolchain choice, so it changes nothing a consumer can observe.** `JustDummies.Xunit`
binds to xUnit's extensibility surface and its declared floor follows the pin, which the bump already
moved. How this repository *executes* its own suites reaches no package: the collector, the settings file
and the four CI invocations are all build-time, so this decision is answerable on its own evidence rather
than against a compatibility promise.

## Alternatives Considered

### Keep the shim indefinitely

CI is green with it, the suites pass, and the coverage gate reads the reports it always read. Nothing
breaks tomorrow if the property simply stays at false.

Rejected because it is maintenance with no end and no gain. Every xUnit release arrives on the far side
of the bridge, so the shim has to keep being right about a default upstream keeps setting the other way,
while the migration it defers stays the same size. It also keeps the two-platform divergence above as a
permanent property of the repository rather than an accident of timing.

### Migrate the runner in the same change as the xUnit bump

The tidier history: one pull request moves the framework, the runner, the collector and the four CI
invocations together, and no shim ever exists.

Rejected on timing, and it is what was attempted first. At that point Stryker 4.16.0 could not run these
suites on MTP (`stryker-net#3117`), so the combined change carried a permanently red mutation leg and sat
unmergeable. Meanwhile `dotnet test` was refusing every suite, which is an urgent CI failure and not
something to hold hostage to a cross-cutting migration. Splitting them let the urgent half land in two
files and kept this half reviewable on its own.

### Replace the collector with Microsoft's coverage extension

It is the platform's first-party collector and has a build for every MTP line, which would have made the
staged migration above possible.

Rejected because it emits a different format, read by a different Sonar importer, so it changes what the
coverage gate consumes at the same moment as the runner — and it would have to be adopted twice, once to
stage the migration and once to settle on a final collector. `coverlet.MTP` reaches the end state in one
move.

### Drop `xunit.runner.visualstudio` as dead weight

Nothing in `dotnet test` loads the VSTest adapter once the platform changes, so the package could have
been removed rather than bumped.

Rejected as out of the migration's scope and not free: the adapter is also what lets an IDE that only
speaks VSTest discover these tests, and removing it would trade a build-time saving for a working-day
regression on any editor not yet fluent in the new platform. It is bumped with its siblings and stays.

## Consequences

### Positive

* The suites run on the platform xUnit itself targets, so future majors stop being blocked on a bridge
  that no longer exists.
* One platform, one answer: `dotnet test` and the mutation legs exercise the suites the same way, so a
  mutation result and a green suite can be compared again.
* The shim disappears, and with it a default this repository had to keep overriding.
* Seven copies of the coverage wiring collapse into one shared import, so the suites cannot drift apart
  in what they measure.

### Negative

* Every documented `dotnet test` invocation changes shape, so muscle memory and any copy of a command
  outside this repository go stale at once.
* Contributors on an IDE that cannot yet drive the new platform keep test discovery only through the
  retained VSTest adapter, which no longer matches how CI runs the same suites.

### Risks

* The support floor is the one leg that cannot be exercised outside CI — .NET Framework needs Windows —
  so its migration is proven by the `framework-floor` job rather than locally. Its projects build clean
  against the new pins; that they still *run* is what that job answers.
* The collector timestamps each report so seven suites can share one results directory. Two reports
  written inside the same millisecond would collide; the platform's own per-module prefix is the remedy
  if it is ever observed.

## Follow-up Actions

* Decide, on the `xunit` train's next release, whether the floor the bump already moved warrants its own
  version signal to consumers. The latest published adapter still ships against 3.2.2, so that signal has
  not been owed yet.

## References

* [ADR-0007](0007-floor-the-library-on-net-framework-4-7-2.md) — the .NET Framework support floor the
  `framework-floor` job proves, and which the collector is scoped off.
* [ADR-0018](0018-adapt-dummies-to-xunit-v3-through-a-companion-package.md) — why the adapter exists and
  binds to xUnit's extensibility surface.
* [ADR-0047](0047-declare-the-adapters-library-dependency-independently.md) — how the adapter's *library*
  dependency is chosen at pack time; its xUnit dependency is not chosen that way and follows the pin.
* [ADR-0026](0026-measure-justdummies-mutation-against-the-unit-suite-only.md) — the mutation suite,
  already driven on this platform, and the half of the two-platform divergence that was there first.
* [`workflows/sonar`](../workflows/sonar.en.md) — how the coverage report reaches the quality gate.
