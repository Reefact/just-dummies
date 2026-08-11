# ADR-0066 | Load MSBuild from the installed SDK, never from the tool's own files

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0066-load-msbuild-from-the-sdk-never-from-the-tool.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-11
**Accepted:** 2026-08-11
**Decision Makers:** Reefact

## Context

The `dum` tool opens a real project on disk before it scaffolds anything, and that requires an
MSBuild-aware Roslyn workspace. ADR-0065 places that work in the CLI: the scaffolding engine is
handed a compilation and knows nothing of MSBuild, so the whole of this concern lives in the shell
around it.

MSBuild is not an ordinary library. It is part of the installed .NET SDK, and the assemblies a
process uses must be the ones that SDK provides. A tool carrying its own copy loads that copy
instead, and the divergence surfaces at run time — after the project has begun to open — as a
failure that names neither the tool, nor the SDK, nor the version that disagreed.

`MSBuildLocator` exists for this: it finds the installed SDK at startup and binds MSBuild from
there. It also refuses, at build time, a project that would deploy an MSBuild assembly of its own
(MSBL001), which turns a run-time trap into a compile-time error a contributor cannot miss.

The tool ships as a .NET tool, and a .NET tool deploys its whole dependency closure as files —
the same packaging fact ADR-0063 reasons from for the library. Nothing that appears in the closure
is inert: it is a file beside the executable, and MSBuild's loader will find it.

The workspace layer the tool compiles against carries MSBuild assemblies as a transitive
dependency, at the exact version it was itself built against. The tool needs those assemblies to
compile and must not deploy them, so both halves — the version and the deployment — follow from
the workspace layer rather than from a preference expressed here.

Dependency automation proposes upgrades one package at a time. It cannot see that a version is the
consequence of another package's choice, so it reads a derived number as a stale one and proposes
to raise it every week.

## Decision

The tool compiles against MSBuild and never deploys it: MSBuild is located in the installed SDK at
startup, and the compile-time reference is held at whatever version the workspace layer resolves
rather than at a version chosen here.

## Rationale

**The failure this prevents is the kind that cannot be debugged from its symptom.** A mismatched
MSBuild assembly does not fail where it is wrong; it fails later, inside a load the tool did not
write, with a message that names an internal type. Choosing the SDK's copy removes the class of
failure rather than making it less likely.

**A tool that reads a developer's project must use the MSBuild that builds it.** Anything else
answers a question about a project that does not exist — a project as this tool's own MSBuild would
have evaluated it, not as the developer's SDK does.

**The version is a consequence, so treating it as a choice can only introduce error.** Following
what the workspace layer resolves keeps the compile-time surface and the loaded assemblies in step
by construction. Choosing a different number puts them out of step, and nothing checks that
agreement until run time.

**The guard is worth having because it is checkable.** The locator's build-time refusal means the
arrangement is enforced by the build rather than by memory, which is what makes it safe to leave the
deployment exclusion to a single reference rather than to a reviewer's attention.

**Barring the automation costs nothing, because no upgrade of that package alone can be right.**
Either the workspace layer moved, in which case its own upgrade brings the new version along, or it
did not, in which case raising the number breaks the agreement the previous paragraph rests on.
Automation cannot distinguish the two, and a human re-match is the only correct response to either.

The trade-off accepted is legibility: a derived version looks stale to a reader who does not know it
is derived. That is answered by writing the reason where the version lives, not by making the
version free.

## Alternatives Considered

### Ship MSBuild beside the tool

Considered because it removes the dependency on an installed SDK, and would let the tool run on a
machine that has none.

Rejected because it is the failure mode itself, not a way around it: the tool would load its own
MSBuild instead of the developer's, and evaluate their project under an engine that never builds
it. The locator refuses the arrangement at build time precisely because the run-time symptom is
unreadable.

### Choose the MSBuild version here, and upgrade it on its own schedule

Considered because it makes the dependency explicit and maintainable like every other one, and
because a derived version is easy to mistake for neglect.

Rejected because the number is not the project's to choose. Any value other than the one the
workspace layer resolves puts what the tool compiles against out of step with what it loads, and
that disagreement has no compile-time symptom — it waits for the first project that exercises the
part where the two versions differ.

### Let dependency automation propose the upgrade, and rely on CI to reject a bad one

Considered because CI does reject it today, so the cost looks like noise rather than risk.

Rejected because it relies on a red build to re-derive a fact already established, every week, and
because the dangerous case is the green one. The current refusal comes from a build-time check that
happens to see this particular shape; an upgrade that compiles cleanly and still diverges from the
loaded assembly would pass the same gate.

## Consequences

### Positive

* The tool evaluates a developer's project under the developer's SDK, which is the only reading of
  that project that means anything.
* An attempt to deploy MSBuild fails the build rather than the run, so the arrangement is enforced
  where it is cheap to fix.
* The compile-time version and the loaded assemblies stay in agreement by construction, with no
  check to write and none to forget.
* A weekly upgrade proposal that could never be accepted stops being opened, reviewed and closed.

### Negative

* The tool requires an installed .NET SDK, and says so rather than working around it.
* The pinned version reads as stale to anyone who has not read why it is pinned, which is why the
  reason travels with it.
* One dependency is now outside automation, so it moves only when someone moves it.

### Risks

* The workspace layer moves and the version is not re-matched, leaving the reference behind what
  the layer requires — the first version bump to make this pin a downgrade will surface as NU1605,
  but only on the build that attempts it.
* A future contributor reads the automation exclusion as licence to exclude other dependencies for
  convenience rather than for a derived-version reason.

## Follow-up Actions

* Re-match the compile-time MSBuild version by hand whenever the workspace layer moves, and revisit
  the automation exclusion at the same time.

## References

* ADR-0065 — the engine knows nothing of MSBuild, which is why this concern is the CLI's alone.
* ADR-0063 — the packaging fact this reasons from: a .NET tool ships its closure as files.
* `Directory.Packages.props`, `JustDummies.Cli/JustDummies.Cli.csproj` — where the reference and its
  reason live.
* `.github/dependabot.yml` — where the automation exclusion and its reason live.
* Pull request #60 — the upgrade proposed on that package alone, and the build failure that showed
  what it costs; pull request #63 — the exclusion that stops it recurring.
