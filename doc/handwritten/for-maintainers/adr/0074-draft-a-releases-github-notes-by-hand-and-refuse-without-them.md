# ADR-0074 | Draft a release's GitHub notes by hand from the changelog, and refuse without them

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0074-draft-a-releases-github-notes-by-hand-and-refuse-without-them.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-18
**Accepted:** 2026-08-18
**Decision Makers:** Reefact

## Context

JustDummies publishes four independently-versioned release trains (`tools/trains.sh`). Each train
keeps a `CHANGELOG.md`, in Keep a Changelog format, drafted by the `changelog` GitHub Actions
workflow from merged pull requests and reviewed by a human in a pull request before it is merged
to `main`.

Until now, `tools/packaging/release-notes.sh` — invoked by `release.yml` at tag time, and
rehearsed by `release-dryrun.yml` on every push to `main` — built a GitHub Release's body directly
from `git log`: it walked the commits since the train's previous tag and kept the ones whose
Conventional Commit scope belongs to the train being released. [ADR-0013](0013-require-a-scope-on-the-version-driving-commit-types.md)
requires that scope on every version-driving commit precisely so this filter could partition
correctly.

That body is what a consumer reads on the repository's Releases page, and what NuGet's package
page links to — the only per-version, product-facing text this project ships, distinct from
`CHANGELOG.md`'s cumulative, technical record of every constraint and edge case. A commit subject
is written for a reviewer of that one diff, not for a developer deciding whether a new version is
worth adopting: a real past release listed `refactor(cli): guard through
ArgumentNullException.ThrowIfNull` beside `feat(cli): read project defaults the command line
overrides`, in the same list, with nothing distinguishing the one a consumer would care about from
the one they would not. The old script's own fallback — printing `_No user-facing changes in this
component._` when no commit matched — already conceded that a commit log answers "what changed for
the maintainer", not "what changed for you".

By the time a train's version is tagged, a text describing that release in product terms already
exists in the repository: the changelog section for that version, reviewed by a human before it
was merged. Nothing in `release.yml`'s pipeline read it.

## Decision

A published GitHub Release's notes are read verbatim from a committed, hand-drafted,
product-facing file — one per release train and major version — and `release.yml` refuses to
publish, rather than falling back to anything derived from commit history, when that file or the
version's own section inside it does not exist.

## Rationale

**Consumer-facing text and maintainer-facing text answer different questions.** A commit message
explains a diff to a reviewer; a release note explains a version to someone deciding whether to
upgrade. Deriving the second from the first, mechanically, conflated the two and served neither —
evidenced by the old script needing its own "no user-facing changes" escape hatch for the case
where the mechanical derivation had nothing useful to say.

**The source this decision reads from is not new invention.** The changelog section for a version
is already reviewed before it merges, and it is already written in product terms — most of its
bullets already open on a self-contained outcome sentence. Producing a release note from it is a
presentation step, not a request to originate content nobody has reviewed.

**Refusing on a missing file follows the precedent ADR-0013 already set.** There, an unscoped
commit is refused rather than guessed into a default train, because guessing produces a subtler,
harder-to-notice error than refusing. The same trade applies here: a release published with a
commit-derived placeholder looks like a release note and is not one, while a failed `release.yml`
run is loud, immediate, and points exactly at what is missing.

**Generation happens ahead of the tag, not against it.** The `release-train` skill already draws
the line that tagging and publishing are the maintainer's actions, prepared in advance. Keeping
this file's authorship out of `release.yml` entirely — no model call at tag time — keeps a release
of an immutable, published artifact from ever depending on an unreviewed generation step racing
the publish.

## Alternatives Considered

### Keep deriving from git log, and tighten the commit-message convention instead

Considered because it needs no new file and no new manual step. Rejected: a commit message is
bound to describe a diff, because that is what commit messages are for; no wording convention
turns `refactor(cli): guard through ArgumentNullException.ThrowIfNull` into product-facing prose
without inventing content the commit was never written to carry.

### Generate the release note in CI at tag time, the way the `changelog` workflow drafts a changelog

Considered because the pattern already exists and works well for the changelog: a model drafts,
a human reviews in a pull request, then it is merged. Rejected for a release specifically: the
changelog workflow can afford that review cycle because nothing has been published yet when it
runs. `release.yml` runs on a tag about to produce an immutable NuGet package and a permanent
GitHub Release — reaching that point with unreviewed prose about to become the release's public
text removes exactly the check the changelog workflow relies on. A future variant that drafts into
a reviewed pull request *before* the tag, the same way the changelog does, would not contradict
this decision — it would still produce the committed file this design reads; only the *manner* of
producing it was rejected here, not the possibility of assisting the draft.

### Keep the commit-derived list as a fallback when the hand-authored file is missing

Considered as a softer landing than refusing outright. Rejected: a fallback that silently produces
a worse artifact is what this decision turns away from. A missing release note should surface as a
gap to fill before a tag is pushed, not be quietly patched over by the mechanism it was meant to
replace.

## Consequences

### Positive

* A GitHub Release's body is legible to a developer deciding whether to upgrade, in the same
  register across all four trains.
* Commit history, the changelog and the release note each answer one question, instead of one
  artifact being asked to answer two questions poorly.
* A missing release note is caught by a failed release, before publishing, rather than discovered
  afterward on the Releases page.

### Negative

* Writing the release note is now a manual step — the `release-notes` skill — a maintainer or
  agent must remember to do before tagging; nothing in the repository still produces it
  end-to-end automatically the way the old script did.
* Two files must now stay in step: the changelog section and the release note drafted from it.
  Nothing enforces their agreement beyond the discipline the `release-notes` skill describes.

### Risks

* A tag pushed before the release note is written fails the release outright. Mitigation: the
  `release-train` skill's "before a release" checklist now lists drafting it ahead of the public
  API baseline step, and failing loudly here is the point, not a defect to route around.
* The release note can drift from the changelog it was drafted from, if the changelog changes
  after the note is written. Mitigation: the same risk `CHANGELOG.md` itself already carries
  between drafting and merge, addressed the same way — human review at drafting time, not an
  automated check.

## Follow-up Actions

* None required. `.claude/skills/release-notes/SKILL.md` carries the operative instruction,
  `tools/packaging/release-notes.sh` enforces the refusal, and `release-dryrun.yml` rehearses it
  against every train's latest published tag.

## References

* [ADR-0013](0013-require-a-scope-on-the-version-driving-commit-types.md) — the scope-based
  partition. Its Context and References still describe `tools/packaging/release-notes.sh` as
  selecting commits by scope; this record changes that mechanism without altering ADR-0013's own
  decision, which continues to govern `CHANGELOG.md` and stands unedited, as an accepted record
  must.
* [ADR-0051](0051-land-pull-requests-by-rebase.md) — its Consequences
  note that `tools/packaging/release-notes.sh` must keep filtering merge commits from history; that
  script no longer reads commit history at all, for the same reason: this record changes the
  mechanism, not ADR-0051's decision, which stands unedited.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — the refuse-loudly-
  rather-than-silently-degrade ethos this decision borrows for a missing release note.
* [ADR-0048](0048-publish-only-from-a-commit-that-is-on-main.md) — unaffected; still governs which
  commit a tag may point at.
* `.claude/skills/release-notes/SKILL.md`, `.claude/skills/release-train/SKILL.md` — where the
  format and the procedure live.
