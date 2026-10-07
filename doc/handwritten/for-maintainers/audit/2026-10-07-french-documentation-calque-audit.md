# JustDummies — French documentation calque audit

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./2026-10-07-french-documentation-calque-audit.fr.md)

**Date:** 2026-10-07
**Audited revision:** `2d99385` (tip of `main` at audit time)
**Scope:** the 179 `*.fr.md` pages under `doc/` — ~251 000 words across the user
documentation, the decision base, the workflow pages, the two specifications, the earlier
audits and the migration record. The English pages are **not** in scope: they are the
canonical side (`.claude/rules/documentation.md`), and this audit judges the French against
French, not against them.
**Status:** advisory. Per the repository's own convention (ADR-0002), it produces
recommendations, never blockers. The term choices in §4 are `@reefact`'s, not an auditor's.

**Why it exists.** A French page is a translation, and the failure mode of a translation is
not a typo: it is a word that is grammatical, looks like its English source, and means
something else. Such a word survives rereading because nothing about it is broken. The
change that preceded this audit found three of them one at a time
([ADR-0023](../adr/0023-ship-justdummies-analyzers.fr.md), ADR-0052, ADR-0092 and their
neighbours) and that is exactly the method this audit replaces: **finding a calque by meeting
it is not a method, and it leaves the rest standing.** The first thing this audit established
is that the preceding pass was incomplete — see finding 1.

---

## 1. Method

Not a reading. The corpus is a quarter of a million words and reading it whole is neither
feasible nor what would make the result trustworthy: a reader who has already accepted a word
twenty times stops seeing it.

The audit is **term-driven and reproducible**. A battery of regular expressions, each
encoding one suspected calque, was run over every `*.fr.md`; every hit was then read in
context and classified by hand against its English original where the judgement needed it.
The probe list is in §6 so a later audit can re-run it and compare.

Three probe families:

* **False friends** — a French word that exists, is grammatical, and does not mean what its
  English twin means (`supporter`, `adresser`, `réaliser`, `assumer`, `prétendre`, `sensible`,
  `définitivement`, `supposer`, `délivrer`, `éventuellement`, `initier`, `opportunité`,
  `consistant`, `versatile`, `compléter`).
* **English words left standing in French prose** — `record`, `leg` rendered as a body part,
  `gate`, `story`, `in-flight`, and franglais verb forms (`silencier`, `wrapper`, `rebuilder`,
  `skipper`).
* **Mechanical conventions** — the space before `:` `;` `!` `?`, the space inside `« »`, and
  the apostrophe character.

What the probes cannot see, and this audit therefore does not claim: a sentence that is
idiomatic word by word and still argues less clearly than its English original. §5 says so
plainly.

## 2. What is sound

Most of the corpus is. This is worth recording, because an audit that reports only defects
reads as though the whole were defective.

* **Typography is near-perfect.** 1 967 colons carry the French space before them against
  **2** that do not (`0045-renumber-the-decision-base.fr.md`, `adr/README.fr.md`). Every one
  of the guillemet pairs spaces its content correctly. No corpus of this size holds that by
  accident.
* **The hard false friends are used correctly.** `prétendre` (11 hits), `supposer` (13),
  `réaliser` (4), `définitivement` (4), `sensible` (12) and `assumer` (9) are the words a
  machine translation gets wrong, and all 53 occurrences are right French — `mesuré, pas
  supposé`, `coût assumé`, `code sensible au décalage`, `prétendre le contraire`. Two further
  probes (`éventuellement`, `opportunité`, `initier`, `consistant`, `versatile`) return
  nothing at all.
* **`adresser` is right 10 times out of 11.** The one exception is finding 8.
* **The house vocabulary is deliberate and consistent** where it keeps an English term on
  purpose: `run`, `job`, `runner`, `diff`, `commit`, `opt-in`, `quality gate`. Those are not
  calques; they are the terms French developers use, and the repository has already ruled on
  the family once.

## 3. Findings

Ten defects, 126 lines in all. Each is a word that means something else in French, or a word
that is not French; each has one fix, named. Counts are lines, not occurrences — a line may hold
two.

