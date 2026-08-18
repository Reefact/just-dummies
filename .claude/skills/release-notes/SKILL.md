---
name: release-notes
description: Draft or refresh a JustDummies GitHub release note — a product-facing rewrite of a train's CHANGELOG.md section, cumulative per train and per major version, EN and FR. Use when asked to draft, write, or update release notes, or before tagging a release.
---

# Release notes

**A release note is not a changelog.** `CHANGELOG.md` is the technical, cumulative record —
Keep a Changelog categories, full justification, ADR links, exact behaviour. A release note is
the announcement for **one** version: what a developer gets, in plain language, short enough to
scan before deciding to upgrade. Never edit `CHANGELOG.md` from this skill, and never let a
release note invent a claim the changelog does not already support — the changelog is drafted
first (see the `release-train` skill), reviewed by a human, and is the only source this skill
reads from.

## Where it lives

One pair of files per train, per **major** version, at the package root beside `CHANGELOG.md`:

| Train | English | French |
|---|---|---|
| lib | `JustDummies/RELEASE_NOTES-1.x.en.md` | `JustDummies/RELEASE_NOTES-1.x.fr.md` |
| xunit | `JustDummies.Xunit/RELEASE_NOTES-1.x.en.md` | `JustDummies.Xunit/RELEASE_NOTES-1.x.fr.md` |
| cli | `JustDummies.Cli/RELEASE_NOTES-1.x.en.md` | `JustDummies.Cli/RELEASE_NOTES-1.x.fr.md` |
| catalog | `JustDummies.DiagnosticCatalog/RELEASE_NOTES-1.x.en.md` | `JustDummies.DiagnosticCatalog/RELEASE_NOTES-1.x.fr.md` |

When a train opens its `2.0.0`, start a new pair — `RELEASE_NOTES-2.x.en.md` /
`.fr.md` — rather than appending to the `1.x` file; the closed one stops changing. This mirrors
the `NNNN-slug.md`/`.en.md`/`.fr.md` pairing `JustDummies.Documentation.UnitTests` already
enforces (`DocumentationCorpus.ReadPages()` discovers every `RELEASE_NOTES-*.en.md`/`.fr.md` at
each package root — heading skeleton, fence order and markers must match between the two
languages, exactly as for every other paired page in this repository. See the `documentation`
rule).

## Format

```
# Release notes — <human package name>, 1.x

<one paragraph: which train, where CHANGELOG.md for the full technical record is linked>

## <version> — <Month> <day>, <year>

_<optional one-line theme, the way a maintainer would summarise it to a user deciding
whether to upgrade — omit rather than force one>_

### ⚠️ Breaking changes
### ✨ New
### 🙌 Improvements
### 🐛 Bug Fixes
### 🗑️ Deprecated
```

Rules:

* **Newest version first.** Only published versions get a section — never `[Unreleased]`; a
  release note describes what shipped, not what is pending.
* **Keep only the categories that have content.** Delete the empty ones; do not print an empty
  heading.
* **No `Refused, on purpose` section, and no house category beyond the five above.** The
  changelog's own `### Refused, on purpose` (ADR-0046: what was denied by design, not a bug) is
  real and worth keeping there — but a product announcement never lists what it declined to
  build, and neither should this: an unbounded "here's what we said no to" invites exactly the
  sprawl a release note exists to avoid. A refusal directly relevant to a
  bullet just introduced (a new flag's rejected spelling, say) can earn a short clause on that
  bullet; it does not earn a section. A changelog may also carry other house sections
  (`### Requires`, `### Notes`); fold their user-facing part into the paragraph under the version
  heading rather than inventing a category for each.
* **One bullet, one sentence you could read aloud.** The changelog already opens most of its
  bullets on a bolded outcome clause — *"See what your constraints left of a pool you
  supplied."* — that sentence, or a light trim of it, **is** the release note bullet. Drop the
  paragraph of justification, the edge cases, the "why" that belongs in the changelog. Keep an
  `(ADR-NNNN)` link only when a reader deciding to upgrade would plausibly click it — a breaking
  change or a new capability worth the detour, not routine packaging.
* **Invent nothing beyond what the changelog states**, and translate no number, no flag name, no
  type name — `--entry-point`, `IPoolInspection<T>`, `JD029` travel unchanged into French text.
* **Calm, not marketing.** No superlatives the changelog itself does not use — a product
  announcement, not a press release.
* **Every link is a full `https://github.com/Reefact/just-dummies/blob/main/...` URL, never a
  relative one.** This file is read two ways the changelog is not: as a file in the repository
  (where a relative link works) and pasted verbatim into a GitHub Release body, which has no
  directory of its own — a relative `../doc/handwritten/...` or a bare `CHANGELOG.md` resolves
  to nothing there. `JustDummies.Documentation.UnitTests`' link check deliberately skips
  absolute URLs (same reason it skips every external link), so get these right by hand: check
  the path against the real file before writing the link.
* **One physical line per paragraph and per bullet — never hard-wrap prose inside a list
  item.** `CHANGELOG.md` wraps at a fixed column, and that is fine there: a repository *file*
  renders standard CommonMark, where a lone newline inside a paragraph collapses to a space. A
  GitHub *Release body* is "user content" like an issue or a pull-request comment, and GitHub
  renders a lone newline there as a literal line break — the same source that reads as flowing
  prose in `CHANGELOG.md` reads as a sentence chopped mid-clause once pasted into a release. Let
  the editor soft-wrap on display; do not insert the newline into the file yourself.

## Refreshing after a new release

1. Read the newly-released section of `CHANGELOG.md` for the train (never `[Unreleased]` — only
   what a tag now covers).
2. Condense it into the format above, prepended under the file's most recent major-version
   heading (or start a new major-version file — see above).
2. Write the English file, then produce the French translation with the **same heading depths,
   in the same order** — `TranslationParityTests` checks the skeleton, not the prose, but a
   missing or reordered section still fails it.
3. `tools/packaging/release-notes.sh` reads the matching version's section straight from this
   file when a train is tagged — it refuses the release rather than falling back to a commit
   list if the section is missing, so write it **before** the tag is pushed, not after.
