# ADR-0130 | Ship code fixes beside the analyzers, for the one rewrite that keeps the intent

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0130-ship-code-fixes-beside-the-analyzers.fr.md)

**Status:** Proposed
**Proposed:** 2026-11-02
**Decision Makers:** Reefact

## Context

No JustDummies rule shipped a code fix: there was no `CodeFixProvider` in the repository, and the follow-up
ADR-0077 pointed to (issue #95) was never built. The decisions taken on the first developer-UX trial ask for some
(issue #355): several rules report a mistake whose correct spelling is fully determined by what the rule found —
a constraint that narrows nothing, a pair of bounds the author transposed, two inclusive bounds the generator names
as a range in one call.

A code fix runs in an IDE or in `dotnet format`, never in the compiler. It is built on the workspace layer of
Roslyn, which the analyzers do not reference, and it is loaded from the same `analyzers/dotnet/cs` folder as the
rules (ADR-0023). The analyzers are compiled against a Roslyn floor pinned once, so the oldest supported host loads
them (ADR-0001); the `floor` job proves it on the packed package.

A fix that guesses is worse than no fix: a developer who accepts it in a batch has their code changed to something
they did not write. Other rules of this library already report mistakes with several ways out — a contradiction
where either side could be the intended one, a chain reduced to one value where a literal or a wider range could
both be meant — and name them instead of choosing.

## Decision

Code fixes ship in a sibling assembly, `JustDummies.Analyzers.CodeFixes`, packed beside the analyzers under
`analyzers/dotnet/cs`, built on the workspace layer at the same Roslyn floor as the rules, and a rule gets a fix only
where exactly one rewrite keeps the developer's intent.

* A rule that admits a fix hands it what it established through the diagnostic's properties — which argument pair
  is transposed, which two calls make the range and what it is called — so the fix rewrites what the rule found and
  reads nothing again.
* A fix is tested like a rule: in `JustDummies.Analyzers.UnitTests`, by the source it produces, whole.
* The first batch sets the pattern, one of each shape: JD024 removes the constraint that narrows nothing, JD014
  swaps a transposed pair, JD031 writes the pair as its range form. The other rules the issue lists follow the same
  test; the rules that report a contradiction, a chain reduced to one value, or a lifecycle mistake get none.

## Rationale

**A sibling assembly, because the compiler must stay untouched.** The rules carry no workspace reference, which
keeps the analyzer small and its load contract the compiler's own. A fix needs that layer. Packing the fixes in
their own assembly beside the rules keeps both: the compiler reads the rules, an IDE reads both, and neither
assembly carries what the other needs.

**The same floor, for the same reason.** A host old enough to load the rules must load their fixes too, or a
developer on that host sees a rule with no light bulb and no explanation. The pin that holds the rules holds the
fixes.

**The rule tells the fix what it found.** A fix that re-derived the pair or the range from the syntax would be a
second reading of the same chain, which can disagree with the first. The diagnostic already carries the rule's
reading; passing it on through properties makes the fix exactly as right as the rule.

**Only the unique rewrite.** The library's rules name ways out rather than choosing between them; a fix is where
choosing is unavoidable, so it exists only where there is nothing to choose. The three in the first batch are
equivalent by construction: a constraint that narrows nothing removed leaves the same domain, a transposed pair
swapped is the pair the guard accepts, and the range method is the two bounds it replaces.

**Tested by the source produced.** An IDE applies a fix to a document and shows the result; the test does the same
and compares the whole text, trivia included, so a fix that mangles a line next to the one it meant to change fails.

## Alternatives Considered

### Fixes inside the analyzer assembly

Considered because one DLL is simpler to pack and prove. Rejected: the analyzer would then reference the workspace
layer, which the compiler does not load, and its load contract would no longer be the compiler's alone.

### A fix for every rule, choosing the likeliest rewrite

Considered because a light bulb on every diagnostic reads as a complete product. Rejected: a fix that guesses which
side of a contradiction to keep rewrites a test into one the developer did not mean, and a batch application
multiplies that.

### A fix that re-reads the chain

Considered because the syntax is at hand. Rejected: two readings of one chain can disagree, and the rule's is the
one the diagnostic was reported on.

## Consequences

### Positive

* A reported mistake with one correct spelling is corrected where it is reported, singly or in a batch.
* A new fix follows a written pattern: the properties the rule attaches, the harness that applies it.

### Negative

* The package carries a second assembly under `analyzers/dotnet/cs`, and the packaging script asserts both.

### Risks

* A host that loads the fix assembly without the workspace layer logs a load failure where the rules still run;
  the compiler never instantiates a fix, so a build is not affected.

## Follow-up Actions

* The rules the issue lists with a single intent-preserving fix — JD025, JD032, JD035, JD013, JD005, JD011, JD002,
  JD001, JD027 — and the two with a choice offered side by side, JD010 and JD004, follow the pattern set here.

## References

* [ADR-0001](0001-lock-the-analyzer-roslyn-floor.md) — the Roslyn floor.
* [ADR-0023](0023-ship-justdummies-analyzers.md) — the rules travel inside the package.
* [ADR-0077](0077-admit-a-rule-that-reports-a-correct-spelling.md) — the follow-up that first named a code fix.
* Issues #355 and #95.
