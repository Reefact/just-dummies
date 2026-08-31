# ADR-0090 | Run the test suites on Microsoft.Testing.Platform

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0090-run-the-suites-on-microsoft-testing-platform.fr.md)

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

Three facts make the move mandatory rather than optional, and make it one change rather than several:

* `xunit.v3` 4.0.0 depends on MTP 2.x. It cannot be taken while the repository runs on VSTest.
* `xunit.v3` 3.2.2 pins its MTP variant to an **exact** version range on the v1 line, so the repository
  cannot be lifted onto MTP 2.x while staying on 3.2.2.
* `coverlet.MTP` — the same project's (`coverlet-coverage/coverlet`) replacement for the collector —
  exists only against MTP 2.x, in every version it has published. Coverage cannot cross to the new
  platform ahead of the xUnit major.

So the runner, the collector and the xUnit major are one indivisible step. Dependabot nevertheless
proposed them as three separate pull requests (#85, #86, #87), each of which is red on its own.

Two other constraints bear on the change. The mutation workflow already drives Stryker with its `mtp`
test runner, so it is unaffected. And `JustDummies.Xunit` compiles against `xunit.v3.extensibility.core`
and declares it as a **published** dependency, so the adapter's compatibility floor moves with the pin —
this is not a development-only bump.

## Decision

The repository's test suites run on Microsoft.Testing.Platform, opted into for every caller through
`global.json`, with coverage produced by `coverlet.MTP` and configured by a settings file copied beside
each test application.

## Rationale

**There is no version of this repository that both keeps VSTest and takes xUnit v4.** The three facts in
Context close every intermediate state: the bridge is gone on the pinned SDK, 3.2.2 cannot reach MTP 2.x,
and the collector's successor does not exist below it. A decision that would normally be staged — move
the runner, then move the framework — has no staging available, so recording it as one decision matches
what actually happened rather than tidying it after the fact.

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

**The adapter's floor moves because a compile-time dependency cannot be published as an older one.**
`JustDummies.Xunit` binds to xUnit's extensibility surface; it is built against what the repository
pins, and shipping a package that claims to work against a version it was not compiled against would be
a promise nothing checks. Raising the declared floor is the honest reading of what the package now is.

## Alternatives Considered

### Stay on VSTest and decline the xUnit major

The repository works today, so nothing forces the move *this week*: closing the three pull requests and
telling Dependabot to ignore the major would cost nothing immediately.

Rejected because the reprieve is temporary and shrinks. The bridge is already gone on the pinned SDK;
every subsequent xUnit release is on the far side of it, so the debt grows while the migration stays the
same size. Declining also freezes the adapter's binding at a version its own upstream has moved past,
which is the position ADR-0018 gave the companion package precisely to avoid.

### Migrate the runner first, take the xUnit major second

The natural staging: land the risky, cross-cutting change — runner, coverage, four CI invocations — on
its own so it can be reviewed for itself, then let the three dependency bumps become trivial.

Rejected as unavailable, not as undesirable. It was the preferred plan until measurement showed
`coverlet.MTP` has no build against MTP 1.x, and `xunit.v3` 3.2.2 pins the v1 line exactly. The staged
version would therefore have had to cross the platform boundary with no coverage at all, under a gate
that blocks on it.

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

* The three red Dependabot pull requests (#85, #86, #87) are answered by one change, and none of them
  can be merged on its own.
* The suites run on the platform xUnit itself targets, so future majors stop being blocked on a bridge
  that no longer exists.
* Seven copies of the coverage wiring collapse into one shared import, so the suites cannot drift apart
  in what they measure.

### Negative

* **`JustDummies.Xunit`'s published dependency floor rises to `xunit.v3.extensibility.core` 4.0.0.** A
  consumer still on the 3.x line cannot take the next adapter release without moving too. This is a
  consumer-facing change on the `xunit` train, not a build detail, and it is the part of this decision
  that is not reversible by editing this repository.
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

* Close #85, #86 and #87 as superseded once this lands; none is mergeable alone.
* Decide, on the `xunit` train's next release, whether the adapter's floor raise warrants its own
  version signal to consumers.

## References

* [ADR-0007](0007-floor-the-library-on-net-framework-4-7-2.md) — the .NET Framework support floor the
  `framework-floor` job proves, and which the collector is scoped off.
* [ADR-0018](0018-adapt-dummies-to-xunit-v3-through-a-companion-package.md) — why the adapter exists and
  binds to xUnit's extensibility surface.
* [ADR-0047](0047-declare-the-adapters-library-dependency-independently.md) — how the adapter's *library*
  dependency is chosen at pack time; its xUnit dependency is not chosen that way and follows the pin.
* [ADR-0026](0026-measure-justdummies-mutation-against-the-unit-suite-only.md) — the mutation suite,
  already driven on this platform and unaffected.
* [`workflows/sonar`](../workflows/sonar.en.md) — how the coverage report reaches the quality gate.
