# `dum` — first field measurement

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./2026-09-02-dum-first-field-measurement.fr.md)

**Date:** 2026-09-02
**Measured revision:** `f179fa0` (tip of `main`), against `1c71f74` (its parent) for the before column
**Scope:** the `dum` scaffolder run against seven repositories that are not this one.
**Status:** advisory, and a **snapshot** — the numbers describe one day and one container, not a rule.

**Why it exists.** The specification said, until this measurement, that *no project outside this
repository has exercised the tool yet*. Two benches held it — the named corpus and the generative
sweep — and both live inside one compilation. This is the first time `dum` was pointed at code
nobody wrote for it, and it found within the first pass a defect neither bench could reach
(§"What it found", 1). Recording the numbers is what lets a later run say whether anything moved.

---

## 1. Method

One probe project per repository, in the shape §3.1 describes: a test project referencing both the
library under test and JustDummies. Nothing in the target repositories was modified; the probes live
outside this one and are not committed.

```
dum generate <every public type> --project <probe>.csproj --dry-run --format json
```

The public-type inventory comes from a `grep` over top-level declarations in the library's sources,
not from reflection — see §5 for what that misses. Every repository was measured **twice**, with the
tool built from `1c71f74` and from `f179fa0`, so the before and after columns differ in one commit
and nothing else.

## 2. The corpus

| repository | revision | types | scaffolded | parameters | before `guard`/`unread` | after `guard`/`unread` | `no source` |
|---|---|---:|---:|---:|---:|---:|---:|
| `Reefact/first-class-errors` | `99dc5da` | 31 | 20 | 28 | 0 / 0 | **7 / 11** | 0 |
| `Reefact/luxafor-lighting-device-controller` | `160cf86` | 8 | 4 | 7 | 0 / 0 | 0 / 1 | 0 |
| `tpierrain/Diverse` | `73c98b6` | 11 | 8 | 22 | 0 / 0 | 0 / 0 | 0 |
| `tpierrain/NFluent` | `c6e2aac` | 23 | 8 | 8 | 0 / 0 | 0 / 1 | 0 |
| `nodatime/nodatime` | `67f7885` | 58 | 26 | 73 | 0 / 0 | **0 / 24** | 0 |
| `stryker-mutator/stryker-net` | `4fa9ee7` | 122 | 116 | 154 | 0 / 0 | 0 / 11 | 31 |
| `NuGet/NuGet.Client` (`NuGet.Versioning`) | `e6aaa9a` | 10 | 9 | 22 | 0 / 2 | 0 / 2 | 0 |
| **total** | | **263** | **191** | **314** | **0 / 2** | **7 / 50** | **31** |

**191 of 263 types scaffolded — 72.6 %.** The 72 refusals: 55 `NoEligibleConstructor`,
8 `TypeAmbiguous`, 8 `TypeIsGeneric`, 1 `TypeNotFound`. No refusal in the corpus was wrong about
*whether* to refuse; §"What it found", 3 is about one being wrong about *why* — and the change
carrying this page redistributes those 72 into 43 `NoEligibleConstructor`, 15 `TypeIsGeneric`,
8 `TypeAmbiguous`, 5 `TypeIsAbstract`, 1 `TypeNotFound`, moving no type in or out of refusal.

## 3. What the fix of `f179fa0` was worth

**Fifty-five of 314 parameters — 18 % — went from silence to a constraint or a mark.** Before that
commit, a type reached through a project reference lost its guards without a word whenever the two
projects did not bind the same references, which is what a `netstandard2.0` library under a
`net8.0` test project is.

The two ends of the range say what the commit is:

* **`first-class-errors`**: 18 of 28 parameters gained something — 7 real constraints
  (`Any.String().NotBlank()` where the base row had been drawn), 11 honest `unread guards`.
* **`nodatime`**: 24 of 73 parameters gained an `unread guards`. Before, `dum` would have handed a
  NodaTime user 26 generators whose recap read *all inferred*, over invariants it had never read —
  and `LocalTime`, `LocalDate` and `AnnualDate` reject most of what they would have drawn.

`Diverse` gained nothing, and that is the control the table needs: its value types declare no
constructor guard at all, so there was nothing to recover. Its throws are all in methods.

## 4. What it found

### 1 — Guards silently dropped across a project reference

