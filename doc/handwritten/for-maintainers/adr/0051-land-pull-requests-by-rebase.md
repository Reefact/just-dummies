# ADR-0051 | Land pull requests by rebase

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0051-land-pull-requests-by-rebase.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-07
**Accepted:** 2026-08-07
**Decision Makers:** Reefact

## Context

Until now this repository landed every pull request with a **merge commit**. `main` carries the
result: `Merge pull request #5` through `Merge pull request #9`, each bracketing the commits of the
branch it brought in.

That choice was never recorded as a decision. It lived as a premise in the prose instead, asserted in
seven places — `CONTRIBUTING.md` twice, `AGENTS.md`, `CLAUDE.md`, the `/tidy-history` command, the
`history-hygiene` hook and the commit linter — each restating "this repository merges with a merge
commit" to justify a rule that depends on it. Nothing tied them together, and nothing detected that
they had all become false at once.

Three rules argue from that premise today:

* **Tidying a branch before review is mandatory**, because a branch's commits reach protected history
  as they are (`AGENTS.md`, "Tidying history before a pull request").
* **A pull request title is read in three places**, one of them the `Merge pull request #NN` commit
  GitHub writes (`CONTRIBUTING.md`, "Pull request titles").
* **A branch is disposable**, because the merge commit preserves its history (`CONTRIBUTING.md`, "The
  doctrine").

A release is cut by tagging a commit, and the release workflow refuses to publish from a commit that
is not an ancestor of `main` ([ADR-0048](0048-publish-only-from-a-commit-that-is-on-main.md)). Under
a merge commit, a branch's own commits are ancestors of `main` once it lands, so tagging the branch
head published.

Dependabot pull requests are merged by a workflow rather than by hand
(`.github/workflows/dependabot-automerge.yml`), which names the merge method explicitly and cannot
fall back to another. Each such pull request carries exactly one commit, already conventional, with
its type imposed by `.github/dependabot.yml` — so a merge commit doubles the history a one-line
version bump writes.

The repository's merge-method setting is the enforcement point: GitHub refuses any method the
repository does not allow, whoever asks and however they ask.

## Decision

Pull requests land on `main` by **rebase**, and the repository allows no other merge method.

## Rationale

**A linear `main` is the shape the rest of the conventions already assume.** Every rule in
`CONTRIBUTING.md` puts the record on the *commit*: one intention per commit, a conforming header on
each, the scope that partitions release trains, the `Refs:` footer that ties a commit to its issue.
The merge commit added a second, weaker record on top — a title GitHub wrote, unlinted, carrying no
scope and therefore excluded from the release notes. Rebasing removes the layer that carried nothing
the commits did not already carry better.

**It sharpens the rule that matters most here, rather than weakening it.** Landing by rebase does not
make a messy branch cheaper; it makes it more expensive. Under a merge commit, a branch's commits at
least arrived bracketed — the merge commit marked where they began and ended, and a reader could skip
the range. Rebased, they arrive one by one on the line, indistinguishable from every other commit,
with nothing to mark them as one unit. The obligation to tidy a branch *before* it lands therefore
gains force under this decision, which is why the rules that state it are restated rather than
relaxed.

**The alternative that hides mess is the one worth refusing.** Squashing would also produce a linear
`main`, and would do it by discarding the per-commit record the conventions are built on. That is the
opposite trade: it would make tidying history cosmetic instead of load-bearing, and it would collapse
a pull request carrying a feature, the refactor that prepared it and its tests into one commit whose
type cannot honestly name it — the very situation `CONTRIBUTING.md` cites to explain why a *branch*
has no type.

**Naming one method, and allowing only that one, is what makes the decision hold.** The repository
setting refuses the others outright, so no contributor and no workflow can land a pull request a
different way by habit or by accident. The premise the prose argues from is then enforced by the
platform rather than asserted by seven paragraphs.

## Alternatives Considered

### Keep the merge commit

The status quo, and the premise the documentation was written against: keeping it would have cost
nothing to write.

Rejected because the merge commit's only unique contribution to `main` is the pull request title, and
that title is unlinted, scope-less and absent from the release notes — a record the commits already
keep, kept worse. On Dependabot pull requests, which land without a human touching them, it doubles
the history a one-line version bump writes.

### Squash and merge

It produces the same linear `main`, and it forgives a messy branch: whatever the branch carried
arrives as a single commit.

Rejected because that forgiveness is the defect. This repository's conventions record the change on
the commit — one intention each, a conforming header each, a scope that decides which release train
publishes it. Squashing collapses all of that into one commit whose single type cannot name a pull
request that legitimately carries several, and it would turn the tidy-history rule from a requirement
into a nicety.

### Allow every method and choose per pull request

The most flexible option: a merge commit for a wide feature branch, a rebase for a one-commit bump.

Rejected because a merge method chosen case by case is a merge method nobody can rely on. The rules
in `CONTRIBUTING.md` and `AGENTS.md` argue from what happens to a branch's commits when it lands; if
that answer varies per pull request, none of them can state the consequence, and the tidy-history
obligation becomes conditional on a choice made after the branch is already written.

## Consequences

### Positive

* `main` becomes a single line of conventional commits, each linted, scoped and readable on its own.
* The tidy-history rule gains its strongest justification: nothing brackets a branch's commits any
  more, so nothing hides a mess.
* A Dependabot version bump costs `main` exactly one commit, the one Dependabot wrote.
* The merge-method setting enforces the premise the documentation argues from, so the two cannot
  drift apart again.

### Negative

* The pull request title no longer appears anywhere in `main`'s history; the request's identity lives
  in the pull request itself and in its commits.
* A branch's commit hashes never reach `main` — the rebase replays them as new commits — so a hash
  read on a branch cannot be cited as a commit on `main`.
* `main` keeps the merge commits of the pull requests that landed before this decision. Tools that
  read history must go on filtering them; `tools/packaging/release-notes.sh` and the commit-lint CI
  job already do.

### Risks

* **Tagging a branch head no longer publishes.** Since a branch's commits are not ancestors of `main`
  after landing, [ADR-0048](0048-publish-only-from-a-commit-that-is-on-main.md)'s check refuses a tag
  placed on the branch rather than on the commit `main` received. This is the check working as
  designed, but it turns a previously harmless habit into a refused release.
* **A branch that merged `origin/main` into itself carries merge commits into the rebase.**
  `CONTRIBUTING.md` permits that merge once a branch is shared. How GitHub replays such a branch is
  its own behaviour, not this repository's, and is worth confirming on the first shared branch that
  needs it.

## Follow-up Actions

* Allow only rebase merging in the repository's pull request settings — the enforcement point of this
  decision.
* Confirm on the first shared branch that merged `origin/main` in that its landing behaves as
  expected.

## References

* [ADR-0048](0048-publish-only-from-a-commit-that-is-on-main.md) — publishing requires the tagged
  commit to be an ancestor of `main`, which this decision makes stricter in practice.
* [ADR-0013](0013-require-a-scope-on-the-version-driving-commit-types.md) — the scope on a commit
  decides which release train publishes it, one of the per-commit records squashing would have cost.
* `CONTRIBUTING.md` ("Branches", "Pull request titles"), `AGENTS.md` ("Tidying history before a pull
  request") — the rules restated against this premise.
