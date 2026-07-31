# ADR-0068 | Extract JustDummies into its own repository

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0068-extract-justdummies-into-its-own-repository.fr.md)

**Status:** Proposed
**Proposed:** 2026-07-31
**Decision Makers:** Reefact
**Supersedes:** [ADR-0011](0011-host-dummies-as-a-standalone-package.md) (the colocation half of it only — see below)

## Context

[ADR-0011](0011-host-dummies-as-a-standalone-package.md) decided two things at once, and only one of them is
being replaced here. It decided **what JustDummies is** — an independent package that must not reference any
FirstClassErrors project — and it decided **where it lives** — colocated in `Reefact/first-class-errors`,
to reuse that repository's CI, packaging, release, SBOM, SourceLink and governance infrastructure while the
API iterated quickly and its earliest consumers sat next door.

The first half held and is not in question. The second half was explicitly provisional: ADR-0011 rejected
"create a separate repository immediately" on cost grounds, not on principle, and recorded that the
no-reference rule exists precisely so that "a later repository extraction [stays] mechanical rather than
architectural."

The conditions that justified colocation have expired:

* The library has its own product surface — 28 first-party analyzers (ADR-0044), an xUnit v3 adapter
  (ADR-0039), a two-suite test bed (ADR-0040), a specified scaffolder, and a product website at
  `https://justdummies.io` that the packages already advertise as their `PackageProjectUrl`.
* Its release cadence is not FirstClassErrors'. Riding a shared tag namespace forced a `dum-v*` train
  whose only purpose was to avoid colliding with `lib-v*` and `cli-v*`.
* The colocation now costs the host: `FirstClassErrors.Testing` cannot express its dependency as a normal
  `PackageReference`, and instead carries a private `ProjectReference` plus a hand-written pack target that
  embeds `JustDummies.dll` inside its own `lib/` — a workaround ADR-0026 accepted only "until JustDummies
  is published."
* Issues, pull requests and CI runs for two unrelated products share one queue.

## Decision

JustDummies — the library, its analyzers, its xUnit adapter, its test bed, its documentation, its ADRs and
its specified scaffolder — lives in **`Reefact/just-dummies`**, which becomes the sole source repository for
the product. `Reefact/first-class-errors` becomes a consumer of the published packages.

The move preserves history: the repository's `main` was produced with `git filter-repo` from
`Reefact/first-class-errors` at `SOURCE_CUTOFF_SHA = fbf523b86acebdd34ba0bbfd437683864be3cb9c`, keeping every
author, date and commit message, and following the paths through their historical names.

## Consequences

### Commit hashes differ from the source repository

Filtering rewrites every commit, so no SHA here matches the SHA of the same change in
`Reefact/first-class-errors`. The full mapping is committed at
[`../migration/commit-map.txt`](../migration/commit-map.txt) (1350 entries), alongside the exact path
specification the filter ran with.

**Issue and pull-request references in historical commit messages (`#123`, `Refs: #229`) point at
`Reefact/first-class-errors`, not at this repository.** They were deliberately left untouched: rewriting them
would have falsified messages their authors actually wrote, and there is no correct target to rewrite them
to. Read any number in a commit dated before 2026-07-31 as a source-repository number.

### The `dum-v*` train is replaced by three trains

No `dum-v*` tag was ever pushed — the train existed in `release.yml` and was never used — so the rename
discarded nothing. This repository publishes on `lib-v*` (JustDummies), `xunit-v*` (JustDummies.Xunit) and
`cli-v*` (the `dum` scaffolder, wired ahead of the implementation).

Splitting the adapter onto its own train introduces a hazard the single train did not have:
`JustDummies.Xunit` carries a `ProjectReference` on `JustDummies`, so `dotnet pack` stamps its dependency at
the version being packed. Publishing `xunit-v0.2.0` while the library sits at `lib-v0.1.0` would ship an
adapter demanding a library version that was never released. `tools/packaging/pack.sh` refuses such a pack by
requiring the stamped dependency version to match an existing `lib-v*` tag.

### FirstClassErrors keeps its copy until the first publication

Nothing was deleted from `Reefact/first-class-errors` by this extraction. Four of its projects reference
JustDummies, and one of them — `FirstClassErrors.Testing` — *ships* it. Removing the source there requires a
restorable `JustDummies` package on nuget.org, which does not exist yet. The cutover is prepared, not
performed; the corresponding decision on that side supersedes ADR-0026's embedding workaround.

### Two decisions stay in FirstClassErrors

[ADR-0026](0026-rebase-testing-arbitrary-values-on-dummies.md) and ADR-0061 have FirstClassErrors as their
subject even though they concern JustDummies: one records why `FirstClassErrors.Testing` rebased its
arbitrary values on this library, the other why that repository runs these analyzers on its own code. They
are not carried here. [ADR-0011](0011-host-dummies-as-a-standalone-package.md) and
[ADR-0022](0022-floor-the-library-on-net-framework-4-7-2.md) are the reverse case — they bind both products —
so they exist in both repositories.

### ADR numbers are preserved, and the sequence has gaps

The ADRs that came along kept their numbers, so this repository's set is 0011, 0013, 0015, 0020, 0022, 0025,
0030–0033, 0035–0042, 0044, 0045, 0047–0054, 0058, 0059, 0063–0066. Renumbering them would have broken every
cross-reference in the accepted texts and in the source repository. New ADRs continue from 0068 — above
FirstClassErrors' highest — so a number never means two different decisions across the two repositories.

## Alternatives Considered

### Keep the colocation and publish from FirstClassErrors

Considered because it changes nothing and the infrastructure already works. Rejected because it preserves
every cost listed above — the embedded-DLL workaround, the shared tag namespace, the shared issue queue —
and each of them grows with the product rather than shrinking.

### Start the new repository from a squashed import

Considered because it is trivial and produces a clean root commit. Rejected because it discards the
attribution and the reasoning of 420 commits: the ADRs in this repository cite commits that explain *why* a
generator behaves as it does, and a squash would leave those citations dangling.

### Fork `Reefact/first-class-errors`

Rejected because a fork carries the entire FirstClassErrors history and working tree, and GitHub would keep
presenting the result as a derivative of a repository this product is meant to stand apart from.
