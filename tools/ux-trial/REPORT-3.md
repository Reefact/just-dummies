# Developer-UX trial 3 — JustDummies and `any`: report

## 1. Summary

**Legend.** Every domain was tested by three independent sessions, one per developer profile: **D = discovery** (has never used JustDummies or `any`), **R = regular** (has used them for months), **E = expert** (an expert test crafter, demanding about what a dummy is). A finding id such as `07R-3` reads domain 07, profile R, third finding of that session. "D / R / E" in a table, or "(D, R)" after a domain, gives a count or a list per profile.

**Verdict.** The library holds; the scaffolder's edge is where the time goes.
- 50 domains, three profiles each: 150 sessions, 150 suites, **43 867 test cases, 0 failed, 601 skipped**. Every skip records a domain defect. That count is from the coordinator's own run of each suite from the repository, not from the sessions' reports.
- Every suite stayed green over repeated runs with fresh seeds (11 to 222 runs per suite, as reported by each session).
- 1 530 findings: 994 about the library, the tool or the docs, grouped into **151 root causes**, and 536 about the trial's own domain models, grouped into 264 clusters.
- Each of the 151 root causes was checked adversarially by reproduction against the packed packages: **53 confirmed, 63 partly, 33 by design, 1 refuted, 1 not reproduced**. Of the 116 confirmed or partly items, 15 are medium severity and 101 low; none is high.
- **One core bug** was found, and it is a correctness one: an ordered pair accepts or refuses the same two pins depending on the order of the calls ([CORE-1](#core-1)). It was met once in 150 sessions.

What worked:
- The DSL, pin-and-refine `With…` pairs, the build-blocking `TODO_verify` sentinels and `[Reproducible]` worked in every domain. Wherever the scaffolder could not vouch for a guard, the build stopped at a sentinel; the gaps are the shapes documented as silent ([CLI-10](#cli-10)) and one compound-statement case ([CLI-5](#cli-5)).
- Drawn values revealed real domain defects in at least 14 domains, each replayed from its seed: a covariance sum the domain refuses (28), a solver input out of range (32), a duration sum that cannot be written (33), a population driven extinct (21), and several overflows of quantities left without a ceiling (see [§9](#9-domain-defects-found-by-the-sessions)).
- Six times a drawn value showed that a value the test treated as a dummy was not one, and the seed replayed it on the first try ([§7](#7-what-worked)).
- Most REPORT-2 items have been addressed and hold in the field ([§1.2](#12-since-report-2)).

What slowed people down:
- **Rules that tie parameters together.** 219 findings in 49 of the 50 domains: a derived parameter whose pins stop reaching the draw ([CLI-1](#cli-1)), ties through another draw that the docs cover only in another guide ([DOCS-2](#docs-2), [DOCS-6](#docs-6)), an order plus a maximum gap ([CORE-2](#core-2)), and the absence of a re-scaffold-safe seam (a decision, [§5](#5-candidates-for-a-decision)).
- **First-run setup outside the library's concepts**: the Microsoft.Testing.Platform opt-in on the .NET 10 SDK ([DOCS-1](#docs-1); its frequency is an artefact of the harness, see [§2](#2-limits-of-this-trial)) and the restore `any` needs ([CLI-2](#cli-2)).

### 1.1 Fix first

Ranked by what each costs across domains, not by count alone. "Findings" is the number of session findings the root cause gathers; D/R/E counts them per profile.

| # | Item | Kind / severity | Verdict | Findings | Domains | D / R / E |
|---|---|---|---|---|---|---|
| 1 | [CORE-1](#core-1) Two pins on an ordered pair are accepted or refused depending on call order; through a scaffolded generator no order works | bug, medium | confirmed | 1 | 1 | 0 / 1 / 0 |
| 2 | [CLI-1](#cli-1) A parameter derived in `Generate()` keeps `With…` pins that compile and are not honoured; a contradictory pin is repaired instead of refused | friction, medium | confirmed | 83 | 41 | 26 / 29 / 28 |
| 3 | [CLI-4](#cli-4) A same-type `void` guard helper is never read, while the same body behind a returning helper is read in full | friction, medium | confirmed | 23 | 13 | 7 / 6 / 10 |
| 4 | [CLI-5](#cli-5) Throws inside one compound statement (an `if` block, a loop) share one pointer: later throws are neither named nor counted, and ties through a local are not called out | defect, medium | partly / confirmed | 18 | 11 | 5 / 6 / 7 |
| 5 | [XU-1](#xu-1) `[Reproducible]` reports a seed and a replay snippet for a failing test that drew nothing | defect, low | confirmed | 19 | 16 | 5 / 2 / 12 |
| 6 | [DOCS-2](#docs-2) + [DOCS-6](#docs-6) Ties through another draw (a member, a kind, a count, a collection's elements) have working `.Then(...)` forms, but the CLI page's tie section never shows or links them | docs, medium | partly / confirmed | 60 | 34 | 28 / 11 / 21 |
| 7 | [DOCS-1](#docs-1) The adapter's `xunit.v3` 4.x floor needs the Microsoft.Testing.Platform opt-in on the .NET 10 SDK, and no user page says so | docs, medium | partly | 148 | 50 | 50 / 50 / 48 |
| 8 | [DOCS-3](#docs-3) A null-guard test through a scaffolded `With…` needs a cast (CS0121); the page shows it only for nullable parameters | docs, low | confirmed | 20 | 17 | 3 / 5 / 12 |
| 9 | [CORE-2](#core-2) An order plus a maximum gap scaffolds to an `Any.OrderedPair` the constructor refuses on every draw, and no page shows the pin-keeping edit | friction, medium | partly | 19 | 8 | 8 / 6 / 5 |
| 10 | [CLI-3](#cli-3) + [DOCS-5](#docs-5) A one-sided `decimal`/`double` row reaches the type's limit one draw in ten, the recap only says `guard`, and the CLI page explains JD030 but not JD036 | friction / docs, low | partly | 48 | 29 | 14 / 15 / 19 |
| 11 | [CLI-2](#cli-2) The user page never says the project must be restored; an unrestored batch is reported as the first type's refusal | friction, low | partly | 46 | 34 | 23 / 16 / 7 |
| 12 | [CLI-7](#cli-7) The recap and the JSON report never say two parameters were drawn together through `Any.OrderedPair` | defect, low | confirmed | 11 | 8 | 5 / 4 / 2 |
| 13 | [CLI-9](#cli-9) The regex hint is lost for a `Regex` field built with target-typed `new("…")`, against the spec's own sentence, and for `Match(...).Success` | defect, low | partly | 8 | 8 | 2 / 2 / 4 |

### 1.2 Since REPORT-2

The verifiers checked each prior item they met against the packed build. These REPORT-2 items are addressed and held in this trial; a residue, where one remains, has its own entry.

| REPORT-2 | What changed | Residue |
|---|---|---|
| CLI-1 `\|\|` range guards unread | a `\|\|` is split into separate guards (ADR-0128) | [CLI-46](#cli-low) |
| CLI-3 only the first unread guard named | "first unread guard …, and N more" and "ties a to b" (#373) | [CLI-5](#cli-5), [CLI-27](#cli-low) |
| CLI-8 derived parameter, pins lost | answered for a direct order: `Any.OrderedPair` (ADR-0126, #404) emitted by the scaffolder (#405) | [CLI-1](#cli-1), [CORE-2](#core-2) |
| CLI-10 interface-typed parameter | `abstract` provenance with a TODO naming the generator to write (#375) | [CLI-11](#cli-11) for framework types |
| CLI-12 ambiguous short name refused | resolves to the project's type and names the alternative | [CLI-40](#cli-low) |
| CLI-13 regex TODO omits the pattern | the TODO quotes the pattern and lists the places to retype (#374) | [CLI-9](#cli-9), [CLI-15](#cli-15) |
| CLI-14 returning helper silent | marked (#407), then read one level down (#416) | [CLI-4](#cli-4), [CLI-10](#cli-10) |
| CLI-15 missing guard rows | inline `TimeSpan` bounds, `decimal.Round` → `WithScale`, `IndexOfAnyExcept`, `.Length` through a helper (#408) | [CLI-8](#cli-8), [CLI-22](#cli-22) |
| CLI-18 namespace not named | the recap prints `in namespace X (--namespace to change).` (#378) | [DOCS-4](#docs-4) |
| CLI-19 nullable null pin | `Without…()` where the default draws null (#411); null one draw in ten (ADR-0117) | [DOCS-3](#docs-3) |
| CLI-20 generator passed to `With…` | generated remarks and a page paragraph (#391) | [CLI-21](#cli-21) |
| CLI-22 constant copied as a value | a named constant is referenced (#377) | [CLI-32](#cli-low) |
| CLI-24 stale five-line comment | "then delete these two lines" (#412) | [CLI-34](#cli-low), [CLI-37](#cli-low) |
| CORE-2 `Except` without `IEnumerable<T>` | every typed builder has both overloads | — |
| CORE-3 no `DistinctBy` | added and documented on the collections page | — |
| CORE-4 no "B differs from A" | `DifferentFrom` on every `IAny<T>` (ADR-0121) | [DOCS-12](#docs-12), [DOCS-13](#docs-13), [CORE-7](#core-7) |
| CORE-5, CORE-12 dependent draw, choice | `.Then(...)` (ADR-0119), `Any.Boolean().Then(...)` as the documented choice | [DOCS-2](#docs-2), [AN-1](#an-1) |
| CORE-6 no constant generator | `Any.Fixed` | — |
| DOCS-1 read shapes unlisted | the CLI page has a read/unread shape table | — |
| DOCS-3 date span undocumented | "An unconstrained draw spans the whole type" on the dates page | [DOCS-35](#docs-low) |
| DOCS-8 getting-started never mentions `any` | linked from "Where to go next" | [DOCS-10](#docs-10) |
| XU-1 pasted snippet needs a `using` | the snippet is fully qualified | — |
| AN-1 Info rules invisible | the remedy is in the nupkg readme, the CLI page and the JD030/JD031 pages | [DOCS-9](#docs-9) |
| REPORT.md AN-C3 scaffolded entries invisible to JD007 | JD007 fires on scaffolded `Any.X()` entries (#365) | — |

Still open as decisions: the log-scale spread (REPORT-2 CORE-14), the private `AnyValid…()` bases (CLI-17), the re-scaffold-safe seam (CLI-8's general case). See [§5](#5-candidates-for-a-decision).

### 1.3 Fluency and test counts

A cell reads "fluency / test cases (skipped)". Cases and skips are the coordinator's run of each suite from the repository; fluency is each session's own score.

| Domain | D | R | E |
|---|---|---|---|
| 01-payments | 4 / 185 (3) | 4 / 230 (5) | 4 / 306 (5) |
| 02-trading | 4 / 169 (3) | 4 / 286 (3) | 4 / 231 (5) |
| 03-insurance | 4 / 175 (2) | 4 / 206 (2) | 4 / 193 (3) |
| 04-manufacturing | 4 / 233 (5) | 4 / 200 (6) | 4 / 266 (5) |
| 05-cad-drawing | 4 / 643 (11) | 4 / 400 (7) | 4 / 521 (11) |
| 06-warehouse | 4 / 188 (7) | 4 / 224 (5) | 4 / 244 (7) |
| 07-freight | 4 / 158 (4) | 4 / 245 (6) | 4 / 248 (4) |
| 08-patient-record | 4 / 193 (5) | 4 / 182 (4) | 4 / 249 (4) |
| 09-laboratory | 4 / 205 (3) | 4 / 246 (4) | 4 / 213 (3) |
| 10-pharma-serialisation | 4 / 213 (4) | 4 / 234 (5) | 4 / 271 (8) |
| 11-telecom | 4 / 202 (4) | 4 / 198 (4) | 4 / 207 (5) |
| 12-smart-grid | 4 / 200 (5) | 4 / 232 (5) | 4 / 238 (6) |
| 13-payroll | 4 / 201 (1) | 4 / 190 (1) | 4 / 189 (2) |
| 14-ecommerce | 4 / 295 (9) | 4 / 300 (6) | 4 / 262 (7) |
| 15-hotel-booking | 4 / 213 (2) | 4 / 301 (6) | 4 / 290 (9) |
| 16-aviation | 4 / 287 (5) | 4 / 290 (5) | 4 / 323 (6) |
| 17-real-estate | 4 / 242 (5) | 4 / 244 (2) | 4 / 297 (3) |
| 18-education | 4 / 174 (4) | 4 / 182 (2) | 4 / 163 (4) |
| 19-iam | 4 / 187 (4) | 4 / 156 (2) | 4 / 171 (3) |
| 20-agritech-iot | 4 / 188 (5) | 4 / 180 (4) | 4 / 203 (5) |
| 21-orbital-microbiology | 4 / 334 (2) | 4 / 329 (3) | 4 / 380 (4) |
| 22-pulmonary-nodules | 4 / 186 (2) | 4 / 161 (2) | 4 / 223 (2) |
| 23-captive-breeding | 4 / 207 (2) | 4 / 252 (2) | 4 / 239 (5) |
| 24-radiocarbon-chronology | 4 / 211 (4) | 4 / 238 (3) | 4 / 290 (6) |
| 25-adaptive-video | 4 / 332 (4) | 4 / 240 (3) | 4 / 311 (5) |
| 26-robotic-milking | 4 / 302 (5) | 4 / 185 (3) | 4 / 222 (4) |
| 27-railway-interlocking | 4 / 186 (4) | 4 / 156 (3) | 4 / 173 (4) |
| 28-satellite-conjunction | 4 / 249 (6) | 4 / 214 (4) | 4 / 266 (6) |
| 29-parimutuel-betting | 4 / 207 (1) | 4 / 221 (1) | 4 / 262 (2) |
| 30-liquidity-gap | 4 / 263 (0) | 4 / 264 (1) | 4 / 259 (3) |
| 31-vessel-stowage | 4 / 324 (0) | 4 / 364 (1) | 4 / 390 (0) |
| 32-grid-load-flow | 4 / 261 (3) | 4 / 273 (1) | 4 / 304 (3) |
| 33-music-score | 4 / 219 (2) | 4 / 309 (1) | 4 / 306 (2) |
| 34-manuscript-reconstruction | 4 / 269 (1) | 4 / 293 (1) | 4 / 379 (3) |
| 35-dna-variants | 4 / 266 (4) | 4 / 199 (3) | 4 / 235 (4) |
| 36-knitting-patterns | 4 / 149 (0) | 4 / 164 (3) | 4 / 205 (1) |
| 37-batch-genealogy | 4 / 275 (5) | 4 / 286 (10) | 4 / 392 (6) |
| 38-carbon-ledger | 4 / 310 (8) | 4 / 222 (14) | 4 / 298 (10) |
| 39-geothermal-reservoir | 4 / 189 (2) | 4 / 237 (1) | 4 / 287 (1) |
| 40-catastrophe-losses | 4 / 225 (4) | 4 / 254 (6) | 4 / 299 (11) |
| 41-shift-planning-time-zones | 4 / 295 (4) | 4 / 366 (4) | 4 / 426 (3) |
| 42-token-ledger | 4 / 452 (5) | 4 / 429 (4) | 4 / 525 (4) |
| 43-passive-components | 4 / 527 (4) | 4 / 593 (5) | 4 / 694 (5) |
| 44-vaccine-cold-chain | 4 / 491 (3) | 4 / 401 (5) | 4 / 445 (10) |
| 45-healthcare-claims | 4 / 197 (2) | 4 / 213 (3) | 4 / 226 (3) |
| 46-network-acl | 4 / 641 (1) | 4 / 536 (1) | 4 / 649 (1) |
| 47-international-catalogue | 4 / 621 (5) | 4 / 600 (3) | 4 / 723 (5) |
| 48-tax-rules-engine | 4 / 475 (2) | 4 / 522 (2) | 4 / 497 (4) |
| 49-clinical-trial-crf | 4 / 418 (6) | 4 / 500 (6) | 4 / 489 (6) |
| 50-fx-settlement | 4 / 370 (2) | 4 / 322 (3) | 4 / 317 (3) |
| **Total** | **14 002 (184)** | **14 069 (186)** | **15 796 (231)** |

That is 43 867 cases from 25 254 test methods across 150 suites, 0 failed, 601 skipped. Every session scored fluency 4/5, so the mean is 4.00 — see [§2](#2-limits-of-this-trial), item 3, before reading anything into it. For 01–20, the first trial's `Tests` suite is kept beside the three new suites and is not counted here.

---

## 2. Limits of this trial

Read these before the findings. None of them invalidates a verdict, because each verdict was reproduced independently; they change how much the counts mean.

1. **The harness inflated one finding.** The workspaces lacked the repository's `global.json`, and the brief prescribed `dotnet test --project`. That switch exists only in Microsoft.Testing.Platform mode, so nearly every session met the opt-in on its first run. The documentation gap is real ([DOCS-1](#docs-1)); its frequency, 148 findings in 50 domains, is an artefact. The same harness had sessions hand-write the `.csproj` from a template, which is why so many met the restore prerequisite ([CLI-2](#cli-2)) before the documented `dotnet add package` would have restored for them.
2. **The `MODEL.md` files of 01–20 were written from the code for this trial**, and they describe surprising behaviours neutrally — `$` accepting a trailing newline, for example. That steered sessions towards those domain defects: the trailing-newline family appears in 19 of those 20 domains.
3. **Every one of the 150 sessions scored fluency 4/5.** A score that never varies measures nothing. It is reported, and no conclusion is drawn from it.
4. **The repository's maintainer instructions were loaded into every session.** The briefs told the sessions to act as consumers, but some of the project's vocabulary (ADR numbers, spec sections) may have leaked into how they read the user pages.
5. **Isolation slipped.** Several sessions wrote scratch files outside their own workspace, into the shared scratch root. There is no evidence any session read another's work, but it cannot be excluded.
6. **A container restart interrupted 6 sessions.** Their workspaces were reset and they restarted from zero.
7. **One suite failed when the coordinator ran it from the repository.** In 49's expert suite, `A_death_cannot_be_reported_before_the_onset_of_an_event_already_logged` failed on seed −2085749253: it asserted that no death was recorded while the event it had logged was drawn at grade 5, which is itself a death. The grade was not a dummy for that assertion; the draw showed it, and the seed replayed it. The grade is now pinned at 3, and [NOTES.expert.md](domains/49-clinical-trial-crf/NOTES.expert.md) records it. The session's 12 repeated runs had not met that combination.
8. **The domain defects were not independently verified** ([§9](#9-domain-defects-found-by-the-sessions)). Each session reports that it demonstrated its defects with a test kept as `Skip = "domain defect…"`.

---

## 3. By profile

Of the 994 library, tool and docs findings, 335 came from discovery sessions, 305 from regular and 354 from expert. The three profiles met the same top items ([DOCS-1](#docs-1), [CLI-1](#cli-1), the regex retype); what differs is the tail.

### Discovery: what trips a developer new to the library

- **Getting started.** The `extension(Any)` entry class on getting-started has no prose, no C# 14 note and no pointer to `any generate --entry-point any` (9 findings, all discovery: [DOCS-10](#docs-10)). No page names the `JustDummies` namespace before line 295 ([DOCS-11](#docs-11)). The restore `any` needs is undocumented ([CLI-2](#cli-2), 23 of its 46 findings).
- **Ties.** Newcomers looked for a tie on the CLI page and did not find the `.Then(...)` forms that live in the composition guide ([DOCS-2](#docs-2), 21 of 45 findings).
- **Analyzers.** The pages say the rules "start working on your next build"; an Info rule never prints in a console build, so a newcomer cannot tell "nothing to report" from "not loaded" ([DOCS-9](#docs-9)).
- **Dead links** in the docs bundle (10 findings) were the harness's doing and are refuted ([§6](#6-refuted-and-not-reproduced)).

### Regular: what slows down a habitual user

- **Hand surgery on tied generators.** Deriving one parameter from another in `Generate()` leaves its pins compiling and inert ([CLI-1](#cli-1)); some hand-wrote a `ReferenceEquals(_x, _xRecipe)` test, others a nullable "pinned" field, to tell a pin from a draw.
- **Mechanical edits.** The regex retype in four or five places per file (by design, [§5](#5-candidates-for-a-decision)), the stale remarks it leaves ([CLI-15](#cli-15)), adding an entry point later only through `--force` ([CLI-17](#cli-17)).
- **Base-constructor hand-offs** (`: base(message)`) blocked three regular sessions with no guard to read ([CLI-25](#cli-25)).
- **Exceptions from a composer** surface wrapped in `AnyGenerationException`, deliberately and without a user page saying so ([DOCS-23](#docs-low)).

### Expert: what limits a crafter

- **Testing refusals through a generator.** A rule-breaking value is a pin, and a tie the generator enforces must be bypassed with the constructor; no page says so ([DOCS-8](#docs-8), 9 of 13 findings expert). The null pin needs a cast ([DOCS-3](#docs-3), 12 of 20 expert).
- **The seed line on tests that drew nothing** ([XU-1](#xu-1), 12 of 19 expert): experts noticed it because they read every failure output.
- **Raising every JD rule to warning.** The scaffold then trips its own JD030/JD036, deliberately ([DOCS-5](#docs-5), 10 of 17 expert), and on an ordered pair JD036 announces an edge draw that never happens ([CLI-20](#cli-20)).
- **Missing composition pieces**: a set exclusion on `IAny<T>` ([CORE-7](#core-7)), a log-scale draw, an ordered pair over a comparable value object (both decisions).

---

## 4. By component

Each item gives its kind and severity (as reassessed by the verifier), its verdict, where it was met (domain with profiles, then the session finding ids, `<domain><D|R|E>-<n>`), what happens, a minimal repro, its relation to the earlier reports, and the recommendation. Low-value items are gathered in a compact table at the end of each component; none is dropped.

### Core

#### CORE-1 — Two pins on an ordered pair are accepted or refused depending on call order
<a id="core-1"></a>
- **Kind / severity:** bug, medium · **Verdict:** confirmed
- **Met in:** 37 (R). Finding 37R-5.
- **What happens:** `WithFirst(v)` with no second pin checks `v` against the second side's base; with a second pin present it checks only the order. `WithSecond` mirrors it. So the first pin applied is checked against a base the second pin then replaces, and the same two pins succeed in one order and fail in the other. Both directions fail: pins above both bases fail when `WithFirst` comes first, pins below both bases when `WithSecond` comes first.
  ```
  WithFirst(2030-01-25).WithSecond(2030-06-01): ConflictingAnyConstraintException: Cannot apply WithFirst(01/25/2030) because no second value lies above 01/25/2030: Between(2020-01-01, 2029-12-31) already requires values less than or equal to 2029-12-31.
  WithSecond(2030-06-01).WithFirst(2030-01-25): OK -> (01/25/2030, 06/01/2030)
  int WithSecond(-10).WithFirst(-20): ConflictingAnyConstraintException … Between(0, 100) already requires values greater than or equal to 0.
  int WithFirst(-20).WithSecond(-10): OK -> (-20, -10)
  ```
  The scaffolder makes it worse: the generated pair is rebuilt on every `With…` call, always `WithFirst` before `WithSecond`, so through a scaffolded generator no call order works. The session bypassed the generator with the constructor.
- **Minimal repro:**
  ```csharp
  AnyOrderedPair<DateOnly> pair = Any.OrderedPair(
      Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2029, 12, 31)),
      Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2029, 12, 31)), strict: true);
  pair.WithFirst(new DateOnly(2030, 1, 25)).WithSecond(new DateOnly(2030, 6, 1));            // throws
  pair.WithSecond(new DateOnly(2030, 6, 1)).WithFirst(new DateOnly(2030, 1, 25)).Generate(); // (2030-01-25, 2030-06-01)
  ```
- **Why it is a bug:** once both sides are pinned, `Generate()` returns the two literals and never reads either base, so the declaration is satisfiable. A refusal there is a false conflict, not a contradiction. The library's own test (`AnyOrderedPairTests`, lines 99–100) asserts order independence, but only for pins inside the base. No wrong value is ever drawn: the failure is loud, at the arrange line.
- **Prior:** new. `Any.OrderedPair` arrived after REPORT-2 (ADR-0126).
- **Recommendation:** make the outcome independent of call order. Choosing which outcome is a decision for ADR-0126: either pins are literals and a single pin is checked against the other base only while that side stays unpinned (deferring the check, which matches "a pinned value is a literal"), or every pin is always checked against the other base, so the accepted orders above are refused too. Add the out-of-base, both-orders cases beside the existing test, and update the composition page (EN/FR), which describes a single pin only.

#### CORE-2 — An order plus a maximum gap scaffolds to an ordered pair that refuses every draw
<a id="core-2"></a>
- **Kind / severity:** friction, medium · **Verdict:** partly
- **Met in:** 03 (D, R), 04 (D, R, E), 07 (D, R, E), 11 (D, R, E), 15 (D, R, E), 19 (D, R, E), 37 (D), 44 (D). Findings 03D-2, 03R-2, 04D-8, 04R-7, 04E-2, 07D-4, 07R-2, 07E-4, 11D-4, 11R-4, 11E-1, 15D-5, 15R-7, 15E-2, 19D-4, 19R-3, 19E-2, 37D-6, 44D-2.
- **What happens:** for `start >= end` plus `end.DayNumber - start.DayNumber > 31`, the scaffolder emits `Any.OrderedPair(_startRecipe, _endRecipe, strict: true)` over two unbounded `Any.DateOnly()` recipes and marks both `to verify … ties end to start`. With the sentinels deleted, the constructor refused 1 000 of 1 000 seeds, and pinning either side did not help. `AnyOrderedPair<T>` exposes only `WithFirst`, `WithSecond` and `Generate`, so it cannot carry a gap.
  ```
  start: unread guard at BillingPeriod.cs, line 7: if (end.DayNumber - start.DayNumber > 31) { ... — ties end to start
  ✓ AnyBillingPeriod.cs — 2 of 2 parameters inferred, 2 to verify.
  ```
- **Minimal repro:** `BillingPeriod(DateOnly start, DateOnly end)` with the two guards above; `any generate BillingPeriod`; delete the two TODO pairs; draw 1 000 times.
- **Why partly:** the build stops at the tie by design (spec §5.3; ADR-0126 restricts the pair to an order). A pin-keeping edit exists with the shipped API — an 18-line rewrite of the generated `StartAndEnd()` with `.Then` and a per-side `AfterOrEqualTo`/`BeforeOrEqualTo` bound held 1 000 of 1 000 draws and kept both pins — but no page shows it.
- **Prior:** the residue of REPORT-2 CLI-8, answered for orders only.
- **Recommendation:** docs first: an "order plus a maximum gap" sample under "When a guard ties two parameters" (EN/FR, compiled). Then, small in the cli train: when a parameter already in a drawn pair has another unread guard tying the same two parameters, say so in the recap and the TODO ("the order is drawn by the pair; this guard is not") and point the TODO at `StartAndEnd()`. A pair carrying a gap is the "relation other than an order" the ADR index names as the revival condition; it needs a decision.

#### CORE-3 — Ordered-pair conflict messages name `WithFirst`/`WithSecond` and print a pinned date as `MM/dd/yyyy`
<a id="core-3"></a>
- **Kind / severity:** defect, low · **Verdict:** confirmed
- **Met in:** 06 (R), 37 (E), 42 (E), 44 (R). Findings 06R-11, 37E-8, 42E-5, 44R-7.
- **What happens:** a scaffolded `WithMaxCelsius(-40m)` is refused as `Cannot apply WithSecond(-40) because no first value lies below -40 …`, a call the user never wrote. Pinned values go through `AnyDerivation.Display` in the invariant "G" format, so one sentence mixes `03/10/2026` with an ISO bound `2024-12-31`; sub-seconds are dropped, so two distinct pins can render the same and the message looks self-contradictory. This is the same under every culture.
- **Minimal repro:**
  ```csharp
  Any.OrderedPair(Any.DateOnly().Between(new DateOnly(2015, 1, 1), new DateOnly(2024, 12, 31)),
                  Any.DateOnly().Between(new DateOnly(2015, 1, 1), new DateOnly(2024, 12, 31)), strict: true)
     .WithFirst(new DateOnly(2026, 3, 10));
  // Cannot apply WithFirst(03/10/2026) because no second value lies above 03/10/2026: Between(2015-01-01, 2024-12-31) …
  ```
- **Prior:** new.
- **Recommendation:** render temporal values in `AnyDerivation.Display` with the round-trip "O" format the generators use for their bounds (a plain fix; `DateOnly`/`TimeOnly` behind `NET8_0_OR_GREATER`; `Display` also serves `Combine`, `DifferentFrom` and collections, so update any probe that pins those messages). Naming the scaffolded member instead of `WithFirst` touches the public surface — an `OrderedPair` overload carrying side names, or a re-quoting `With…` — and is a decision.

#### CORE-4 — A `Local` pin on a `DateTime` ordered pair is refused or accepted depending on pin order
<a id="core-4"></a>
- **Kind / severity:** defect, low · **Verdict:** partly
- **Met in:** 14 (D). Finding 14D-4.
- **What happens:** `WithFirst(local)` while the other side is drawn checks the other side through `After(local)`, which ADR-0114 refuses for a `Local` kind; the refusal names `instant`. Pinned the other way round, the same `Local` value is accepted. Refusing while the other side is drawn is honest (ADR-0114); depending on order is not.
- **Prior:** new; ancestor REPORT.md CORE-C7.
- **Recommendation:** decide one rule with [CORE-1](#core-1): refuse a `Local` pin on a `DateTime` pair whatever the other side holds, or treat it as a supplied value. Name the pair's side rather than `instant` in the refusal.

#### CORE-5 — `Any.StringMatching` reads an astral character as two UTF-16 units, silently
<a id="core-5"></a>
- **Kind / severity:** defect, low · **Verdict:** partly
- **Met in:** 47 (D, R). Findings 47D-1, 47R-2, 47R-3.
- **What happens:** a quantifier after an astral literal repeats only the low surrogate; a class holding one draws lone surrogates (10 of 12 draws); an astral class range is refused with the raw surrogates printed, which a UTF-8 console shows as two U+FFFD.
  ```
  \U00020000{3}: len=4 units=[D840 DC00 DC00 DC00] wellFormed=False realRegexMatches=True
  ```
  Every draw passed the real .NET engine, so this is ADR-0027 working: under .NET semantics the pattern's language does contain those strings. What is wrong is the silence. `WithChars` refuses a surrogate (ADR-0075), and the `WithoutChars` message claims "no draw ever yields half of an astral code point".
- **Prior:** new.
- **Recommendation:** do not treat pairs as atoms (that breaks ADR-0027). State on the strings page (EN/FR) that a pattern is read in UTF-16 code units and that an astral character is written as a group, `(\U00020000){3}`. Print surrogates as `\uXXXX` in the range refusal. Optionally extend the JD014 family to a constant pattern whose parse splits a pair.

#### CORE-6 — `DifferentFrom` over a generator that yields one value gives a hedged message
<a id="core-6"></a>
- **Kind / severity:** friction, low · **Verdict:** partly
- **Met in:** 27 (D). Finding 27D-2.
- **What happens:** `Any.Fixed(s).DifferentFrom(s)` exhausts its 1 000-draw budget and reports "Could not draw a value different from …: 1000 draws in a row produced it", although the cardinality hint already knows the domain is empty. The value is escaped as ADR-0075 requires (the report of raw control characters is refuted). The test was about the equal-signals refusal, so the value was not a dummy; the session switched to the constructor.
- **Recommendation:** when the operand's cardinality is 1 and contains the excluded value, refuse before the loop with a definite message ("the generator yields only that value").

#### Core, compact
<a id="core-low"></a>

| Id | Item | Kind, verdict | Met in | Recommendation |
|---|---|---|---|---|
| CORE-7 <a id="core-7"></a> | `IAny<T>` has only a single-value `DifferentFrom`; excluding a set means chaining it | idea, partly | 31 (E), 50 (E): 31E-7, 50E-4 | Document the constructive route through the component (`WithValue(v => v.Except(a, b))`), checked when declared. An `Except(IEnumerable<T>)` on `IAny<T>` amends ADR-0121. Every typed builder already has `Except(IEnumerable<T>)`, which closes REPORT-2 CORE-2. |
| CORE-8 <a id="core-8"></a> | No `Any.BigInteger()`, and no page mentions `BigInteger` | idea, partly | 42 (R, E): 42R-2, 42E-2 | Document the one-line composition `Any.ArrayOf(Any.Byte()).WithCount(32).As(b => new BigInteger(b, isUnsigned: true))` and a closed-range form. A first-class factory needs a bounded default for an open side, so an ADR first (the 2026-07-20 audit lists it). The sessions' ~60-line builders came from the missing recipe, not a missing capability. |
| CORE-9 <a id="core-9"></a> | No documented way to draw an undefined enum value for a refusal test | idea, partly | 17 (R): 17R-9 | A paragraph on enums-and-choices: `Any.Int32().Except(Enum.GetValues<T>().Select(…))`, which stays undefined when a member is added. Not `AnyEnum<T>.Undefined()`: it contradicts the type's contract and raises a `[Flags]` question. |

### CLI (`any`)

#### CLI-1 — A parameter derived in `Generate()` keeps pins that compile and are not honoured
<a id="cli-1"></a>
- **Kind / severity:** friction, medium · **Verdict:** confirmed
- **Met in:** 01 (R), 02 (R, E), 05 (D, R), 07 (D, E), 08 (R, E), 09 (D), 10 (R), 11 (D, R, E), 12 (D, R, E), 13 (D, E), 14 (D), 16 (E), 17 (E), 18 (D), 20 (D, R, E), 21 (R, E), 22 (D), 24 (D, E), 25 (D, R, E), 26 (E), 28 (D, R, E), 29 (R, E), 30 (D, R, E), 31 (R, E), 32 (R, E), 34 (D, R, E), 35 (D), 36 (D, R, E), 38 (D, R, E), 39 (D), 40 (D, R, E), 41 (R), 42 (D, E), 43 (R), 44 (R), 45 (D, R, E), 46 (R), 47 (R, E), 48 (D, R), 49 (D, R, E), 50 (D, R). Findings 01R-2, 02R-8, 02E-2, 05D-4, 05R-3, 07D-5, 07E-5, 08R-8, 08E-2, 09D-3, 10R-3, 11D-3, 11R-5, 11E-2, 12D-5, 12R-3, 12E-2, 13D-3, 13E-3, 14D-5, 16E-8, 17E-6, 18D-4, 20D-8, 20R-2, 20E-5, 21R-4, 21E-5, 22D-4, 24D-4, 24E-2, 25D-10, 25R-6, 25E-5, 26E-3, 28D-6, 28R-3, 28E-5, 29R-3, 29E-7, 29E-8, 30D-1, 30R-3, 30E-3, 31R-2, 31E-3, 31E-4, 32R-4, 32E-5, 34D-5, 34R-2, 34E-2, 35D-5, 36D-2, 36R-6, 36E-4, 38D-3, 38R-5, 38R-6, 38E-10, 39D-6, 40D-1, 40R-8, 40E-2, 41R-6, 42D-3, 42E-3, 43R-6, 44R-8, 45D-3, 45R-6, 45E-6, 46R-5, 47R-5, 47E-3, 48D-2, 48R-4, 49D-6, 49R-3, 49E-4, 50D-3, 50D-4, 50R-3.
- **What happens:** the recap names a tie precisely (`ties kind to quantity`); the CLI page advises deriving one side in `Generate()`. After that edit:
  ```
  WithKind(Sms).WithQuantity(5)                 -> Kind=Sms Quantity=1   (the pin is ignored)
  Assert.Throws(… WithKind(Sms).WithQuantity(5)) -> No exception was thrown (a contradictory pin is repaired, not refused)
  WithKind(Sms).WithQuantity(q => q.GreaterThan(100)) -> Quantity=1
  derived Generate(), WithQuantity(5): pin ignored on 102 of 300 seeds
  per-parameter Generate(), WithQuantity(5): constructor threw on 103 of 300 seeds
  ```
  The build is clean and no JD rule fires. No public seam tells a pinned parameter from a drawn one; sessions hand-wrote `_xPinned` flags or tested `ReferenceEquals(_x, _xRecipe)`, an undocumented artefact of the field layout. For a direct order the tool already emits a nullable `_{param}Pin` field and honours it; for every other tie it emits only `_x = Any.Fixed(value)`.
- **Minimal repro:** `UsageRecord(UsageKind kind, long quantity, …)` with `if (kind == UsageKind.Sms && quantity != 1) throw`; `any generate UsageRecord`; write `long quantity = kind == UsageKind.Sms ? 1L : _quantity.Generate();` in `Generate()`; then `Any.UsageRecord().WithKind(UsageKind.Sms).WithQuantity(5).Generate()`.
- **Why it matters:** a declared call-site constraint the generator silently does not honour can give a false green, and a contradictory pin that should reach the constructor's refusal is repaired instead. The page's warning (lines 746–756) states the consequence, then advises "Pin both, or write the tie in `Generate()`" — the second half is what disconnects the pin. Its wording, "the test then depends on the seed", also frames the outcome as the test's; what a failure there shows is a generator that does not honour its own declaration.
- **Prior:** REPORT-2 CLI-8, confirmed; answered since for orders only. Second trial in a row; the most recurrent friction of this one.
- **Recommendation:** extend the ordered pair's pin record to every parameter the recap already flags "ties X to Y": emit the nullable `_{param}Pin` (set by the value overload, cleared by the refining one, carried by the copy constructor), so a hand-written `Generate()` can read `if (_quantityPin is { } q) …`. That needs no solver and widens no bound: the tool already knows the tie and already emits this shape for one case. Have the tied parameter's TODO name that field. On the CLI page (EN/FR), add a compiled discriminant-plus-payload sample that honours a present pin and refuses a contradictory one, and reword "depends on the seed".

#### CLI-2 — The restore `any` needs is undocumented, and an unrestored batch is reported as its first type's refusal
<a id="cli-2"></a>
- **Kind / severity:** friction, low · **Verdict:** partly
- **Met in:** 34 domains — 01 (R), 02 (D, E), 03 (D), 04 (D), 07 (D, E), 08 (E), 09 (D), 11 (D, R, E), 12 (D), 13 (D, R), 14 (R), 15 (D), 16 (R), 17 (D, R, E), 18 (D), 19 (E), 20 (D, R), 21 (E), 22 (R), 23 (R), 28 (D, R), 29 (D), 30 (D), 32 (D, R), 33 (D), 34 (D), 36 (D, R), 37 (R), 39 (D, R), 40 (R), 42 (D), 43 (R), 46 (D), 47 (D). 46 findings, from 01R-3 to 47D-9.
- **What happens:**
  ```
  any generate Alpha Beta Gamma --dry-run
  ✗ Alpha: this project has not been restored, so its package references cannot be read.
    Restore it, then run the command again: dotnet restore
  (exit 1; nothing for Beta or Gamma; no closing summary)
  ```
  Under `--format json`, `results` holds one entry for `Alpha` with `summary.failed: 1`; the other arguments are missing. The early stop is deliberate and tested ("said once"); labelling a fact about the project as one type's refusal is not, and the user page says a batch's types "are processed independently". The project-did-not-load paths already report a run-level `refusal`.
- **Why partly:** the message itself is clear (REPORT.md CLI-B7's fix holds); "exit 2" is refuted (it exits 1, as the spec says); an auto-restore is refused by design (spec §7). The frequency is mostly the harness's hand-written `.csproj` ([§2](#2-limits-of-this-trial)).
- **Recommendation:** one sentence in the CLI page's "Requires" (EN/FR): the project must be restored. Report "not restored" (and "library not referenced") as a run-level refusal on stderr and in the JSON's top-level `refusal`; state the early stop in spec §7 and next to "processed independently".

#### CLI-3 — A one-sided floating-point row reaches the type's limit one draw in ten, and the recap only says `guard`
<a id="cli-3"></a>
- **Kind / severity:** friction, low · **Verdict:** partly
- **Met in:** 01 (E), 06 (D), 07 (D, R), 09 (R), 10 (D, E), 11 (D), 13 (R), 14 (R), 15 (D, R), 16 (D), 21 (D, R, E), 29 (R), 30 (D, E), 31 (R), 33 (E), 38 (D), 40 (D, R, E), 48 (R, E), 49 (D, E), 50 (R). Findings 01E-14, 06D-2, 07D-6, 07R-4, 09R-5, 10D-6, 10E-6, 11D-2, 13R-3, 14R-5, 15D-9, 15R-12, 16D-5, 21D-5, 21R-9, 21E-6, 21E-7, 29R-5, 30D-7, 30E-4, 31R-8, 33E-9, 38D-5, 40D-8, 40R-10, 40E-5, 48R-11, 48E-12, 49D-7, 49E-8, 50R-9.
- **What happens:** `ThrowIfNegative(nightlyRate)` scaffolds to `Any.Decimal().GreaterThanOrEqualTo(0m)  guard`, with `remark: null` in the JSON. By ADR-0117 the open side reaches `decimal.MaxValue` one draw in ten, so domain arithmetic on a fresh draw overflows on about 8–10 % of draws. The only signal is JD036, at Info. An unguarded date row has an empty last column.
- **Why partly:** the draw is deliberate and documented (numbers page; the spec says the engine will not invent a range to quieten JD036), and it is the draw doing its job: several domains learned this way that a quantity had no ceiling ([§9](#9-domain-defects-found-by-the-sessions)). Not held: dates overflow at about the offset divided by the span (`+90 days` never in 20 000 draws); whole-domain money ranges (48, 50) and integer rows have other causes.
- **Recommendation:** leave the draw alone and surface it through the existing `remark` channel (#361's size remarks): one sentence under the rows for a floating-point row with an open side ("one draw in ten reaches double.MaxValue (ADR-0117); declare its range in `AnyValid…()` if the domain computes with it — JD036"). Word an optional date remark as a reach, not a frequent overflow. Pairs with [DOCS-5](#docs-5).

#### CLI-4 — A same-type `void` guard helper is never read
<a id="cli-4"></a>
- **Kind / severity:** friction, medium · **Verdict:** confirmed
- **Met in:** 01 (E), 02 (R, E), 03 (E), 04 (D, R), 05 (D), 08 (R, E), 12 (D, R, E), 13 (D, E), 14 (E), 16 (E), 18 (D, R, E), 28 (D, R, E), 45 (D). Findings 01E-12, 02R-5, 02E-4, 03E-2, 04D-7, 04R-8, 05D-3, 08R-4, 08E-6, 12D-2, 12R-2, 12E-4, 13D-2, 13E-2, 14E-4, 16E-10, 18D-3, 18R-8, 18E-3, 28D-5, 28R-4, 28E-7, 45D-5.
- **What happens:** changing only the helper's return type flips the result.
  ```
  void Validate(amount):    amount  decimal  Any.Decimal()  to verify, unread guards
                            amount: unread guard at Price.cs, line 7: Validate(amount);
  decimal Validate(amount): amount  decimal  Any.Decimal().Positive().LessThan(PriceReturning.Ceiling)  guard
  ```
  A parameter passed but not guarded inside the helper is blocked too; a guard the recipe already satisfies (`!double.IsFinite`) costs nothing inline but blocks through the helper; the TODO quotes the call, never a rule inside.
- **Minimal repro:** `public Price(decimal amount) { Validate(amount); Amount = amount; }` with `private static void Validate(decimal a) { if (a <= 0m) throw …; if (a >= Ceiling) throw …; }`, against the same body returning `a`.
- **Prior:** REPORT.md CLI-G4 (still open); REPORT-2 CLI-14 fixed for returning helpers (#407, #416), whose text already noted the void case.
- **Recommendation:** this trial is the ADR-0085 field report. Extend #416's one-level read to a same-type `void` helper under the same gate (ordinary, non-virtual method with a block body in the same tree, no `ref`/`out`), folding as a delegated constructor. With the result discarded there is no normalisation to tell apart, so this removes an asymmetry and adds no mechanism. It is not a bug — the build stops instead of drawing wrong values.

#### CLI-5 — Throws inside one compound statement share one pointer
<a id="cli-5"></a>
- **Kind / severity:** defect, medium · **Verdict:** partly (the counted part is documented), confirmed for the loop case
- **Met in:** 07 (R), 10 (R), 16 (D), 17 (E), 24 (D, E), 26 (D, R, E), 30 (R), 36 (D, E), 39 (E), 45 (R, E), 46 (R). Findings 07R-3, 10R-4, 16D-3, 17E-4, 24D-1, 24E-1, 26D-2, 26D-4, 26R-1, 26E-1, 26E-2, 30R-4, 36D-3, 36E-2, 39E-3, 45R-5, 45E-8, 46R-6.
- **What happens:** naming the first unread guard and counting the rest ("…, and 1 more", #373) is the documented answer to REPORT-2 CLI-3. But `MarkIfItRejects` marks per leading statement: when an `if` block or a loop holds several throws, every parameter it touches gets one place, the first throw. So:
  ```
  cow: unread guard at Session.cs, line 12: if (last > startedAt) ...
  settings: unread guard at Session.cs, line 12: if (last > startedAt) ...      (line 17, the only guard on settings, is never named)
  items: … line 7 (a for loop with two throwing ifs; JSON "further": 0)
  network: unread guard at Prefix.cs, line 11: if (hostBits != 0) { ... }     (no "ties": the tie goes through a local)
  ```
  A parameter only the second throw tests is sent to a line that does not mention it, there is no "and N more", and a tie through a local (`hostBits`, `produces`, `last`) is never called out. After deleting the two TODO lines on the named guard, a `Cow` generator was refused 416 of 2 000 draws.
- **Prior:** the residue of REPORT-2 CLI-3 and close to REPORT-2 CLI-6.
- **Recommendation:** mark one place per throw, at that throw's innermost holding statement, for the parameters its own deciding parts reach (including through copies), so `Further` counts every throw. Attach the "ties" note whenever a throw's deciding parts reach two or more parameters, as spec §5.6 promises. Add golden or corpus rows for the Session, Row, Prefix and Curve shapes.

#### CLI-6 — Sentinels on guards the default recipe already satisfies
<a id="cli-6"></a>
- **Kind / severity:** friction, low · **Verdict:** partly
- **Met in:** 02 (D), 03 (D, E), 06 (D), 11 (D, E), 20 (R, E), 26 (R), 44 (E), 45 (R), 46 (R, E), 49 (D). Findings 02D-7, 03D-4, 03E-3, 06D-6, 11D-5, 11E-3, 20R-4, 20E-6, 26R-4, 44E-12, 45R-4, 46R-7, 46E-6, 49D-3.
- **What happens:** `at.Kind != DateTimeKind.Utc`, `at.Offset != TimeSpan.Zero`, `!Enum.IsDefined` in a loop or behind `HasValue &&`, and enum `&&`/switch guards that list every member all come back `to verify, unread guards`; with the sentinels deleted each gave 0 refusals in 2 000 draws. The user learns the recipe was right only from another page.
- **Why partly:** specified (§5.3's closed set; a guard "contains no `&&`" and is not under a loop; ADR-0083). The subset cases (a switch covering some members) do refuse about a third of draws, so they must stay marked.
- **Recommendation:** an ADR-0085 field report for exact rows: `Kind != Utc` → none (as `!double.IsFinite`); `Offset != Zero` → `.WithOffset(TimeSpan.Zero)`; an enum switch or `!=` chain that covers every declared member → none. Leave the in-loop and `&&`-on-nullable shapes to the existing placement rule.

#### CLI-7 — The recap and the JSON never say two parameters were drawn together
<a id="cli-7"></a>
- **Kind / severity:** defect, low · **Verdict:** confirmed
- **Met in:** 06 (R, E), 22 (D), 23 (D), 24 (E), 25 (D), 29 (R), 32 (D, R), 34 (D, R). Findings 06R-5, 06E-3, 22D-2, 23D-3, 24E-5, 25D-8, 29R-4, 32D-6, 32R-6, 34D-4, 34R-4.
- **What happens:**
  ```
  minCelsius  decimal  Any.Decimal().GreaterThanOrEqualTo(-40m)  guard
  maxCelsius  decimal  Any.Decimal().LessThanOrEqualTo(40m)      guard
  ✓ AnyTemperatureRange.cs — 2 of 2 parameters inferred.
  ```
  The file draws them through `Any.OrderedPair(…, strict: true)`. The engine holds the pairing (`ScaffoldPlan.Pairings`) but only the emitter reads it. When the order is the only guard, the row reads `Any.Int32()  guard`: a `guard` tag with no visible tightening, against the page's own definition of the word. Spec §6 calls the recap "the mechanism that keeps the tool honest about what it inferred".
- **Recommendation:** render the pairings: one line per pair under the rows (`minCelsius, maxCelsius: drawn together through Any.OrderedPair (minCelsius < maxCelsius, line 7)`), a `pairedWith` field in the JSON, the word in the CLI page's table and spec §6 (EN/FR), a recap test beside `OrderedPairTests`, and any ux-trial probe that pins a recap.

#### CLI-8 — A bound held in a `static readonly` field is not read
<a id="cli-8"></a>
- **Kind / severity:** friction, low · **Verdict:** partly
- **Met in:** 04 (E), 08 (D), 16 (R), 19 (D, R), 20 (D, R, E), 48 (E). Findings 04E-7, 08D-4, 16R-4, 19D-2, 19R-4, 20D-7, 20R-5, 20E-4, 48E-7.
- **What happens:** `ThrowIfGreaterThan(maximumLifetime, HardMaximumLifetime)` with `static readonly TimeSpan HardMaximumLifetime = TimeSpan.FromHours(24)` stays `to verify`; the same guard written `TimeSpan.FromHours(24)` inline is read. Even `static readonly int` is unread. The TODO gives no reason, unlike the regex one.
- **Why partly:** for 5 of the 9 members (`Half` and `DateOnly` bounds), `static readonly` is not the cause: no spelling of a `Half` or date bound is read at all.
- **Recommendation:** say so on the CLI page (EN/FR): a `static readonly` field is not a constant; `TimeSpan` bounds are read only inline; `Half` and date bounds are never read. Following a `static readonly` whose single write is an initializer the reader already evaluates exactly is a candidate ADR-0085 decision.

#### CLI-9 — The regex hint is lost for a target-typed `new(...)` field and for `Match(...).Success`
<a id="cli-9"></a>
- **Kind / severity:** defect, low · **Verdict:** partly
- **Met in:** 02 (D), 04 (R), 09 (D), 15 (R), 27 (E), 32 (E), 43 (E), 49 (E). Findings 02D-3, 04R-9, 09D-6, 15R-10, 27E-2, 32E-7, 43E-4, 49E-6.
- **What happens:** the pattern is quoted, with the `AnyPattern` retype, for `Regex.IsMatch(p, "…")`, `[GeneratedRegex]` and a field built with `new Regex("…")`. It is lost for a field built with target-typed `new("…")` (02D-3, 27E-2, 32E-7), because `Guards.PatternOf` matches only `ObjectCreationExpressionSyntax` — a slip against spec §5's own sentence ("that of a `Regex` field built from a constant"), with no test on the field case. It is also lost for `Match(...).Success`, the half of REPORT-2 CLI-13 that #374 never closed, and for a conjunction or a helper.
- **Minimal repro:** `private static readonly Regex Format = new("^[A-Z]{1,2}[0-9]{1,4}$");` and `if (!Format.IsMatch(value)) throw …` → the TODO has no pattern; `new Regex("…")` → it has.
- **Recommendation:** match `BaseObjectCreationExpressionSyntax` (on the 4.8.0 Roslyn floor) and add the field rows to `GuardReadingTests`; extend `RegexPatternIn` to `Match(p).Success`, including a local assigned from `Match`. 09D-6 belongs to [CLI-4](#cli-4).

#### CLI-10 — A guard behind a call the tool does not follow leaves the row unmarked
<a id="cli-10"></a>
- **Kind / severity:** friction, medium · **Verdict:** partly
- **Met in:** 05 (D, R, E), 33 (D), 48 (D, R, E), 50 (D). Findings 05D-2, 05R-2, 05E-2, 33D-4, 48D-1, 48R-1, 48E-1, 50D-2.
- **What happens:** the recap shows no mark and says "N of N parameters inferred"; the file compiles and the constructor refuses most draws. In 48, a domain-wide `Guard` class left `label` as a bare `Any.String()` and `IncomeKind` as `Any.Enum<IncomeKind>()`, which draws `None` about one time in five. In 50, a weekend trade date was scaffolded as a plain `Any.DateOnly()`.
- **Why partly:** a validator of another type, a computing method of another object and an operator over an aggregate are documented as silent ("What the tool still cannot see stays silent"; spec §9, ADR-0085). The inconsistent shape is 05: a parameterless same-type `Validate()` called below the first assignment is silent, while three neighbouring shapes are marked.
- **Recommendation:** for 05, a decision: a call to a same-type method with source whose body throws, placed below the first assignment, marks every parameter `to verify`, as ADR-0110 does for `if (TooBig()) throw` — it only adds refusals. The other-type shapes are §9 non-goals; reading a static helper of another type in the same compilation is an option to argue under ADR-0085/0046, not a fix.

#### CLI-11 — A framework type with no library factory is composed as a blind `new AnyX()`
<a id="cli-11"></a>
- **Kind / severity:** defect, medium · **Verdict:** partly
- **Met in:** 32 (R, E), 42 (D, R, E), 47 (E). Findings 32R-3, 32E-8, 42D-1, 42D-2, 42R-1, 42E-1, 47E-5.
- **What happens:**
  ```
  baseUnits  BigInteger   new AnyBigInteger()   AnyX, constraint unavailable
  fromEndMva Complex      new AnyComplex()      AnyX
  culture    CultureInfo  new AnyCultureInfo()  AnyX
  ```
  The build then fails with CS0246, which hides the file's `TODO_verify` sentinel for the unread `baseUnits < 0`. The documented next step, `any generate BigInteger`, does not work for two of the three types.
- **Why partly:** the blind call is specified (spec §5.2, §5.4: "CS0246, or a clean build, is the only verdict"); "no TODO sentinel" is refuted — it is there, masked. But ADR-0089's premise is "a domain type of the developer's own", and the spec's "any other non-generic named type" is wider than that.
- **Recommendation:** a decision narrowing ADR-0089: treat a metadata type with no base-table row like an abstract one — a §5.5 sentinel, a recap word (`framework`), and a TODO naming a hand-written `AnyBigInteger : IAny<BigInteger>` or `Any.Int64().As(v => new BigInteger(v))` rather than `any generate`. Otherwise document the case on the CLI page and in spec §5.4.

#### CLI-12 — `ThrowIfLessThan(a, b)` between two parameters is not paired, while `if (a < b) throw` is
<a id="cli-12"></a>
- **Kind / severity:** friction, medium · **Verdict:** confirmed
- **Met in:** 10 (D, R, E), 17 (D, R, E). Findings 10D-4, 10R-2, 10E-2, 17D-7, 17R-4, 17E-3.
- **What happens:**
  ```
  issuedOn       DateOnly  Any.DateOnly()  to verify, unread guards
  effectiveFrom  DateOnly  Any.DateOnly()  to verify, unread guards
  issuedOn: unread guard at RecallNotice.cs, line 5: ArgumentOutOfRangeException.ThrowIfLessThan(effectiveFrom, issuedOn);
  ```
  The helper's second argument must be a compile-time constant, "the same as a comparison's other side" — an analogy that went out of date when the pair row made a parameter a legal other side. The tool reads one spelling and refuses its exact equivalent, which the spec itself calls "the worst of both outcomes". A test pins the current behaviour.
- **Recommendation:** read the two-argument helpers as orders when both arguments are distinct parameters of one non-nullable type the library has a pair for: `ThrowIfLessThan(a, b)` → `OrderedPair(b, a, strict: false)`, `…OrEqual` → `strict: true`, mirrored for `GreaterThan`. Replace the pinning test with a pairing assertion and add a corpus row.

#### CLI-13 — A negated order between two parameters is not paired
<a id="cli-13"></a>
- **Kind / severity:** friction, low · **Verdict:** confirmed
- **Met in:** 31 (D, R, E). Findings 31D-2, 31R-3, 31E-2.
- **What happens:** `!(high > low)` is left `to verify`, even on `int`, while the one-parameter reader reads `!(p > N)`; `high <= low || double.IsInfinity(high)` is paired. `TryOrdering` does not unwrap a logical not.
- **Recommendation:** accept a `LogicalNotExpression` around a comparison of the two parameters in `TryOrdering`, mapping the kind through the existing `Negated(SyntaxKind)`.

#### CLI-14 — A helper whose bound is passed as an argument is read only for the bound written inside it
<a id="cli-14"></a>
- **Kind / severity:** friction, low · **Verdict:** confirmed
- **Met in:** 21 (D, R, E), 22 (D, R, E). Findings 21D-4, 21R-3, 21E-2, 22D-3, 22R-3, 22E-2.
- **What happens:** `Row = Guard(row, MaxPlane, nameof(row))` with `if (double.IsNaN(value) || value < Min || value > max) throw` gives `Any.Double().GreaterThanOrEqualTo(Spacing.Min)  to verify, guard, unread guards` for both `row` and `slice`; each TODO quotes the same helper line, not the call that holds its own bound. No call-site constant is substituted into a helper parameter. Safe — every parameter is marked and the build stops. The `void` variant in 21 is documented.
- **Recommendation:** point the mark at the call site, or add it (`value > max — max is the helper's parameter, given MaxPlane at line 10`); one sentence on the CLI page and in spec §5.6 (EN/FR). Update golden files and any probe pinning the message.

#### CLI-15 — Replacing a scaffolded base is a hand retype, and it leaves the `With…(Func)` remarks naming the old type
<a id="cli-15"></a>
- **Kind / severity:** friction / defect, low · **Verdict:** partly (retype), confirmed (remarks)
- **Met in:** 12 (E), 15 (R), 28 (D, E), 30 (D), 31 (R), 35 (R), 44 (R), 47 (R), 48 (D). Findings 12E-5, 15R-9, 28D-7, 28E-6, 30D-3, 31R-4, 35R-3, 44R-11, 47R-6, 48D-8.
- **What happens:** switching to `AnyPattern` or `IAny<T>` means retyping the factory, the recipe field, the recipe constructor and the `Func` of the refining overload; every missed site is a compile error (the claim that a missed site compiles is refuted). The retype the TODO and the page prescribe leaves `/// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> …` behind; the compiler cannot catch it, and a blind find-and-replace turns the escaped text into invalid XML.
- **Recommendation:** write the remark without the concrete builder type ("a lambda of the type of `configure`"), so it cannot go stale; update the golden files and the spec sample. Widen the page's `IAny<T>` paragraph beyond `IAny<string>` with one collection line. The retype itself is by design (see the regex row in [§5](#5-candidates-for-a-decision)).

#### CLI-16 — A character-class predicate in a lambda is not read, and its TODO gives no direction
<a id="cli-16"></a>
- **Kind / severity:** friction, low · **Verdict:** confirmed
- **Met in:** 30 (D, E), 35 (E), 37 (E), 40 (D). Findings 30D-2, 30E-2, 35E-10, 37E-12, 40D-7.
- **What happens:** `!code.All(c => c is >= 'A' and <= 'Z')`, `!siteCode.All(char.IsAsciiLetterUpper)` and `annualLosses.Any(x => x < 0m)` are unread, safely; the TODO says only "verify against …". A regex gets a quoted pattern and a hint; this gets nothing, although `code.AsSpan().IndexOfAnyExcept("AB…Z") >= 0` is read as `.WithChars(...)` today.
- **Recommendation:** a direction in the TODO for a char-class predicate (`.WithChars("…")` or `.Alpha().InUpperCase()` keep the `AnyString` type), the `IndexOfAnyExcept` rewrite in the page's "A tip that works today", and these rows as an ADR-0085 field report.

#### CLI-17 — Adding an entry point to a scaffolded type needs `--force`, which overwrites the edited generator
<a id="cli-17"></a>
- **Kind / severity:** friction, low · **Verdict:** confirmed
- **Met in:** 08 (D), 18 (R), 30 (R), 35 (D), 37 (R). Findings 08D-5, 18R-2, 30R-6, 35D-2, 37R-7.
- **What happens:** `any generate Accession --entry-point any` after a hand edit: `✗ …/AnyAccession.cs already exists. Re-run with --force to overwrite it. Whatever you changed in it is lost.` (exit 1, no `.Entry.cs` written). The generator it would have written is byte-identical to the first scaffold. "Land together or not at all" (spec §7, ADR-0070) targets the half-scaffold case; adding only the missing entry file later completes a scaffold and never touches the developer's file.
- **Recommendation:** document the non-destructive route (`--output <scratch>` and keep only the entry file; or decide in `any.json` before the first scaffold) and name it in the refusal next to `--force`. Writing only the entry file when the generator exists amends spec §7 and ADR-0070: a decision.

#### CLI-18 — A guard on a member of a parameter is unread, and the TODO does not point to the documented fix
<a id="cli-18"></a>
- **Kind / severity:** friction, low · **Verdict:** partly
- **Met in:** 09 (E), 11 (E), 23 (D, R), 42 (D). Findings 09E-4, 11E-4, 23D-4, 23R-5, 42D-5.
- **What happens:** `if (maximumPairKinship.Value > 0.25) throw` → `new AnyKinshipCoefficient()  to verify, AnyX, unread guards`. Unread by design (spec §5.3; ADR-0046 refuses propagation). "No reusable fix" is refuted: "Where the valid base lives" shows exactly this case (`return new AnyRoute().WithDomestic(true);`), and a named refinement in the partial class is the reuse route.
- **Recommendation:** extend "A tip that works today" to a member rule, with a link to "Where the valid base lives"; optionally have the TODO name the composed generator's `With…` methods (a hint, no reading).

#### CLI-19 — A field added beside a scaffolded generator is reset by the next generated `With…`
<a id="cli-19"></a>
- **Kind / severity:** docs, medium · **Verdict:** confirmed
- **Met in:** 21 (D), 28 (R), 36 (D), 45 (R). Findings 21D-2, 28R-2, 36D-7, 45R-7.
- **What happens:** the generated copy constructor copies only the scaffolded fields, and every `With…` starts with `new AnyX(this)`. A `private bool _expedited;` added in `AnyShipment.Custom.cs` with an `Expedited()` method in the page's copy/write/return shape is silently reset by the next generated `With…`. The build is clean. The header says "members you add in a neighbouring file survive" — true of the source, not of a field's value.
- **Recommendation:** say it in "Adding the overload yourself" (EN/FR) with a compiled sample: express the variant through an existing field (a recipe pinned with `Any.Fixed`), apply a lifecycle step last with `.As(…)`, or add the line to the copy constructor knowing `--force` drops it. Consider qualifying the header line.

#### CLI-20 — A scaffolded ordered pair of one-sided floating-point recipes raises JD036 although the pair bounds both ends
<a id="cli-20"></a>
- **Kind / severity:** friction, low · **Verdict:** partly
- **Met in:** 06 (D, E), 22 (E), 44 (E). Findings 06D-3, 06E-2, 22E-4, 44E-9.
- **What happens:** `TemperatureRange` scaffolds `Any.Decimal().GreaterThanOrEqualTo(-40m)` and `Any.Decimal().LessThanOrEqualTo(40m)` tied by `Any.OrderedPair`. Raised to warning, JD036 reports both lines with "one draw in ten spans the whole type", which is false in context: the pair narrows each draw by the other's bound. JD036 reasons about one expression by design, and the spec excuses JD036 in emitted code; the excuse's reason ("a range the domain never stated") does not hold for a pair.
- **Recommendation:** say on the CLI page next to the JD030 paragraph that in a pair the other side's bound already caps the draw and `Between(lo, hi)` on each side states it. Emitting the opposite bound on each member is a decision (spec §5.3/§17).

#### CLI-21 — Passing a prepared generator to a scaffolded `With…` gives a bare CS1503
<a id="cli-21"></a>
- **Kind / severity:** friction, low · **Verdict:** confirmed
- **Met in:** 13 (E), 17 (D, R), 42 (R). Findings 13E-6, 17D-10, 17R-8, 42R-6.
- **What happens:** `WithRoute(domestic)` with `domestic` an `AnyRoute` → `CS1503 cannot convert from AnyRoute to Route`, no JD diagnostic. The compile error is intended (ADR-0109); the remarks and page paragraph added since (#391) were read by two of the four sessions, who still hit it. "Library `With…`" is refuted: no library `With…` has this pair.
- **Recommendation:** a decision between a JD rule on the failed overload resolution (naming both routes: refine with `x => x.…`, or pin `prepared.Generate()`) and an emitted `[Obsolete(error: true)] With…(G generator)`.

#### CLI-22 — `p.Scale > N` on a decimal is outside the guard reader's set
<a id="cli-22"></a>
- **Kind / severity:** idea, low · **Verdict:** confirmed
- **Met in:** 02 (D, R), 12 (D, E). Findings 02D-5, 02R-6, 12D-3, 12E-3.
- **What happens:** `if (kwh.Scale > 3) throw` keeps `Any.Decimal().GreaterThanOrEqualTo(0m)  to verify, guard, unread guards`; `decimal.Round(kwh, 3) != kwh` gives `.WithScale(3)  guard`. Without the scale, 179 938 of 200 000 draws have Scale > 3. A `.Scale` row would rest on drawn values having Scale ≤ N under `WithScale(N)` — true today through `Math.Round`, not promised by ADR-0015, and false for a supplied value (`OneOf(1.0000m).WithScale(3)` keeps Scale 4).
- **Recommendation:** either amend ADR-0015 (or a new ADR) to promise Scale ≤ N for drawn values and add the row, pinned by a property test; or keep the row out and have the TODO suggest `.WithScale(N)`.

#### CLI-23 — `any generate` never says other constructors were left out
<a id="cli-23"></a>
- **Kind / severity:** friction, medium · **Verdict:** partly
- **Met in:** 46 (D, R, E). Findings 46D-3, 46R-2, 46E-3.
- **What happens:** `Prefix(V4 network, int length)` and `Prefix(V6 network, int length)` tie on width; the first in source order is taken. The recap prints `constructor Prefix(V4, int)` and "2 of 2 parameters inferred"; nothing mentions `Prefix(V6, int)`, and the generator can never reach half the type's value space — a draw that can never expose an IPv6 dependency. Widest-first is specified and tested; `--ctor` is deferred (spec §16).
- **Recommendation:** make the edge loud: a recap line naming the constructors left out and why (`not scaffolded: Prefix(V6, int) — widest first, ties by source order`), the same list in the JSON. Whether a tie of equal width should be refused like tied factories (§5.1.2) is a decision.

#### CLI-24 — A tie written in `Generate()` by refining a composed part drops a pin the test set inside that part
<a id="cli-24"></a>
- **Kind / severity:** friction, medium · **Verdict:** confirmed
- **Met in:** 08 (E), 22 (R). Findings 08E-3, 22R-2.
- **What happens:** `Dosage dosage = _dosageRecipe.WithUnit(u => u.Except(route.Forbidden…)).Generate();` starts from the test's recipe but replaces a unit the test pinned inside `Dosage`, silently and only on some seeds. The rule (a refinement after a pin replaces it) is specified (ADR-0116, Proposed); the tie section's "Two consequences follow" never mentions the composed case, and the author cannot see the nested pin.
- **Recommendation:** add the third consequence on the CLI page (EN/FR) and show the two shapes that keep a nested pin (draw the side the test may pin first, then refine the other).

#### CLI-25 — A parameter handed to a framework base constructor is marked as an unread guard
<a id="cli-25"></a>
- **Kind / severity:** defect, low · **Verdict:** confirmed
- **Met in:** 23 (R), 36 (R), 42 (R). Findings 23R-4, 36R-5, 42R-7.
- **What happens:** every parameter reaching `: base(message)` or `: base($"… {a} … {b}")` on an exception type gets `to verify, unread guards` and blocks the file, with no guard and no explanation.
- **Recommendation:** a decision on the ADR-0085 path, narrowest first: read the framework exception constructors (`Exception(string)` and the like) as non-rejecting.

#### CLI-26 — The scaffolder spells a type by its short name even where it is ambiguous
<a id="cli-26"></a>
- **Kind / severity:** defect, low · **Verdict:** confirmed
- **Met in:** 44 (R). Finding 44R-6.
- **What happens:** `TypeNames.cs` writes every parameter type by its short name and adds a `using` for its namespace, without checking the other namespaces the file opens. With the domain's own alias, the emitted `Shipment` fails with CS0104, against the remark that the short name and its `using` are one decision.
- **Recommendation:** before writing, check each short name against every opened namespace and qualify on a clash.

#### CLI, compact
<a id="cli-low"></a>

| Id | Item | Kind, verdict | Met in | Recommendation |
|---|---|---|---|---|
| CLI-27 | A three-parameter order (min < opt < max) is marked, not drawn (by design, ADR-0126); the count does not say the further guard ties parameters | friction, partly | 21 (R, E): 21R-2, 21E-4, 21E-16 | ", and 1 more, which ties a, b and c", or name a tying guard first. |
| CLI-28 | `Math.Round(double, N) != p` is unread with a generic TODO; no page shows a double in hundredths | friction, partly | 17 (D, R, E): 17D-8, 17R-6, 17E-5 | Report it as `constraint unavailable`, with a TODO naming the replacement (`Any.Int32().Between(…).As(k => k / 100d)`, or a decimal with `WithScale(2)` converted by `.As`); one sentence on the numbers page. `WithScale` on `AnyDouble` is a decision. |
| CLI-29 | `p % N != 0` is unread, and the rewrite tip does not point to `MultipleOf`/`Even` | idea, partly | 18 (D, R), 25 (E): 18D-5, 18R-3, 25E-3 | One sentence in "A tip that works today"; a row is an ADR-0085 decision. |
| CLI-30 | A batch scaffold blocks the whole project until every `to verify` file is resolved | friction, partly | 29 (E), 43 (E): 29E-9, 43E-6, 43E-7 | Keep the block (ADR-0083); advise scaffolding the types tests draw, aggregates first. |
| CLI-31 | A multi-argument static factory is refused without naming the near-miss factory or the hand-written route | friction, confirmed | 10 (D, R, E): 10D-3, 10R-5, 10E-3 | Name `Of(int, int)` and the hand-written route in the refusal; a multi-argument factory as a constructor is a candidate decision. |
| CLI-32 | A negated public constant is folded to a literal: `Between(-999999999999999m, Money.MaxMagnitude)` | friction, confirmed | 50 (D, R): 50D-10, 50R-8 | See through a unary minus and emit `-Money.MaxMagnitude`. |
| CLI-33 | A batch recap ends with no list of the files that will not compile yet | friction, confirmed | 39 (E), 42 (R): 39E-6, 42R-8 | Extend the #379 closing summary to name them. |
| CLI-34 | Deleting the two documented lines leaves a blank line after the brace | friction, confirmed | 22 (R), 40 (D): 22R-6, 40D-6 | Drop the blank line the emitter writes after the sentinel; update golden files. |
| CLI-35 | A quoted guard ending in `;` gets `;;` in the TODO and `;,` in the recap | defect, confirmed | 21 (E), 30 (D): 21E-3, 30D-4 | Make the separator depend on how the quoted line ends. |
| CLI-36 | Closed string sets (`is not ("FR" or "BE")`) stay `Any.String()` with nothing pointing to `OneOf` | idea, confirmed | 14 (D, E): 14D-2, 14E-2 | Have the TODO name `Any.String().OneOf(…)`. |
| CLI-37 | The header's TODO(any) block stays after the last recipe is accepted | friction, confirmed | 23 (E): 23E-4 | Make the block say to delete itself once no TODO is left. |
| CLI-38 | A `[Flags]` enum is scaffolded as a plain `Any.Enum<T>()`, with no pointer to `AllowingCombinations()` | friction, confirmed | 46 (E), 48 (R): 46E-5, 48R-6 | Keep the default (ADR-0020); add a recap remark naming the opt-in. |
| CLI-39 | The regex TODO does not say that `StringMatching` drops the length guard the base carried | docs, confirmed | 46 (E): 46E-7 | Name the dropped constraints in the TODO. |
| CLI-40 | "Also answers to that name" names nested or static framework types `any` could never scaffold | defect, confirmed | 19 (R, E): 19R-5, 19E-4 | Exclude static classes from `Nameable`; decide on nested types in §3.2. |
| CLI-41 | The sentinel's CS0103 names the parameter, not the reason | idea, confirmed | 09 (D): 09D-4 | Document how to re-list open guards (`--format json`); a richer identifier is a decision on ADR-0060. |
| CLI-42 | Scaffolded files are long (1 247 lines for 7 types, ~60 % plumbing) | idea, confirmed | 17 (E): 17E-8 | A decision against ADR-0109's "one shape for every parameter" (`--minimal`/`--with`). |
| CLI-43 | On an unrestored project, a type reached through a transitive reference is "no type by that name" | defect, confirmed | 45 (R): 45R-2 | Print the not-restored refusal when `!restored` and the type is not found. |
| CLI-44 | The header's re-run line omits `--output`, `--namespace`, `--entry-point-namespace` | defect, confirmed | 33 (E): 33E-7 | Repeat every option that shaped the file, `--output` relative to the project. |
| CLI-45 | A guard on a value computed from a parameter is marked with no note | defect, partly | 35 (D, R): 35D-3, 35R-2 | Keep the refusal (propagation, ADR-0046); give the copy-trail mark a note. |
| CLI-46 | A `StartsWith(const)` branch of a `\|\|` guard stays unread with no remedy named | friction, partly | 20 (E): 20E-3 | Document `.StartingWith(…)` plus `.WithChars(…)` for a prefix plus character loop; no `StringMatching` hint. |
| CLI-47 | Size remarks cite "(ADR-0076)" as a bare number | friction, partly | 20 (D): 20D-9 | Replace with a link to the user page, or drop the number. |
| CLI-48 | A nullable parameter's sentinel sits in `AnyValid…()`, but its `null` comes from `.OrNull()` in the private constructor | friction, partly | 33 (E): 33E-3 | Keep D10; for a guard involving nullness, have the TODO say that this factory never draws `null` and that the default's `null` comes from `.OrNull()` in the constructor. |
| CLI-49 | Idea: a one-call pinned copy of an existing value (`AnyX.Like(x)`) | idea, partly | 47 (R): 47R-11 | A documented recipe, not a scaffold (reading a parameter back from a property is a heuristic). |
| CLI-50 | Pin docs carry the "one shared instance for a reference type" clause on value types too | docs, partly | 05 (E): 05E-6 | Drop the clause for primitives, enums and `string`. |

### Analyzers

#### AN-1 — JD012 flags a pool of generators that `Then(g => g)` flattens, the remedy its own message names
<a id="an-1"></a>
- **Kind / severity:** defect, low · **Verdict:** partly
- **Met in:** 36 (R, E), 48 (D, R). Findings 36R-8, 36E-5, 48D-3, 48R-2, 48R-3.
- **What happens:**
  ```
  warning JD012: Call Generate() on each pooled generator, or choose one with Then: Any.ElementOf inferred a pool of recipes, so drawing from it yields a recipe rather than a value
      <- Any.ElementOf(leaves).Then(leaf => leaf)
  ```
  At run time that form draws values, split about evenly, and replays from one seed. The analyzer never looks at how the pool is used; the JD012 page's "What it does not flag" and its tests are silent on this case. The sub-claims are refuted: `Any.Boolean().Then(b => b ? x : y)` is the documented clean choice, `Any.Int32().Between(0, n - 1).Then(i => generators[i])` is clean for n branches, and `IAny<out T>` is covariant, so no upcast is needed.
- **Prior:** REPORT-2 CORE-12, answered by `.Then` (ADR-0119); the false positive is new.
- **Recommendation:** do not report a `OneOf`/`ElementOf` pool that is the receiver of `Then` with a non-generator result; add the test both ways and the case to the JD012 page (EN/FR). If the rule stays syntactic, reword the message so "choose one with Then" means drawing an index or flag.

#### AN-2 — A constant `null` in `Any.OneOf<T?>(…)` builds with no diagnostic and always throws
<a id="an-2"></a>
- **Kind / severity:** idea, low · **Verdict:** confirmed
- **Met in:** 46 (D, R, E). Findings 46D-5, 46R-8, 46E-8.
- **What happens:** `Any.OneOf<byte?>(null, (byte)6, (byte)17)` and `Any.OneOf("a", null)` (T inferred as `string?`) build clean and throw `ArgumentException: The values must not contain a null element; use OrNull() …` — at declaration, not at `Generate()` as the sessions thought. JD014 hands `null` to nullable analysis, which cannot see a nullable `T`. `ArgumentGuardFacts.MayRefuse` already recognises the case and never reports it.
- **Recommendation:** report an inline constant null element in JD014's `OneOf` row, with the guard's own wording; narrow the JD014 page's "left to nullable analysis" line (EN/FR); add unit tests.

#### AN-3 — With JD011 opted in, JD005 and JD011 both report a generator concatenated into a string
<a id="an-3"></a>
- **Kind / severity:** defect, low · **Verdict:** confirmed
- **Met in:** 16 (E). Finding 16E-11.
- **What happens:** `Any.String().Generate() + Any.Int32().Between(1, 5)` and `s += Any.Int32()…` draw JD005 and JD011 on the same span, because `string + object` is an implicit conversion JD011 reports.
- **Prior:** the same class as REPORT.md AN-D1, fixed then for JD014/JD017.
- **Recommendation:** let JD005 own the text-rendering shapes (ADR-0078, "one mistake draws one diagnostic"): JD011 returns early when the conversion's parent is a string `+` or `+=`.

#### AN-4 — JD036 says "floating-point dummy" on `Any.Decimal()` chains
<a id="an-4"></a>
- **Kind / severity:** friction, low · **Verdict:** partly
- **Met in:** 13 (E), 31 (E), 37 (D). Findings 13E-4, 31E-6, 37D-7.
- **What happens:** `This floating-point dummy declares no range: one draw in ten spans the whole type` on `Any.Decimal().Negative()`. .NET itself calls `decimal` a floating-point type, so the text is not false; but the library's own pages keep `Decimal` apart from "the floating-point builders", and two sessions stopped on it.
- **Recommendation:** name the type the rule fired on (`This {0} dummy declares no range…`), keeping the catalog title, its summary, the unit-test message and the changelog in step. The ID and severity do not change.

#### AN-5 — JD009 does not follow a static initializer into a helper that draws
<a id="an-5"></a>
- **Kind / severity:** idea, low · **Verdict:** confirmed
- **Met in:** 25 (R). Finding 25R-7.
- **What happens:** `private static readonly int Inline = Any.Int32()….Generate();` draws JD009; `private static readonly int ViaHelper = Fixtures.Rate(1);`, whose body draws, does not. The rule is lexical by design; the hazard is real.
- **Recommendation:** a "What it does not flag" bullet on the JD009 and JD007 pages (EN/FR): a draw inside a method the initializer calls is not followed; return the generator from the helper instead.

### xUnit adapter

#### XU-1 — `[Reproducible]` reports a seed for a failing test that drew nothing
<a id="xu-1"></a>
- **Kind / severity:** defect, low · **Verdict:** confirmed
- **Met in:** 02 (D), 04 (E), 07 (D), 09 (E), 23 (E), 27 (E), 33 (E), 37 (R), 41 (D, E), 44 (D, E), 45 (E), 46 (R, E), 48 (E), 49 (D). Findings 02D-13, 04E-4, 07D-9, 09E-5, 23E-7, 27E-7, 33E-8, 37R-11, 41D-5, 41E-5, 44D-6, 44E-14, 45E-10, 46R-9, 46E-9, 48E-4, 49D-8. (17E-9 and 40E-7 were clustered here but describe tests that did draw; there the line is right and must stay.)
- **What happens:**
  ```
  failed SeedReportTests.Literal_only_failure (0ms)
    Assert.Equal() Failure: Values differ
    [JustDummies] These arbitrary values were seeded with 2028111925. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 2028111925)].
  ```
  No `Any.*` call ran. The adapter's own rule is never to name a seed no draw used: an outer level "prints nothing, since replaying its seed would reproduce nothing", and a superseded scope stays silent for the same reason. The no-draw case falls under that rule and is not handled. Under `[assembly: Reproducible]`, a body wrapped in `Any.Reproducibly(...)` prints two seed lines, one naming a seed nothing used. Sessions recording a domain defect found with a literal were sent looking for a drawn value that did not exist.
- **Minimal repro:** `[assembly: Reproducible]` and `[Fact] public void Literal_only_failure() { Assert.Equal(1, 2); }`.
- **Prior:** new.
- **Recommendation:** report only when the scope's source was drawn from. `SeededRandom` already counts draws (ADR-0049), but the adapter cannot read the count: a small public addition on the `Any.UseSeed` handle, or an internals grant, is needed — an ADR-0017 amendment, so route it through a decision. Apply the same rule to `Any.Reproducibly`. Until then, one sentence on the adapter page (EN/FR).

#### XU, compact
<a id="xu-low"></a>

| Id | Item | Kind, verdict | Met in | Recommendation |
|---|---|---|---|---|
| XU-2 <a id="xu-2"></a> | The replay line does not say which level the seeded attribute goes on | idea, partly | 29 (E): 29E-11 | Every natural placement replays (method, class, the assembly attribute changed); a second assembly attribute fails with CS0579. Optional wording: "…by adding [JustDummies.Xunit.Reproducible(Seed = N)] to the failing test", with the reproducibility guide (EN/FR) and any probe. |
| XU-3 <a id="xu-3"></a> | No page shows how to keep a defect-revealing seed runnable on demand | docs, partly | 28 (R): 28R-10 | The `Skip = "…, seed N"` form came from the trial brief. Document `[Fact(Explicit = true), Reproducible(Seed = N)]` with `dotnet test -- --explicit only` on the adapter page (EN/FR). |

### Documentation

#### DOCS-1 — The adapter's `xunit.v3` floor needs the Microsoft.Testing.Platform opt-in on the .NET 10 SDK, and no user page says so
<a id="docs-1"></a>
- **Kind / severity:** docs, medium · **Verdict:** partly
- **Met in:** all 50 domains, every discovery and regular session, and every expert session except 48 and 49 — 148 findings (01D-1 to 50R-1). The frequency is the harness's ([§2](#2-limits-of-this-trial), item 1).
- **What happens:**
  ```
  dotnet test --project Tests -> MSBUILD : error MSB1001: Unknown switch. … Switch: --project
  dotnet test Tests           -> …/microsoft.testing.platform.msbuild/2.5.1/…: Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. …
  ```
  `{"test":{"runner":"Microsoft.Testing.Platform"}}` in `global.json` fixes it. It belongs to JustDummies because the adapter page sets the floor "`xunit.v3` 4.0.2 or later", and `xunit.v3` 4.x runs only on Microsoft.Testing.Platform v2; a team on `xunit.v3` 3.x that raises it to adopt the adapter finds `dotnet test` broken. The same project on 3.2.2 runs under plain `dotnet test`. The only page that says it is a maintainer ADR (ADR-0103).
- **Why partly:** `MSB1001` comes from the brief's `--project` form; a plain `dotnet test <proj>` gets an explicit error naming the opt-in. `global.json` is not the only route (`dotnet run --project`, or `-p:IsTestingPlatformApplication=false`). Getting-started installs only the core package, so it is the wrong page for the note.
- **Recommendation:** a "Running the tests" note on the adapter page (EN/FR) and in `JustDummies.Xunit/README.nuget.md`, next to the existing NU1608 paragraph since both follow from the same floor: the opt-in, the two alternatives, and a link to the SDK's own page rather than a restatement of it.

#### DOCS-2 — The CLI page's tie section never shows or links the `.Then(...)` forms that already cover member, conditional, product and nullability ties
<a id="docs-2"></a>
- **Kind / severity:** docs, medium · **Verdict:** partly
- **Met in:** 05 (D), 08 (D), 09 (D, R), 13 (D), 15 (E), 16 (D), 17 (D, R), 18 (R), 21 (D), 25 (R), 26 (E), 27 (D), 31 (D), 32 (D, R, E), 33 (D, R, E), 34 (D), 35 (E), 37 (D, E), 39 (D, R, E), 40 (E), 41 (D, E), 43 (D, E), 44 (E), 45 (E), 46 (D, E), 47 (D), 49 (E), 50 (R, E). Findings 05D-5, 08D-3, 09D-2, 09R-2, 13D-4, 15E-3, 16D-4, 17D-9, 17R-5, 18R-5, 21D-3, 25R-5, 26E-7, 27D-3, 31D-3, 32D-3, 32D-5, 32R-7, 32E-9, 33D-5, 33R-4, 33E-4, 34D-6, 35E-8, 37D-8, 37E-10, 37E-11, 39D-9, 39R-2, 39E-4, 40E-3, 41D-4, 41D-11, 41E-4, 43D-3, 43D-10, 43E-3, 44E-8, 45E-7, 46D-4, 46E-4, 47D-4, 49E-3, 50R-2, 50E-1.
- **What happens:** the recap names each tie (`ties measurement to range`, `ties cells, cellsX and cellsY`), the TODO stops the build by design, and the user looks on the CLI page. It shows four scalar forms. The composition guide already has a chain of three through `OrderedPair` and `.Then`, a count from a product of drawn dimensions, a conditional through `Any.Boolean().Then(...)`, a bound taken from another draw, and `Any.Fixed`. The verifier wrote each contested shape in one line from those blocks and the scaffolded part generators; each held its tie on every draw:
  ```csharp
  IAny<LabResult> labResults = new AnyReferenceRange()
      .Then(range => new AnyMeasurement().WithUnit(range.Unit).As(m => new LabResult(m, range)));
  ```
- **Why partly:** "no scaffolded form" is by design and recorded (the ADR index's "Considered, not adopted": a general relation is a solver). "No documented form" is refuted for most shapes; what is missing is the link.
- **Recommendation:** a paragraph and one compiled sample, "a tie through another draw — a member, a kind, a count, a null", under "When a guard ties two parameters" (EN/FR), linked to composition's `.Then` section. Fix the stale "shows the three forms" link text in composition (EN line 252, FR line 261).

#### DOCS-3 — A null-guard test through a scaffolded `With…` needs a cast, and the page shows it only for nullable parameters
<a id="docs-3"></a>
- **Kind / severity:** docs, low · **Verdict:** confirmed
- **Met in:** 03 (E), 04 (D, E), 05 (E), 06 (D), 10 (R), 13 (E), 15 (D, E), 16 (E), 19 (R, E), 22 (E), 23 (R), 31 (R), 32 (E), 37 (E), 43 (R), 47 (E), 48 (E). Findings 03E-5, 04D-9, 04E-6, 05E-4, 06D-5, 10R-9, 13E-5, 15D-7, 15E-9, 16E-7, 19R-6, 19E-10, 22E-5, 23R-2, 31R-6, 32E-10, 37E-13, 43R-7, 47E-4, 48E-5.
- **What happens:**
  ```csharp
  Any.PayslipLine().WithLabel(null!).Generate();          // CS0121: ambiguous between WithLabel(string) and WithLabel(Func<AnyString, AnyString>)
  Any.PayslipLine().WithLabel((string)null!).Generate();  // compiles; the guard throws ArgumentNullException
  Any.PayslipLine().WithLabel(value: null!).Generate();   // compiles too, no cast
  ```
  The ambiguity is known and deliberate (ADR-0109; spec: no `Without…()` for a parameter whose guard refuses null, #411). Using the generator for a null-guard test is legitimate: the `null` is the significant literal, the other arguments are dummies. The only cast in the user docs sits in the paragraph about nullable parameters.
- **Recommendation:** one sentence and a sample on the CLI page (EN/FR), "Testing a constructor's guard": the cast and `!`, or the named argument `value:`. Do not add `Without…()` for guarded parameters.

#### DOCS-4 — The quick-start recap omits the namespace line, and "costs your tests no import" is stated without its condition
<a id="docs-4"></a>
- **Kind / severity:** docs, low · **Verdict:** partly
- **Met in:** 01 (D), 02 (R), 04 (R, E), 07 (E), 14 (E), 16 (E), 19 (R), 20 (R), 24 (D, E), 26 (E), 28 (D), 34 (R), 40 (E), 44 (D), 45 (D), 46 (D), 47 (D, R). Findings 01D-8, 02R-10, 04R-10, 04E-3, 07E-7, 14E-12, 16E-12, 19R-9, 20R-7, 24D-6, 24E-9, 26E-5, 28D-8, 34R-7, 40E-15, 44D-10, 45D-2, 46D-7, 47D-10, 47R-7.
- **What happens:** the default namespace is the domain's by decision (ADR-0062, ADR-0065), and the recap now prints `in namespace Payments.Domain (--namespace to change).` (#378). The page's quick-start recap stops before that line, and the page says entry points "cost your tests no import" unconditionally; a test in another namespace gets `CS0117: 'Any' does not contain a definition for 'Party'`, the compiler's text for a static extension member.
- **Recommendation:** add the namespace line to the page's recap (EN/FR) and make the "no import" sentence conditional, naming CS0117 and the two remedies. Do not change the default.

#### DOCS-5 — The CLI page explains why scaffolded strings raise JD030, but not why one-sided floating-point rows raise JD036
<a id="docs-5"></a>
- **Kind / severity:** docs, low · **Verdict:** partly
- **Met in:** 04 (E), 24 (E), 25 (E), 28 (D), 29 (D), 30 (E), 31 (R, E), 32 (E), 33 (R, E), 34 (E), 36 (E), 38 (E), 44 (D, R), 45 (R). Findings 04E-5, 24E-8, 25E-4, 28D-10, 29D-3, 30E-7, 31R-5, 31E-5, 32E-12, 33R-5, 33E-6, 34E-5, 36E-6, 38E-11, 44D-4, 44R-10, 45R-8.
- **What happens:** with JD030 and JD036 raised to warning, a fresh scaffold reports them on `Any.String().NotBlank()`, `Any.Double().Positive()` and one-sided decimal bounds. This is decided (spec: "Informational rules are excused by name, never by severity"; the emitted-code test excuses exactly JD030 and JD036) and the draw is truthful — the probe reached 1.79e308 and 7.9e28, and 28D-10 reports that this edge draw found a real `Vector3.Normalized` defect. The CLI page has a paragraph for JD030 and none for JD036.
- **Recommendation:** extend that paragraph (EN/FR) to floating-point rows, and add a "Scaffolded output" line on the JD036 page pointing back to it. Pairs with [CLI-3](#cli-3) and [CLI-20](#cli-20).

#### DOCS-6 — No recipe for a collection whose elements depend on a drawn sibling, a shared value, their index or their count
<a id="docs-6"></a>
- **Kind / severity:** docs, medium · **Verdict:** confirmed
- **Met in:** 02 (E), 03 (D, R, E), 12 (D), 14 (D, R, E), 18 (D, E), 29 (D), 33 (D, E), 36 (D, R). Findings 02E-6, 03D-3, 03R-3, 03E-4, 12D-4, 14D-3, 14R-3, 14E-6, 18D-10, 18E-4, 29D-4, 33D-8, 33E-5, 36D-4, 36R-7.
- **What happens:** every page sample is scalar. Every shape the members describe, short of a declared sum or neighbour constraint, can be built from documented primitives — a shared value (`Then` + `ListOf` with a refined element), a count from another draw (`Then` + `WithCount`), element i from i (an index-mapping `As`), N generators with N drawn (a `Then` fold) — and each held 2 000 of 2 000 draws and replayed. The `.Then` route forces a retype of the scaffolded `AnyList<T>` (CS0266), which is documented elsewhere.
- **Recommendation:** a "When a rule ties a collection's elements" subsection on the CLI page (EN/FR), cross-linked from composition and collections, one compiled sample per shape, and a plain statement that a sum cap or a neighbour rule is written by hand.

#### DOCS-7 — A single-parameter check-digit value object has no section of its own
<a id="docs-7"></a>
- **Kind / severity:** friction, low · **Verdict:** partly
- **Met in:** 01 (D), 06 (D, R, E), 08 (D, R, E), 10 (D, E), 11 (E), 31 (D), 45 (D), 47 (E). Findings 01D-10, 06D-4, 06R-3, 06E-4, 08D-2, 08R-3, 08E-4, 10D-5, 10E-4, 11E-7, 31D-4, 45D-4, 47E-1, 47E-2.
- **What happens:** for a regex plus a check key, the TODO offers only the `AnyPattern` retype and counts the key as "; and 1 more"; followed literally, the file compiles and the constructor throws on the first run. The only check-digit sample (`Any.StringMatching("[0-9]{15}").As(body => body + LuhnDigit(body))`) sits under "When a guard ties two parameters", and the documented route gives up refinement (`IAny<T>` has no `WithValue`).
- **Recommendation:** a short subsection under "Adjusting a parameter" (EN/FR) for a value whose last characters are computed, showing the shape that keeps refinement (`_value = body.As(b => b + CheckDigit(b))`, the recipe staying the body's builder) next to the pin-only route. Optionally, have the regex TODO mention that further guards follow.

#### DOCS-8 — No user page shows how to test a constructor's own refusal through a generator
<a id="docs-8"></a>
- **Kind / severity:** friction, low · **Verdict:** partly
- **Met in:** 03 (E), 10 (D, R), 19 (D), 22 (E), 23 (E), 24 (E), 31 (E), 32 (E), 35 (E), 41 (R), 45 (E). Findings 03E-6, 10D-7, 10R-8, 19D-5, 22E-9, 23E-5, 23E-6, 24E-4, 31E-8, 32E-6, 35E-11, 41R-7, 45E-11.
- **What happens:** `WithName(n => n.WithLength(41))` over a base capped at 40 is refused (`ConflictingAnyConstraintException: Cannot apply WithLength(41) because WithMaxLength(40) already caps the length at 40.`); an ordered pair or a `DifferentFrom` refuses a pin that breaks its tie. Each is deliberate (ADR-0109, ADR-0126, ADR-0121, ADR-0054) and agrees with the fundamentals: the rule-breaking value is what the assertion is about, so it is a pin, and a generator must refuse a pin that contradicts a tie it declares. The leaf half is on the page, but inside a section about a removed overload, and line 380 presents a rule-breaking pin as a mistake.
- **Recommendation:** a "Testing your constructor's refusal" subsection (EN/FR): the invalid value is a pin, never a refinement (`Assert.Throws<ArgumentOutOfRangeException>(() => Any.Individual().WithAge(-1).Generate())`); a tie the generator enforces is tested with the constructor, the other arguments drawn.

#### DOCS-9 — The first pages promise the analyzers "start working on your next build", and none says how to confirm they are loaded
<a id="docs-9"></a>
- **Kind / severity:** docs, low · **Verdict:** partly
- **Met in:** 04 (D), 05 (D), 07 (E), 08 (E), 10 (E), 13 (D), 19 (E), 20 (D, E), 21 (R), 35 (D), 39 (E), 43 (D). Findings 04D-11, 05D-7, 07E-9, 08E-5, 10E-5, 13D-5, 19E-9, 20D-10, 20E-8, 21R-10, 35D-6, 39E-8, 43D-6.
- **What happens:** `dotnet build` prints "0 Warning(s)" with JD030, JD033 and JD036 firing at Info; even `-v:d` shows no JD line. The remedy is now in the analyzers index and the nupkg readme, and several members quote it, so it is findable. What remains: getting-started (line 64), the FAQ (line 139) and the users' hub (line 60) promise the rules work with nothing to configure, and no page gives a command to confirm they are loaded.
- **Recommendation:** one sentence and a link on those three pages (EN/FR); a "confirm the rules are loaded" line (`dotnet build -p:ErrorLog=jd.sarif`) in the index. Keep the Info defaults.

#### DOCS-10 — Getting-started's `extension(Any)` entry class has no prose, no C# 14 note and no pointer to `any`
<a id="docs-10"></a>
- **Kind / severity:** docs, low · **Verdict:** partly
- **Met in:** 03 (D), 04 (D), 09 (D), 16 (D), 19 (D), 23 (D), 34 (D), 36 (D), 37 (D). Findings 03D-6, 04D-10, 09D-9, 16D-2, 19D-9, 23D-2, 34D-3, 36D-5, 37D-9 — discovery sessions only.
- **What happens:** the page's `AnyEntry` block (lines 197–202) gets no explanation. Below C# 14 it does not parse, with plain parse errors (CS1001, CS1513, CS1022) that never mention the language version — and the library supports net8.0, netstandard2.0 and .NET Framework 4.7.2. `any generate X --entry-point any` writes the same file; the page never says so.
- **Recommendation:** two or three sentences after the block (EN/FR): a C# 14 static extension member, what to write below C# 14, and the command that writes it, linked to the CLI page. Keep the default (ADR-0070).

#### DOCS-11 — No user page names the `JustDummies` namespace before line 295 of getting-started
<a id="docs-11"></a>
- **Kind / severity:** docs, low · **Verdict:** partly
- **Met in:** 27 (D), 29 (E), 31 (D), 34 (R), 48 (D). Findings 27D-5, 29E-6, 31D-5, 34R-6, 48D-7.
- **What happens:** a test file with only `using Xunit;` gets `CS0103: The name 'Any' does not exist`; a helper in the test namespace gets CS1061 on `DifferentFrom`, because the scaffolded file carries its own `using` and the helper does not. The documentation sample compiler injects `using JustDummies;` and `using Xunit;` before every sample, which hides the gap (REPORT.md XU-D1's "remove the prelude injection" was carried out for `JustDummies.Xunit` only). The `using Xunit;` half came from the harness.
- **Recommendation:** one sentence early on getting-started and in the README (EN/FR) — `using JustDummies;`, or `<Using Include="JustDummies" />` once per project — and one line on the CLI page for hand-written helpers. Stop injecting the prelude into `jd:declarations` samples.

#### DOCS-12 — `DifferentFrom` on `IAny<T>` takes one value, and no page joins "these draws must differ" to the tools that say so
<a id="docs-12"></a>
- **Kind / severity:** docs, low · **Verdict:** partly
- **Met in:** 27 (D), 37 (R), 40 (R), 41 (D, E). Findings 27D-6, 37R-10, 40R-11, 41D-6, 41E-6.
- **What happens:** two independent draws from one generator can be equal — 17 collisions in 20 000 seeds for a 1 000-value lot number, as expected. Equal draws are a valid outcome of what was declared; when the code needs them to differ, that is a constraint to declare. `DifferentFrom` (chained for a third value), `ListOf(g).WithCount(8).Distinct()` and `DistinctBy` each gave 0 collisions. Passing a list to `DifferentFrom` gives CS0411, which points the wrong way. composition.en.md:295 (FR :305) says a `ListOf` "produces a whole list of distinct customers" with no `Distinct()`, against the collections page.
- **Recommendation:** under "`.DifferentFrom(value)` on any generator" (EN/FR): one value, chaining for a third, `Distinct`/`SetOf` for N, `DistinctBy` for children by key. After "usually different" in the FAQ and core-concepts, one sentence: when the code needs them different, declare it. Fix composition :295/:305.

#### DOCS-13 — Refining a nested scaffolded part with `DifferentFrom` fails to compile, and no page says why
<a id="docs-13"></a>
- **Kind / severity:** friction, low · **Verdict:** partly
- **Met in:** 09 (E), 27 (R), 41 (D), 44 (R). Findings 09E-7, 27R-8, 41D-2, 44R-9.
- **What happens:** inside `WithId(Func<AnySiteId, AnySiteId>)`, `v.DifferentFrom("PAR-01")` gives CS0411 and `v.DifferentFrom(new SiteId("PAR-01"))` gives CS0266 + CS1662: the call binds to the `IAny<T>` extension, which returns `IAny<T>`. CS0266's "are you missing a cast?" leads to a cast that compiles and throws `InvalidCastException` on every run. The working forms are `WithId(i => i.WithValue(v => v.DifferentFrom(other.Id.Value)))` or a pinned `Any.SiteId().DifferentFrom(other.Id).Generate()`.
- **Recommendation:** one bullet in "What a refinement can do, and what it cannot" (EN/FR) with the two working forms and a warning against the cast hint.

#### DOCS-14 — The generator reference never names the builder type each factory returns
<a id="docs-14"></a>
- **Kind / severity:** docs, low · **Verdict:** confirmed
- **Met in:** 14 (R), 24 (D), 26 (E), 50 (E). Findings 14R-10, 24D-5, 26E-6, 50E-3.
- **What happens:** every reference table reads Factory | Draws | Page. `AnyDecimal`, `AnyDouble`, `AnySet<T>`, `AnySequence<T>`, `AnyArray<T>`, `AnyDictionary<K,V>`, `AnyEnum<T>`, `AnyOneOf<T>` and others are never named. A field, a helper's return type or a recipe parameter must name the builder. Overstated: the compiler names the target in CS0029, and the package ships its XML docs.
- **Recommendation:** a "Builder" column in the reference tables, the naming rule stated once, and one sentence in "When the replacement is another kind of generator" (EN/FR).

#### Documentation, compact
<a id="docs-low"></a>

| Id | Item | Kind, verdict | Met in | Recommendation |
|---|---|---|---|---|
| DOCS-15 | A `TimeOnly` order between two parameters is not paired, and neither the page nor the recap says why | docs, confirmed | 02 (D, R), 41 (E): 02D-4, 02R-7, 41E-2 | Qualify the page's `a > b` row as the spec does: every ordered builder except `TimeOnly`, whose order is cyclic (ADR-0126). |
| DOCS-16 | `category-JustDummies.severity` silently raises nothing; the four category keys are never listed | docs, confirmed | 42 (E), 45 (E), 50 (D): 42E-7, 45E-12, 50D-7 | List the four full category keys beside the bulk example (EN/FR). |
| DOCS-17 | The strings page never says a `WithChars` pool is BMP-only, nor how to draw astral text | docs, confirmed | 47 (D, E): 47D-2, 47E-6 | One passage: a pool is UTF-16 units within the BMP; astral text through `OneOf` (ADR-0075). |
| DOCS-18 | The reproducibility guide covers replaying a seed, never finding one | docs, confirmed | 13 (E), 31 (D): 13E-12, 31D-6 | A "When the failure is rare" section: repeated local runs with fresh seeds, and a seed sweep with `Any.UseSeed`. |
| DOCS-19 | Composition says scaffolds write `.AsNullable()` for every nullable value-type parameter; the default is `.OrNull()` since ADR-0117 | docs, confirmed | 49 (D): 49D-4 | State the actual rule (EN/FR). |
| DOCS-20 | JD038 names two fixes, and neither compiles inside a factory that must return `AnyInt32` | docs, confirmed | 15 (D): 15D-8 | An "Inside a generator's factory" subsection on the JD038 page. |
| DOCS-21 | `Any.OrderedPair` is the only public `Any.*` factory missing from the reference index | docs, confirmed | 17 (E): 17E-7 | One row in the Composition table (EN/FR). |
| DOCS-22 | The collections page never says an unconstrained collection draws 0 to 8 elements (min to min + 8 with a floor) | docs, confirmed | 40 (E): 40E-6 | A short "What an unconstrained collection looks like" section. |
| DOCS-23 | A throwing `As`/`Combine` factory surfaces as `AnyGenerationException` (deliberate, `InnerException` kept), and composition implies the domain exception surfaces | docs, partly | 27 (R), 46 (R), 48 (R): 27R-2, 46R-4, 48R-8 | Keep the wrap (it carries the drawn values and the seed). Say so in composition and errors-and-conflicts, and test a constructor's refusal on the constructor itself. |
| DOCS-24 | No page applies the dependent-draw pieces to two aggregates that must share a drawn value | docs, partly | 28 (E), 29 (R), 30 (E): 28E-9, 29R-6, 30E-5 | One worked example after `.Then` in composition: draw the shared fact once and pin it on both. |
| DOCS-25 | The tie section does not cover a "both present or both absent" nullable pair, which the one-in-ten `OrNull` default now hits (about 18 % of draws) | docs, partly | 23 (D, R, E): 23D-6, 23R-3, 23E-2 | A fifth form: draw the pair as one value through `Then` on one side's `OrNull()`. |
| DOCS-26 | No page says a reference-type generator already is an `IAny<T?>`, so people reach for `AsNullable()` and get CS0453 | docs, partly | 39 (D), 47 (D, R): 39D-7, 47D-12, 47R-4 | State that `AsNullable()` is for value types and that `IAny<T>` is covariant. |
| DOCS-27 | No page says how to treat a drawn object that reaches the assertion only by identity (`Assert.Same`) | docs, partly | 01 (D), 26 (D), 43 (E): 01D-9, 26D-5, 43E-5 | Needs the maintainer's stance against the first fundamental first ([§5](#5-candidates-for-a-decision)); then one paragraph in core-concepts separating the handle from the content. |
| DOCS-28 | A self-referencing parameter's TODO asks for a depth-bounded generator, and no page shows one | docs, confirmed | 05 (R): 05R-4 | A short worked sample on the CLI page (spec §5.4, ADR-0046: the depth is the developer's). |
| DOCS-29 | The docs say JustDummies and a property-based library "coexist happily" but never say how to split a suite | idea, confirmed | 46 (E): 46E-10 | Extend the FAQ answer with the rule: an assertion with an input space is a property. |
| DOCS-30 | The page says a nullable parameter gets `Without…()`, but a parameter left as a TODO keeps only its pin | docs, partly | 48 (D, E): 48D-9, 48E-6 | Qualify "two `With…` methods per parameter" (lines 41 and 331). |
| DOCS-31 | The Characters section does not say `Any.Char()` has no `Between`, or that a range outside a family is `OneOf` | docs, partly | 05 (R), 44 (D): 05R-5, 44D-5 | One sentence; the `OneOf` half is already shown twice (refuted). |
| DOCS-32 | The strings page does not say a free position in a pattern (`\d`, `.`, `[^…]`) draws only printable ASCII | docs, partly | 12 (D), 22 (E): 12D-6, 22E-6 | One sentence under "Supported constructs"; the anchors half is refuted. |
| DOCS-33 | Nothing warns that a class without `Equals` makes `DifferentFrom` inert | docs, partly | 16 (R): 16R-5 | The sentence the `Distinct()` note already has, on composition and the CLI page. |
| DOCS-34 | The integer full-range rule cites ADR-0117, whose one-in-ten limit draw does not apply to integers | docs, partly | 25 (E): 25E-6 | Cite ADR-0031 for integers and say in one clause where ADR-0117 applies. |
| DOCS-35 | The dates page does not point to `Any.OrderedPair` for "a later day", so authors invent an upper bound | docs, partly | 12 (E), 18 (E): 12E-7, 18E-12 | One sentence and a sample in "An unconstrained draw spans the whole type". |
| DOCS-36 | A generator that pins both sides of an inner tie and then applies the caller's lambda lets it re-pin one side | docs, partly | 39 (R): 39R-7 | A paragraph for generator authors: the caller's lambda may re-pin whatever the generator pinned, so pin only the independent part and let the element derive the tied part from its final value. |
| DOCS-37 | A hand-written `Generate()` that reads a pinned value before the constructor intercepts a `null!` meant for the guard | docs, partly | 05 (E): 05E-7 | One sentence in "Derive one draw from another inside `Generate()`". |
| DOCS-38 | No page suggests building a regex-guarded generator from the domain's own `Regex`, so the pattern is copied | docs, partly | 03 (E): 03E-11 | `Any.StringMatching(PolicyNumber.Format)` when the domain exposes it. |
| DOCS-39 | Scenario-prerequisite guidance lives only in core-concepts, unlinked from the headline rule | docs, partly | 39 (E): 39E-7 | One sentence and a link on getting-started and in the FAQ. |
| DOCS-40 | No page applies the dummy rule to a system under test that takes its own seed | docs, partly | 40 (R): 40R-12 | An FAQ entry applying the existing rule: draw it if the assertion holds whatever it is, pin it otherwise. |
| DOCS-41 | `InlineData` cannot carry a decimal (CS0182, a C# limit); xUnit v3 already converts a string argument | idea, partly | 03 (R), 08 (E): 03R-9, 08E-12 | Optional sentence on the adapter page. |
| DOCS-42 | A lone surrogate in an attribute argument arrives as U+FFFD — the C# attribute encoding, not xUnit and not JustDummies | idea, partly | 47 (D, R): 47D-13, 47R-8 | Not a library item; at most a line beside DOCS-41. |
| DOCS-43 | A tuple built in `As` from a non-null element warns CS8619 against a nullable target — C# behaviour | docs, partly | 49 (R): 49R-6 | Not a library item; the cluster's repro does not reproduce. |

---

## 5. Candidates for a decision

None of these is counted as a bug. Each is either decided on purpose — and the trial adds field evidence a decision would weigh — or would need the generator to attempt more than it does today. The scope line (CLAUDE.md, ADR-0046) is the test: a change that makes the generator cleverer than a clear refusal, widens a bound, or adds a solver needs a decision, not a patch.

### 5.1 Where this trial is the field report a recorded decision waits for

| Item | Met in | Decided in | What the trial adds |
|---|---|---|---|
| **Reading regex guards** as `AnyPattern`, instead of `AnyString` plus a TODO and a four- or five-place retype | 47 findings, 27 domains, D15 R17 E15 | spec §5.3 and §16 (a v1.1 item), ADR-0063 | Third trial in a row, the broadest by-design item. The TODO now quotes the pattern and lists the places (#374), which made the retype mechanical; the next step is reading it. |
| **A re-scaffold-safe seam** for a cross-parameter tie (a hand-written `Generate()` is erased by `--force`) | 28 findings, 19 domains, D7 R12 E9 | ADR index, "Considered, not adopted": a `partial` hook that survives `--force` | The strongest recurrence of any refused option, three trials running. With [CLI-1](#cli-1)'s pin record, the edit would at least keep its pins. |
| **An inequality tie scaffolded as `DifferentFrom`** (`a.Equals(b)`, `a == b`) | 7 findings, 4 domains: 24 (D, E), 27 (E), 32 (D, R, E), 35 (E) | ADR-0121, rejected alternative "let `any` read an `a.Equals(b)` guard" | The same argument as #405 for `OrderedPair`: edits lost on `--force`. |
| **A log-scale spread** for floating-point ranges over many decades | 6 findings, 3 domains: 28 (R), 39 (R, E), 43 (D, R, E) | ADR-0117, "A log-uniform spread" rejected; numbers page | Ranges of 11 to 25 decades whose low decades carry the domain's behaviour: about 90 % of `Resistance` draws in the top decade. REPORT-2 CORE-14 again. |
| **An order with one nullable side** (`until < from`, `until is T e && e < from`) | 5 findings, 3 domains: 19 (D, R, E), 38 (D), 49 (E) | spec §5.3: the order row pairs two non-nullable parameters; "a conjunction stays unread" | A narrow, exact extension of the order row (ADR-0082). |
| **An opt-in public accessor** for the private `AnyValid…()` bases | 5 findings, 5 domains: 05 (E), 13 (R), 20 (E), 27 (E), 30 (R) | spec §4.2; the CLI page shows the one-line partial wrapper | REPORT-2 CLI-17 again; an `any.json` switch would emit the wrapper. |
| **A weekday or business-day constraint** | 5 findings, 2 domains: 02 (D, R, E), 50 (D, R) | REPORT.md CORE-C3, refused; ADR index | Keep the refusal; the dates page (EN/FR) still lacks the recipe (`Between(…).As(NextBusinessDay)`), which moves the parameter out of its builder type. |

### 5.2 Decisions raised by confirmed or partly items

| Item | The question |
|---|---|
| [CORE-1](#core-1) | Two pins on an ordered pair: literals checked against each other only, or every pin always checked against the other base? (ADR-0126) |
| [CORE-2](#core-2) | An ordered pair carrying a maximum gap — the "relation other than an order" the ADR index names as its revival condition. |
| [CORE-3](#core-3), [CORE-4](#core-4) | Side names on `AnyOrderedPair` (public surface); one rule for a `Local` pin on a `DateTime` pair (ADR-0114). |
| [CORE-7](#core-7), [CORE-8](#core-8) | `Except(IEnumerable<T>)` on `IAny<T>` (amends ADR-0121); a bounded default for an `Any.BigInteger()` (ADR-0046, ADR-0117). |
| [CLI-4](#cli-4), [CLI-6](#cli-6), [CLI-8](#cli-8), [CLI-16](#cli-16), [CLI-29](#cli-low) | New exact rows for the guard reader under ADR-0085: `void` same-type helpers, guards the recipe already satisfies, `static readonly` bounds, character-class predicates, modulo. |
| [CLI-10](#cli-10), [CLI-25](#cli-25) | Mark a same-type throwing call below the first assignment (ADR-0110's direction); stop marking a hand-off to a framework exception constructor. |
| [CLI-11](#cli-11) | Narrow ADR-0089's blind `new AnyX()` to the developer's own types; give framework types a sentinel. |
| [CLI-17](#cli-17), [CLI-23](#cli-23) | Write only the missing entry file (spec §7, ADR-0070); refuse a tie between constructors of equal width (spec §5.1). |
| [CLI-20](#cli-20), [CLI-22](#cli-22) | Emit the opposite bound on each member of a pair; promise Scale ≤ N for drawn values (ADR-0015). |
| [CLI-21](#cli-21), [CLI-41](#cli-low), [CLI-42](#cli-low) | A JD rule or an emitted `[Obsolete]` overload for a generator passed to `With…`; a richer sentinel (ADR-0060); a minimal scaffold (ADR-0109). |
| [XU-1](#xu-1) | Exposing whether a seed scope was drawn from (ADR-0017, ADR-0049). |
| [DOCS-27](#docs-low) | Is a drawn object that reaches the assertion only by identity a dummy? A stance against the first fundamental, before any page says it. |

### 5.3 By design, recorded

| Item | Met in | Why it stays |
|---|---|---|
| A large guard ceiling is written as read; the narrowing advice is only in the console and JSON | 20 (R, E), 35 (D, E): 20R-3, 20E-2, 35D-4, 35E-7 | Spec §6, REPORT.md CLI-C1/#361. Docs: mention it on the CLI page. |
| `--entry-point any` refuses `Voice` when `VoiceEntry` exists | 33 (D, R, E): 33D-3, 33R-3, 33R-8, 33E-2 | Spec §4.5 uses this exact case; REPORT-2 CLI-11's bug is now a refusal before anything is written. |
| Collection uniqueness guards (`Distinct().Count() != Count`) unread | 27 (E), 38 (R), 40 (R) | Closed set (ADR-0085); field evidence for REPORT.md CLI-G8. |
| A generic wrapper (`Observed<T>`) refused as a target, left to a TODO as a parameter | 49 (D, R, E) | Spec §3.2, §5.4; a docs sample for a hand-written generic generator would help. |
| "N of N parameters inferred" counts `to verify` rows under a tick | 43 (D), 48 (E), 50 (R) | Spec §6, ADR-0083; the page never shows the line. |
| A throwing switch expression assigned to state is marked, not read | 09 (D, R, E) | Spec §5.3 (an `if` before the first write); field report for a row. |
| A scaffolded string row draws the whole of ASCII, control characters included | 41 (R): 41R-8, 41R-9 | ADR-0075 rejected printable ASCII as hiding defects; ADR-0127 refuses guessing from a name. |
| `!(p >= a && p <= b)` and `x = c ? x : throw` unread | 14 (R, E) | ADR-0128 rejected a general normaliser; two narrow rows would need an amendment. |
| A refining lambda on a nullable parameter never draws `null` | 15 (R), 26 (D) | Spec D10, ADR-0117; optionally say it in the overload's remarks. |
| A bound under `is { } e` on a nullable is marked | 36 (E) | Spec §5.3, §9 ("validate what is present"). |
| A refinement has nothing to refine on a composed `IAny<string>` or an `AnyPattern` | 01 (E) | ADR-0109; narrowing needs intersection (ADR-0046). |
| An abstract parameter stays a TODO after its hand-written generator exists | 48 (E) | #375: the tool never looks for it; say so on the page. |
| A capacity drawn over `1..MaxCapacity` breaks a test that appends three rules | 46 (R) | The base is right; the test depends on capacity and must declare it. |
| A scaffolded generator exposes only its `With…` pair, not the leaf vocabulary (`After`, `Before`) | 28 (D) | Spec §4.2, ADR-0109/0116. |
| No entry point without `--entry-point` | 38 (R) | ADR-0070 (the C# 14 floor); point to `any.json`'s `entryPoint`. |
| A list's lambda cannot reach its element recipe | 12 (E) | ADR-0109; a repeat of REPORT-2 CLI-26. |
| No `OrderedPair` over a comparable value object | 34 (D, R, E), 48 (E) | ADR-0126 rejected it (a pin honoured only by rejection); document pair-the-primitive-then-`As`. |
| A hand-written overload writing only the draw field is overridden by a later `With…(Func)` | 37 (D) | ADR-0116 (Proposed); docs: a named variant. |
| `AnyPattern` has no `StartingWith`, no length cap | 11 (R), 16 (E) | ADR-0008, ADR-0046 (no language intersection). |
| A second `OneOf` cannot narrow a base pool | 05 (D) | ADR-0100; `Except` narrows, and the message names it. |
| A decimal drawn without `WithScale` carries 27 or 28 fractional digits | 44 (E) | ADR-0031, ADR-0117; capping it would hide code that rounds. |
| JD036 has no intent marker for a deliberately open draw | 07 (R), 28 (E) | A justified `[SuppressMessage]` is the documented marker. |
| No analyzer warns that two independent draws may collide where a guard needs them distinct | 09 (E) | The requirement is a runtime guard; a heuristic would be noise (ADR-0046). |
| Under Microsoft.Testing.Platform the seed line sits under "Standard output" | 06 (E), 42 (R) | ADR-0018's channel; docs: show a failing run as `dotnet test` prints it. |
| A drawn value the test depended on failed the test and the seed replayed it | 25 (R), 34 (R, E), 44 (D), 47 (D), 48 (R) | The first fundamental working; recorded under [§7](#7-what-worked). Docs: the pages a red test leads to (the reproducibility loop, the FAQ entry "A test failed once and passed on rerun", the adapter's "Replaying a failure") name only "fix the defect"; they should name both outcomes, and the FAQ question itself carries the flaky framing. A drawn-values report (REPORT-2 XU-2) stays not adopted: every replay here worked first time. |

---

## 6. Refuted and not reproduced

- **shipped-docs-dead-links** (10 findings, discovery only: 09D-8, 11D-11, 16D-10, 17D-6, 19D-10, 23D-10, 25D-11, 33D-6, 39D-8, 47D-8) — refuted: the dead links came from how this run's workspace script rebuilt the docs folder, not from the repository or the packages. Fix the harness for the next trial.
- **duplicate-todo-line** (29D-6) — not reproduced: no factory gets two TODO lines; the same line appears once in each of two factories tied by one guard.

Sub-claims refuted inside confirmed or partly items, for the record: the restore refusal exits 1, not 2 ([CLI-2](#cli-2)); a framework-type row does carry its sentinel, hidden by CS0246 ([CLI-11](#cli-11)); a missed retype site never compiles ([CLI-15](#cli-15)); `DifferentFrom`'s message escapes control characters ([CORE-6](#core-6)); the `OneOf` refusal fires at declaration, not at `Generate()` ([AN-2](#an-2)); no library `With…` has a value/lambda pair ([CLI-21](#cli-21)).

---

## 7. What worked

- **Drawn values revealed real domain defects — the reason the library exists.** As the sessions report, each replayed from its seed:
  - 32 (D): `Solve` on a drawn valid network threw `ArgumentOutOfRangeException: Reactive power must be finite and within ±10000 Mvar. Actual value was 17599.82` from `Bus.NetInjection`, on seed −2128493443; replaying the seed gave the same value. A valid bus's generation minus load can leave the range of the `PowerInjection` it builds. See [NOTES.discovery.md](domains/32-grid-load-flow/NOTES.discovery.md).
  - 28 (D, R, E): a drawn conjunction showed that `PositionCovariance.Combine` refuses the sum of two valid covariances (`Actual value was 1308982.4889122401`), so `Conjunction.Assess` throws for valid trajectories; the one-in-ten edge draw also found that `Vector3.Normalized` underflows. "The draw found a domain defect before any of my tests did" ([NOTES.discovery.md](domains/28-satellite-conjunction/NOTES.discovery.md)).
  - 33 (D, R): the exact sum of two valid durations can be unrepresentable — "a real domain issue … that no literal I would have written would have found".
  - 21 (E): a facilitator of very high yield drives the helped species extinct, on seed −1087769823; kept as a skipped test with a sibling that pins a comparable yield.
  - 37 (R): `Any.Quantity().Generate()` hit the 1e9 cap in `ConvertTo` on the third run; a drawn transformation with mixed units left `BatchGenealogy.Record` half-applied about one run in six.
  - Quantities left without a ceiling overflowed under the draw in 06, 13, 14, 15, 29, 36 and 40 — net pay in 64 of 3 000 payslips (13), a gauge width of 6.86e28 (36), 114 of 20 000 portfolios (40) — and a date schedule ran past `DateOnly.MaxValue` one run in six (30). Each session kept the defect as a skipped test rather than capping the generator to hide it.
  - 50 (E): the one red draw led the session to a bare `InvalidOperationException` escaping `SettlementBatch.Book`.
- **A drawn value showed that a value treated as a dummy was not one, and the seed replayed it on the first try.** Six findings in five domains (25R-8, 34R-10, 34E-4, 44D-11, 47D-11, 48R-7), plus 49 (R) and the coordinator's run of 49 (E):
  - 48 (R): a taxpayer's birth date, drawn under a test about an age condition. `[JustDummies] These arbitrary values were seeded with 481195150. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 481195150)].` The session pinned the date in the shared helper.
  - 44 (D): one red run in 87, seed 22259052: the test drew a product whose excursion allowance decides the logger interval, and built the pallets separately.
  - 49 (R): a filler adverse event could itself be fatal; the session added `AnyAdverseEvent.NonFatal()`, and pinning the seed replayed the failure at once.
  - None of these is instability. As one session put it, "The sentence 'a test that fails on a drawn value is never flaky' matched my experience: it was my set-up."
- **Seed replay held everywhere it was tried.** A green run under `[assembly: Reproducible]` prints nothing; a red one prints a paste-ready line, now fully qualified, so REPORT-2 XU-1 is fixed (23D-11, 32D-9). Pasting it gave the same drawn value bit for bit. The 150 suites stayed green over 11 to 222 fresh-seed runs each; 47 (E) alone ran 723 cases green 33 times in the session and 41 more times in verification.
- **The scaffolder's sentinels kept their promise.** Wherever the tool could not vouch for a guard, the build stopped at the exact factory with the guard quoted (05E-14, 08D-11, 26E-12, 31D-9):
  ```
  Container resolved to VesselStowage.Domain.Container; System.ComponentModel.Container also answers to that name.
  // TODO(any): verify against Probe.cs, line 5: if (!Pattern().IsMatch(value)) { ... } — a regex, "^CH(0[1-9]|1[0-2])-C[0-9]{4}$": Any.StringMatching(pattern) replaces the base and returns an AnyPattern, so retype AnyString to AnyPattern here, on the recipe field, in the recipe constructor and in the Func of WithValue — then delete these two lines.
  amount       decimal  Any.Decimal().Positive().LessThanOrEqualTo(10000m).WithScale(3)  guard
  timesPerDay  int      Any.Int32().Positive().LessThanOrEqualTo(6)                    guard
  ```
  The `amount <= 0 || amount > 10000m` guard that REPORT-2 CLI-1 found unread is now read, and so is `decimal.Round(amount, 3) != amount`. The ambiguous short name resolves instead of failing the batch (REPORT-2 CLI-12), and the regex TODO quotes the pattern and lists the edit (REPORT-2 CLI-13). The exceptions are the documented silent shapes ([CLI-10](#cli-10)) and one compound-statement case ([CLI-5](#cli-5)).
- **The scaffolder refuses loudly at its edge.** An abstract parameter, a generic target, an entry-point name clash, a multi-argument factory, an unrestored project: each is a refusal that names what it cannot honour, never a guessed generator (ADR-0046).
- **The analyzers fired accurately and stayed quiet on clean code.** JD007 now fires on a scaffolded `Any.X()` entry point in a field initialiser and in a test-class constructor (REPORT.md AN-C3, #365), and JD013 names `Any.ElementOf` as the replacement (08R-9, 12E-14, 30E-6, 42E-10). Several expert sessions raised all JD rules to warning on suites of 500 to 700 cases and got zero diagnostics on their own code (42E, 46E, 47E, 48D); a deliberate `$"{Any.Int32()}"` probe raised JD005 (50D).
- **The core DSL needed no explanation.** Pins, refinements, `OrderedPair`, `.Then`, `DifferentFrom`, `DistinctBy` and `Any.Fixed`, most of them answers to REPORT-2, were used as documented across the fifty domains; the friction sits around them, in the scaffolder and the docs, not in them.

---

## 8. What the new domains 41–50 taught

- **41 — time zones and daylight saving** ([D](domains/41-shift-planning-time-zones/NOTES.discovery.md), [R](domains/41-shift-planning-time-zones/NOTES.regular.md), [E](domains/41-shift-planning-time-zones/NOTES.expert.md)). The library had nothing to do with the zones: the DST, rest and weekly-limit cases used literal Paris, New York, Tokyo, Sydney and Troll dates, and ids, names, periods and participants were drawn. That is the split the fundamentals ask for. The costs were ties between times of day: a `TimeOnly` order is not paired, by decision, and nothing says why ([DOCS-15](#docs-low)); a window that opens before it closes, or a start plus a wall-clock length, had to be written in `Generate()` ([CLI-1](#cli-1)). A drawn request can fall in a DST gap and be refused, with no way to declare "resolvable in this zone" (41D-11). The domain defects — `IsoWeek(9999, 52)` and roster arithmetic at `DateOnly.MaxValue` — were all found with literals; the discovery and expert sessions report no failure on a drawn value in 28 and 45 runs.
- **42 — numbers beyond `decimal`** ([D](domains/42-token-ledger/NOTES.discovery.md), [R](domains/42-token-ledger/NOTES.regular.md), [E](domains/42-token-ledger/NOTES.expert.md)). `UInt128` is a factory like any other; `BigInteger` is not. The scaffolder writes `new AnyBigInteger()` as if it existed and the build stops on CS0246 ([CLI-11](#cli-11)); no page says how to compose one, though a uniform 256-bit value is one line ([CORE-8](#core-8)). The sessions hand-wrote builder classes instead. Arithmetic ties (a cap that is a multiple of 10^decimals, supply bounded by the cap) went to `Generate()` with hand-maintained pin flags.
- **43 — very small and very large magnitudes** ([D](domains/43-passive-components/NOTES.discovery.md), [R](domains/43-passive-components/NOTES.regular.md), [E](domains/43-passive-components/NOTES.expert.md)). A range over 25 decades is drawn uniformly in value, so about 90 % of `Resistance` draws land in the top decade: the clearest field evidence yet for the deferred log-scale spread ([§5.1](#51-where-this-trial-is-the-field-report-a-recorded-decision-waits-for)). The draws themselves were correct at every magnitude. An expert's four red runs on drawn values were each a value that was not a dummy, or an arithmetic slip.
- **44 — transverse over 06, 07, 10 and 20** ([D](domains/44-vaccine-cold-chain/NOTES.discovery.md), [R](domains/44-vaccine-cold-chain/NOTES.regular.md), [E](domains/44-vaccine-cold-chain/NOTES.expert.md)). One command scaffolded 28 types across four referenced domains, and the composed graph held; the build stopped on 21 of them, because this model validates mostly through regexes and helpers. Composing across domains surfaced what one domain never does: a short type name that collides with another domain's alias (CS0104, [CLI-26](#cli-26)), and many namespaces to import ([DOCS-4](#docs-4)). The one red draw in 87 runs was a cross-domain rule (logger interval against excursion allowance) the test had split across two independent draws.
- **45 — transverse over 03, 08 and 01** ([D](domains/45-healthcare-claims/NOTES.discovery.md), [R](domains/45-healthcare-claims/NOTES.regular.md), [E](domains/45-healthcare-claims/NOTES.expert.md)). 21 to 23 types per session scaffolded in one run (in the expert's, 14 from the referenced domains). A type reached through a transitive project reference before a restore is reported as "no type by that name" ([CLI-43](#cli-low)). The aggregates with five ties to their prescription were rewritten by hand; leaf generators needed little. No drawn value failed in 37 to 72 runs.
- **46 — bit-level and unsigned types** ([D](domains/46-network-acl/NOTES.discovery.md), [R](domains/46-network-acl/NOTES.regular.md), [E](domains/46-network-acl/NOTES.expert.md)). `UInt128` IPv6 addresses and unsigned ranges were drawn without friction; an expert's differential run of 300 000 random addresses against `System.Net.IPAddress` found only the framework's own rendering of low addresses. What hurt was shape, not bits: an `AddressPrefix` with an IPv4 and an IPv6 constructor scaffolded only the first, silently ([CLI-23](#cli-23)); a `[Flags]` enum is drawn one member at a time with nothing pointing to `AllowingCombinations()` ([CLI-38](#cli-low)); a constant `null` in a `OneOf<byte?>` pool compiles and throws ([AN-2](#an-2)). Half the scaffolds were replaced because every parameter of a packet depends on the others.
- **47 — Unicode beyond ASCII** ([D](domains/47-international-catalogue/NOTES.discovery.md), [R](domains/47-international-catalogue/NOTES.regular.md), [E](domains/47-international-catalogue/NOTES.expert.md)). The domain's own refusals (CJK ranges missing U+3005, the okina, full-width forms) were found with realistic literal data, as they should be. On the library side, `Any.StringMatching` with an astral character silently draws ill-formed UTF-16 while `WithChars` refuses the same input ([CORE-5](#core-5)), and no page says how to draw non-BMP text ([DOCS-17](#docs-low)). A lone surrogate cannot travel through `[InlineData]` — a C# encoding limit, not the library's ([DOCS-42](#docs-low)).
- **48 — polymorphism and recursive trees** ([D](domains/48-tax-rules-engine/NOTES.discovery.md), [R](domains/48-tax-rules-engine/NOTES.regular.md), [E](domains/48-tax-rules-engine/NOTES.expert.md)). Abstract parameters are left to a TODO by design, and the tool never picks up a hand-written `AnyICondition` once it exists ([§5.3](#53-by-design-recorded)). Choosing among generators works with `Then`, but JD012 flags the `ElementOf(...).Then(g => g)` form its own message recommends ([AN-1](#an-1)). The costliest gap was a domain-wide `Guard` class of another type: silent, so `IncomeKind` drew `None` one time in five and the recap still said "inferred" ([CLI-10](#cli-10)). A drawn birth date under an age-condition test was the clearest non-dummy of the trial.
- **49 — absent values** ([D](domains/49-clinical-trial-crf/NOTES.discovery.md), [R](domains/49-clinical-trial-crf/NOTES.regular.md), [E](domains/49-clinical-trial-crf/NOTES.expert.md)). The domain's `Observed<T>` wrapper types five of the six hardest parameters, and a generic type cannot be scaffolded ([§5.3](#53-by-design-recorded)). `null` one draw in ten by default is now the rule, and the composition page still says otherwise ([DOCS-19](#docs-low)); an order with one nullable side is not paired ([§5.1](#51-where-this-trial-is-the-field-report-a-recorded-decision-waits-for)). The domain needs "was this pinned, possibly to null?", which the expert rebuilt with a private `Pin<T>` struct ([CLI-1](#cli-1)). This is also the domain whose drawn grade showed, in the coordinator's run, that a "filler" event was not a dummy ([§2](#2-limits-of-this-trial), item 7).
- **50 — currency-dependent decimal scale** ([D](domains/50-fx-settlement/NOTES.discovery.md), [R](domains/50-fx-settlement/NOTES.regular.md), [E](domains/50-fx-settlement/NOTES.expert.md)). Ten of eleven scaffolds stopped on a tie — code to minor units, amount to currency, rate places to pair — the very ties this domain was built around; constraining one scaffolded parameter's scale from another's drawn value has no supported idiom ([CLI-1](#cli-1)). Rounding and midpoint cases stayed literals, which "read as specifications"; the red draws in 50 (R) and 50 (E) were sheet-rate ranges too wide for the scenario, whose crosses fall below what a three-place quote can carry and round to 0 — the domain was right to refuse, and in 50 (R) a trade side left drawn under a test about a buy was not a dummy either. A negated constant bound is copied as a value ([CLI-32](#cli-low)), weekday dates have no constraint (refused, [§5.1](#51-where-this-trial-is-the-field-report-a-recorded-decision-waits-for)), and the generator reference never names the builder types ([DOCS-14](#docs-14)).

---

## 9. Domain defects found by the sessions

**Reported, not independently verified.** The 536 domain findings below, grouped into 264 clusters, come from the sessions. Each session reports that it demonstrated its defects with a test kept as `Skip = "domain defect…"` and confirmed it red by un-skipping it once; the verification pass did not reproduce them. They are the trial's own invented models, so none of them is a defect in JustDummies.

Recurring families, by cluster title (a cluster can fall in two):
- a regex ending in `$`, which .NET also matches before a final `\n`: 43 domains (in 01–20, the `MODEL.md` steered sessions there, [§2](#2-limits-of-this-trial) item 2);
- arithmetic past a missing ceiling, or a date past `MaxValue`: about 22 domains — the family drawn values found most often;
- internal storage handed out behind `IReadOnlyList`, or the caller's list kept: about 24 domains;
- `\d` accepting non-ASCII digits: 18; `NullReferenceException` instead of `ArgumentNullException`: 16; culture-dependent `ToString`: 12; NaN let through: 5; an undefined enum member accepted: 5.

"Found by" says how the session reports finding them: **draw** where a drawn value revealed a defect (the evidence the library exists for), **literal** where the session says it used chosen edge cases, and **cases** where the notes do not say.

| Domain | Notes | Findings (clusters) | Most telling defects | Found by |
|---|---|---|---|---|
| 01-payments | [D](domains/01-payments/NOTES.discovery.md) [R](domains/01-payments/NOTES.regular.md) [E](domains/01-payments/NOTES.expert.md) | 16 (7) | `Iban` passes its regex with a trailing `\n`, then throws `FormatException`; `PaymentBatch` keeps the caller's list, so its invariants break after construction; IBAN check digits 00, 01 and 99 accepted | literal |
| 02-trading | [D](domains/02-trading/NOTES.discovery.md) [R](domains/02-trading/NOTES.regular.md) [E](domains/02-trading/NOTES.expert.md) | 11 (6) | a stop-limit sell requires the stop below the limit, inverted; a null order in a batch throws `NullReferenceException` | literal |
| 03-insurance | [D](domains/03-insurance/NOTES.discovery.md) [R](domains/03-insurance/NOTES.regular.md) [E](domains/03-insurance/NOTES.expert.md) | 13 (8) | a valid 367-day policy can never be renewed; a claim declared twice counts twice at renewal; `BonusMalus.AfterYear` loses a cent in binary floating point | cases |
| 04-manufacturing | [D](domains/04-manufacturing/NOTES.discovery.md) [R](domains/04-manufacturing/NOTES.regular.md) [E](domains/04-manufacturing/NOTES.expert.md) | 11 (4) | `Cadence.TimeToProduce` overflows `TimeSpan` with an unrelated exception; `$` and `\d` in two regexes | literal |
| 05-cad-drawing | [D](domains/05-cad-drawing/NOTES.discovery.md) [R](domains/05-cad-drawing/NOTES.regular.md) [E](domains/05-cad-drawing/NOTES.expert.md) | 21 (9) | `FitKind.Interference` can never be produced; `StandardTolerance` lets NaN through and indexes out of range; BOM totals overflow `int` silently | literal |
| 06-warehouse | [D](domains/06-warehouse/NOTES.discovery.md) [R](domains/06-warehouse/NOTES.regular.md) [E](domains/06-warehouse/NOTES.expert.md) | 16 (5) | an unbounded unit weight overflows `WeightKg` and `InspectionReasons` (seed −113536279); `TemperatureRange` reports an excessive maximum on `minCelsius` | draw |
| 07-freight | [D](domains/07-freight/NOTES.discovery.md) [R](domains/07-freight/NOTES.regular.md) [E](domains/07-freight/NOTES.expert.md) | 7 (3) | `ContainerCode.Create` with a short owner throws `IndexOutOfRangeException` instead of `FormatException` | literal |
| 08-patient-record | [D](domains/08-patient-record/NOTES.discovery.md) [R](domains/08-patient-record/NOTES.regular.md) [E](domains/08-patient-record/NOTES.expert.md) | 12 (6) | `AgeOn` is negative before birth and a 29 February birth ages on 28 February; dispensing dates overflow near `DateOnly.MaxValue` | literal |
| 09-laboratory | [D](domains/09-laboratory/NOTES.discovery.md) [R](domains/09-laboratory/NOTES.regular.md) [E](domains/09-laboratory/NOTES.expert.md) | 11 (5) | `ReferenceRange` accepts NaN critical limits and `Classify(NaN)` returns Normal; `LabReport.Results` mutable through a cast | literal |
| 10-pharma-serialisation | [D](domains/10-pharma-serialisation/NOTES.discovery.md) [R](domains/10-pharma-serialisation/NOTES.regular.md) [E](domains/10-pharma-serialisation/NOTES.expert.md) | 7 (4) | `Gtin(string)` accepts non-ASCII digits that `Gtin.FromBody` refuses; `$` in four regexes | literal |
| 11-telecom | [D](domains/11-telecom/NOTES.discovery.md) [R](domains/11-telecom/NOTES.regular.md) [E](domains/11-telecom/NOTES.expert.md) | 15 (6) | `BillFor` and `OverageFor` overflow on accepted inputs; a bundle "priced in cents" accepts fractions of a cent | cases |
| 12-smart-grid | [D](domains/12-smart-grid/NOTES.discovery.md) [R](domains/12-smart-grid/NOTES.regular.md) [E](domains/12-smart-grid/NOTES.expert.md) | 16 (8) | `Meter.Record` replaces the last reading before validating it, corrupting the meter on refusal (high); `DailyLoadCurve` keeps the caller's list | literal, through generator-built meters |
| 13-payroll | [D](domains/13-payroll/NOTES.discovery.md) [R](domains/13-payroll/NOTES.regular.md) [E](domains/13-payroll/NOTES.expert.md) | 12 (6) | no ceiling on a payslip line, so net pay overflows `decimal` (64 of 3 000 drawn payslips); null lines accepted; a contract hired after 2100 can never be paid | draw |
| 14-ecommerce | [D](domains/14-ecommerce/NOTES.discovery.md) [R](domains/14-ecommerce/NOTES.regular.md) [E](domains/14-ecommerce/NOTES.expert.md) | 14 (7) | `GiftCard.Redeem` accepts a negative amount and raises the balance; `Money` has no maximum, so totals overflow | draw (overflow), literal |
| 15-hotel-booking | [D](domains/15-hotel-booking/NOTES.discovery.md) [R](domains/15-hotel-booking/NOTES.regular.md) [E](domains/15-hotel-booking/NOTES.expert.md) | 11 (6) | `Reservation.Cancel` sets the status before computing the penalty; `Money` overflow in `Cancel` (seed 350311511); the e-mail pattern refuses `user@example.co.uk` | draw |
| 16-aviation | [D](domains/16-aviation/NOTES.discovery.md) [R](domains/16-aviation/NOTES.regular.md) [E](domains/16-aviation/NOTES.expert.md) | 15 (6) | `AircraftRegistration("")` throws `IndexOutOfRangeException`; the N-number pattern's 8-character branch is dead; a slot accepts sub-millisecond ticks | cases |
| 17-real-estate | [D](domains/17-real-estate/NOTES.discovery.md) [R](domains/17-real-estate/NOTES.regular.md) [E](domains/17-real-estate/NOTES.expert.md) | 15 (8) | a trailing `\n` bypasses the plan-number-0000 rule; no rent ceiling, so deposits overflow; `%2F` decoded into a path separator | cases |
| 18-education | [D](domains/18-education/NOTES.discovery.md) [R](domains/18-education/NOTES.regular.md) [E](domains/18-education/NOTES.expert.md) | 11 (6) | a student number with non-ASCII digits makes `IntakeYear` throw `FormatException`; nothing ties the birth date to the semester | cases |
| 19-iam | [D](domains/19-iam/NOTES.discovery.md) [R](domains/19-iam/NOTES.regular.md) [E](domains/19-iam/NOTES.expert.md) | 8 (3) | a scope's action becomes `"read\n"`; `AllowedScopes` and `RedirectUris` are live sets, so an allow-list widens through a cast | literal |
| 20-agritech-iot | [D](domains/20-agritech-iot/NOTES.discovery.md) [R](domains/20-agritech-iot/NOTES.regular.md) [E](domains/20-agritech-iot/NOTES.expert.md) | 10 (4) | NaN humidity poisons the average and disables irrigation; `IrrigationPolicy` refuses NaN only by its sign bit | literal (from `MODEL.md`) |
| 21-orbital-microbiology | [D](domains/21-orbital-microbiology/NOTES.discovery.md) [R](domains/21-orbital-microbiology/NOTES.regular.md) [E](domains/21-orbital-microbiology/NOTES.expert.md) | 12 (8) | a very productive facilitator drives the helped species extinct (seed −1087769823); `Advance` depends on how the caller slices time | draw |
| 22-pulmonary-nodules | [D](domains/22-pulmonary-nodules/NOTES.discovery.md) [R](domains/22-pulmonary-nodules/NOTES.regular.md) [E](domains/22-pulmonary-nodules/NOTES.expert.md) | 7 (4) | `Track` leaks `ArgumentOutOfRangeException` when the mapped centroid leaves the field; a landmark located twice counts as two | literal |
| 23-captive-breeding | [D](domains/23-captive-breeding/NOTES.discovery.md) [R](domains/23-captive-breeding/NOTES.regular.md) [E](domains/23-captive-breeding/NOTES.expert.md) | 10 (4) | `RecordDeath` can break "dam not dead before the birth" after registration; `RankSiresFor` lists sires `Propose` refuses | literal |
| 24-radiocarbon-chronology | [D](domains/24-radiocarbon-chronology/NOTES.discovery.md) [R](domains/24-radiocarbon-chronology/NOTES.regular.md) [E](domains/24-radiocarbon-chronology/NOTES.expert.md) | 10 (5) | a marine shell is accepted, then `CorrectedAge` throws; an age beyond the curve is refused only if the likelihood underflows | cases |
| 25-adaptive-video | [D](domains/25-adaptive-video/NOTES.discovery.md) [R](domains/25-adaptive-video/NOTES.regular.md) [E](domains/25-adaptive-video/NOTES.expert.md) | 16 (7) | NaN passes the `<= 0` guards; an almost empty buffer still climbs a rung; counters overflow at `int.MaxValue` into a misleading refusal | cases |
| 26-robotic-milking | [D](domains/26-robotic-milking/NOTES.discovery.md) [R](domains/26-robotic-milking/NOTES.regular.md) [E](domains/26-robotic-milking/NOTES.expert.md) | 13 (7) | a refused kick-off stays in the anomalies; the phase regresses from Milking to Attaching; a quarter under the detach threshold never completes | cases |
| 27-railway-interlocking | [D](domains/27-railway-interlocking/NOTES.discovery.md) [R](domains/27-railway-interlocking/NOTES.regular.md) [E](domains/27-railway-interlocking/NOTES.expert.md) | 14 (6) | `RequestRoute` is not atomic when the lock time is default (high); three refusal reasons are unreachable | cases |
| 28-satellite-conjunction | [D](domains/28-satellite-conjunction/NOTES.discovery.md) [R](domains/28-satellite-conjunction/NOTES.regular.md) [E](domains/28-satellite-conjunction/NOTES.expert.md) | 9 (4) | `PositionCovariance.Combine` refuses the sum of two valid covariances, so `Assess` throws for valid trajectories (high); `Vector3` under- and overflows; interpolation can pass under the Earth's surface | draw (the first two) |
| 29-parimutuel-betting | [D](domains/29-parimutuel-betting/NOTES.discovery.md) [R](domains/29-parimutuel-betting/NOTES.regular.md) [E](domains/29-parimutuel-betting/NOTES.expert.md) | 7 (4) | no ceiling on a dividend, so `Payout` overflows; `Settle` checks the race by code only | draw |
| 30-liquidity-gap | [D](domains/30-liquidity-gap/NOTES.discovery.md) [R](domains/30-liquidity-gap/NOTES.regular.md) [E](domains/30-liquidity-gap/NOTES.expert.md) | 8 (4) | a schedule near `DateOnly.MaxValue` fails with a raw framework exception (1 run in 6); contractual flows rounded two different ways | draw |
| 31-vessel-stowage | [D](domains/31-vessel-stowage/NOTES.discovery.md) [R](domains/31-vessel-stowage/NOTES.regular.md) [E](domains/31-vessel-stowage/NOTES.expert.md) | 3 (3) | an even row count gives more starboard rows than port rows; `Rotation.Ports` bypasses the constructor's guards | cases |
| 32-grid-load-flow | [D](domains/32-grid-load-flow/NOTES.discovery.md) [R](domains/32-grid-load-flow/NOTES.regular.md) [E](domains/32-grid-load-flow/NOTES.expert.md) | 6 (4) | `Bus.NetInjection` throws for a valid bus (seed −2128493443); `BusId` accepts `"ABC\n"` | draw, literal |
| 33-music-score | [D](domains/33-music-score/NOTES.discovery.md) [R](domains/33-music-score/NOTES.regular.md) [E](domains/33-music-score/NOTES.expert.md) | 6 (4) | the sum of two valid durations can be unrepresentable; enharmonic ties refused | draw |
| 34-manuscript-reconstruction | [D](domains/34-manuscript-reconstruction/NOTES.discovery.md) [R](domains/34-manuscript-reconstruction/NOTES.regular.md) [E](domains/34-manuscript-reconstruction/NOTES.expert.md) | 6 (3) | a variant's length is checked before normalisation; `WitnessSiglum("A\n")` accepted | literal |
| 35-dna-variants | [D](domains/35-dna-variants/NOTES.discovery.md) [R](domains/35-dna-variants/NOTES.regular.md) [E](domains/35-dna-variants/NOTES.expert.md) | 14 (7) | `ApplyAll` depends on recording order for an insertion and a change at one position (high); an insertion before the first base normalises to `g.0_1ins` | cases |
| 36-knitting-patterns | [D](domains/36-knitting-patterns/NOTES.discovery.md) [R](domains/36-knitting-patterns/NOTES.regular.md) [E](domains/36-knitting-patterns/NOTES.expert.md) | 10 (5) | a drawn width of 6.86e28 overflows `Gauge`; a pattern `Expand()` can never work is accepted | draw |
| 37-batch-genealogy | [D](domains/37-batch-genealogy/NOTES.discovery.md) [R](domains/37-batch-genealogy/NOTES.regular.md) [E](domains/37-batch-genealogy/NOTES.expert.md) | 12 (8) | `Record` is not all-or-nothing (high); the 1e9 quantity cap breaks conversions of valid quantities; decimal shares do not sum to 1 | draw |
| 38-carbon-ledger | [D](domains/38-carbon-ledger/NOTES.discovery.md) [R](domains/38-carbon-ledger/NOTES.regular.md) [E](domains/38-carbon-ledger/NOTES.expert.md) | 13 (7) | refused operations raise `ArgumentException`, not the model's `LedgerRuleViolationException`; an "append-only" journal hands out its storage | literal |
| 39-geothermal-reservoir | [D](domains/39-geothermal-reservoir/NOTES.discovery.md) [R](domains/39-geothermal-reservoir/NOTES.regular.md) [E](domains/39-geothermal-reservoir/NOTES.expert.md) | 5 (3) | `IsFaceNeighbourOf` overflows `int` and reports far cells as neighbours; a step at the maximum stable step is always refused for an injector | cases |
| 40-catastrophe-losses | [D](domains/40-catastrophe-losses/NOTES.discovery.md) [R](domains/40-catastrophe-losses/NOTES.regular.md) [E](domains/40-catastrophe-losses/NOTES.expert.md) | 13 (8) | no ceiling on insured values, so sums overflow (114 of 20 000 drawn portfolios); `DistanceKmTo` is NaN for some antipodal pairs | draw (overflow), literal |
| 41-shift-planning-time-zones | [D](domains/41-shift-planning-time-zones/NOTES.discovery.md) [R](domains/41-shift-planning-time-zones/NOTES.regular.md) [E](domains/41-shift-planning-time-zones/NOTES.expert.md) | 11 (4) | `IsoWeek(9999, 52)` throws; a roster accepts an employee whose home site only shares an id | literal |
| 42-token-ledger | [D](domains/42-token-ledger/NOTES.discovery.md) [R](domains/42-token-ledger/NOTES.regular.md) [E](domains/42-token-ledger/NOTES.expert.md) | 9 (4) | `$` in four places, including `ExactDecimal.Parse`; `NonceExhausted` unreachable | literal |
| 43-passive-components | [D](domains/43-passive-components/NOTES.discovery.md) [R](domains/43-passive-components/NOTES.regular.md) [E](domains/43-passive-components/NOTES.expert.md) | 11 (6) | `MODEL.md` gives the wrong value for an inductor's `104`; `PreferredValue.CompareTo` is 0 while `Equals` is false; a temperature coefficient of −0.0 refused | literal |
| 44-vaccine-cold-chain | [D](domains/44-vaccine-cold-chain/NOTES.discovery.md) [R](domains/44-vaccine-cold-chain/NOTES.regular.md) [E](domains/44-vaccine-cold-chain/NOTES.expert.md) | 14 (6) | non-ASCII zeros and a trailing `\n` bypass the zero-sequence rule; `HistoryOf` exposes a history whose `Record` bypasses the consignment's rules | literal |
| 45-healthcare-claims | [D](domains/45-healthcare-claims/NOTES.discovery.md) [R](domains/45-healthcare-claims/NOTES.regular.md) [E](domains/45-healthcare-claims/NOTES.expert.md) | 10 (6) | Arabic-Indic zeros bypass the zero-sequence refusal; `Rates` hands out its dictionary | literal |
| 46-network-acl | [D](domains/46-network-acl/NOTES.discovery.md) [R](domains/46-network-acl/NOTES.regular.md) [E](domains/46-network-acl/NOTES.expert.md) | 3 (1) | `RuleId("a\n")` accepted | literal |
| 47-international-catalogue | [D](domains/47-international-catalogue/NOTES.discovery.md) [R](domains/47-international-catalogue/NOTES.regular.md) [E](domains/47-international-catalogue/NOTES.expert.md) | 10 (5) | script tables omit 々, the okina, full-width Latin and half-width katakana, so ordinary names are refused; `Merge` leaves two records `Add` would refuse | literal (realistic data) |
| 48-tax-rules-engine | [D](domains/48-tax-rules-engine/NOTES.discovery.md) [R](domains/48-tax-rules-engine/NOTES.regular.md) [E](domains/48-tax-rules-engine/NOTES.expert.md) | 7 (3) | a sum of incomes past `Money.MaxAmount` throws a bare `ArgumentOutOfRangeException` | cases |
| 49-clinical-trial-crf | [D](domains/49-clinical-trial-crf/NOTES.discovery.md) [R](domains/49-clinical-trial-crf/NOTES.regular.md) [E](domains/49-clinical-trial-crf/NOTES.expert.md) | 7 (3) | `VitalSigns.IsAnyObserved` ignores the diastolic pressure; five patterns accept a trailing `\n` | literal (by reading the code) |
| 50-fx-settlement | [D](domains/50-fx-settlement/NOTES.discovery.md) [R](domains/50-fx-settlement/NOTES.regular.md) [E](domains/50-fx-settlement/NOTES.expert.md) | 8 (4) | `Book` lets a bare `InvalidOperationException` escape for an unquotable reference; accumulated positions are not bounded | literal, one led to by a draw |
| **Total** | | **536 (264)** | | |

The skipped cases (601) and the domain findings (536) are different counts: one defect can be kept as several skipped cases, and one session can find a defect another re-reports.

---

## 10. Method

- **Domains.** 01–40 existed. 01–20 got a `MODEL.md` written from their code for this trial; 21–40 kept theirs. 41–50 are new, each built around one specificity: 41 time zones and daylight saving, 42 numbers beyond `decimal` (`BigInteger`, `UInt128`), 43 very small and very large magnitudes (pF to TΩ), 44 transverse over 06, 07, 10 and 20, 45 transverse over 03, 08 and 01, 46 bit-level and unsigned types (`UInt128` IPv6, `[Flags]`), 47 Unicode beyond ASCII (NFC, graphemes, bidi), 48 polymorphism and recursive trees, 49 absent values (nullable, missing-data reasons), 50 a decimal scale that depends on the currency, with explicit rounding.
- **Sessions.** Three independent profiles per domain — discovery, regular, expert — so 150 sessions. Each ran in an isolated workspace outside the repository, holding only the domain, its `MODEL.md`, the user documentation, the packed `any` and the packages `1.0.0-uxtrial.1` packed from `main` at the time. No session saw another's work, the earlier suites or reports, or the library's sources. Every session ran on the same model and effort; verification ran on a stronger configuration. Each session scaffolded its generators with `any`, wrote xUnit tests on real cases, ran its suite repeatedly with fresh seeds, kept each domain defect as a skipped test, logged its work in `NOTES.<profile>.md` and scored its fluency.
- **What is kept.** Each domain holds its three profile suites (`Tests.Discovery`, `Tests.Regular`, `Tests.Expert`) and their notes. For 01–20 the first trial's `Tests` suite, with its probes, is kept beside them; for 21–40 the second trial's suites and notes were replaced (git history keeps them).
- **Findings.** 1 530 in all, each with a kind, a component, a severity, evidence and a repro. Finding ids read `<domain><D|R|E>-<n>`.
- **Verification.** The 994 library, tool and docs findings were grouped into 151 root causes. Each root cause was verified adversarially: a verifier tried to refute it by reproduction against the packed packages and the trial's `any`, then gave a verdict (confirmed, partly, by design, refuted, not reproduced), a reassessed kind and severity, its relation to REPORT.md and REPORT-2.md, and a recommendation. This report uses the reassessed severities. The verification workspaces lived outside the repository and are not committed; the repro in each item is enough to rebuild them.
- **Domain defects.** The 536 domain findings were grouped into 264 clusters and not verified ([§9](#9-domain-defects-found-by-the-sessions)).
- **Coordinator's run.** The coordinator ran all 150 suites from the repository: 43 867 cases, 0 failed, 601 skipped, after the one fix described in [§2](#2-limits-of-this-trial), item 7.
- **Ids.** `CORE-n`, `CLI-n`, `AN-n`, `XU-n` and `DOCS-n` are stable within this report and unrelated to the same-looking ids of REPORT.md and REPORT-2.md, which are always cited with their report's name.
