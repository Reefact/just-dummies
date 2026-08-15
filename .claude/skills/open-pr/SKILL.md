---
name: open-pr
description: Prepare and open a pull request on JustDummies — the title convention, the template sections, honest testing claims, the architecture-decision box, and what an agent must never do. Use when the maintainer asks for a pull request, or when finalizing a branch for review.
---

# Opening a pull request

**Do not open a pull request unless the maintainer explicitly asks for one.** Pushing a
branch is not a request for a pull request.

**No agent merges a pull request or enables auto-merge on it.** `@reefact` merges.

## Before you open it

1. Run the **`adr-check`** skill and know your outcome — the template has a box for it.
2. Run the **`tidy-history`** skill. This repository lands by rebase, so every commit on the
   branch reaches `main` on its own. The hook blocks pull-request creation when the branch
   carries CI-fatal history (pending `fixup!`/`squash!`/`amend!`, or a header the linter
   rejects); softer mess is your judgement.
3. Have the tests you intend to claim actually run.

## Title

English, and it names the **whole** change (`CONTRIBUTING.md`, "Pull request titles"):

* a **single-intention** pull request mirrors its commit header — `type(scope): description`;
* a **multi-intention** pull request uses a short descriptive title, with no borrowed
  `type:` prefix;
* ≤ 72 characters;
* issue references go in the description, **never** in the title.

Nothing lints this — it stands on the review.

## Body

Follow [`.github/pull_request_template.md`](../../../.github/pull_request_template.md) and
fill the sections that apply. Its own header says it: **do not invent information**, and
delete a section only if it truly does not apply.

**Testing** — tick only what you actually ran. If you did not run something, say so
explicitly rather than leaving the box ambiguous. Two claims that are easy to make and wrong:

* **Never claim the pull request "passed the mutation bar".** Nothing enforces a mutation
  score (ADR-0025); the check reports and does not block. `justdummies.json` and
  `justdummies-analyzers.json` both set `break: 0`.
* The `JustDummies mutation gate` check being green means it ran, not that a threshold was
  met.

**Documentation** — the French twin box is real: every page changed has a twin that must
change with it.

**Architecture decisions** — tick exactly the outcome `adr-check` produced. For a conflict,
write `⚠️ Conflicts with ADR-NNNN (<title>)` with the precise contradiction.

**Related issues** — issue-closing keywords (`Closes #123`) belong here, in the description.
They never go in a commit message; a commit uses a `Refs: #NN` footer instead.

## Commits on the branch

Conventional Commits per `CONTRIBUTING.md`: a closed type list
(`feat, fix, build, chore, ci, docs, perf, refactor, revert, style, test`), the scopes
`core, analyzers, xunit, cli, catalog`, an imperative lowercase header within 72 characters
with no trailing period, and `Refs: #NN` in a footer when a GitHub issue exists. **Scope is
required on `feat` and `fix`** — it drives the release-train partition (ADR-0013), so it is
not decoration. Work with no product scope (agent tooling, repository chores) takes a type
without a scope.

Enable the local hook once per clone: `git config core.hooksPath .githooks`. The same check
runs in CI on every pull request.

## After it is open

Pushing further commits re-opens the history question — run `tidy-history` again. Responding
to review findings is the **`review-feedback`** skill.
