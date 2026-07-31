# Migration record — extraction from `Reefact/first-class-errors`

This directory is the audit trail of the one-off extraction that created this repository. It exists so the
rewritten history can be reconciled with the source repository later, by a maintainer who was not there.
Decision: [ADR-0068](../adr/0068-extract-justdummies-into-its-own-repository.md).

## Facts

| | |
| --- | --- |
| Source repository | `Reefact/first-class-errors` |
| `SOURCE_CUTOFF_SHA` | `fbf523b86acebdd34ba0bbfd437683864be3cb9c` (its `main` at extraction time) |
| Target repository | `Reefact/just-dummies` |
| `TARGET_ORIGINAL_SHA` | `ef85c8ffcb2cc6696a78d000cbe1cbc5027719dd` (its single `Initial commit`, LICENSE only) |
| Backup of the original target | branch `archive/pre-history-extraction` |
| Tool | `git filter-repo` 2.47.0, run on a fresh clone, never on a working clone |
| Commits before / after | 1350 → 420 |
| Merge commits preserved | 156 |
| Extraction date | 2026-07-31 |

The source repository was never force-pushed and never committed to directly by this migration.

## Files here

| File | What it is |
| --- | --- |
| `filter-repo-paths.txt` | the exact path specification the filter ran with, comments included |
| `commit-map.txt` | old SHA → new SHA for all 1350 source commits (`0000…` = commit dropped) |
| `ref-map.txt` | old ref → new ref |
| `suboptimal-issues.txt` | `filter-repo`'s report of commit hashes referenced in messages that no longer exist |
| `nuget-trusted-publishing.md` | the manual nuget.org setup the release workflow needs (not done yet) |

## How the boundary was decided

A path list built from directory names alone would have been wrong three times over, so the boundary was
derived from the history itself:

* **Renamed paths.** The product was called `Dummies` before it was called `JustDummies`. The filter
  therefore lists `Dummies/`, `Dummies.UnitTests/`, `Dummies.Xunit/`, `Dummies.Xunit.UnitTests/`,
  `tools/dummies-check/`, `.github/workflows/dummies.yml` and
  `specifications/dummies-generation.{en,fr}.md` alongside their current names. Omitting them would have
  truncated the history at the rename.

* **Renumbered ADRs.** Four decisions changed number in place — `0010→0011`, `0043→0044`, `0048→0049`,
  `0050→0051` — and one was a draft that was created and dropped
  (`0023-prune-the-exotic-width-numeric-generators`, commit *"docs(dummies): drop ADR-0023 draft"*). The
  specification lists **every path each decision ever occupied**, matched by slug rather than by number.

* **Files with no "dummies" in their path.** The 56 analyzer documentation pages
  (`doc/handwritten/for-users/analyzers/JD001…JD028.{en,fr}.md`) and roughly 25 ADRs about the `Any`
  generation engine carry no such marker. They were found by reading every ADR title and by grepping file
  *contents*, not paths.

Note that `git log -- <path>` applies history simplification and silently reports nothing for some of these
paths; `git log --full-history` or a raw scan of every commit's tree is needed to see them. `filter-repo`
itself does not simplify, so the listed paths are matched regardless.

## What was deliberately left behind

| Kept in `Reefact/first-class-errors` | Why |
| --- | --- |
| ADR-0026 *Rebase the testing package's arbitrary values on JustDummies* | its subject is `FirstClassErrors.Testing` |
| ADR-0061 *Run the JustDummies analyzers on the repository's own code* | its subject is that repository's build |
| ADR-0006 *Supply arbitrary test values from a single seedable source* | its subject is the `FirstClassErrors.Testing` companion package |
| `icon.png` history | its only two commits are FirstClassErrors-motivated; the file was copied in the bootstrap commit instead, so an unrelated commit would not become this repository's root |

ADR-0011 and ADR-0022 bind both products and therefore exist in **both** repositories.

## Infrastructure that was rewritten, not extracted

`Directory.Build.props`, `Directory.Packages.props`, `build/PublicApiBaseline.props`,
`build/Net472TestFloor.props`, `JustDummies.sln`, `tools/trains.sh`, `tools/packaging/pack.sh` and most
workflows contained substantial FirstClassErrors-specific content. They were recreated in the bootstrap
commit rather than carried over, so this repository's build describes this repository.

`build/sonar-profile.globalconfig` is the one file carried byte-identical: it is generated from a SonarCloud
quality profile, and keeping it unchanged keeps the build emitting the same rule set it did before the
extraction. Regenerate it from this repository's own SonarCloud project once that project exists.

## Known follow-ups

* **nuget.org trusted publishing is not configured** — see `nuget-trusted-publishing.md`. `release.yml` and
  `release-dryrun.yml` fail at the OIDC exchange until it is.
* **Sonar, Scorecard and the Claude-driven workflows** (`adr-check`, `changelog`, `dependabot-autofix`) were
  ported as-is and need `SONAR_TOKEN` / `ANTHROPIC_API_KEY` plus a SonarCloud project keyed
  `reefact_just-dummies`. They are red until then, by explicit choice: porting them keeps the intent visible
  rather than quietly dropping it.
* **`tools/analyzer-count-check` was not ported.** It asserts that a README advertises the right number of
  analyzers; `JustDummies/README.nuget.md` makes no such claim, so the check had no invariant to guard here.
  Re-add it if the README starts advertising the 28 rules.
* **One source branch carries unmerged JustDummies work**: `agent/extract-adr-specifications` in
  `Reefact/first-class-errors` (31 commits ahead of its `main`, 4 JustDummies files touched). It was not
  included in the cutoff and must be recreated here by hand if it is still wanted.
