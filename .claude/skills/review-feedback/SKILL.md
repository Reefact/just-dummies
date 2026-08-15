---
name: review-feedback
description: Respond to review findings on a JustDummies pull request — which of the three routes each finding takes, how to reply, when to escalate to the maintainer, and the two-cycle cap. Use when acting on a Codex or human review, addressing review comments, or replying on a pull-request thread.
---

# Responding to review feedback

This governs the agent that *fixes* a pull request in response to a review, not the reviewer
(that is the `review-pr` skill). The human maintainer **`@reefact` is the only authority that
merges**; no agent merges, and no agent enables auto-merge on its own pull request.

## For each finding, take exactly one route

* **You agree, and the fix is clear and local** — implement it, push, and reply on the thread
  with `Resolved in <sha>`. You MAY ask the reviewer (`@codex`) for a single confirming
  re-review; never open a back-and-forth.
* **You believe the finding is wrong** — reply on the thread with the concrete technical
  reason and mention `@reefact` to arbitrate. Do **not** ping `@codex` to argue: a peer
  reviewer has no authority to settle the disagreement.
* **The finding needs a human judgement** — architecture, a product trade-off, an ambiguous
  requirement, a security or compatibility policy — mention `@reefact` and wait. Do not
  decide unilaterally.

## Rules

* **Never mention both `@codex` and `@reefact` on the same thread**: a bot round-trip or a
  human decision, never both.
* **At most two fix / re-review cycles per finding.** If it is still open after that, stop and
  mention `@reefact` instead of continuing.
* Keep replies short and factual; the diff is the record.
* Be frugal about posting. Reply when a round resolves the finding, hits a real blocker, or
  raises a question — do not narrate each fix.

## After pushing the fixes

* Pushing further commits re-opens the history question — run the **`tidy-history`** skill.
  A commit that only fixes an earlier commit of the same branch ("address review") is exactly
  what it flags.
* If a fix changed the nature of the change — a new public API contract, a platform floor, a
  policy — re-run the **`adr-check`** skill; the outcome recorded in the description may no
  longer be true.
* Do not merge, and do not enable auto-merge.

`AGENTS.md`, "Responding to review feedback", carries the same rules for other agents.
