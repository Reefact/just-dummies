---
name: release-train
description: Cut or prepare a JustDummies release — the four independent trains, the changelog, the public API baseline, packaging and publication, and adding a new train. Use when asked to release, tag, publish, cut a version, draft a changelog, or check release readiness.
---

# Release trains

Four packages version **independently**, each owning a tag prefix, a set of Conventional
Commit scopes, a NuGet label and a changelog file:

| Train | Tag prefix | Scope(s) | Changelog |
|---|---|---|---|
| library | `lib-v*` | `core` | `JustDummies/CHANGELOG.md` |
| xUnit adapter | `xunit-v*` | `xunit` | `JustDummies.Xunit/CHANGELOG.md` |
| diagnostic catalogue | `catalog-v*` | `catalog` | `JustDummies.DiagnosticCatalog/CHANGELOG.md` |
| `dum` tool | `cli-v*` | `cli` | `JustDummies.Cli/CHANGELOG.md` |

**`tools/trains.sh` is the single source of truth for that mapping.** It is *sourced*, never
executed. Read it before assuming anything about the partition — `release-notes.sh` and
`collect-prs.sh` both source it precisely so the two can never disagree.

A commit with **no scope** belongs to no train and is dropped from release notes. That is why
scope is required on `feat` and `fix` (ADR-0013).

## Before a release

1. **Changelog.** The `changelog` workflow (dispatch) drafts a train's `[Unreleased]` from its
   merged pull requests and opens a review pull request. Prefer it to editing by hand.
2. **Release notes.** Once the changelog section for the version being cut is reviewed and
   merged, draft or refresh `RELEASE_NOTES-<major>.x.en.md`/`.fr.md` for the train from it — the
   `release-notes` skill. `tools/packaging/release-notes.sh` reads this file at tag time and
   refuses the release rather than falling back to anything else if the section is missing, so
   write it **before** the tag, not after.
3. **Public API baseline.** `RS0016`/`RS0017` track the surface of `JustDummies` and
   `JustDummies.Xunit` in committed `PublicAPI/<tfm>/` files; each target framework has its
   own, and `dotnet format` rewrites one per run. **Do not promote `PublicAPI.Unshipped.txt`
   into `Shipped.txt` at a preview** — only at the first STABLE release. Promoting early turns
   every later removal into a violation, and below 1.0 this library keeps the right to remove.
   The same holds for `AnalyzerReleases.Unshipped.md`. Full procedure: `CONTRIBUTING.md`,
   "Public API baseline".
4. **A draw sequence may change below 1.0** — a generated value's relationship to its seed is
   not a versioned contract yet. Say so in the changelog when it does.
5. **Rehearse.** `release-dryrun` packs and builds the SBOM without publishing.

## Packaging

`tools/packaging/pack.sh` is the one way a published package is produced — `release` and
`release-dryrun` both call it. It packs with `--no-build` into `./artifacts`, embeds and
verifies the SBOM, and carries guards that must not be weakened:

* the `cli` package declares **no JustDummies dependency** in its nuspec **and** bundles no
  `JustDummies.dll` beside the tool (ADR-0063). Both checks are needed — a .NET tool ships its
  closure as files, so the nuspec check alone passes on an empty dependency list;
* `JustDummies.Xunit`'s declared dependency on `JustDummies` must match a published `lib-v*`
  tag; publishing one that matches none is `NU1102` for the consumer, on an immutable artifact
  (ADR-0047).

## Publishing

The `release` workflow triggers on a train's tag, packs, attests and publishes to NuGet
without a stored API key — see
[`nuget-trusted-publishing.en.md`](../../../doc/handwritten/for-maintainers/workflows/nuget-trusted-publishing.en.md).
**Tagging and publishing are the maintainer's actions.** Prepare the release; do not push a
release tag on your own authority.

## Adding a new train

One data edit — a row in `trains_rows()` in `tools/trains.sh` — then the static edits GitHub
and the tooling force (tag trigger, workflow choice options, commit-lint scopes, packaging).
The full checklist, with what happens on its own and how to verify, is
[`AddingAReleaseTrain.en.md`](../../../doc/handwritten/for-maintainers/AddingAReleaseTrain.en.md).
Adding a scope also means updating `SCOPES` in `tools/commit-lint/lint-commit-message.sh` and
the scope list wherever it is documented.

## Where each train stands

`cli` has published up to `cli-v1.1.0-beta.1`. A tool takes no public-API baseline: what a
version commits to there is the command line, not a set of types — which is why its 1.0.0 is a
**beta** rather than a preview. Check `git tag` for the others rather than assuming.
