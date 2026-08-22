# ADR-0081 | Declare one top-level type per file, enforced by a third-party style analyzer

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0081-declare-one-top-level-type-per-file.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-22
**Accepted:** 2026-08-22
**Decision Makers:** Reefact

## Context

This repository's style rules live in two places. Those Roslyn can express are restated in
`.editorconfig` and enforced by the build; `JustDummies.sln.DotSettings` remains the source of
truth for the rest ([ADR-0034](0034-enforce-the-style-rules-the-compiler-can-express.md)). That
split exists because a rule held only in the DotSettings is read by Rider and by nothing else:
the explicit-type rule drifted to 203 violations across 17 files while nominally at error
severity, and nothing ever reported one.

"A file declares one top-level type" falls on the unenforced side of that split. No built-in
Roslyn rule expresses it, so `.editorconfig` cannot carry it. The repository's own `JD`
analyzers cannot either: they ship inside the published package
([ADR-0023](0023-ship-justdummies-analyzers.md)), where a rule about this repository's file
layout would reach every consumer of JustDummies and govern code it has no business governing.

The tree does not follow the rule today. Measured across the solution, 21 files hold more than
one top-level type, 11 of them in the shipping projects and 10 in the test projects. Three
files carry most of it: a regular-expression node hierarchy, a random-source file that has
accumulated unrelated helpers, and the CLI's run report.

`StyleCop.Analyzers` carries the rule as `SA1402`, its file-name corollary as `SA1649`, and a
field-accessibility rule as `SA1401`. Its last stable release is 1.1.118, published in 2019 —
before records and before file-scoped namespaces, both of which this codebase uses. Its active
line is a pre-release, 1.2.0-beta.556, which is what the .NET ecosystem consumes in practice.

The package's surface was measured on this codebase rather than estimated. Enabled whole it
reports 24 380 warnings. Declining the families that govern spacing, layout, `this.` prefixes,
regions, file headers, underscore-prefixed fields and XML documentation — territory the
DotSettings owns or house conventions contradict — leaves 1 074. Keeping only the three rules
above leaves 152, with no effect on the existing Sonar, .NET-analyzer or `IDE*` surfaces.
Declining the rule in the test projects leaves 72, across the 11 shipping files. One rule,
`SA0001`, survives its category being declined and needs a decline of its own.

Two behaviours were measured rather than assumed. `SA1402` does **not** exempt types that
differ only by generic arity: `Toto` and `Toto<T>` in one file are reported. `SA1649` accepts
both `Toto.cs` and `Toto{T}.cs` as the file for `Toto<T>`, and rejects `TotoOfT.cs` and the
metadata spelling. The tree holds no `Toto` / `Toto<T>` pair today: 16 generic and 337
non-generic top-level types, with no name in both sets.

Three existing arrangements bear on how this can be adopted. `SonarAnalyzer.CSharp` is already
referenced as a build-time-only asset, which is what keeps an analyzer out of every published
package's dependency graph ([ADR-0003](0003-host-dummies-as-a-standalone-package.md)).
`EnforceCodeStyleInBuild` is deliberately not scoped to CI, so a configured rule reports at the
moment the code is built rather than once a pull request exists. And the CI ratchet promotes
every warning to an error, so a rule left at warning severity blocks on the way in.

A suppression in this repository names its rule through a catalogue constant rather than a
string literal, and the `DCAT` analyzers report a literal one as an error
([ADR-0050](0050-name-a-suppressed-rule-through-a-catalogue-constant.md)). A catalogue
describing the StyleCop rules is published as `DiagnosticCatalog.StyleCop`.

## Decision

A file declares a single top-level type, enforced at build time by a third-party style analyzer
taken as a build-time-only asset, with every one of that analyzer's other rules declined and
the rule itself declined in the test projects.

## Rationale

**The unenforced arrangement is the one that already failed here.** ADR-0034 exists because a
style rule whose only home was the DotSettings drifted to 203 violations while nobody could see
it. Writing "one type per file" into the agent instructions and trusting review would rebuild
exactly that arrangement, with the same blind spot: a contributor who does not open Rider — a
human without ReSharper, or an agent, which cannot parse the file under any circumstance — is
told nothing.

**Enforcement belongs at the build, not at the pull request.** The repository already made this
choice once, by refusing to scope `EnforceCodeStyleInBuild` to CI: the point is to reach
whoever writes the code, while they write it. An analyzer inherits that property for free. A
check that runs only in CI would speak for the first time once the branch is pushed, which is
later than the rule is worth.

**The rule already exists, correctly, and this repository cannot host its own copy.** The `JD`
analyzers are published inside the package, so a convention about this repository's file layout
has no home there; a private analyzer project would mean writing a Roslyn rule that already
exists, and carrying it. The existing rule also handles what a heuristic would get wrong —
partial types, nested types, the difference between a top-level declaration and C# quoted
inside a test fixture — and brings the file-name corollary with it.

**Declining the rest is what keeps ADR-0034 intact rather than what undermines it.** The
measurement shows the great majority of the package's rules govern spacing, braces, blank lines
and column alignment: precisely the territory ADR-0034 leaves to the DotSettings, and precisely
what this repository cannot reproduce with any tool an agent can run. Several others contradict
conventions this codebase holds on purpose — underscore-prefixed fields, regions. Adopting them
would not extend the enforced set, it would set two sources of truth against each other. The
boundary moves by one deliberate band, not by two hundred rules.

