---
name: adr-check
description: Check a change against the JustDummies architecture decision base and draft an ADR when one is warranted. Use before finalizing a pull request, or whenever a change touches a public API contract, a cross-cutting invariant, a platform floor, a dependency or security policy, or the semantics of a diagnostic id.
---

# Checking a change against the decision base

Every pull request is checked against the base under
`doc/handwritten/for-maintainers/adr/` (ADR-0002). **The check is the habit; the ADR is the
exception** — most pull requests embark no architectural decision. This is **advisory**:
produce a recommendation, never a blocker.

You **draft and propose**. You never accept, supersede or deprecate an ADR, and you never
flip a status — that is `@reefact`'s call, exactly as no agent merges a pull request. When
it is genuinely unclear whether a change is significant enough, say so and let the
maintainer judge rather than guessing.

## 1 — Select, do not read the base

The base holds far more records than this check is meant to read. Reading them all is not
what it means, and never was.

1. Read only the index table:
   `grep '^| \[ADR-' doc/handwritten/for-maintainers/adr/README.md`
   — one row per decision: *number · title · status · origin*.
2. Pick the **2 to 5** whose titles could plausibly bear on this change. Match on the
   subject the change touches, not on the words in the diff.
3. Read those files, and only those. Widen only if one of them points at another.
4. **`ADR-0046` is in scope by default** for any question shaped *"should the generator
   handle this case too?"* — it is the rule the other bounding decisions share.
5. A **`Superseded` row** in the index is history: read the successor named in the row, not
   the superseded record, unless you need to know what changed.

Two shortcuts worth knowing:

* `doc/handwritten/for-maintainers/specifications/adr-implementation-reference.md` says what
  each accepted decision actually enforces and where — often faster than the ADR itself when
  the question is "is this already guaranteed somewhere?".
* A citation written `ADR-00NN (first-class-errors)` belongs to the other repository and is
  not in this index. A bare number always means this base.

## 2 — Decide the outcome

A record is warranted for a **significant, lasting decision** — one a future maintainer
would ask *"why did they do it this way?"* about. The test: **if the implementation changed
but the decision stood, the ADR should not need editing.**

**Likely warrants one:** a new or changed public API contract (a factory surface, a
constraint's semantics, a `[ValueObject]` type); a change to a cross-cutting invariant;
raising or lowering a supported-platform floor (target framework, SDK, Roslyn floor); a
dependency or security/compatibility policy; changing the semantics of a diagnostic id.

**Does not:** a bug fix; a refactor with no observable contract change; formatting; a value
that simply follows the existing taxonomy; tests, documentation, translations, samples;
routine dependency bumps; CI or tooling tweaks with no policy change.

Bias hard toward silence. A false alarm on a routine change trains the maintainer to ignore
the check.

Four outcomes — state the result in the pull-request description:

| Outcome | What you do |
|---|---|
| **No decision** | Say so in one line. Tick the first box of the template's *Architecture decisions* section. Stop. |
| **Create** | Draft one ADR **per decision**, `Status: Proposed`. |
| **Supersede** | Draft the **successor** as `Proposed`; name the ADR it would replace. Never edit the existing record, never flip its status. Accepted ADRs are immutable historical records. |
| **Alert** | The change contradicts an **accepted** ADR. Do not proceed silently: flag it in the description as `⚠️ Conflicts with ADR-NNNN (<title>)`, state the precise contradiction, and let the maintainer decide — accept it as a supersession, or change the code. |

A branch may hit none, one, or several of these.

## 3 — Draft it

* Copy `doc/handwritten/for-maintainers/adr/template.md`. Its HTML comments explain each
  section; delete them as you fill them in.
* Number it as the next free `NNNN`. Numbers are **stable handles, not a reading order**
  (ADR-0045) — do not renumber anything to make an order read better.
* Name it `NNNN-kebab-case-summary.md`, and write the **French twin** `NNNN-….fr.md`,
  cross-linked in the header. English is canonical.
* Header: `**Status:** Proposed` and a `**Proposed:** YYYY-MM-DD` line. **One dated line per
  state the decision actually reached, and no date is ever overwritten** (ADR-0036).
* Add a row to the index table in `README.md` **and** `README.fr.md`, and bump the origin
  count in the "Where these decisions come from" list.
* Link it from the pull request.

Shape discipline, from the base's own conventions:

* One decision per file. **Decision** is one single sentence, with no justification in it.
* **Context** is facts only — everything Rationale argues from must be stated there first.
* **Rationale is argument, not a design document.** If a paragraph explains *how something
  is built* rather than *why the decision is right*, it belongs in the reference docs and
  the ADR links to it. No code, config, YAML, exact flags or step-by-step walkthroughs.
* Alternatives get why they were considered **and** why they were rejected.

## 4 — Recording and accepting are one intention

If you draft an ADR as `Proposed` and the same branch later accepts it, that is **one
intention, not two** — squash them before the pull request. The history hook flags this
case; the general rule is in the `tidy-history` skill.

## Reference

* [`adr/README.md`](../../../doc/handwritten/for-maintainers/adr/README.md) — conventions and the index.
* [`workflows/adr-check.en.md`](../../../doc/handwritten/for-maintainers/workflows/adr-check.en.md) — the advisory CI check that reads a pull request against the base.
* `AGENTS.md`, "Architecture decisions" — the same procedure for other agents.