| # | Term | Lines | Files | Fix |
|---|---|---|---|---|
| 1 | `patte` for *leg* | 38 | 4 | `job`, or `cible` once |
| 2 | `supporté` for *supported* | 38 | 20 | `pris en charge` |
| 3 | `ce record` for *this record* | 30 | 12 | `cet enregistrement` / `cette décision` |
| 4 | `silencier` | 6 | 2 | `supprimer` / `faire taire` |
| 5 | `histoire` for *story* | 3 | 3 | per context |
| 6 | `délivrer` for *deliver* | 4 | 4 | `livrer` / `obtenir` |
| 7 | `en vol` for *in-flight* | 2 | 2 | `en cours` |
| 8 | `n'adresse` for *does not address* | 1 | 1 | `ne traite` |
| 9 | `wrappe` | 1 | 1 | `déborde` |
| 10 | `rebuilde`, `skippé`, `floors` | 3 | 3 | `reconstruit`, `sauté`, `planchers` |

### 1 — `patte`, the third spelling of *leg*

**The headline finding, and the one that justifies auditing rather than correcting as you
go.** The change that preceded this audit wrote `jambe` out of the corpus and replaced it, by
sense, with `job` (a CI matrix cell) or `cible` (a target framework). It missed that the same
English word had also been rendered **`patte`** — an animal's leg — 38 times across
[ADR-0024](../adr/0024-guard-public-and-internal-arguments-against-null.fr.md),
[ADR-0028](../adr/0028-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.fr.md)
(the bulk of them) and [`workflows/README.fr.md`](../workflows/README.fr.md).

So *leg* reached French three ways: `jambe`, `patte`, `job`. One of the three was chosen; two
were calques; and the pass that fixed one of the calques left the larger one standing. A
term-by-term correction cannot find this, because it only ever sees the spelling it was
looking for.

The fix is the one already decided for `jambe`, and it splits the same way: `job` where the leg
is a matrix cell — 37 of the 38 — and `cible` in the one line where it is a target framework
(ADR-0024, *la patte moderne*, which the English calls *the modern leg*). The split is the
point: a blind substitution would have written *le job moderne* there and said something
false.

### 2 — `supporté`

French `supporter` means to bear or to endure. A platform is not endured, it is **prise en
charge**. The base already knows this: [ADR-0001](../adr/0001-lock-the-analyzer-roslyn-floor.fr.md)
writes *un hôte pris en charge* in its second paragraph.

This is the finding that reaches furthest: 20 files, and four of them are **user
documentation** — `guides/faq.fr.md`, `packages/README.fr.md`, `packages/justdummies.fr.md`,
`generators/enums-and-choices.fr.md`. *Le plancher supporté est .NET Framework 4.7.2* is read
by users, and it says the floor is endured.

### 3 — `ce record`

In French, a `record` is a sporting achievement. It is not a written record, and an ADR is
never one. The base's own French index titles itself *Enregistrements de décisions
d'architecture*, and the pages already say **`cet enregistrement`** 55 times and **`cette
décision`** 125 times. The 30 lines that say `ce record` are the outliers, concentrated in
ADR-0075 through ADR-0097.

Not to be confused with the C# `record` keyword, which is correct and must stay: the
specification and `JD028.fr.md` use it properly.

### 4 to 10 — the rest

* **`silencier`** (ADR-0050, ADR-0052) is not a French verb. The domain already has the right
  one: a `[SuppressMessage]` **supprime** a rule.
* **`histoire`** renders *story* in three places where the English means a case or an account,
  not a tale: *framework-floor story* (ADR-0019), *compatibility story* (ADR-0063), *a clean
  story* (ADR-0093). The same defect was fixed in ADR-0023 (*analyzer story*) by the preceding
  change, which again shows it was found rather than searched for.
* **`délivrer`** is the English *deliver*. A signal in French is **livré** at best, and ADR-0093's
  *aucun budget abordable ne le délivre* means *obtains*, not *delivers*.
* **`en vol`** for an in-flight CI run is aviation English; a run is **en cours**.
* **`n'adresse`** (ADR-0093) is *address* in the sense of *treat*: **ne traite**.
* **`wrappe`** (ADR-0031) describes integer overflow; French says the arithmetic **déborde**.
  The `new Wrapper(value)` in `JD027.fr.md` is a code sample and must not be touched.
