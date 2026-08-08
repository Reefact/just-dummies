# ADR-0053 | Rewrite the published history to a single line, and carry the release tags onto it

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0053-rewrite-the-published-history-to-a-single-line.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-08
**Accepted:** 2026-08-08
**Decision Makers:** Reefact

## Context

[ADR-0051](0051-land-pull-requests-by-rebase.md) made rebase the only merge method, so every pull
request lands as a line of commits from then on. It recorded what it deliberately left behind:
"`main` keeps the merge commits of the pull requests that landed before this decision. Tools that
read history must go on filtering them". `main` therefore carried two shapes at once — linear ahead
of the decision, merge-shaped behind it.

Behind it, `main` held 485 commits, 162 of them merges: 121 written by GitHub as
`Merge pull request #NN`, and 41 back-merges a branch made by pulling `main` into itself, which
`CONTRIBUTING.md` permits once a branch is shared. 120 of the 121 pull-request merges were
fast-forwardable — the branch was already up to date, so the merge commit's tree was identical to the
branch tip's and it contributed no content of its own.

Five tags are published, four of them carrying a GitHub Release, and the packages they produced are
on nuget.org. A published version there is immutable: it can be unlisted, it cannot be corrected
([ADR-0048](0048-publish-only-from-a-commit-that-is-on-main.md)). Those packages are built with
SourceLink, which embeds the repository URL **and the commit SHA** into the published symbols, so a
debugger fetches sources by SHA rather than by tag.

[ADR-0048](0048-publish-only-from-a-commit-that-is-on-main.md) also decided that a release publishes
only from a commit that is an ancestor of `main`, and its follow-up records a tag-protection ruleset
restricting the creation, update and deletion of release tags. That note gives moving a tag as one of
the harms the ruleset exists to prevent: it "would break the link between an immutable published
artifact and the commit its tag names".

No stable version is published. Everything on nuget.org is a preview: `JustDummies 0.1.0-preview.1`
and `1.0.0-preview.1`, `JustDummies.Xunit 1.0.0-preview.1`, `JustDummies.DiagnosticCatalog
1.0.0-preview.2`. 42 commits on `main` carried a signature, 33 of them non-merge commits that any
rewrite must recreate.

## Decision

The history `main` already carried is rewritten to the single line
[ADR-0051](0051-land-pull-requests-by-rebase.md) mandates going forward, and every published release
tag is retargeted onto the commit carrying the identical tree.

## Rationale

**Pre-1.0 is the only window where this is cheap, and it closes on its own.** The cost of rewriting
published history is proportional to what depends on the old commit identifiers. Today that is four
preview packages, days old, with no stable release behind them. After 1.0.0 the same operation would
touch versions consumers are entitled to treat as permanent, and the answer would have to be no. The
decision is therefore not "is a rewrite ever acceptable" but "is it acceptable now", and the window
is the whole argument.

**The link a tag carries is preserved in the sense that decides it: the tree.** Each retargeted tag
names a commit whose tree is byte-identical to the one it named before, so the sources a published
package was built from remain exactly reachable, under the same tag, at a different identifier. What
changes is the identifier, not the content — and a package's provenance is a claim about content.
This is the narrow reading of ADR-0048's follow-up note, and it is the reading this decision adopts:
that note was written to forbid moving a tag onto *different* sources, which is the accident a
ruleset cannot distinguish from this one and rightly refuses by default.

**Leaving the tags behind would break something ADR-0048 does rely on.** That decision's premise is
that a release tag names a commit reachable from `main`, because `main`'s protection is what proves
the commit was checked. Tags left on the pre-rewrite commits would name commits no longer on `main`
at all — the ancestry the release path borrows its guarantee from would be gone, and the tags would
depend for their survival on an archive ref nobody is obliged to keep. Carrying them is the option
that keeps the invariant true.

**This is the last moment such a rewrite can be taken, and taking it is what lets the rule hold
afterwards.** Carrying the tags required suspending the tag-protection ruleset, and the argument for
suspending it — no stable release, four previews days old — expires at 1.0.0. Restoring the ruleset
is therefore not housekeeping but the act that closes the window: after it a release tag cannot be
moved at all, and ADR-0048's note governs without exception. This decision buys a linear history
once, at the price of a suspension it also ends — which is why it is an exception that confirms that
note rather than a reading that weakens it.

**A rewrite is safe here because its correctness is checkable, not merely intended.** The operation
has an exact success criterion — the tip's tree must be unchanged — and it either holds or it does
not. Around it, the commit count, every commit's message, author and dates, the absence of conflict
markers and the tags' tree identity are all decidable before anything is published. That is what
separates this from a rewrite one hopes went well, and it is why the boundary this repository draws
elsewhere — attempt less, refuse loudly, verify what you claim ([ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md))
— is satisfied rather than bent.

**Half the job would have kept the defect it was meant to remove.** Removing only the 120
fast-forwardable pull-request merges is provably lossless and needs no content decision at all, but
it leaves 41 back-merges and therefore leaves `main` branching. The reason to touch history was that
`main` did not have the shape ADR-0051 assumes; a history that still branches has not acquired it.

