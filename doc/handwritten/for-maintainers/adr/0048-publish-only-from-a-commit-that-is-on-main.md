# ADR-0048 | Publish only from a commit that is on main

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0048-publish-only-from-a-commit-that-is-on-main.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-01
**Accepted:** 2026-08-01
**Decision Makers:** Reefact

## Context

A release is cut by pushing a train-prefixed tag (`lib-v*`, `xunit-v*`, `cli-v*`), which triggers
`.github/workflows/release.yml`. That workflow checks out **the tagged commit** — its checkout step
declares no `ref:`, so the ref that triggered the run is the ref that is built — packs it, and pushes
the result to nuget.org through trusted publishing.

`main` is protected and requires its checks. **Tags are not branches**, and branch protection does not
reach them. Nothing between a tag push and a publish established that the tagged commit had been
reviewed or checked.

The commit need not even be one anyone has seen: `git push origin <tag>` carries the objects the tag
requires, so a commit that exists only in a local clone arrives with the tag and is published from.

The workflow does re-run `dotnet build` and `dotnet test` on the tagged commit, but that is a subset of
what protects `main`. The .NET Framework 4.7.2 floor, the Roslyn analyzer floor, the packaged-asset
compatibility check, CodeQL and the Sonar analysis run on pull requests and not in the release path.

A published version on nuget.org is immutable. It can be unlisted; it cannot be corrected. The first
release, `JustDummies 0.1.0-preview.1`, has already demonstrated the consequence in a smaller way: it
shipped carrying another product's icon, and the fix required a new version rather than a correction.

## Decision

A release publishes only from a commit that is an ancestor of `main`; the release workflow verifies
this before packing and refuses otherwise.

## Rationale

**The cost is asymmetric, so the check belongs before the publish.** Everything else in this pipeline
is recoverable by pushing another commit. A wrong publish is not: the version is spent, and the only
remedy is another version. A guard whose failure mode is "the release stops and you tag again" is
cheap against that.

**Ancestry of `main` is a sufficient proof, not an approximation.** `main` is protected and requires
its checks, so a commit reachable from `main` provably went through them. The check does not need to
know *which* checks ran or what they concluded — it borrows the guarantee the branch already carries.

**It is a bounded check that refuses loudly, which is what this repository prefers** (ADR-0046).
Interrogating the tagged commit's check runs through the API would be more precise, would need a token
and a reachable API, would have to decide what a missing or skipped check means — and would reach the
same verdict in every case that matters. The cheap version is not a compromise here; it is the
proportionate one.

**The likely failure is a mistake, not an attack.** Tagging an old commit, tagging before a merge
completes, tagging a local branch believed to be up to date: these are ordinary slips, and they are
exactly what an ancestry check catches. That it also blocks a deliberate publish from an unreviewed
commit is a second benefit, not the premise.

**It answers a different question from tag protection, and both are wanted.** A GitHub tag-protection
rule restricts *who* may create a release tag. This decision restricts *what* may be published. Neither
implies the other, and only the second can be expressed in the repository.

## Alternatives Considered

### Rely on a GitHub tag-protection rule alone

Restricting tag creation to maintainers is worth doing and is not in conflict with this record.
Rejected as sufficient: it constrains who pushes the tag, not where the tag points. The maintainer
entitled to release is precisely the person able to tag the wrong commit by accident.

### Verify the tagged commit's check runs through the GitHub API

The direct reading of "did this commit pass?". Rejected: it needs a token and a reachable API inside
the publish path, it must take a position on skipped, stale and missing checks, and for every commit on
`main` it returns what ancestry already establishes. More moving parts, same answer.

### Re-run the full check suite inside the release workflow

Rejected: it duplicates the pipeline, lengthens the publish, and still proves nothing about review — a
commit can pass every check and never have been looked at.

### Accept the risk

Defensible while the repository has one maintainer. Rejected because the artifact is immutable and the
guard costs one `git fetch`, so the trade is not close.

## Consequences

### Positive

* A tag on a commit that never reached `main` stops the release instead of publishing it.
* The release path inherits `main`'s protection without duplicating it.
* The failure message says what to do — tag a commit that is on `main` — rather than reporting an
  internal condition.

### Negative

* Publishing from a branch is no longer possible, including for a hotfix. That is the decision, not an
  oversight: the fix goes through `main` first.
* `workflow_dispatch` is exempt, since its ref is already a branch rather than a tag. A manual dispatch
  from a non-`main` branch therefore remains possible for whoever can dispatch it.

### Risks

* The guard's premise is external to it. If `main`'s protection were relaxed or its required checks
  removed, ancestry would still pass while proving much less. Nothing in this repository detects that;
  it is a property of the repository settings.
* A future GitHub change to how a tag-triggered checkout configures its remote could break the fetch
  the check depends on. It fails closed — the step errors rather than skipping — so the failure would
  stop a release rather than let one through.

## Follow-up Actions

* Consider a GitHub tag-protection ruleset on `lib-v*`, `xunit-v*` and `cli-v*`. It is a repository
  setting, complementary to this record, and cannot be committed here.
  * 2026-08-01 — done. A ruleset covering those three patterns now restricts the creation, update and
    deletion of release tags. It answers a different question from the check recorded here — *who may
    tag* rather than *what may be published* — and its update and deletion rules cover a window this
    record does not: the ancestry check runs when the tag is pushed and says nothing about the tag
    being moved afterwards, which would break the link between an immutable published artifact and the
    commit its tag names. The setting lives in the repository configuration and is not visible from the
    sources, so this line records that it was applied; it does not verify that it still is.

## References

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — the preference for a
  bounded check that refuses loudly over a more capable mechanism.
* [ADR-0047](0047-declare-the-adapters-library-dependency-independently.md) — the other release-path
  decision made alongside this one.
* `.github/workflows/release.yml` — where the check is applied, and why each of its two mechanics is
  load-bearing.