**The test projects are excluded because their grouping is the unit a reader wants.** A file
named after the diagnostics it covers, holding one class per diagnostic, announces its contents
in its own name; splitting it would produce files nobody navigates to and lose the grouping the
name promises. The existing test-scoped section of `.editorconfig` already carries declines of
this shape.

**The generic-arity case needs no exception, which is why none is granted.** The concern that
opened this question — whether `Toto` and `Toto<T>` would be forced apart — is answered by the
file-name corollary rather than by a derogation: the pair separates into two files that both
satisfy the rule, and neither needs a suppression. Granting an exception the mechanism cannot
express, for a case the naming already resolves, would have added a rule with no work to do.

**The costs accepted are a pre-release dependency and a bounded migration.** The stable release
predates language features this codebase uses and would report against its own gaps, so the
pre-release line is the only usable one; it is also what the ecosystem runs. The migration is
11 files and 36 extracted types, known rather than estimated, and it is paid once.

## Alternatives Considered

### Keep the convention unenforced, in the agent instructions and in review

Considered because it costs nothing to adopt, needs no dependency, and leaves every judgement
call — including the three files where grouping is defensible — with the reader.

Rejected because ADR-0034 is the record of this exact arrangement failing in this exact
repository. A rule that only a reader enforces is enforced only on the files that reader opens,
and the contributors who most need to be told are the ones who cannot be.

### A check script under `tools/`, run by CI

Considered because it takes no dependency, and because `tools/` already holds checks of this
shape that CI runs and that are deliberately kept outside the solution.

Rejected on two counts. It would speak only once a pull request exists, forfeiting the
build-time visibility the repository chose deliberately elsewhere. And counting top-level types
from outside the compiler means reimplementing, as a heuristic, what the language defines:
partial and nested types, and the C# written inside test fixtures' string literals. Measured,
such a heuristic already disagreed with the compiler's own answer on this tree.

### A first-party analyzer, private to this repository

Considered because the repository already builds, tests and ships Roslyn analyzers, so the
infrastructure — a pinned Roslyn floor, a test project, established conventions — exists.

Rejected because it would reimplement a rule that already exists and is already correct, and
because a `JD` rule carries an upkeep this convention does not warrant: an id, a message, a
release-tracking entry and a documentation page in each language. The published catalogue is
for rules this library's users need, not for this repository's file layout.

### Adopt the analyzer's whole rule set

Considered because a dependency taken for three rules is a poor trade, and because the package
carries genuinely valuable rules beyond them — member ordering above all.

Rejected as posed, because the measurement shows what "the whole set" means here: the largest
families govern layout the DotSettings owns, and adopting them would put two configurations in
conflict over the same lines. Member ordering is the one band worth revisiting, at 606 sites
and in probable conflict with this codebase's regions; it is a separate decision, and bundling
it here would hide it.

## Consequences

### Positive

* The rule reports where the code is written — in the IDE, in `dotnet build`, and to an agent
  that cannot read the DotSettings — instead of in a review comment or not at all.
* The file-name corollary comes with it, so a file's name and its type stay in step.
* The analyzer is a build-time-only asset, so no published package's dependency graph changes.
* The exception the question started from disappears: `Toto` and `Toto<T>` each get a file, and
  the naming convention for the generic one is fixed rather than left to taste.

### Negative

* A migration of 11 shipping files and 36 extracted types lands before the rule can be
  enforced, touching three files where the grouping was deliberate.
* The repository takes a dependency on a pre-release package, and the whole of that package's
  rule surface has to be declined explicitly and kept declined.
* A rule surviving its category's decline showed that declining by family is not sufficient on
  its own; the configuration has to be verified against a build rather than reasoned about.

### Risks

* The pre-release line has been the ecosystem's de facto stable for years, but it carries no
  release commitment; a future version may add rules inside already-declined families. Declining
  by family rather than rule by rule is what bounds that.
* Splitting the three cohesive files trades one readable unit for several small ones. The
  regular-expression hierarchy in particular leaves a seven-line abstract base in a file of its
  own, which carries little on its own.

## Follow-up Actions

* Split the 11 shipping files, extract the type that does not belong to the node hierarchy, and
  give each generic type the braces spelling of its file name.
* Write the declines with their reasons, in the shape the existing entries in `.editorconfig`
  use, rather than as a bare list.
* Decide member ordering separately, against its measured 606 sites and the region conventions
  it would meet.

## References

* [ADR-0034](0034-enforce-the-style-rules-the-compiler-can-express.md) — the split this decision
  moves by one band, and the drift that justified it.
* [ADR-0035](0035-state-the-coding-rules-where-an-agent-can-act-on-them.md) — where a rule has
  to live for an agent to act on it.
* [ADR-0039](0039-derive-the-build-rule-set-from-the-quality-profile.md) — the shape this
  repository gives an enforced rule set: membership stated, exceptions written with their reason.
* [ADR-0003](0003-host-dummies-as-a-standalone-package.md) — why an analyzer is taken as a
  build-time-only asset.
* [ADR-0050](0050-name-a-suppressed-rule-through-a-catalogue-constant.md) — how a suppression
  names its rule, should one ever be needed here.
* [ADR-0023](0023-ship-justdummies-analyzers.md) — why the first-party analyzers cannot host a
  repository-internal convention.
