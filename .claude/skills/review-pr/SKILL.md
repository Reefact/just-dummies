---
name: review-pr
description: Review a JustDummies pull request to this repository's contract — Conventional Comments format, the nine labels and their blocking status, what to report and in what order, and what never to report. Use when asked to review a pull request, review a diff, or leave review comments on a branch.
---

# Reviewing a pull request

This is the repository's review contract. It is mandatory, not stylistic: a comment that
does not follow it is noise in a thread the maintainer has to read.

## Work the delta

Review what the pull request changed. `git diff origin/main...HEAD`, the changed-file list,
and any previous findings on the same branch come before any repository-wide scan. Scan
wider only when the change's nature demands it.

## Output format — mandatory

Every inline comment uses exactly this shape, with nothing around it:

```text
<label> [(decorations)]: <subject on one line>

<optional discussion>
```

`< >` marks a placeholder to replace and `[ ]` marks an optional part — write neither the
angle brackets nor the square brackets literally. Decorations, when present, go in
parentheses (for example `(security)`).

* The entire comment is written in **English** — label, decorations, subject and discussion.
  Code identifiers, API names and exception messages are quoted verbatim.
* Never publish an unlabelled comment.
* Exactly **one label** and **one independent finding** per comment. At most two decorations.
* Do **NOT** add a severity/priority prefix — no `P0`, `P1`, `P2`, `P3`, `critical`, `major`,
  `minor`, anywhere in the comment. Blocking status is carried only by the label and the
  `(blocking)` / `(non-blocking)` decoration.
* No introduction or conclusion around the comment. Place it on the smallest relevant code
  range. Do not repeat the same finding on multiple lines.

Canonical example:

```text
issue (correctness): The redraw loop can exit without satisfying the declared exclusion.

`AnyString.Excluding` redraws while the candidate is excluded, but the bounded-redraw guard
returns the last candidate when the budget runs out instead of throwing. A generator that
cannot honour its constraint must fail loudly (ADR-0012), not hand back a value that violates
the invariant the caller declared.

Raise `AnyGenerationException` when the budget is exhausted, as the collection path does.
```

## Labels (one per comment)

* `issue:` confirmed defect that must be addressed — *blocking*.
* `todo:` small, obvious, local, non-debatable required change — *blocking*.
* `chore:` mandatory process step before merge; name the command/file — *blocking*.
* `question:` code looks suspicious but evidence is insufficient to assert a defect — *non-blocking*.
* `suggestion:` concrete optional improvement (never for incorrect code — use `issue:`) — *non-blocking*.
* `nitpick:` purely subjective, optional preference; should be rare — *non-blocking*.
* `note:` relevant information, no change expected — *non-blocking*.
* `thought:` design/architecture observation out of scope; must state no change is required here — *non-blocking*.
* `praise:` genuinely good and worth preserving; explain what and why — *non-blocking*.

Override a default only when the finding genuinely differs, e.g. `suggestion (blocking):` or
`issue (non-blocking):`. Never restate a default (`issue (blocking):`,
`nitpick (non-blocking):`).

Allowed decorations: `(blocking)`, `(non-blocking)`, `(if-minor)`, `(security)`, `(perf)`,
`(test)`, `(archi)`. One normally, never more than two.

## What to report, in priority order

Correctness → security → data integrity → regressions → public API / compatibility →
concurrency / reliability → significant performance → missing tests for a demonstrated risk →
violations of an explicit repository rule (e.g. a value object converted to `struct`).

**Do NOT report:** formatter-enforced style; an issue already flagged by a `JDxxx` analyzer
or by the Sonar profile; naming already enforced by tooling; speculative problems with no
execution path; broad refactors unrelated to the pull request; personal style presented as a
requirement; pre-existing issues the pull request does not materially affect.

If there is no relevant finding, approve without manufacturing comments.

## What this repository will care about

* A generator that cannot honour a declared constraint must **fail loudly**, never hand back
  a value that violates it (ADR-0046). Correctness is never what gets bounded.
* A `[ValueObject]` type turned into a `struct`.
* A public surface change with no matching `PublicAPI/<tfm>/` baseline update.
* A change to a diagnostic id's semantics, a platform floor, or a dependency policy with no
  ADR — say so as a `chore:` naming the `adr-check` procedure.
* An analyzer rule changed without its four companions (message, `AnalyzerReleases`, the
  `JDxxx.{en,fr}.md` pages, the README table).
* An English page changed without its French twin.

## Final summary

Keep it concise. Report only: the number of blocking findings, the number of non-blocking
findings, and the main risk areas. Do not restate every inline comment. If nothing was found,
state clearly that no blocking issue was found. The summary is **not** a Conventional Comment
and needs no label.

Do not merge the pull request or enable auto-merge on it.