## Alternatives Considered

### Keep the merge commits

The status quo ADR-0051 explicitly accepted, and the only option costing nothing.

Rejected because it leaves `main` in two shapes indefinitely, and every tool that reads history keeps
paying for it. The cost of the alternative — rewriting — only ever rises, so keeping the merge
commits is a decision to keep them forever, taken by default rather than on its merits.

### Remove the pull-request merges only, and keep the back-merges

Provably lossless: those 120 merges were fast-forwardable with an identical tree, so dropping them is
graph surgery with no content decision, no conflict and nothing to verify beyond the shape.

Rejected as insufficient once its result was seen. It removes the noise a reader notices first — the
`Merge pull request #NN` lines — but `main` still branches at 41 points, so the linear shape the rest
of the conventions assume is still not the shape `main` has.

### Leave the tags on the pre-rewrite commits, preserved by an archive ref

It keeps every published SHA valid, so SourceLink keeps resolving and no released artifact's
identifier changes.

Rejected because it trades a bounded, one-time loss for an unbounded obligation. The archive ref
becomes load-bearing forever, with nothing recording why, and the first cleanup that removes it
silently breaks what it was protecting. It also leaves release tags off `main`, which is the
ancestry ADR-0048 argues from.

### Start a fresh history at the current tree

The cleanest possible `main`: one commit, no past to reconcile.

Rejected because the per-commit record is what this repository's conventions are built on — one
intention per commit, a conforming header, a scope that decides which release train publishes it.
Discarding 323 such commits to gain a shape is the trade ADR-0051 already refused when it rejected
squashing.

## Consequences

### Positive

* `main` is a single line of 323 conventional commits, with no merge commit and no commit that
  GitHub, rather than an author, wrote.
* ADR-0051's premise is now true of the whole history, not only of what lands after it, so the rules
  that argue from it no longer carry an exception.
* Tools that read history stop needing a merge filter to be correct.
* Every release tag is an ancestor of `main` again, so ADR-0048's check keeps the meaning it was
  given.

### Negative

* Every commit identifier on `main` changed. A SHA cited anywhere outside this repository — an issue,
  a review, a bookmark — no longer resolves, and the pre-rewrite history is not recoverable.
* `main` carries no signed commit. 33 non-merge commits were signed and a rewrite cannot re-sign
  them; the signatures are lost rather than invalidated.
* SourceLink cannot resolve for the versions already published: their symbols name commits that no
  longer exist. Those versions are unlisted and deprecated in consequence (see Follow-up Actions).
* A few commits in the middle of the history carry content that momentarily differs from what their
  branch held, because linearizing two divergent lineages has to reconcile them somewhere. The tip is
  exact; a commit picked at random from the middle may not build.

### Risks

* **`git bisect` can land on a commit that does not build.** The transient states above are ordinary
  for any linearization, including a plain rebase, but they are new to this repository's `main`.
* **The decision is not repeatable, and must not be repeated.** It rests on there being no stable
  release, which stops being true at 1.0.0. The restored tag-protection ruleset is what enforces that
  in practice: a later rewrite would have to lift it deliberately, and that moment is the one to
  reread this record. Nothing else in the repository detects that the argument has expired.

## Follow-up Actions

* Unlist **and** deprecate on nuget.org every version published from the pre-rewrite history, with a
  message stating what it is and that source-stepping no longer resolves:
  *"Preview published before the repository's history was rewritten. Superseded — this version is
  unsupported and will receive no fixes. Source-stepping (SourceLink) does not resolve for it."*
  The set is every version published before this decision: `JustDummies 0.1.0-preview.1` and
  `1.0.0-preview.1`, `JustDummies.Xunit 1.0.0-preview.1`, and `JustDummies.DiagnosticCatalog
  1.0.0-preview.2`. Unlisting alone would not be enough: an unlisted version still restores by exact
  version, so the deprecation is what actually reaches a consumer holding one.
* Re-enable the tag-protection ruleset, `main`'s branch protection and the release workflow, all
  three suspended for the rewrite. They are repository settings and cannot be committed here; this
  line records that they must be restored. Restoring the ruleset is the step that ends this
  decision's exception, so it is the one that must not be forgotten.
* ADR-0048's follow-up note is **not** amended. It argues that moving a published tag breaks the
  artifact-to-commit link, and it goes on governing every tag from here. This decision is the single,
  bounded exception to it, taken while no stable release exists and taken precisely so the note can
  be enforced afterwards without a pre-1.0 history contradicting it.

## References

* [ADR-0051](0051-land-pull-requests-by-rebase.md) — the linear shape this decision applies
  retroactively.
* [ADR-0048](0048-publish-only-from-a-commit-that-is-on-main.md) — the ancestry check, and the
  tag-protection note this decision reads narrowly.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — the preference for a
  bounded operation whose correctness is checked rather than hoped for.
* [ADR-0045](0045-renumber-the-decision-base.md) — the renumbering whose explanation assumed the git
  history had never been rewritten; corrected by this change.