Reported and fixed in `f179fa0`; the row above is its measurement. It is recorded here because of
**how** it was found: every shape the generative sweep draws lives in one compilation, so no number
of them could reach it. A bench that builds its own inputs cannot test the assumption it builds them
under.

### 2 — A field report on §5.3, in ADR-0085's own sense

NodaTime's dominant constructor-validation idioms are both outside what the guard reader covers, and
both are correctly marked rather than misread:

```csharp
// LocalTime — one || chain spanning four parameters. §9 names a cross-parameter rule as out of reach.
if (hour < 0 || hour > HoursPerDay - 1 ||
    minute < 0 || minute > MinutesPerHour - 1 || ...)
{
    Preconditions.CheckArgumentRange(nameof(hour), hour, 0, HoursPerDay - 1);
    ...
}

// AnnualDate — validation delegated to a helper internal to the project, not to a named guard
// library (ADR-0086).
GregorianYearMonthDayCalculator.ValidateGregorianYearMonthDay(2000, month, day);
```

This is a report from outside the loop, which is exactly what
[ADR-0085](../adr/0085-change-the-guard-reader-only-against-a-field-report.md) asks for before §5.3
moves. **Its own remedy, taken in order, is the first one: no change.** The `unread guards` mark
already answers both, the developer meets a line they delete once, and nothing is drawn over an
invariant nobody honoured. Recorded so the next proposal to widen the table starts from a real
constructor rather than from an argument.

### 3 — `NoEligibleConstructor` shadowing a more precise refusal

`Scaffolder.Scaffold` decides `NoEligibleConstructor` **before** it asks whether the type is generic
or abstract. An abstract type whose constructors are `protected` — the ordinary shape of an abstract
type — therefore hears *"`Generate()` needs a public instance constructor"*, when the answer it
should get is *"scaffold a concrete type that derives from it"*. `ScaffoldStatus.TypeIsAbstract`
exists to say that and is, in practice, nearly unreachable: it is only asked once a public
constructor has already been found.

**Twelve of the corpus's 55 `NoEligibleConstructor` refusals — 5 abstract, 7 generic — hide a more
actionable reason this way.** Named by the tool once the order is corrected, which is a firmer count
than the `grep` that first suggested it: `DiagnosableException` and `NuGet.Versioning`'s
`VersionRangeBase` are abstract; `PublicMessageStage<TError>` and Stryker's `MutatorBase<T>` are
generic, and the second is one the `grep` had put in the wrong column.

The refusal's sentence has a second gap, independent of the order: it names only the constructor
route, never the recognised static factory of §5.1.2. The author of a validating value object — a
private constructor behind a public `Create`, the audience that rule was written for — is advised to
do the opposite of what their design intends.

Neither is a contract change: §7 gives every one of these refusals exit code `1`, so nothing a
script reads moves.

### 4 — A specification cross-reference that no longer resolves

§5.1.2 sends the reader to "a recognised static factory (§5.4)", and §5.4 stopped defining that at
[ADR-0089](../adr/0089-draw-a-composed-parameter-through-the-generator-its-type-owns.md) — it now describes
only how a scaffolded generator wins. The rule lives in the code alone, and two of the code's own
remarks carry the same dead pointer back to §5.4.

## 5. What this measurement does not establish

* **Three repositories are partly degraded.** `luxafor` (`net462`), `NFluent` (`net35`, `net462`) and
  `NuGet.Versioning` (`net472`) target .NET Framework versions whose reference assemblies are not
  installed in this Linux container; MSBuild reported the failure and `dum` printed it. **The
  `NuGet.Versioning` row is counted in neither direction**: its before and after numbers are
  identical, in a state that is not representative, and no conclusion is drawn from that.
* **The type inventory is a `grep`, not reflection.** A public type declared other than at the head
  of a line is missed, so 263 is a floor rather than a census.
* **Nothing here is a mutation score.** Nothing in this repository enforces one (ADR-0025), and this
  page makes no claim about one.
* **A scaffolded type is not a verified one.** "Scaffolded" means a file was produced; whether its
  generator draws values the domain accepts is what the named corpus and the seeded draw oracle
  answer, and they were not run against these repositories.

## 6. What to do with it

The two findings this page can act on — 3 and 4 — are fixed in the change that carries this page.
Finding 2 is deliberately left alone, under ADR-0085's own first remedy. Finding 1 is closed.

A later measurement should reuse §1 verbatim against the same seven revisions before adding new
ones: a number that moves is only informative when the corpus did not.
