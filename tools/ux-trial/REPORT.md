# Developer-UX trial — final report

🌍 **Languages:** English (this file) | 🇫🇷 [Français](./REPORT.fr.md)

**Scope.** This trial tested JustDummies and its `any` scaffolder the way a first-time user meets them.
Twenty invented business domains were each modelled with ordinary constructor guards, scaffolded with
`any`, and tested with xUnit. The domains are listed in [§11](#11-per-domain-results). The packed
packages `1.0.0-uxtrial.1` were consumed like a real user's, from a local feed, outside
`JustDummies.sln` (see [`README.md`](README.md)). Participants learnt the API from the user
documentation and the shipped PublicAPI baseline only. They ran `any generate` on every domain type,
kept the recaps verbatim in their `NOTES.md`, and left deliberate probes and `Bug_…` tests. Every
finding was then deduplicated into a root cause. Each root cause went to an adversarial verifier,
whose job was to refute it: check it against the ADRs, the specification and the user docs, and
leave a minimal reproduction under [`repros/`](repros/).

**Limits.** Participants and verifiers are agents, not human developers. Fluency scores are a
relative signal between domains, not a measurement. Each root cause had exactly one verifier, and
for domain 20 two triaged items were checked by reading rather than by a re-run (they are marked).
Every item below is judged against the two fundamentals and "The name is the scope" in
[`CLAUDE.md`](../../CLAUDE.md), and against the principles agreed with the maintainer below. Where
those principles overrule a verifier's verdict, the item says so.

> **Principles agreed with the maintainer**
>
> * **The scaffolder's honesty line.** `any` produces a *working base* the developer owns and edits;
>   it may be incomplete. That is fine as long as the CLI says where — `to verify`, a TODO that does
>   not compile. A recap that says "inferred" or "guard", or a ✓, on a recipe that is wrong is a bug.
>   An honestly flagged gap is not.
> * **Cross-parameter invariants** flagged `to verify` are expected scaffolder behaviour (ADR-0060),
>   not a defect. Improvements are welcome only if they guide the hand edit without making generation
>   cleverer.
> * **A refusal is an intended boundary only when an ADR or the documentation decides it.** A clear
>   but undecided refusal a real domain needs is a candidate for a decision, never a bug.

IDs are stable so that issues can cite them: `CLI-`, `CORE-`, `AN-`, `XU-`, `DOC-`, followed by
`B` (bug), `D` (defect), `G` (inference gap, honestly flagged), `F` (analyzer false negative) or
`C` (candidate for a decision).

---

## 1. Summary

| | |
|---|---|
| Domains | 20 |
| `any generate` sessions recorded | 143 (several cover more than one type) |
| Raw findings reported by participants | 203 |
| Verified root causes (verifier verdicts) | 29, plus 7 triaged items for domain 20 and 11 fact-checks of non-bug claims |
| **Drawn values that violated a declared constraint** | **0** |
| Bugs | 9 new (6 `cli`, 3 `core`), plus 1 already filed (#296) |
| Defects (messages, reports, documentation) | 5 `cli`, 10 `core`, 5 `analyzers` (plus 2 false negatives), 1 `xunit`, 13 cross-cutting documentation rows |
| Candidates for a decision | 21 (never counted as bugs) |
| Mean fluency score | 3.85 / 5 (seventeen domains at 4, three at 3) |

The library never broke its correctness line. Across 20 domains, no drawn value violated a declared
constraint. Contradictory constraints failed fast. Every synchronous seed replay that was tried
reproduced its run. The serious problems sit in the scaffolder. In a few precise shapes, `any`
vouches for code it has not checked: it reports "inferred", or ✓, on a file that compiles and then
produces rejected values, a constant, or a stack overflow. That breaks the honesty line the
maintainer set (see the principles above). It is also the failure ADR-0083 ranks as the most expensive.

### Fix first

| Rank | ID | Scope | Severity | Why |
|---|---|---|---|---|
| 1 | [CLI-B1](#cli-b1) | `cli` | high | A throwing guard below the first state write is neither read nor flagged. The recap says "inferred" (or "guard" with a bound dropped), the file compiles, and every draw is rejected. Hit in 4 domains. |
| 2 | [CLI-B2](#cli-b2) | `cli` | high | `any generate <Enum>` prints ✓ and emits `return new TEnum();`: a constant, and an undeclared value when the enum has no zero member. Hit in 6 domains. |
| 3 | [CLI-B3](#cli-b3) | `cli` | high | A self-referencing aggregate is scaffolded as `new AnyT()` inside `AnyT`'s own constructor. The recap reports it as "inferred", and the first instantiation overflows the stack and kills the test host. |
| 4 | [CLI-B4](#cli-b4) | `cli` | high | A guard reached through a local derived from the parameter is silent. The recap says "inferred" while 200 of 200 draws are rejected. The maintainer spec lists this as a residue, which is the defect. |
| 5 | [CORE-B1](#core-b1) | `core` | medium | Outside a scope, a first draw inside an awaited helper loses its seed. The failure message then names a seed that does not replay the run. |
| 6 | [CORE-B2](#core-b2) | `core` | medium | `As(...)`/`Combine(...)` failure messages print drawn strings raw, NUL and ESC included, contrary to ADR-0075. |
| 7 | [CLI-B5](#cli-b5) | `cli` | medium | `--format json` reports `Scaffolded` with `failed: 0` for a write it refused, while the run exits 1. This breaks ADR-0071. |
| 8 | [CORE-D1](#core-d1) | `core` | medium | Decimal conflict messages state falsehoods: they report a strict bound as inclusive, and name as "the only value left" a point neither bound allows. |
| 9 | [XU-D1](#xu-d1) | `xunit` | medium | No user page shows `using JustDummies.Xunit;`, so 13 of 20 domains failed their first build. The cause is a hidden prelude in the documentation sample compiler. |
| 10 | [AN-D1](#an-d1) | `analyzers` | medium | JD014 and JD017 report the same undeclared enum value at the same span, against ADR-0078's "one phenomenon, one id". |

Next in line: [CLI-B6](#cli-b6) (the header names a regeneration command that fails) and
[CORE-B3](#core-b3) (`Between(-ε, ε)` only ever draws 0).

---

## 2. `cli` — the `any` scaffolder (JustDummies.Cli + JustDummies.GenAny)

### Bugs

<a id="cli-b1"></a>
#### CLI-B1 — A throwing guard below the first state write is neither read nor marked (high)

Tracked in [#297](https://github.com/Reefact/just-dummies/issues/297).

<a id="cli-b2"></a>
#### CLI-B2 — `any generate <Enum>` reports ✓ and emits `return new TEnum();` (high)

Tracked in [#298](https://github.com/Reefact/just-dummies/issues/298).

<a id="cli-b3"></a>
#### CLI-B3 — A self-referencing type is scaffolded into unbounded recursion (high)

Tracked in [#299](https://github.com/Reefact/just-dummies/issues/299).

<a id="cli-b4"></a>
#### CLI-B4 — A guard reached through a derived local is silent while the recap says "inferred" (high)

Tracked in [#300](https://github.com/Reefact/just-dummies/issues/300).

<a id="cli-b5"></a>
#### CLI-B5 — `--format json` reports a refused write as `Scaffolded` (medium)

Tracked in [#301](https://github.com/Reefact/just-dummies/issues/301).

<a id="cli-b6"></a>
#### CLI-B6 — The short-name lookup lists candidates C# could never bind, and the header names a command that fails (medium)

Tracked in [#302](https://github.com/Reefact/just-dummies/issues/302).

<a id="cli-b7"></a>
#### CLI-B7 — `any generate` on an unrestored project claims the project does not reference JustDummies (already filed)

This was found during the pilot and is filed as
[#296](https://github.com/Reefact/just-dummies/issues/296). It is listed here so the `cli` train
carries it. Every domain ran `dotnet restore` first because of it.

### Defects (messages, wording, recap/JSON)

<a id="cli-d1"></a>
**CLI-D1 — "to verify" never says which guard went unread, and cites the maintainer spec's "§9" (medium).**
Tracked in [#303](https://github.com/Reefact/just-dummies/issues/303).

<a id="cli-d2"></a>
**CLI-D2 — "Delete that one line, and you are done" is false when the right base has another builder type (medium).**
Tracked in [#304](https://github.com/Reefact/just-dummies/issues/304).

<a id="cli-d3"></a>
**CLI-D3 — A nullable reference parameter loses its `?` in the value pin, the recap and the JSON (medium).**
Tracked in [#305](https://github.com/Reefact/just-dummies/issues/305).

<a id="cli-d4"></a>
**CLI-D4 — `--namespace` rewrites the analysed type's name in the recap and the JSON (low).**
Tracked in [#306](https://github.com/Reefact/just-dummies/issues/306).

<a id="cli-d5"></a>
**CLI-D5 — `p >= N` / `p <= N` with N ≠ 0 are unread, while `ThrowIfGreaterThanOrEqual(p, N)` is read (low).**
Tracked in [#307](https://github.com/Reefact/just-dummies/issues/307).

### Inference gaps honestly flagged (not bugs)

Each of these shapes is not read, and in every case observed the recap marked the parameter
`to verify, unread guards`, and the build stopped at the TODO line. That is ADR-0060 and ADR-0083
working as designed. Widening the reader is governed by ADR-0085 ("change the guard reader only
against a field report"). The domains column is that field report.

| ID | Guard shape not read | Flagged? | Domains |
|---|---|---|---|
| CLI-G1 | Relational/or patterns: `x is < 1 or > 12`, `{ Length: < 8 or > 20 }` | yes | 02, 03, 04, 09, 13, 14 |
| CLI-G2 | Combined conditions: `x < a \|\| x > b`, `!(x > 0)`, `IsNaN(v) \|\| v < Min` | yes | 09, 13, 15, 20 |
| CLI-G3 | Regex guards (`Regex.IsMatch`, `[GeneratedRegex]`, static `Regex` fields) — *deliberately* not read, spec §5.3 (line 827); see §9 | yes | 02, 03, 05, 06, 07, 11, 12, 15 |
| CLI-G4 | Delegated private `Validate(...)` helpers | yes | 01, 02, 03, 05, 15 |
| CLI-G5 | Temporal guards (`TimeSpan`/`DateTime`/`DateOnly` bounds; `static readonly` bounds are not compile-time constants) | yes | 05, 13, 16, 17, 19 |
| CLI-G6 | Length through helpers: `ThrowIfGreaterThan(s.Length, N)`, `ThrowIfZero(s.Length)` | yes | 01, 19, 20 |
| CLI-G7 | Scale guards: `decimal.Round(v, n) != v`, `v.Scale > n` | yes | 08, 12, 13, 18 |
| CLI-G8 | Some count shapes (`Distinct().Count()`); `Count > N` and `Count == 0` *are* read | yes | 18, 19 |
| CLI-G9 | Arithmetic and helpers on the value: `Math.Abs(v) > 90`, `v < (Half)0` | yes | 20 |
| CLI-G10 | Check digits and checksums (Luhn, mod 97, GS1) | yes | 01, 06, 08, 10, 11 |
| — | `p >= N` / `p <= N` with N ≠ 0 | yes, but inconsistent with the helper form: [CLI-D5](#cli-d5) | 20 |

A named constant is inlined as its literal (`ThrowIfGreaterThan(kg, Weight.MaxGrossKilograms)` gives
`.LessThanOrEqualTo(32500d)`, domain 07). The value is correct, so this is cosmetic only.

### Candidates for a decision

Detailed in [§8](#8-candidates-for-a-decision): [CLI-C1](#cli-c1) (large count ceilings steer
scaffolded aggregates), [CLI-C2](#cli-c2) (count ceilings above the million cap reported as unread),
[CLI-C3](#cli-c3) (prefer referenced-project types in the short-name lookup), [CLI-C4](#cli-c4)
(read the or-pattern range guards of CLI-G1/G2), [CLI-C5](#cli-c5) (turn a literal regex guard into
`Any.StringMatching`).

### Expected behaviour

<a id="cli-e1"></a>
**CLI-E1 — Cross-parameter invariants are flagged "to verify" (ADR-0060).**
`start < end`, claims within a policy period, `stop < limit`, expiry after manufacture: every one of
the 20 domains had at least one such invariant. The scaffolder marked each one `to verify` and
blocked the build, as ADR-0060 and spec §16 decide. This is not a defect and not a top friction. The
generator is a working base the developer owns. Participants resolved the invariants with disjoint
windows, a hand-written `Generate()`, or `Any.Combine`.
[`repros/02-trading-1`](repros/02-trading-1) shows the shape. Three improvement ideas fit, and none
of them makes generation cleverer:

* **(a) Documentation.** Add a CLI-page section, "When a guard ties two parameters", with compiled
  samples for:
  * the start + duration reparametrisation, which draws `start` and a positive `duration` rather
    than two bounds;
  * deriving one draw from another inside `Generate()`;
  * the one-line check-digit form `.As(body => body + Luhn(body))`. Domain 11 found it clean, and
    it also answers CLI-G10.
* **(b) A TODO comment that quotes the unread guard**, and says so when the guard reads two
  parameters. This is [CLI-D1](#cli-d1).
* **(c) Only as a possible later ADR: a re-scaffold-safe `partial` hook for the final assembly**, so
  that `--force` does not erase a hand-written cross-parameter rule. Domains 09, 10 and 12 lost, or
  feared losing, such edits. The current overwrite is disclosed in the refusal message and the
  header, so it is not a defect today.

---

## 3. `core` — JustDummies (generators, constraints, messages, reproducibility)

**No drawn value violated a declared constraint in any of the 20 domains.** The items below are
about messages, documentation and one replay path.

- *WithScale.* The verifier checked `WithScale` exactly, on `decimal.GetBits` with `BigInteger`,
  across magnitudes from 0 to `decimal.MaxValue`. Every value stayed on the 10⁻ⁿ lattice that
  ADR-0015 decides ([CORE-D8](#core-d8)).
- *DateTime Kind.* The Kind behaviour is classified as a documentation defect ([CORE-D7](#core-d7)),
  but its consequence is surprising and is stated there plainly.
- *Seed outside a scope.* Only the async-flow case is a real defect ([CORE-B1](#core-b1)). The rest
  is by design.

### Bugs

<a id="core-b1"></a>
#### CORE-B1 — Outside a scope, a seed picked inside an awaited helper does not flow back, so the reported seed does not replay the run (medium)

Tracked in [#308](https://github.com/Reefact/just-dummies/issues/308).

<a id="core-b2"></a>
#### CORE-B2 — `As(...)`/`Combine(...)` failure messages print drawn strings raw, contrary to ADR-0075 (medium)

Tracked in [#309](https://github.com/Reefact/just-dummies/issues/309).

<a id="core-b3"></a>
#### CORE-B3 — `Any.Double().Between(-double.Epsilon, double.Epsilon)` only ever draws 0 (low)

Tracked in [#310](https://github.com/Reefact/just-dummies/issues/310).

### Defects (messages, wording, documentation of a contract)

<a id="core-d1"></a>
**CORE-D1 — Decimal conflict messages misstate strict bounds (medium).**
Tracked in [#311](https://github.com/Reefact/just-dummies/issues/311).

<a id="core-d2"></a>
**CORE-D2 — A decimal strict bound that empties a `WithScale` lattice names only one side (medium).**
Tracked in [#312](https://github.com/Reefact/just-dummies/issues/312).

<a id="core-d3"></a>
**CORE-D3 — Distinct exhaustion over a foreign `IAny<T>` states as fact that its values "are not reproducible" (medium).**
Tracked in [#313](https://github.com/Reefact/just-dummies/issues/313).

<a id="core-d4"></a>
**CORE-D4 — Floating-point conflict messages restate a strict bound as the next representable value (low).**
Tracked in [#314](https://github.com/Reefact/just-dummies/issues/314).

<a id="core-d5"></a>
**CORE-D5 — An offset refused near `DateTimeOffset.MaxValue`/`MinValue` does not state the limit it implies (low).**
Tracked in [#315](https://github.com/Reefact/just-dummies/issues/315).

<a id="core-d6"></a>
**CORE-D6 — The `SetOf` cardinality refusal names no count (low).**
Tracked in [#316](https://github.com/Reefact/just-dummies/issues/316).

<a id="core-d7"></a>
**CORE-D7 — `DateTime` draws are always `Kind = Utc` and compare by ticks: decided in XML docs, absent from the user page (medium, documentation).**
Tracked in [#317](https://github.com/Reefact/just-dummies/issues/317).

<a id="core-d8"></a>
**CORE-D8 — The `WithScale` user page says it "fixes the number of decimal places"; ADR-0015 decides a value lattice (medium, documentation).**
Tracked in [#318](https://github.com/Reefact/just-dummies/issues/318).

<a id="core-d9"></a>
**CORE-D9 — `DateTimeOffset.WithGranularity` grids the UTC instant, not the local clock; the user docs never say so (medium, documentation).**
Tracked in [#319](https://github.com/Reefact/just-dummies/issues/319).

<a id="core-d10"></a>
**CORE-D10 — The distinct-cardinality refusal moved to `Generate()` without a record; ADR-0004, JD016 and the collections page still promise declaration time (medium, documentation + ADR).**
Tracked in [#320](https://github.com/Reefact/just-dummies/issues/320).

### Inference gaps

Not applicable: the library infers nothing. The bounded draws it refuses are listed in §9.

### Candidates for a decision

Detailed in [§8](#8-candidates-for-a-decision): CORE-C1 (TimeOnly across midnight), CORE-C2
(dependent draw), CORE-C3 (weekday / business day), CORE-C4 (decimal step), CORE-C5 (Guid v4),
CORE-C6 (ordered series), CORE-C7 (DateTime Kind inheritance), CORE-C8 (local-clock granularity),
CORE-C9 (a subset `OneOf` intersecting), CORE-C10 (a public "draws ambient" declaration).

### Expected behaviour

- A transposed single call such as `Any.Int32().Between(10, 1)` throws `ArgumentException`, not a
  conflict. This is documented and caught by JD014 (§9).
- Over a synchronous body, the seed reported outside a scope replays from the test's first draw, and
  the same seed is reported for every failure in that test.
- A second `OneOf` inside a `With…` lambda is refused as a declaration conflict (ADR-0100, JD034).
  The missing piece is documentation ([DOC-9](#doc-9)).

---

## 4. `analyzers` — JDxxx

### Defects

<a id="an-d1"></a>
**AN-D1 — JD014 and JD017 both report one undeclared enum value in `Any.Enum<T>().OneOf(...)` (medium).**
Tracked in [#321](https://github.com/Reefact/just-dummies/issues/321).

<a id="an-d2"></a>
**AN-D2 — JD023 names only the constraint that empties the domain, without its arguments (low).**
Tracked in [#322](https://github.com/Reefact/just-dummies/issues/322).

<a id="an-d3"></a>
**AN-D3 — JD014 and JD023 suppress different negative-test shapes (low).**
Tracked in [#323](https://github.com/Reefact/just-dummies/issues/323).

<a id="an-d4"></a>
**AN-D4 — The JD014 page claims to mirror the guards "exactly", and a test files a refused argument under "legal" (low).**
Tracked in [#324](https://github.com/Reefact/just-dummies/issues/324).

<a id="an-d5"></a>
**AN-D5 — Info rules (JD020, JD022, JD024, JD029, JD030, JD031, JD033) never appear in `dotnet build` output, and no user page says so (medium, documentation).**
Tracked in [#325](https://github.com/Reefact/just-dummies/issues/325).

### False negatives against a rule's documented scope

<a id="an-f1"></a>
**AN-F1 — JD016 ignores a typed numeric `OneOf` pool (low).**
Tracked in [#326](https://github.com/Reefact/just-dummies/issues/326).

<a id="an-f2"></a>
**AN-F2 — JD014 misses non-finite constant bounds, and `Half` in `OneOf` (low).**
Tracked in [#327](https://github.com/Reefact/just-dummies/issues/327).

### Candidates (outside every rule's documented scope)

Detailed in [§8](#8-candidates-for-a-decision): AN-C1 (a drawn value reaching an assertion), AN-C2
(JD023 beyond integers), AN-C3 (`--entry-point any` members invisible to JD007/JD009), AN-C4 (the
remaining constant misses), AN-C5 (unsupported regex constructs at build time), AN-C6 (a refinement
lambda that discards its argument).

### Info rules invisible in `dotnet build`

This is [AN-D5](#an-d5): a documentation defect, not a code defect. It changes how every Info rule
is experienced outside an IDE.

---

## 5. `xunit` — the adapter

<a id="xu-d1"></a>
**XU-D1 — No user page shows `using JustDummies.Xunit;` (medium; 13 of 20 domains).**
Tracked in [#328](https://github.com/Reefact/just-dummies/issues/328).

---

## 6. `catalog`

Nothing was found. No domain suppressed a JDxxx rule through `JustDummies.DiagnosticCatalog`, so this
trial says nothing either way about the package.

---

## 7. Documentation (cross-cutting)

Rows that carry a component ID are described in their section. Every fix applies to the English page
**and** its French twin.

| ID | Page | Defect | Component | Fix |
|---|---|---|---|---|
| [XU-D1](#xu-d1) | `packages/justdummies-xunit`, `README.nuget.md`, getting started | `using JustDummies.Xunit;` never shown; hidden sample prelude | `xunit` | Show the usings; remove the prelude injection |
| [CLI-D2](#cli-d2) | `packages/justdummies-cli` (§"to verify"), spec §5.6 | "delete that one line" is false when the base type changes | `cli` | Say what to retype; compiled sample |
| [CORE-D7](#core-d7) | `generators/dates-and-times` | `DateTime` Kind is always `Utc`, tick comparison, default range | `core` | "DateTime and Kind" paragraph |
| [CORE-D8](#core-d8) | `generators/numbers` | `WithScale` described as fixing decimal places | `core` | Value lattice wording, link ADR-0015 |
| [CORE-D9](#core-d9) | `generators/dates-and-times` | Granularity under an offset grids the UTC instant | `core` | State it; XML summary too |
| [CORE-D10](#core-d10) | ADR-0004, ADR-0009/0011, `analyzers/JD016`, `generators/collections` | Cardinality refusal documented at declaration; it runs at `Generate()` | `core` | Amending ADR, then the pages |
| [AN-D5](#an-d5) | `analyzers/README`, `packages/justdummies` | Info rules invisible in console builds | `analyzers` | One sentence plus an `.editorconfig` snippet |
| <a id="doc-1"></a>DOC-1 | `generators/enums-and-choices:157`, `guides/errors-and-conflicts:63` | `Any.Boolean().Except(true, false)` does not exist in the public API | `core` | [#329](https://github.com/Reefact/just-dummies/issues/329) |
| <a id="doc-2"></a>DOC-2 | `generators/strings:56`, `analyzers/JD030:24` | "`NonEmpty()` leaves the ceiling where it was"; it gives 1 to 1025 (JD030's own sentence says so) | `core` | [#329](https://github.com/Reefact/just-dummies/issues/329) |
| <a id="doc-3"></a>DOC-3 | `generators/collections:50`, `guides/design-principles:72` | Cite ADR-0029 for the million cap; superseded by ADR-0076 | `core` | [#329](https://github.com/Reefact/just-dummies/issues/329) |
| <a id="doc-4"></a>DOC-4 | `generators/strings` (regex tables) | `\xNN`/`\uNNNN` work but are unlisted; `\b`, `\B`, `\G`, `\p{…}`, `\k<…>` and class subtraction are refused but unlisted; `IgnorePatternWhitespace` throws `ArgumentException`, unmentioned; the 65,536-character limit is absent; every refusal ends with the same fixed list | `core` | [#329](https://github.com/Reefact/just-dummies/issues/329) |
| <a id="doc-5"></a>DOC-5 | `packages/justdummies-cli:254` | "A lambda never widens the base"; `_ => Any.Int32()` replaces it | `cli` | [#330](https://github.com/Reefact/just-dummies/issues/330) |
| <a id="doc-6"></a>DOC-6 | `guides/errors-and-conflicts:101-108` | `Seed` documented "inside a reproducible scope" only; it is also set outside | `core` | [#308](https://github.com/Reefact/just-dummies/issues/308) (with [CORE-B1](#core-b1)) |
| <a id="doc-7"></a>DOC-7 | `packages/justdummies-cli` | Silent on regex guards never being read (spec §5.3 decides it) | `cli` | [#330](https://github.com/Reefact/just-dummies/issues/330) |
| <a id="doc-8"></a>DOC-8 | `packages/justdummies-cli` | No section on where a cross-parameter invariant goes | `cli` | [#330](https://github.com/Reefact/just-dummies/issues/330) |
| <a id="doc-9"></a>DOC-9 | `packages/justdummies-cli:243-247` (refinement) | Does not say a once-only constraint (`OneOf`, families, `WithLength`, `MultipleOf`, `WithScale`) cannot be declared again in the lambda; `Except`/`DifferentFrom` narrow a pool; JD034 cannot see into the lambda | `cli` | [#330](https://github.com/Reefact/just-dummies/issues/330) |
| <a id="doc-10"></a>DOC-10 | `packages/justdummies-cli` (refinement) | To reuse a prepared refinement, hold a `Func<AnyX, AnyX>`, not a built `IAny<T>` (ADR-0109; CS0266 does not teach it) | `cli` | [#330](https://github.com/Reefact/just-dummies/issues/330) |
| <a id="doc-11"></a>DOC-11 | `packages/justdummies-cli` | A scaffolded string row raises JD030 (Info) by design | `cli` | [#330](https://github.com/Reefact/just-dummies/issues/330) |
| <a id="doc-12"></a>DOC-12 | `guides/errors-and-conflicts` | A transposed single call (`Between(10, 1)`) is an argument mistake, not a conflict | `core` | [#329](https://github.com/Reefact/just-dummies/issues/329) |
| <a id="doc-13"></a>DOC-13 | `analyzers/JD007`, `JD009` | "Written inline from `Any`" suggests `Any.X()` from `--entry-point any` is seen; it is not | `analyzers` | Depends on [AN-C3](#an-c3) |

Suggested `JustDummies.Documentation.UnitTests` guards: resolve inline `Any.<Factory>().<Member>(`
spans against the PublicAPI baseline (red on DOC-1), and fail on a link to a superseded ADR (red on
DOC-3). Issue #296 ([CLI-B7](#cli-b7)) is a CLI message defect, not a page defect, so it has no row
here.

---

## 8. Candidates for a decision

None of these is a bug. In each case the library either refuses clearly or has no member, and the
trial supplies the field report an ADR would need. Options are given in one line each.

<a id="core-c1"></a>
**CORE-C1 — `TimeOnly` ranges across midnight.**
*Who:* 02, 03, 04, 09, 12, 13, 15, 16 (night shifts, curfews, trading sessions, overnight windows).
*Now:* `Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(2, 0))` throws `ArgumentException`
("The start … must be at or before the end"). *Clear?* Yes, but the dates page names neither the
refusal nor a workaround. No ADR decides a wrapping window, so this is not an intended boundary. The
BCL's `TimeOnly.IsBetween` wraps. *Options:*
**(A)** Align with `IsBetween`: reversed bounds wrap. This changes what an existing call means, and
JD014 can no longer detect a transposed `Between` on `TimeOnly`.
**(B)** Add an explicit member (`BetweenAcrossMidnight`, or a `Wrapping` form) and keep the refusal,
with the refusal message pointing to the new member.
The lead leans to **B**: it keeps every existing call's meaning and the typo detection, and refuses
loudly with a way out. It is the maintainer's call. Either way, the dates page should name the
current refusal and the `OneOf` of two `Between`s workaround.

<a id="core-c2"></a>
**CORE-C2 — Dependent draw (draw one value, then build a recipe from it).**
*Who:* 01, 02, 07, 17. *Now:* there is no `Bind`/`SelectMany`. Participants nested `Generate()`
inside `As`/`Combine`. Their probes show it replays under an ambient seed, but nothing documents
that, and under an `AnyContext` the nested draw would come from the ambient source (the ADR-0102
partial-replay case). *Clear?* There is no refusal: the gap is silent. *Options:* document the nested
`Generate()` as a supported idiom, with its replay limits / add a bind combinator / do both. A
smaller first step is idea (a) of CLI-E1.

<a id="core-c3"></a>
**CORE-C3 — Weekday / business-day constraint.**
*Who:* 01, 02. *Now:* no member. Participants used a precomputed `OneOf` pool (01) or an `Except`
over about 1,148 weekend dates (02). *Clear?* No refusal, no doc idiom. *Options:* a `DayOfWeek`
filter, which is constructive over the ordinal space / document the pool idiom for holiday calendars,
which are domain data and arguably outside ADR-0046's scope.

<a id="core-c4"></a>
**CORE-C4 — A decimal step / `MultipleOf` on `AnyDecimal`.**
*Who:* 02 (tick size), 18 (half-point grades, a 41-value pool). Domain 17 asked for a grid on
`AnyDouble`, which ADR-0015 refuses deliberately ("0.1 is not representable"). *Now:* `WithScale`
only, and it is once-only (ADR-0015). *Clear?* The absence is clear, and the workaround is
`Any.Int32().Between(..).As(k => k * 0.25m)`. *Options:* `AnyDecimal.MultipleOf(decimal)` on the
existing lattice machinery / document the `As` workaround. ADR-0015's own revisit condition applies.

<a id="core-c5"></a>
**CORE-C5 — RFC 9562 version-4 `Guid`.**
*Who:* 19. *Now:* `Any.Guid()` draws 128 random bits, so the version nibble is arbitrary, and code
that checks `Guid.Version == 4` rejects most draws. *Clear?* There is no member and no doc mention.
*Options:* `Version4()`/`WithVersion(4)`, which fixes 6 bits constructively / document the
`.As(...)` mask.

<a id="core-c6"></a>
**CORE-C6 — Ordered (monotonic) series.**
*Who:* 12 (a half-hourly load curve). *Now:* no member. The workaround is to sort a distinct draw in
`As`, or to accumulate positive increments. *Options:* an `Ordered()`/`StrictlyIncreasing()`
collection constraint, which is constructive / document the workaround.

<a id="core-c7"></a>
**CORE-C7 — Should a `DateTime` draw inherit the `Kind` its bounds share?**
This follows from [CORE-D7](#core-d7). *Options:* keep `Utc` and document it / inherit the `Kind`
when every bound shares a non-`Utc` one / refuse mixed or `Local` bounds. Each option changes the
contract, so it needs an ADR.

<a id="core-c8"></a>
**CORE-C8 — Granularity on the local clock under a declared offset.**
This follows from [CORE-D9](#core-d9). *Who:* 07, 09, 19. *Options:* keep the UTC grid and document
it / anchor the grid at `MinValue − offset` when the offset is fixed (constructive) / refuse
`WithGranularity` combined with `WithOffsetBetween`.

<a id="core-c9"></a>
**CORE-C9 — Let a second, subset `OneOf` intersect with the first.**
*Who:* 05. *Now:* refused with "is already defined" (ADR-0100, JD034). *Options:* keep it and
document `Except` ([DOC-9](#doc-9)) / reopen ADR-0100 so that a subset intersects, as a bound does.

<a id="core-c10"></a>
**CORE-C10 — A public way for a user or scaffolded generator to declare that it draws ambient.**
*Who:* 06, 18. *Now:* `IHasRandomSource` and `IReproducibilityHint` are internal, so the full-replay
guidance can never apply to an `any` generator. *Options:* a public marker emitted by the scaffolder
/ keep the hedge ([CORE-D3](#core-d3)) only.

<a id="cli-c1"></a>
**CLI-C1 — Large count ceilings steer scaffolded aggregates.**
*Who:* 20. *Now:* the guard `readings.Count > 500_000` becomes `.WithMaxCount(500000)`, and under
ADR-0076 a maximum steers the draw. One default `AnyReadingBatch` drew 406,641 readings in about 2 s,
and the harness had to be killed. The recap says only `guard`. Narrowing `AnyValidReadings()` then
makes refinements such as `WithReadings(r => r.WithCount(100_000))` conflict, which is correct under
ADR-0109. Narrowing only the default constructor's argument worked. *Clear?* No warning at all.
*Options:* the recap flags a guard-derived ceiling above a threshold and suggests narrowing the
default constructor / emit a default size window inside the whole base / revisit ADR-0076's
collection clause for composite elements. Not re-run by the verifier: the mechanism is shown in
[`repros/20-4`](repros/20-4).

<a id="cli-c2"></a>
**CLI-C2 — Count ceilings above the million cap are reported as unread.**
*Who:* 20. *Now:* `if (values.Count > 2_000_000) throw` is marked `to verify, unread guards`, and it
blocks the build. Sizes are capped at 1,000,000 (ADR-0076), so the guard holds by construction. Spec
§5.3 decides the refusal, but its stated reason ("would throw inside the emitted constructor")
explains why nothing is emitted, not why the guard is called unread. *Options:* read the guard as
satisfied by construction (`guard`, plus a recap note "above the size cap") / keep the refusal and
correct the spec's justification and the recap text. Repro: [`repros/20-4`](repros/20-4).

<a id="cli-c3"></a>
**CLI-C3 — Prefer project types over framework types of the same name.**
*Who:* 03 (`Claim`), 04 (`Machine`), 12. *Now:* every type of the right arity and visibility competes
equally, and the refusal is clear and lists the candidates (§3.2). *Options:* prefer types declared
in the source of referenced projects before widening to metadata / keep it and rely on the full name.
This needs a §3.2 or ADR decision. The arity and accessibility filter is a bug ([CLI-B6](#cli-b6)).

<a id="cli-c4"></a>
**CLI-C4 — Read or-pattern and `||` range guards.**
*Who:* see CLI-G1/G2 (10 domains). *Now:* these guards are flagged honestly. An or-pattern of two
constant relational patterns on one parameter maps exactly to `Between`. *Options:* widen the reader
for that one shape, which ADR-0085 permits against a field report / keep it as is.

<a id="cli-c5"></a>
**CLI-C5 — Turn a literal regex guard into `Any.StringMatching`.**
*Who:* 02, 07, 15. *Now:* not read, by design (spec §5.3). The spec names this a v1.1 candidate, and
4 of 5 realistic patterns fall outside the regular subset (ADR-0008). *Options:* emit
`StringMatching` only when the pattern parses in the subset, otherwise keep the TODO / keep it as is,
with [DOC-7](#doc-7).

<a id="an-c1"></a>
**AN-C1 — A rule for a drawn value reaching an assertion.**
*Who:* 02, 03, 05, 06, 07, 10, 16, 19. That is eight domains, and all of them cite the first
fundamental's corollary: "the value the assertion is about is not a dummy". *Now:* no rule.
*Clear?* Silent. *Options:* an Info-level rule for an `any`-named local, or a value returned by
`Generate()`, flowing into `Assert.*` / a documentation-only stance. Data flow across helpers would
be heuristic, so the scope must be bounded up front.

<a id="an-c2"></a>
**AN-C2 — JD023 beyond integers.**
*Who:* 02, 09, 13, 16, 17, 18 (crossed constant decimal, double and `TimeSpan` bounds, a typical
reference-range typo). *Now:* "Integer generators only" is documented. *Options:* extend to decimal
and temporal ordinals, which fold exactly / keep integers only, and let floating point stay out.

<a id="an-c3"></a>
**AN-C3 — Scaffolded `--entry-point any` members are invisible to JD007/JD009.**
*Who:* 03, 07, 11, 14, 16. *Now:* `Any.Leg()` compiles to a member of `AnyLegEntry`, and the walk
stops at consumer extensions by design (`GeneratorFacts.cs:71, 87-94`). `new AnyX()` and locals are
documented limits, and an inline `Any.Combine` *is* seen. *Options:* trust a C# 14 `extension(Any)`
member whose body returns a new `IAny<T>`, or one carrying a scaffold marker / document the
limitation ([DOC-13](#doc-13)). Repro:
[`repros/analyzers-scaffolded-iany-blind`](repros/analyzers-scaffolded-iany-blind).

<a id="an-c4"></a>
**AN-C4 — The remaining constant misses.**
*Who:* 01, 05, 09, 10, 12, 13, 15, 19. Each miss is outside its rule's documented scope (ADR-0106:
"a question of scope goes to a decision rather than to a test"):

- JD016 does not count `AnyChar` families (`Numeric()` = 10);
- JD014 does not check a transposed `Between` on `DateOnly`, `DateTime`, `TimeOnly`,
  `DateTimeOffset` or `TimeSpan`;
- JD034 has no `Guid` `Empty()`/`NonEmpty()` pair.

*Options:* accept each as a small, sound extension / document the scope. Repro:
[`repros/analyzers-constant-misses`](repros/analyzers-constant-misses).

<a id="an-c5"></a>
**AN-C5 — Unsupported regex constructs at build time.**
*Who:* 08. *Now:* `UnsupportedRegexException` at the first call. It is clear, and names the construct
and its position. *Options:* a rule that parses constant patterns with the library's own parser /
nothing.

<a id="an-c6"></a>
**AN-C6 — A refinement lambda that discards its argument.**
*Who:* 18. *Now:* `WithCredits(_ => Any.Int32())` replaces the base, and only the domain guard
catches it. *Options:* an Info rule on a `With…(Func<…>)` lambda whose parameter is unused /
[DOC-5](#doc-5) only.

---

## 9. Intended boundaries met

Each boundary below is decided by an ADR, the specification, or the user docs. The last column
judges the refusal message the participants actually saw.

| Boundary | Decided by | Domains | Refusal clear? |
|---|---|---|---|
| Regex constructs outside the regular subset (lookarounds, `\b`, `\p{…}`, backreferences, class subtraction, atomic groups, scoped `(?i:)`) | ADR-0008 | all 20 probed | **Yes**: construct and position. The trailing list is fixed rather than specific ([DOC-4](#doc-4)). |
| `RegexOptions.IgnorePatternWhitespace` | `Any.Pattern.cs:71` | 03, 06, 08, 10, 11, 13 | Yes, as `ArgumentException`. Missing from the page ([DOC-4](#doc-4)). |
| A string or pattern element without an advertised cardinality fails through the bounded redraw | ADR-0004, ADR-0009, ADR-0027 | 01, 04, 10, 19 | Yes, as `AnyGenerationException` with the seed and a replay snippet. |
| Two-argument static factories are not recognised | Spec §5.1 rule 2, ADR-0089 | 10 (`ExpiryMonth.Of(year, month)`) | Yes, names the rule. ADR-0085 is the record to reopen it against this report. |
| Regex guards are not read | Spec §5.3 (line 827), v1.1 candidate | 02, 03, 05, 06, 07, 11, 12, 15 | Flagged `to verify`. The user page is silent ([DOC-7](#doc-7)). |
| Cross-parameter rules | ADR-0060, ADR-0083, spec §16 | all 20 | Flagged and blocking. See [CLI-E1](#cli-e1). |
| `Any.Int32().Between(10, 1)` throws `ArgumentException`, not a conflict | `errors-and-conflicts` ("Ordinary argument mistakes"), JD014 page | 11 | Yes, and JD014 flags it at build time ([DOC-12](#doc-12) adds one line). |
| Sizes above 1,000,000 | ADR-0076 | 17, 20 | Yes, at declaration; JD014 flags constants. |
| Floating-point draws within an ordinary magnitude (`Between(-MaxValue, MaxValue)` stays within about 1e6) | ADR-0031 | 05, 20 | Not a refusal; documented. |
| `WithScale` declared once; no lattice on binary floating point | ADR-0015 | 02, 17 | Yes. |
| A value set (`OneOf`) declared once per generator | ADR-0100, JD034 | 05 | "Is already defined": clear about the rule, but the CLI page does not prepare the reader ([DOC-9](#doc-9)). |
| The refine overload takes no `IAny<T>` | ADR-0109 | 20 | `CS0266` does not teach it ([DOC-10](#doc-10)). |
| A foreign `IAny<T>` counts as a separate source for replay | ADR-0102 | 06, 18 | The hedge is intended; its wording overclaims ([CORE-D3](#core-d3)). |
| A nullable parameter's default recipe never draws null | ADR-0064 | 01, 02, 05, 15 | Held. The pin's type is [CLI-D3](#cli-d3). |
| A scaffolded string row raises JD030 (Info) | Spec §5.2 and §17 (compile-the-output tests excuse JD030 by name) | 04 | Intended ([DOC-11](#doc-11)). |
| JD028 is silent on scaffolded or held generators; JD007/JD009 are silent on `new AnyX()` and locals | JD028/JD007/JD009 pages, "What it does not flag" | 01, 06, 11, 14, 15, 17, 18 | Documented. The `Any.X()` spelling is [AN-C3](#an-c3). |
| JD032 does not cover `Between` | JD032 page, ADR-0078 | 14 | Documented ("still covered by JD024"). |
| Constant-conflict rules are silent inside `Assert.Throws` | JD014/JD016/JD023 pages | 04, 15 | Documented. The JD014/JD023 inconsistency is [AN-D3](#an-d3). |
| JD023 covers integer generators only | JD023 page | 02, 09, 13, 17, 18 | Documented ([AN-C2](#an-c2)). |
| `--force` overwrites the generated file | Refusal message and header | 09, 10, 12 | Yes, disclosed. Idea (c) of [CLI-E1](#cli-e1). |

`TimeOnly` across midnight is **not** listed: no ADR decides it ([CORE-C1](#core-c1)).

---

## 10. What works well

Each point below is backed by at least one domain's notes and probes.

* **Conflict messages name both sides, including across a scaffolded base and a test refinement.**
  For example: "Cannot apply GreaterThan(400) because Between(1, 365) already requires values less
  than or equal to 365." (03), and "Cannot apply Before(1890-01-01) because Between(1900-01-01,
  2025-12-31) already requires…" (08). All 20 domains praised this. The exceptions are the
  decimal/double wording items in §3.
* **Regex refusals name the construct and its position:** "uses a negative lookahead '(?!…)' at
  position 8" (18). Every format the domains actually needed generated correctly. Examples: IBAN and
  BIC (01), GS1 strings with `\x1D` (10), E.164 (11), SSCC with named groups and `\A..\z` (06), and
  ISIN and FIX timestamps (02). `\d` draws ASCII digits only.
* **Seed replay works end to end.** `[Reproducible]` prints a copy-pasteable
  `[Reproducible(Seed = …)]`. Pinning it reproduced the exact failing value in 01, 04, 06, 07, 08,
  11, 13, 14, 16, 17 and 19. `Any.UseSeed`, `WithSeed` and `Reproducibly` replayed whole scaffolded
  graphs: a depth-3 bill of materials (05), a `PaymentBatch` with nested draws (01), and a `Flight`
  (16). A 100,000-reading batch replayed field for field (20).
* **Pool inspection explains why a value never draws:** for example "376012345678 rejected by
  WithLength(13)" (06), "N12345 by StringMatching(…)" (16), and "12.025 rejected by WithScale"
  (13).
* **Recaps are honest in the vast majority of cases.** Fourteen domains recorded no silent guard
  miss: every domain except 04, 06, 07, 14, 16 and 19 (domain 20 probed eight shapes on purpose).
  Read guards were honoured in 500-draw harnesses (02: 7 × 500 draws, 08: 7 × 500). The
  non-compiling `TODO_verify` line turns the build output into an exact work list (04, 10, 16). The
  silent cases are confined to [CLI-B1](#cli-b1)–[CLI-B4](#cli-b4).
* **The refine-lambda `With…` design (ADR-0109)** reads like the business rule:
  `Arbitrary.CreditTransfer().WithDebtor(p => p.InCountry("DE"))` (01),
  `WithDriver(d => d.WithYearsLicensed(y => y.GreaterThanOrEqualTo(3)))` (03),
  `WithRoute(route => route.WithDistanceKm(5_800))` (16). A refinement that contradicts the base
  fails naming both sides (07, 10, 14). Domain 02 called it "a real improvement over replacing a
  generator".
* **Composition through `AnyX` (ADR-0089)** worked first time across nested aggregates (14), and a
  ten-parameter constructor scaffolded without hitting the arity cap (15).
* **Edge handling of numbers and time is exact.** Signed zero is never drawn from
  `NonZero`/`Negative`. One-ulp ranges, subnormal ranges, `Half` ladders, `DateOnly`/`DateTime` at
  `Min`/`MaxValue`, `Between(d, d)`, and granularity grids with no point in range all behave, and
  the last is refused naming both sides (05, 07, 09, 12, 15, 16, 20).
* **The `--format json` report is clean and self-checking**, apart from [CLI-B5](#cli-b5) (03, 05, 09,
  12, 18). `--force` re-runs are byte-identical (15).
* **The analyzers fire accurately on constant mistakes**, with no false positives on scaffolded or
  real code. Rules that fired include JD004, JD006, JD013–JD018, JD023, JD025–JD027, JD034 and
  JD035. JD018 caught a nested `Any.Reproducibly` under `[Reproducible]` (08, 13, 17). JD014 said
  "the two arguments look transposed" (13).
* **Arbitrary values found real defects in the participants' own domains**, which is fundamental 1
  in action. In domain 17, `AddYears(10)` overflowed near `DateOnly.MaxValue`. In domain 16, the
  500-draw harness caught a wrong hand-written registration pattern.

---

## 11. Per-domain results

Fluency is the participant's own 1–5 score: a relative signal, not a measurement. Test counts are
filled in from a measured run.

| Domain | Model | Tests | Fluency | Comment | Notes |
|---|---|---|---|---|---|
| 01 payments | SEPA transfer: IBAN mod-97, BIC, Money, business-day date, pain.001 batch | 55 / 57 (2 `Bug_`) | 4 | Test side very fluent; check digits and cross-field rules took the hand work | [NOTES](domains/01-payments/NOTES.md) |
| 02 trading | Order book: ticker, limit/stop prices, tick size, session | 59 / 59 | 4 | Literals and lambda refinements say what they mean; tick grid and price links need hand edits | [NOTES](domains/02-trading/NOTES.md) |
| 03 insurance | Car policy: coverage period, deductible, bonus-malus, claims | 47 / 49 (2 `Bug_`) | 4 | Nested refinements pleasant; both cross-parameter rules hand-patched | [NOTES](domains/03-insurance/NOTES.md) |
| 04 manufacturing | MES work order: batch code, cadence, tolerances, machine states | 66 / 68 (2 `Bug_`) | 4 | Replay trivial, messages excellent; a silent recipe and the enum scaffold cost trust | [NOTES](domains/04-manufacturing/NOTES.md) |
| 05 CAD drawing | ISO 286 fits, drawing references, recursive bill of materials | 57 / 58 (1 `Bug_`) | 3 | Scalar and pattern DSL good; recursive scaffold crashed the host | [NOTES](domains/05-cad-drawing/NOTES.md) |
| 06 warehouse | Pallet receipt: SSCC, EAN-13, bin location, quantities | 64 / 67 (3 `Bug_`) | 4 | Core DSL reads like the domain; two silent CLI misses | [NOTES](domains/06-warehouse/NOTES.md) |
| 07 freight | Shipment: weight, volume, ISO 6346, ETA across zones | 62 / 63 (1 `Bug_`) | 4 | Smooth; 3 of 7 generators restructured; one silent guard | [NOTES](domains/07-freight/NOTES.md) |
| 08 patient record | Prescription: NIR, molecule, dosage, route, birth date | 55 / 55 | 4 | Messages and replay excellent; checksum and cross-field rules rewritten | [NOTES](domains/08-patient-record/NOTES.md) |
| 09 laboratory | Lab results, reference ranges, units, sampling times | 67 / 67 | 4 | Constraints fluent; cross-field invariants in a file `--force` overwrites | [NOTES](domains/09-laboratory/NOTES.md) |
| 10 pharma serialisation | Packs: GTIN, batch, expiry, serial uniqueness | 57 / 57 | 4 | Every guessed name existed; GTIN check digit forced signature edits | [NOTES](domains/10-pharma-serialisation/NOTES.md) |
| 11 telecom | Mobile line: MSISDN, IMEI (Luhn), ICCID, plan, usage | 49 / 49 | 4 | Arrange blocks show chosen versus arbitrary; resolving "to verify" rewrites more than documented | [NOTES](domains/11-telecom/NOTES.md) |
| 12 smart grid | Meter index, delivery point, half-hourly load curve | 37 / 39 (2 `Bug_`) | 3 | Nested refinements exact; ordered series and cross-field rules rewrote 4 of 8 files | [NOTES](domains/12-smart-grid/NOTES.md) |
| 13 payroll | Payslip: contract, hours, bonuses, deductions | 43 / 44 (1 `Bug_`) | 4 | Reads like the domain; value pin versus lambda caused one red run | [NOTES](domains/13-payroll/NOTES.md) |
| 14 e-commerce | Cart and order: SKU, quantity, discount, address | 47 / 48 (1 `Bug_`) | 4 | Natural; one silent miss; cross-element rules lost cases | [NOTES](domains/14-ecommerce/NOTES.md) |
| 15 hotel booking | Stay, room, guests, cancellation policy | 48 / 48 | 4 | Fluent; the stay interval needed hand-restructured generators | [NOTES](domains/15-hotel-booking/NOTES.md) |
| 16 aviation | Flight: IATA/ICAO codes, registration, slot | 66 / 67 (1 `Bug_`) | 4 | Temporal vocabulary fits slots; one silent recipe; temporal guards unread | [NOTES](domains/16-aviation/NOTES.md) |
| 17 real estate | Land registry, energy diagnosis, listings | 50 / 50 | 3 | Value objects read well; every aggregate had a cross-field rule | [NOTES](domains/17-real-estate/NOTES.md) |
| 18 education | Enrolment, grades on a half-point grid, semesters | 51 / 51 | 4 | Constraints and seeds fluent; half-point grade needs a 41-value pool | [NOTES](domains/18-education/NOTES.md) |
| 19 IAM | Access token, roles, scopes, expiry, redirect URI | 61 / 63 (2 `Bug_`) | 4 | `Any.Uri()` is excellent; a silent "guard" recap and the enum scaffold | [NOTES](domains/19-iam/NOTES.md) |
| 20 agritech IoT | Field sensors: GPS, humidity, `Half` battery, batches of 500k | 56 / 58 (2 `Bug_`) | 4 | Numeric vocabulary maps onto geodesy; a large ceiling makes the default dummy huge | [NOTES](domains/20-agritech-iot/NOTES.md) |
| **Total** | | **1097 / 1117** (20 red, all `Bug_`) | | | |

---

## 12. Rejected or downgraded findings

| Reported as | Verdict | Why |
|---|---|---|
| `WithScale(2)` above ~7.92e26 returns a lower scale, "high bug" (03) | Downgraded to a documentation defect ([CORE-D8](#core-d8)) | Every value is on the 10⁻ⁿ lattice ADR-0015 decides; `decimal.Scale` padding was rejected there. The trial test that asserted `decimal.Scale` has been rewritten as a lattice check (`WithScale_2_near_decimal_MaxValue_stays_on_the_lattice_without_trailing_zeros`), and passes. |
| `WithScale` ignored when the range holds a single value (18) | Same as above | `Between(20m, 20m).WithScale(1)` returns `20`, which is on the 0.1 lattice. The trial test was rewritten likewise (`WithScale_on_a_single_value_range_keeps_the_value_on_the_lattice`), and passes. |
| The scaffolder emits a chain its own analyzer flags, JD030 (04) | Intended | Spec §5.2/§17. Inventing a length would claim an invariant the domain never declared ([DOC-11](#doc-11)). |
| `Between(10, 1)` should throw a conflict, not `ArgumentException` (11) | Intended | Documented argument mistake, caught by JD014. |
| `PublicAPI.Shipped.txt` is empty (05, 17) | Not a user-facing defect | A pre-1.0 artifact of the harness: the surface lives in `Unshipped` until the first release. |
| JD028 silent on scaffolded or held generators (01, 06, 14, 15, 17, 18) | Intended | Documented: "only a chain that provably builds a new value here qualifies". The user owns the file and may return shared instances. |
| `Any.Combine` invisible to JD007/JD009/JD028 (07) | Refuted for inline use | Inline `Combine` fires on all three. A local-held generator is a documented limit. |
| JD032 does not cover `Between` (14) | Intended | JD032 page and ADR-0078: "still covered by JD024". |
| `Double().Between(0, 100).Between(50, 200)` "always fails" | Refuted | It folds to [50, 100]. |
| Constant-conflict silence inside `Assert.Throws` is undocumented (04) | Refuted | It is documented. The remaining inconsistency is [AN-D3](#an-d3). |
| The cardinality gate is not uniform between char and string (10) | Intended | ADR-0004 decides the bounded-draw fallback. Only the timing is a defect ([CORE-D10](#core-d10)). |
| `AnyPattern` "cannot be refined" (18) | Overstated | `OneOf`, `Except` and `DifferentFrom` exist; the regex is narrowed by editing it. |
| A cross-field guard reading two parameters flags only one (17) | Downgraded | This happens only through a derived local (the §9 residue). The build is still blocked by the flagged parameter. Covered by improvement (b) of [CLI-E1](#cli-e1). |
| Outside a scope, the reported seed "cannot replay the failing draw" (05) | By design, except the async case | A synchronous body replays from its first draw. The async variant is [CORE-B1](#core-b1). |
| "Count guards are not read" (18) | Overstated | `Count > N` → `WithMaxCount`, `Count == N` → `WithCount` and `Count == 0` are read (06, 12, 15). |
| A named constant is inlined as a literal (07) | Cosmetic | The folded value is correct. |
| `With…(Func<AnyX, AnyX>)` cannot take a prepared `IAny<T>` (20) | Intended (ADR-0109) | Documentation only: [DOC-10](#doc-10). |
| `WithScale` is once-only and cannot be narrowed; no grid on `AnyDouble` (02, 17) | Intended (ADR-0015) | A decimal step is [CORE-C4](#core-c4). |
| Crossed constant decimal/double/date bounds compile silently (many) | Out of documented scope | JD023 is integer-only by its page: [AN-C2](#an-c2). |
| `--force` discards hand-resolved bases (10) | Disclosed behaviour | Idea (c) of [CLI-E1](#cli-e1). |

---

## 13. Next steps

1. **Issues.** Opened with the maintainer's approval, one per root cause; the sections above link
   to them.
   * `cli`: [#297](https://github.com/Reefact/just-dummies/issues/297)–[#307](https://github.com/Reefact/just-dummies/issues/307), plus [#296](https://github.com/Reefact/just-dummies/issues/296) (CLI-B7, filed during the pilot).
   * `core`: [#308](https://github.com/Reefact/just-dummies/issues/308)–[#320](https://github.com/Reefact/just-dummies/issues/320).
   * `analyzers`: [#321](https://github.com/Reefact/just-dummies/issues/321)–[#327](https://github.com/Reefact/just-dummies/issues/327).
   * `xunit`: [#328](https://github.com/Reefact/just-dummies/issues/328).
   * Documentation: [#329](https://github.com/Reefact/just-dummies/issues/329) (generators and errors pages: DOC-1–4, DOC-12) and [#330](https://github.com/Reefact/just-dummies/issues/330) (CLI page:
     DOC-5, DOC-7–11); DOC-6 rides with [#308](https://github.com/Reefact/just-dummies/issues/308).
   * Not opened, because each waits on a decision: the candidates of §8, and DOC-13 (depends on
     AN-C3). They go to the maintainer as decision requests, not as bugs.
2. **Bugs, red then green.** For each bug, write the red test named in its row in the suite named
   there (ADR-0019 decides `JustDummies.UnitTests` versus `.PropertyTests`). Put the test and the fix
   in the same `fix(<scope>)` commit, with `Refs: #NN`. Spec or ADR text that a fix changes (spec
   §5.1/§5.3/§5.6/§6.1/§7/§9, ADR-0004) moves with it, together with its French twin.
3. **Confirm on the trial.** Re-pack (`tools/ux-trial/prepare.sh`) and re-run the affected domains.
   * The `Bug_` tests listed for CORE-B3, CORE-D1 and CORE-D2 should turn green as they stand.
   * For CLI-B1, CLI-B2 and CLI-B4, the `Bug_` tests replay frozen emitted recipes or files. Confirm
     instead on a `--force` re-scaffold: the recap must mark the parameter, or refuse the enum. Then
     retire or rewrite those tests.
   * The two `WithScale` tests (03, 18) that asserted a rejected contract have already been rewritten
     as lattice checks (§12).
4. **Decisions first where a fix depends on one:**
   * the enum target refuse-or-delegate choice ([CLI-B2](#cli-b2));
   * the comment text in spec §5.6 ([CLI-D1](#cli-d1));
   * the owner of enum membership ([AN-D1](#an-d1));
   * the ADR amending ADR-0004 ([CORE-D10](#core-d10));
   * `TimeOnly` across midnight ([CORE-C1](#core-c1)).