* **`rebuilde`** (`justdummies-mutation.fr.md`), **`skippé`** (`genany-sweep.fr.md`) and
  **`floors`** left in English (ADR-0022) are one line each.

## 4. Term choices, which are the maintainer's

These are not defects. Each is a defensible rendering where a different one may be preferred,
and the count makes the decision worth taking once rather than per page.

* **`barrage` for *gate*** — 37 lines, nearly all in
  [ADR-0022](../adr/0022-gate-pull-requests-on-the-mutation-score-of-the-diff.fr.md). A
  `barrage` is a dam or a roadblock; the metaphor carries, and French developers mostly say
  `gate`. The repository has already leaned that way once, keeping `quality gate` in the
  French of ADR-0103 rather than translating it. Changing 37 lines on that precedent is a call
  this audit does not make.
* **`vérifié contre` / `mesuré contre`** — 13 lines. *Verified against* is `vérifié face à`,
  `confronté à` or `par rapport à` in idiomatic French; `contre` reads as opposition. The
  repository's own 2026-07-20 audit writes *vérifiés de façon contradictoire face au code*, so
  the better form is already in the corpus. Mild, and understandable as it stands.
* **The apostrophe character** — 177 of the 179 pages use the straight `'`. Two use the
  typographic `’`, 230 times: `CONTRIBUTING.fr.md` and `SECURITY.fr.md`, both user-facing.
  French typography prefers `’`; the repository's practice is overwhelmingly `'`. Either
  answer is consistent; the current state is not.

## 5. What this audit does not cover

* **Argument quality.** A page can be idiomatic sentence by sentence and still make its case
  less well than the English it mirrors. That is a rereading, page by page, against the
  English — a different and much larger exercise, and the probes say nothing about it.
* **Terms the probes did not think of.** The method finds what it is pointed at. Finding 1 is
  the proof: `patte` was found only because *leg* was already a known defect and the audit
  asked what else it could have become. A calque nobody has suspected yet is still in there.
* **The English pages**, by scope, and **anything outside `doc/`** — `.claude/`, the
  changelogs and the root markdown files were not probed.
* **Structural parity**, which is not an audit matter: `JustDummies.Documentation.UnitTests`
  checks it on every build.

## 6. The probe list

Re-runnable as-is over `doc/**/*.fr.md`, so a later audit can compare rather than start over.
Each returns candidates, never verdicts — every hit in §3 was read in context before being
counted.

```
\b(ce|cet|cette|le|les|du|des|un|une|son|sa) records?\b
\bsupport(e|ent|é|ée|és|ées)\b
\bsilenci(e|es|ent|er|ée?s?)\b
\bpattes?\b
\bjambes?\b
\bhistoires?\b
\bbarrages?\b
\b(rebuild|skipp|trigger|wrapp|logg|shipp|checke|fetche)(e|es|ent|er|é|ée|és|ées)\b
(mesur|vérifi|compar|test|relu|confront|class|calibr|jug|valid|contrôl)\w* contre\b
\b(support|adress|délivr|réalis|assum|prétend|suppos)\w*
\b(éventuel|définitivement|initie|opportunit|consistan|versatil)\w*
en vol\b
\bfloors\b
```

Two mechanical checks, which a regex alone does not express: the count of `:` preceded by a
space against those that are not, and the count of `U+2019` against `U+0027` per file.

## 7. Recommendation

Apply §3 — ten defects, one named fix each, no judgement left in them. Decide §4 once, as
three answers rather than ninety edits. Leave §5 as the honest statement of what is still
unmeasured: the next audit of this corpus should be a rereading against the English, not
another probe battery, because the probes have now returned what they can.
