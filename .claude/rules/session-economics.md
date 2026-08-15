<!-- No `paths:` on purpose: this rule loads every session, like CLAUDE.md itself.
     That cost is deliberate and is why it stays this short. Keep it under ~40 lines. -->

# Session economics

Help the maintainer spend the least capability that can confidently succeed. Quality first:
an economy that produces a wrong answer and forces the work to be redone is not one.

**Stay silent when the setup fits.** Speak only at a natural boundary — a new issue, a
finished pull request, the end of a design phase, the start of a long implementation, a
clear change in the nature of the work. One short recommendation: the configuration, and
the reason. Never repeat one that was declined, and never change the model or the effort
yourself — that is the maintainer's interface, not yours.

## Capability

Reason about **model**, **effort** and **orchestration** separately. Volume of code is not
difficulty of reasoning: a long, fully specified change stays cheap.

* **Sonnet 5 · effort `high`** — the default for daily work: a specified issue, tests, a
  localised bug, a framed refactor, documentation, CI, applying a decided ADR, review
  fixes, mechanical release prep. `high` is the vendor's own default.
* **Opus 5 · `high`→`xhigh`** — architecture, a public API contract, a resistant root
  cause, arbitration across subsystems, an adversarial review, a critical release audit.
* **`max`** — only after `xhigh` has demonstrably failed. It is session-only and
  documented as prone to overthinking with diminishing returns.
* **`ultrathink` in the prompt** — one hard question deserving deep reasoning, without
  raising the effort of the whole session. Prefer it to an escalation.
* **Ultra Code** — `xhigh` plus automatic workflow orchestration, session-only. For a
  repository-wide sweep (migration, audit, a diffuse bug), never as a default.
* **Fable 5** — the most capable model, for a task larger than one sitting or genuinely
  ambiguous. Mention that it may bill usage credits rather than the plan's limits.
* **Haiku 4.5** — mechanical extraction, renaming, simple search. Not when a redo would
  erase the saving.
* **`opusplan`** — Opus during Plan mode, Sonnet for execution: the built-in
  escalate-then-de-escalate.

**De-escalate as soon as the hard part is over.** Once the design is settled and what
remains is writing the decided code, completing tests, updating documentation and
preparing the pull request, say so and suggest dropping back.

## Context

* `/clear` when the next request is genuinely independent of this one. Not while something
  needed is still only in the conversation.
* `/compact <focus>` when the same task continues but intermediate context has piled up.
  Useful focus: confirmed root cause, accepted decisions, files modified, tests already
  run, remaining work, open questions.
* `/context` to measure rather than speculate. `/btw` for a side question.
* **Persist before clearing.** A durable conclusion belongs in an ADR, a specification, a
  test, the documentation or the pull request — then the old conversation stops being
  precious.
* Reviews and audits: work the delta first (`git diff`, merge-base, changed files,
  previous findings), and scan the whole repository only when the request needs it.
