# Developer-UX trial — final report

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

The guard reader stops at the first assignment to a field or property. Only that one statement is
checked for a throw. Every statement after it is never visited, so a guard written there gets no
`unread guards` mark. ADR-0083 and spec §5.6 require exactly that mark ("a statement that throws is
a guard whatever its shape"). Spec §9 lists the shapes known to escape, and a guard below a state
write is not among them.

```csharp
public FreightRate(decimal rate, string currency) {
    Rate = rate;
    if (currency.Length != 3) { throw new ArgumentException("ISO 4217", nameof(currency)); }
    Currency = currency;
}
// any: currency  string  Any.String().NonEmpty()        — "2 of 2 parameters inferred", compiles
// ThrowIfNegativeOrZero(rack); Aisle = aisle; ThrowIfGreaterThan(rack, 99);
// any: rack  int  Any.Int32().Positive()  guard         — the <= 99 bound is lost under "guard"
```

The verifier drew 200 values per scaffold. Every variant was rejected 200 of 200 times. A throw
expression or switch expression is correctly marked when it *is* the first state write. It is
missed whenever an earlier assignment precedes it, which is what domains 06 and 14 hit.

| | |
|---|---|
| Domains | 06 (`BinLocation.level`, switch expression after two writes), 07 (`FreightRate.currency`), 14 (`PromoCode.maxUses`, throw expression after five writes), 19 (`RoleSet`, check on the field copy) |
| Repro | [`repros/cli-silent-guard-miss`](repros/cli-silent-guard-miss) (variants a1, a2, d) |
| Source | `JustDummies.GenAny/Guards.cs:162-169` (the `break` at the first state assignment), `JustDummies.GenAny.UnitTests/GuardBoundaryTests.cs:216-236` (pins the expression but never checks provenance, and its rationale is wrong) |
| Red test | `JustDummies.GenAny.UnitTests`, next to `AThrowingCheckAfterTheFirstAssignmentIsNotAGuard`. Scaffold `kept = value; if (value <= 0) { throw …; }` and assert `Provenance.HasFlag(Provenance.UnreadGuards)`. Add a second case, `ThrowIfNegativeOrZero(v); Kept = v; ThrowIfGreaterThan(v, 99);`, and assert `UnreadGuards` rather than a bare `Guard`. |
| Fix | At the first non-guard state write, stop *reading* guards but keep *walking* the body. Mark every parameter that a later throwing statement (or discarded-result call) mentions as `UnreadGuards`, so that the existing ADR-0083 blocking line fires. This refuses louder and infers nothing new, which stays inside ADR-0046. |
| Trial `Bug_` tests | `06 Bug_Scaffolded_level_recipe_for_BinLocation_ignores_its_guard`, `07 Bug_Scaffolded_currency_recipe_ignores_a_guard_written_below_a_property_assignment`, `14 Bug_Scaffolded_AnyPromoCode_misses_the_throw_expression_guard_on_maxUses`, `19 Bug_Scaffolded_AnyRoleSet_ignores_a_distinctness_guard_written_against_a_field`. **These do not turn green on their own.** 06 and 07 replay the frozen emitted recipe. 14 and 19 use the file as emitted. After the fix, confirm by re-scaffolding: the recap must show `to verify, unread guards` and the build must stop at the TODO line. Then retire or rewrite these tests. |

<a id="cli-b2"></a>
#### CLI-B2 — `any generate <Enum>` reports ✓ and emits `return new TEnum();` (high)

Roslyn synthesizes a parameterless constructor for an enum. The exclusion of synthesized
constructors only checks `TypeKind.Struct`, so an enum is "scaffolded" as `new TEnum()`. That always
returns `(TEnum)0`. The value is undeclared when the enum has no zero member, and constant when it
has one. The recap reads `✓ AnyCurrency.cs — no constructor parameters to infer.`, and the run exits
with code 0. Spec §5.1.3 promises the parameterless path yields a *valid* value, and no spec row or
ADR treats an enum as a target.

```csharp
public enum Currency { Eur = 1, Usd = 2, Gbp = 3 }
// AnyCurrency.Generate() { return new Currency(); }  → 200 draws, 1 distinct value, Enum.IsDefined == false
```

| | |
|---|---|
| Domains | 01, 03, 04, 05, 06, 19 |
| Repro | [`repros/cli-enum-scaffold`](repros/cli-enum-scaffold) (its own `Bug_scaffolded_enum_generator_draws_an_undeclared_constant`) |
| Source | `JustDummies.GenAny/Scaffolder.cs:283` (`ChosenConstructor`) and `:251-252` (`EligibleFactories`): the implicit-constructor exclusion is limited to `TypeKind.Struct` |
| Red test | `JustDummies.GenAny.UnitTests/ConstructorChoiceTests.cs`: add `public enum Subject { One = 1, Two = 2 }` to `ATypeTheEmittedFileCouldNotConstructIsRefused`, asserting a refusal status and `outcome.File == null`. Today the status is `Scaffolded`. |
| Fix | Refuse `TypeKind.Enum` before constructor choice, with a message that names `Any.Enum<TEnum>()` (exit 1, nothing written). Alternatively, emit a generator that delegates to `Any.Enum<TEnum>()`. Choosing between the two is a small spec decision for §5.1/§7, plus the French twin. At a minimum, extend the struct exclusion to enums. |
| Trial `Bug_` tests | `01 Bug_Scaffolded_AnyTransferStatus_never_varies`, `01 Bug_Scaffolded_AnyChargeBearer_draws_an_undeclared_enum_value`, `03 Bug_Scaffolded_AnyResponsibility_draws_more_than_the_zero_member`, `04 Bug_Scaffolded_AnyShiftKind_draws_an_undeclared_member`, `05 Bug_Scaffolded_AnyProjectionMethod_draws_an_undeclared_enum_value`, `06 Bug_Scaffolded_AnyPalletType_draws_an_undeclared_member`, `06 Bug_Scaffolded_AnyReceiptStatus_never_varies`, `19 Bug_Scaffolded_AnyRole_for_an_enum_only_ever_draws_the_default_member`. They turn green after `--force` re-scaffolding only if the delegation option is chosen. If the refusal option is chosen, confirm on the refusal and rewrite these tests against `Any.Enum<T>()`. |

<a id="cli-b3"></a>
#### CLI-B3 — A self-referencing type is scaffolded into unbounded recursion (high)

```csharp
public Node(string name, IReadOnlyList<Node> children) { … }
// any:  children  IReadOnlyList<Node>  Any.ListOf(new AnyNode())     ✓ AnyNode.cs — 2 of 2 parameters inferred
// public AnyNode() : this(name: AnyValidName(), children: AnyValidChildren()) { }
// dotnet test → "Stack overflow." exit 134, zero tests ran
```

The recursion is eager: the public constructor itself recurses, so even
`new AnyNode().WithChildren(...)` cannot escape it. A direct `Employee(string name, Employee? manager)`
gives the same result, and the recap also drops the `?`. The cycle check in `GeneratorFor` never
contains the target type. The remarks at `GeneratorFor.cs:38-40` say that this guard "belongs with
§5.4", but it was never implemented. Neither the spec nor any ADR mentions self-reference.

| | |
|---|---|
| Domains | 05 (recursive bill of materials) |
| Repro | [`repros/cli-recursive-aggregate`](repros/cli-recursive-aggregate) |
| Source | `JustDummies.GenAny/GeneratorFor.cs:270` (root `Draw` with an empty `underway`), `:366` (cycle check), `:431-450` (`Composed` writes `new AnyT()` unconditionally) |
| Red test | `JustDummies.GenAny.UnitTests/CompositionTests.cs`: scaffold `Node(string name, IReadOnlyList<Node> children)` and assert that `children` is not resolved to an expression containing `new AnyNode()`, and that it is reported as unresolved or to verify. Add a sibling case for `Employee(string, Employee?)`. |
| Fix | Seed the cycle check with the target, as `Draw(type, MaximumDepth, [target])`. A parameter that reaches the target directly, through a nullable, or through a collection becomes an explicit TODO with the recap reason "self-referencing: recursion needs a depth bound you write". Emitting an empty list, a null or a depth parameter instead is a cleverer generator, and needs an ADR or a §5.4 amendment rather than a patch. |
| Trial `Bug_` tests | None: domain 05 hand-wrote `WithDepth`. Confirm on the repro: re-scaffold `Node` and check the TODO. |

<a id="cli-b4"></a>
#### CLI-B4 — A guard reached through a derived local is silent while the recap says "inferred" (high)

```csharp
public AircraftRegistration(string mark) {
    bool valid = mark switch { null => false, { Length: > 7 } => false, _ => Hyphenated.IsMatch(mark) };
    if (!valid) { throw new ArgumentException(…, nameof(mark)); }
    Mark = mark!;
}
// any: "1 of 1 parameters inferred", no "to verify"; the file compiles; most draws are rejected
```

`MarkIfItRejects` marks only the parameters that the throwing statement names syntactically, and
`if (!valid)` names none. The verifier reproduced four shapes, with 197 to 200 of 200 draws rejected
(one shape throws `FormatException` instead): `Match m = Regex.Match(value, …)`,
`bool ok = value.StartsWith(…)`, `int parsed = int.Parse(week)` and a `switch` into `bool`.

**Classification note.** The verifier classified this as the documented §9 residue ("a local copy of
the parameter"), which `GuardBoundaryTests.cs:167` pins. Under the scaffolder's honesty line, a recap that
says "inferred" while the recipe is wrong is a bug, whatever a page states. The spec's §9 listing is
therefore the defect. Moreover, the fix *marks* the guard and does not *read* it, which stays inside
ADR-0046.

| | |
|---|---|
| Domains | 04 (`BatchCode`, a `Match` on a regex), 16 (`AircraftRegistration`) |
| Repro | [`repros/cli-silent-guard-miss`](repros/cli-silent-guard-miss) (variants b1–b4) |
| Source | `JustDummies.GenAny/Guards.cs:512-515` (`MarkIfItRejects` only sees parameters mentioned syntactically); spec `justdummies-tool.md` §9 (lists the residue); `JustDummies.GenAny.UnitTests/GuardBoundaryTests.cs:167` |
| Red test | `JustDummies.GenAny.UnitTests/GuardBoundaryTests.cs`: scaffold `bool ok = value.StartsWith("LOT-"); if (!ok) { throw …; }` with no state write before it, and assert `UnreadGuards` on `value`. |
| Fix | When a throwing statement's condition reaches a parameter through a local initialised from that parameter (a one-step data flow), mark that parameter `UnreadGuards`. Remove the residue from spec §9 (EN/FR). The user CLI page should name whatever residue remains. |
| Trial `Bug_` tests | `04 Bug_Scaffolded_AnyBatchCode_recipe_misses_the_format_guard_silently` (replays the frozen recipe, so it stays red by construction), `16 Bug_Scaffolded_AnyAircraftRegistration_as_emitted_draws_marks_the_constructor_rejects` (uses `AsScaffolded/`, which is also frozen). Confirm by re-scaffolding, then retire both. |

<a id="cli-b5"></a>
#### CLI-B5 — `--format json` reports a refused write as `Scaffolded` (medium)

When the output file already exists and `--force` is not given, stderr says
`✗ … already exists. Re-run with --force`, and the run exits 1. Stdout, however, carries
`{"status":"Scaffolded","files":[],…,"summary":{"scaffolded":1,"failed":0}}`. The shell copies the
*engine's* status into the report. ADR-0071 and spec §6.1 require the summary to be "checkable
against the rows rather than believed", with exit codes that do not move.

| | |
|---|---|
| Domains | 01, 19 |
| Repro | [`repros/cli-json-status`](repros/cli-json-status) |
| Source | `JustDummies.Cli/GenerateCommand.cs:216-221`, `JustDummies.Cli/ScaffoldReport.cs:25-27`, `JustDummies.Cli/RunReport.cs:44-45` |
| Red test | `JustDummies.Cli.UnitTests/GenerateCommandTests.cs`, `JsonRecordsAnExistingFileAsAFailure`: pre-write `AnyOrder.cs`, run with `Format = "json"`, and assert exit code 1, `status != "Scaffolded"`, `summary.scaffolded == 0`, `summary.failed == 1`, and that the file is untouched. |
| Fix | Give the write-refused branch a shell-level status (e.g. `"FileExists"`). Keep the path in `files` with `written: false`. Document the status in spec §6.1/§7 (EN/FR). |
| Trial `Bug_` tests | None. Confirm with the repro's second run. |

<a id="cli-b6"></a>
#### CLI-B6 — The short-name lookup lists candidates C# could never bind, and the header names a command that fails (medium)

`any generate Measurement` refuses as ambiguous because of `System.Diagnostics.Metrics.Measurement<T>`,
which has arity 1. `any generate Scope` lists `JustDummies.Xunit.ReproducibleAttribute.Scope`, an
`internal` type, and a `ref struct`. After a successful `any generate Ambig.Domain.Meter`, line 2 of
the generated file says ``// `any generate Meter --force` overwrites it.``, but that command fails
with the same ambiguity refusal. Refusing a *real* ambiguity is specified (§3.2) and the refusal
message is clear. Neither the over-wide pool nor the false header is specified.

| | |
|---|---|
| Domains | 03 (`Claim`), 09 (`Measurement`), 12 (header), 17 (header, transitive packages), 19 (inaccessible types) |
| Repro | [`repros/cli-ambiguous-names`](repros/cli-ambiguous-names) |
| Source | `JustDummies.GenAny/TypeLookup.cs` (`Matching`/`Named`), `Scaffolder.cs:53-62`, `GeneratorEmitter.cs:64`, `EntryPointEmitter.cs:83` |
| Red test | `JustDummies.GenAny.UnitTests/TypeLookupTests.cs`. (1) Source `Shop.Measurement`, plus metadata with an internal `Other.Measurement` and a public `Other.Measurement<T>`: `"Measurement"` resolves to `Shop.Measurement`. (2) For an ambiguous short name given in full, line 2 of the header names a command that `TypeLookup.Find` resolves to exactly that type. |
| Fix | Filter candidates by construction: arity 0 when no type-argument list is given, and accessible from `compilation.Assembly`. Write a spelling that resolves into the header (the full name when the short name is ambiguous), and update the golden files. Whether project types should be preferred over framework ones is [CLI-C3](#cli-c3). |
| Trial `Bug_` tests | None. |

<a id="cli-b7"></a>
#### CLI-B7 — `any generate` on an unrestored project claims the project does not reference JustDummies (already filed)

This was found during the pilot and is filed as
[#296](https://github.com/Reefact/just-dummies/issues/296). It is listed here so the `cli` train
carries it. Every domain ran `dotnet restore` first because of it.

### Defects (messages, wording, recap/JSON)

<a id="cli-d1"></a>
**CLI-D1 — "to verify" never says which guard went unread, and cites the maintainer spec's "§9" (medium).**
The emitted comment reads `// TODO(any): 'string value' may be guarded by something the scaffolder
could not read (§9).` When a parameter has three guards, nothing says which one was not read. The
recap and the JSON provenance say no more, and "§9" points to "Non-goals for v1.0" in a maintainer
page that no user page links to. Domains: 02, 09, 11, 12. Repro:
[`repros/cli-todo-wording`](repros/cli-todo-wording). Source: `GeneratorEmitter.cs:263-267`
(`WriteVerify`), `GuardReading.cs:54` (stores names only, never the node), `Cli/Recap.cs:132,179`.
Red test (`JustDummies.GenAny.UnitTests`): a parameter with a read `Length > 7` guard and an unread
`Regex.IsMatch` guard. Assert that the TODO contains the unread guard's text or line and does not
contain "§9". Add a `Recap` twin in `JustDummies.Cli.UnitTests`. Fix: keep the first unread guard's
location and a one-line excerpt per parameter, print it in the TODO, the recap and the JSON, and
replace "§9" with a link to the user CLI page. The comment text is specified in spec §5.6, so the
spec changes with it. This is also improvement idea (b) of [CLI-E1](#cli-e1).

<a id="cli-d2"></a>
**CLI-D2 — "Delete that one line, and you are done" is false when the right base has another builder type (medium).**
Replacing `Any.String()…` with `Any.StringMatching(...)` (which returns `AnyPattern`), or with
`.As(...)` (which returns `IAny<T>`), fails with `CS0029`. The `AnyValidX()` return type and the
`WithX(Func<TBuilder, TBuilder>)` overload must be retyped by hand. This was the single most
widespread friction: 16 domains (01, 02, 03, 04, 05, 06, 07, 10, 11, 13, 14, 16, 17, 18, 19, 20).
The scaffolder is right to leave the developer a base to edit (see the principles at the top). The defect is the page
and spec §5.6 promising a one-line resolution. Source: `doc/handwritten/for-users/packages/justdummies-cli.en.md:82-91`
and spec §5.6. Fix: say what else to retype when the replacement has another type, with a compiled
sample in `JustDummies.Documentation.UnitTests`. Alternatively, emit the factory as `IAny<T>` where
no refine overload depends on it, which is a spec decision.

<a id="cli-d3"></a>
**CLI-D3 — A nullable reference parameter loses its `?` in the value pin, the recap and the JSON (medium).**
For `Party(Bic? agent, string? note, int? priority)`, the scaffold emits `WithAgent(Bic value)`. The
documented "always null" route, `.WithAgent((Bic?)null)`, then warns `CS8625`, which is a build break
under `TreatWarningsAsErrors`. The recap prints `constructor Party(string, Bic, string, int?)`, and
the JSON reports `"type": "Bic"`. `int?` is kept correctly. ADR-0064 rules out drawing null *by
default*. It does not rule out letting the pin accept null, which is exactly where the ADR says a
test opts in. Domains: 01, 02, 05, 15 (and 05's `Employee?`, see CLI-B3). Repro:
[`repros/cli-nullable-parameter`](repros/cli-nullable-parameter). Source: `TypeNames.cs:66-74`
(drops `NullableAnnotation`), `GeneratorEmitter.cs:307-309`. Red test (`JustDummies.GenAny.UnitTests`,
next to `BaseTableTests`): a `Customer? agent` parameter emits `WithAgent(Customer? value)`, and a
caller passing `(Customer?)null` compiles with no `CS8625`. Fix: type the pin, the field and the
private constructor parameter `T?`. Covariance keeps the never-null base assignable. Print `T?` in
the recap and the JSON, and document the reference-type cast next to `(decimal?)null`.

<a id="cli-d4"></a>
**CLI-D4 — `--namespace` rewrites the analysed type's name in the recap and the JSON (low).**
With `--namespace Foo.Bar`, the recap shows `Analyzing Foo.Bar.Sku` and the JSON shows
`"type": "Foo.Bar.Sku"`. That type does not exist. The emitted file is correct. Domains: 14, 16.
Repro: [`repros/cli-namespace-recap`](repros/cli-namespace-recap). Source: `Scaffolder.cs:127,140`
(fills `TargetType.Namespace`, documented as "the namespace the type is declared in", with the
override), `Cli/Recap.cs:60,212`, `Cli/ScaffoldReport.cs:45,58`. Red test
(`JustDummies.Cli.UnitTests/RecapTests.cs` plus a JSON twin): with an override, the first line still
reads `Analyzing Shop.Domain.Order`. Fix: keep the declared namespace on `TargetType`, and carry the
file namespace separately.

<a id="cli-d5"></a>
**CLI-D5 — `p >= N` / `p <= N` with N ≠ 0 are unread, while `ThrowIfGreaterThanOrEqual(p, N)` is read (low).**
`if (longitude >= 180.0) throw` stays `to verify, unread guards`. The helper form yields
`.LessThan(180d)`. Spec line 928 says the helpers "map to the same numeric rows a comparison would",
but the comparison row is missing. This is the one gap in the table below that is an inconsistency
rather than a boundary. It fails loudly, so it is low severity. Domain: 20. Repro:
[`repros/20-3`](repros/20-3). Source: `Guards.cs:1172-1184` (`Numeric`: the `>=`/`<=` rows only
`when value == 0`) against `Guards.cs:882-892`. Red test (`JustDummies.GenAny.UnitTests`): a guard
`value >= 180.0` scaffolds to `.LessThan(180d)` with status `guard`. Fix: add the two rows to
`Numeric` and to the spec §5.3 table (EN/FR), plus a `GuardCorpus` row and a golden file. Keep the
zero rows as the more specific match.

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

```csharp
static async Task<int> DrawAsync() { await Task.Yield(); return Any.Int32().Generate(); }
int a = await DrawAsync();          // first draw: lazy seed S1, set in the helper's flow, then lost
int b = Any.Int32().Generate();     // the caller picks a second lazy seed, S2
// a later failure says "reproduce this run with Any.Reproducibly(S2, ...)";
// replaying under ReproduciblyAsync(S2, …) gives every value shifted by one
```

The lazy ambient seed is assigned to an `AsyncLocal` in whichever execution context draws first. A
value assigned inside an awaited method does not flow back to the caller. The exception's XML doc
promises "the seed that replays the run", and CLAUDE.md says "any sequential run replays from the
seed it reports". An awaited helper is a sequential run.

Most of what was reported here is by design (verifier variants V1, V2 and V4):

- the same seed is reported for every failure in a test;
- replay restarts at the test's first draw;
- each sequential test gets its own seed.

The errors page also under-describes the behaviour. It mentions `Seed` only "inside a reproducible
scope", but `Seed` is also set outside one (domain 08).

| | |
|---|---|
| Domains | 05 (reported the by-design part), 08 (the documentation part); the async variant was found by the verifier |
| Repro | [`repros/core-seed-outside-scope`](repros/core-seed-outside-scope) (V3 and V3b fail) |
| Source | `JustDummies/AmbientRandomSource.cs:38-46` (lazy `AsyncLocal` seed), `:51`, `:65` (replay guidance); `doc/handwritten/for-users/guides/errors-and-conflicts.en.md:101-108` |
| Red test | `JustDummies.UnitTests`, next to `AmbientSeedScopeTests.cs`. Outside any scope, draw `a` in an awaited helper and then `b`, trigger an `AnyGenerationException`, replay under `Any.ReproduciblyAsync(ex.Seed!.Value, …)`, and assert `(ra, rb) == (a, b)`. |
| Fix | Either keep the lazy seed in a holder that the outer flow already owns, or stop promising replay outside a scope ("this flow drew from seed S; wrap the body in `Reproducibly`/`ReproduciblyAsync` to make it replayable"). Either way, correct the errors page (EN/FR). |
| Trial `Bug_` tests | None. |

<a id="core-b2"></a>
#### CORE-B2 — `As(...)`/`Combine(...)` failure messages print drawn strings raw, contrary to ADR-0075 (medium)

`AnyDerivation.Display` wraps a string in quotes without escaping it, so NUL, CR, FF and ESC reach
`AnyGenerationException.Message`. As a result, consoles drop characters, `grep` treats the
`dotnet test` output as a binary file, and an ESC can start an ANSI sequence. ADR-0075 requires the
library to "escape control characters wherever it renders a drawn value". The library already has
the escaper, `CharacterPools.Escape`, and `AnyChar` uses it. The default `Any.String()` covers the
whole of ASCII, so this message is commonly hit.

| | |
|---|---|
| Domains | 04 |
| Repro | [`repros/core-raw-control-chars`](repros/core-raw-control-chars) (2 of 2 fail) |
| Source | `JustDummies/AnyDerivation.cs:120-133` (`Display`); callers `AnyExtensions.cs:46`, `Any.Combine.cs:46-302` |
| Red test | `JustDummies.UnitTests`: `Any.String().WithChars("\0\r\u001b").WithLength(3).As<string, object>(v => throw new ArgumentException()).Generate()` throws a message that contains `\0`, `\r` and `\x1b` as escapes and no character below 0x20. Add a twin for `Combine`. |
| Fix | Render strings and chars through `CharacterPools.Escape`, and escape the `ToString()` fallback too. |
| Trial `Bug_` tests | None. |

<a id="core-b3"></a>
#### CORE-B3 — `Any.Double().Between(-double.Epsilon, double.Epsilon)` only ever draws 0 (low)

The interval holds three doubles, but in `CandidateIn` the half-span `upper/2 - lower/2` underflows
to 0, so every draw is the midpoint. The drawn value is valid. What breaks is that the feasible
request `ArrayOf(...).WithCount(2).Distinct()` is refused. ADR-0097 states "two values the type
represents must be two values a draw can land on". This is a vanishingly small corner, but it is a
correctness refusal of a feasible request, not a bound on ambition.

| | |
|---|---|
| Domains | 20 |
| Repro | [`repros/20-1`](repros/20-1) |
| Source | `JustDummies/ContinuousIntervalSpec.cs:438-446` (`CandidateIn`) |
| Red test | `JustDummies.UnitTests`: a seeded loop over `Any.Double().Between(-double.Epsilon, double.Epsilon)` reaches each of {-ε, 0, ε}, or `ArrayOf(...).WithCount(3).Distinct()` succeeds. A `JustDummies.PropertyTests` invariant over a few-ulp spans is a natural follow-up. |
| Fix | When `half == 0` but `lower < upper`, pick by index among the enumerable representable values. Consume exactly one unit sample, as ADR-0049 requires. |
| Trial `Bug_` tests | `20 Bug_Double_between_minus_and_plus_epsilon_only_ever_draws_zero`, `20 Bug_Two_distinct_doubles_from_minus_to_plus_epsilon_are_refused`. Both should turn green after re-packing. |

### Defects (messages, wording, documentation of a contract)

<a id="core-d1"></a>
**CORE-D1 — Decimal conflict messages misstate strict bounds (medium).**
`Any.Decimal().GreaterThan(100m).LessThan(10m)` gives "…GreaterThan(100) already requires values
greater than **or equal to 100**". `GreaterThan(36m).LessThan(36m)` gives "…forbids 36, **the only
value** GreaterThan(36) and LessThan(36) leave". `Positive().Negative()` says the same of 0. These
are false statements, where `Int32` and `Double` are worded correctly. The cause: a strict decimal
bound is stored as inclusive plus a point exclusion. Domains: 01, 02, 12, 13. Repro:
[`repros/core-decimal-conflict-messages`](repros/core-decimal-conflict-messages). Source:
`JustDummies/DecimalIntervalSpec.cs:132-157`, `:740-750` (`PinningClause`);
`ConflictingAnyConstraintException.cs:182-191`. Red test (`JustDummies.UnitTests`, decimal conflict
tests): a theory over the three chains asserting the exact message, for example "…GreaterThan(100)
already requires values greater than 100." Fix: remember strictness per bound, add "greater/less
than {bound}" factories, and treat two strict bounds at one point as an empty interval before the
exclusion check. Trial `Bug_` tests: `12 Bug_Decimal_crossed_bounds_message_misstates_what_GreaterThan_requires`,
`12 Bug_Decimal_empty_open_interval_message_claims_a_value_is_left`,
`13 Bug_Decimal_sign_conflict_message_claims_zero_is_left`.

<a id="core-d2"></a>
**CORE-D2 — A decimal strict bound that empties a `WithScale` lattice names only one side (medium).**
`Any.Decimal().Between(0m, 5000m).WithScale(2).Negative()` gives "Cannot apply Negative() because it
forbids every WithScale(2) value between 0 and 0." `Between` is never named. `Int32` gives the clean
two-sided form. A second variant names the applying constraint on both sides of "because" (domain 10).
Domains: 03, 10. Repro: [`repros/core-scaled-range-message`](repros/core-scaled-range-message).
Source: `DecimalIntervalSpec.cs:148-159`, `:669-672`, `:734`. Red test
(`JustDummies.UnitTests/ConflictMessageProvenanceTests.cs`): the message contains "Between(0, 5000)".
Fix: refuse the bound before splitting it into a bound and an exclusion, as the ordinal spec does,
and give the scaled exhaustion branch the pinning clause. Trial `Bug_` test:
`03 Bug_Negative_on_a_scaled_deductible_range_names_neither_Between_nor_the_real_conflict`.

<a id="core-d3"></a>
**CORE-D3 — Distinct exhaustion over a foreign `IAny<T>` states as fact that its values "are not reproducible" (medium).**
Over any `any`-scaffolded generator, the message says that some values "come from a generator that
does not draw from this source, so they are not reproducible from this seed alone". The same run's
`Reproducibly` reporter prints a full replay line, and the scaffolded generator does draw ambient,
replaying exactly. ADR-0102 decides the conservative *hedge*, because the library cannot see inside
a foreign generator. Nothing decides stating the unknowable as fact. Domains: 06, 18. Repro:
[`repros/core-distinct-not-reproducible-msg`](repros/core-distinct-not-reproducible-msg). Source:
`AmbientRandomSource.cs:54-56`, `FixedRandomSource.cs:22-24`, selected through
`CollectionState.cs:378-388` and `AnyDerivation.cs:27-33`. Red test (`JustDummies.UnitTests`): over a
hand-written `IAny<int>` that draws through `Any`, the message does not contain "does not draw from
this source" and does state the conditional replay. Fix: reword to "its values replay from this seed
only if it draws through `Any` (as a generator scaffolded by `any` does)". A public way for a
generator to declare that it draws ambient is [CORE-C10](#core-c10).

<a id="core-d4"></a>
**CORE-D4 — Floating-point conflict messages restate a strict bound as the next representable value (low).**
`Any.Double().GreaterThan(10).LessThan(5)` gives "…greater than or equal to 10.000000000000002", and
`Positive()` gives "≥ 5E-324". The message is literally true and guarded for truthfulness by
`ConflictMessageTruthfulnessProperties`, but it is worded in engine terms the user never wrote.
Domains: 05, 07, 17. Repro: [`repros/core-double-conflict-wording`](repros/core-double-conflict-wording).
Source: `ContinuousIntervalSpec.cs:160-198`. Red test
(`JustDummies.UnitTests/ConflictMessageProvenanceTests`): the message contains "GreaterThan(10)
already requires values greater than 10" and not "10.000000000000002". Fix: keep the declared value
and a strict flag per bound, and share the strict factories with CORE-D1.

<a id="core-d5"></a>
**CORE-D5 — An offset refused near `DateTimeOffset.MaxValue`/`MinValue` does not state the limit it implies (low).**
`After(MaxValue.AddHours(-1)).WithOffset(14h)` is correctly refused and names both constraints. It
never gives the derived instant limit (UTC ≤ MaxValue − 14 h) or the reason (the local time would
leave 0001..9999). The wording also differs with declaration order. Domain: 19. Repro:
[`repros/core-dto-maxvalue-offset`](repros/core-dto-maxvalue-offset). Source:
`AnyDateTimeOffset.cs:386-409` (`WithOffsetRange`). Red test
(`JustDummies.UnitTests/AnyDateTimeOffsetOffsetTests.cs`): the message contains
"9999-12-31T09:59:59.9999999+00:00", or says that the local time would leave the range. Fix: give
the conflict a dedicated factory that names the derived limit and the reason, in both orders.

<a id="core-d6"></a>
**CORE-D6 — The `SetOf` cardinality refusal names no count (low).**
"The generator can produce only 10 distinct value(s), not enough for the 11 elements…" names neither
`WithCount(11)` nor the element chain. CLAUDE.md asks for a message that names both sides. Domains:
05. Source: `ConflictingAnyConstraintException.cs:152-154`. Red test
(`JustDummies.UnitTests/ConflictMessageProvenanceTests`): the message for `SetOf(Any.Boolean()).WithCount(5)`
contains "WithCount(5)". Fix: pass the count call into `SetElementsExceedCardinality` and
`DictionaryKeysExceedCardinality`.

<a id="core-d7"></a>
**CORE-D7 — `DateTime` draws are always `Kind = Utc` and compare by ticks: decided in XML docs, absent from the user page (medium, documentation).**
The XML remarks on `AnyDateTime` (lines 18-20) decide this: generated values carry `DateTimeKind.Utc`,
bounds compare by `Ticks` and ignore their `Kind` (as `DateTime`'s own operators do), and `OneOf`
keeps the `Kind` of the values passed to it. The user page `dates-and-times` (EN/FR) mentions none of
it. **The consequence is surprising and should be stated plainly:** `Between(Local, Local)` can return
a `Utc` value whose *instant* lies outside the bounds' instants. Under `TZ=Europe/Paris`, bounds of
00:00:00 and 00:00:01 local yield a `Utc` value with ticks between them, which is one hour *later* as
an instant. The value satisfies `start <= value && value <= end` under the comparison a test's own
`<=` would use, so no declared constraint is broken. **The maintainer should judge whether the
documented contract is the right one.** The alternative is [CORE-C7](#core-c7). Domains: 02, 04, 06,
11, 16, 17. Repro: [`repros/core-datetime-kind`](repros/core-datetime-kind). Red test
(`JustDummies.Documentation.UnitTests`): a compiled sample on `dates-and-times` showing a `Utc` result
between `Local` bounds by ticks. Fix: add a "DateTime and Kind" paragraph (EN/FR) that also states the
unconstrained range (`MinValue`..`MaxValue`), and advise `Utc` bounds, or `DateTimeOffset` when the
instant matters.

<a id="core-d8"></a>
**CORE-D8 — The `WithScale` user page says it "fixes the number of decimal places"; ADR-0015 decides a value lattice (medium, documentation).**
Every draw is a multiple of 10⁻ⁿ (verified exactly, 500 draws × 5 magnitudes). A `OneOf` value or a
single-point range comes back as written (`20`, not `20.0`). Near 7.9e27, 29 significant digits leave
no room for two fractional digits, yet the value is still on the lattice. ADR-0015 rejects a padded
`decimal.Scale` contract explicitly, and requires the builder docs to "state it as a value lattice".
Domains: 03, 08, 11, 12, 13, 15, 18. Repro: [`repros/core-withscale`](repros/core-withscale). Source:
`doc/handwritten/for-users/generators/numbers.en.md:106`, `numbers.fr.md:111`. Fix: say "keeps the
value expressible in at most n decimal places (a multiple of 10⁻ⁿ); `decimal.Scale` is not padded",
and link ADR-0015. A `JustDummies.PropertyTests` invariant `decimal.Round(v, n) == v` up to
`decimal.MaxValue` would pin the contract. It passes today.

<a id="core-d9"></a>
**CORE-D9 — `DateTimeOffset.WithGranularity` grids the UTC instant, not the local clock; the user docs never say so (medium, documentation).**
`WithGranularity(FromDays(1)).WithOffset(FromHours(2))` yields local 02:00, and with `+05:30` "whole
hours" fall at :30. The XML docs say "the instant", which is honoured, but their examples ("a whole
day", "a quarter-hour") read as local time. Neither the granularity section nor the offset section
of the user page says how the two interact. Domains: 07, 09, 19. Repro:
[`repros/core-dto-granularity`](repros/core-dto-granularity). Source: `AnyDateTimeOffset.cs:201-207`,
`:334-340`; `dates-and-times.en.md:42-88`. Fix: document the behaviour now (EN/FR plus the XML
summary). Whether to change the semantics is [CORE-C8](#core-c8).

<a id="core-d10"></a>
**CORE-D10 — The distinct-cardinality refusal moved to `Generate()` without a record; ADR-0004, JD016 and the collections page still promise declaration time (medium, documentation + ADR).**
`CollectionState.cs:166` runs the check when the specification is whole, deliberately: checking in
the middle of a chain refused one declaration order and accepted another. The behaviour is sound,
and it still fails before any draw. The records disagree with it:

- ADR-0004 ("rejects … immediately"; it even rejects "always fail at generation");
- the context sections of ADR-0009 and ADR-0011;
- `JD016.en.md:14` ("Both throw at declaration time");
- `collections.en.md:93-95` ("refused immediately").

Domains: 01, 04, 05, 10. Repro: [`repros/core-cardinality-gate`](repros/core-cardinality-gate). Fix:
record the move in an ADR that amends ADR-0004, then update the pages (EN/FR). The red test goes in
`JustDummies.UnitTests`: it asserts that `SetOf(Boolean()).WithCount(5)` does not throw at
declaration and throws `ConflictingAnyConstraintException` at `Generate()`. The string and pattern
bounded-draw fallback is ADR-0004's decided boundary and stays as it is (see §9).

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
`OneOf((Currency)42)`, a `[Flags]` non-combination, and `(Currency)0` in a mixed pool each produce
two warnings at the identical line and column. Commit `37ff3ff` added enum membership to JD014
without making either rule stand down. ADR-0078: "two diagnostics on one expression for one mistake
is noise that trains a reader to disable both." Domains: 01, 04, 12. Repro:
[`repros/analyzers-jd014-jd017-double`](repros/analyzers-jd014-jd017-double). Source:
`RejectedConstantArgumentAnalyzer.cs` (~l.117, `case "OneOf"`), `ArgumentGuardFacts.cs:109, 217-234`,
overlapping `EnumUniverseViolationAnalyzer.cs`. Red test (`JustDummies.Analyzers.UnitTests`): run
both analyzers on `Any.Enum<Status>().OneOf((Status)99)` and assert exactly one diagnostic at the
span. Fix: give membership one owner. JD017 is suggested, because its message teaches the category
error. Keep the refusal in the walk that JD034/JD035 share, update JD014 (EN/FR), and write a short
decision note in the spirit of ADR-0078.

<a id="an-d2"></a>
**AN-D2 — JD023 names only the constraint that empties the domain, without its arguments (low).**
The analyzer reports "No value satisfies this chain once GreaterThan is applied". The run time says
"Cannot apply GreaterThan(10) because Between(1, 6) already requires values less than or equal to 6".
The analyzer already holds the earlier state and the folded arguments. Domains: 08, 12. Repro:
[`repros/analyzers-jd023-message`](repros/analyzers-jd023-message). Source: `Descriptors.cs:242`,
`ScalarChainAdmitsNoValueAnalyzer.cs:86-87`. Red test (`Jd023Jd024ScalarChainTests.cs`): the message
contains both "GreaterThan(10)" and "Between(1, 6)". Fix: use a two-sided message that mirrors the
run-time wording, and show it on the JD023 pages.

<a id="an-d3"></a>
**AN-D3 — JD014 and JD023 suppress different negative-test shapes (low).**
Inside `Assert.Throws(() => chain.Generate())`, JD014 still warns because it checks the guarded call
itself, while JD023 is silent because it judges only the outermost invocation. Silence inside
`Assert.Throws` is documented. The inconsistency is not. Domains: 02, 04 (reported), verified in
[`repros/analyzers-constant-misses`](repros/analyzers-constant-misses) (V8b against line 50).
Source: `RejectedConstantArgumentAnalyzer.cs:59`, `ScalarChainAdmitsNoValueAnalyzer.cs:44-47`.
Red test (`JustDummies.Analyzers.UnitTests`): JD023 gives the same verdict as JD014 on
`Assert.ThrowsAny<Exception>(() => <conflicting chain>.Generate())`. Fix: align the two rules, and
state the suppressed shape on both pages.

<a id="an-d4"></a>
**AN-D4 — The JD014 page claims to mirror the guards "exactly", and a test files a refused argument under "legal" (low).**
The Scope section says "the table mirrors SizeGuard and the per-generator guards exactly". That is
untrue: see AN-F2 and AN-C4. `Jd014RejectedConstantArgumentTests.cs:209-210` lists
`Any.Double().Between(double.NaN, 1.0)` under `Does_not_report_a_legal_argument`, although the
comment beside it says the guard refuses it. Fix: list what the rule really covers (EN/FR), and move
that row to a correctly named "not covered" case.

<a id="an-d5"></a>
**AN-D5 — Info rules (JD020, JD022, JD024, JD029, JD030, JD031, JD033) never appear in `dotnet build` output, and no user page says so (medium, documentation).**
The rules fire: they show up in a SARIF error log, and are printed once raised to `warning` in
`.editorconfig`. Hiding Info diagnostics from the console is standard MSBuild behaviour, and
ADR-0077 decided the severity. The user pages ("active on your next build", "at the call site,
which is the one place you can act on it") assume a reader who uses an IDE. In a terminal-only or CI
workflow, the remedy JD030 is meant to teach never reaches the user. Domains: 02, 12, 17 (20 also
notes it). Repro: [`repros/analyzers-info-invisible`](repros/analyzers-info-invisible). Fix: add one
sentence plus an `.editorconfig` snippet to `analyzers/README.md` and `packages/justdummies` (EN/FR),
for example `dotnet_analyzer_diagnostic.category-JustDummies.Constraints.severity = warning`. Do not
change the default severities. Red test (`JustDummies.Documentation.UnitTests`): both pages state
this and carry the snippet.

### False negatives against a rule's documented scope

<a id="an-f1"></a>
**AN-F1 — JD016 ignores a typed numeric `OneOf` pool (low).**
`Any.SetOf(Any.Int32().OneOf(1, 2)).WithCount(3)` and `Any.ArrayOf(Any.Double().OneOf(1.0, 2.0)).WithCount(3).Distinct()`
compile silently and throw at run time. The generic `Any.OneOf(...)` and `Any.Char().OneOf(...)` are
flagged. The page lists "a OneOf/ElementOf pool of constants", and the comment at
`CollectionConstraintsAdmitNoValueAnalyzer.cs:205-209` claims the case is handled, which is false.
Domain: 20. Repro: [`repros/20-6`](repros/20-6). Source: same file, `:200-215, 269`. Red test
(`JustDummies.Analyzers.UnitTests`, JD016): the pool above reports JD016, and a negative case keeps
the shape that #188 made legal (widened by `Containing`). Fix: count the first typed `OneOf` met
while walking down, and route a later widening through the existing `mayExtend` hook.

<a id="an-f2"></a>
**AN-F2 — JD014 misses non-finite constant bounds, and `Half` in `OneOf` (low).**
`Between(double.NaN, 90.0)`, `LessThan(double.PositiveInfinity)`, `LessThanOrEqualTo(double.NaN)` and
`Any.Half().OneOf((Half)1, Half.NaN)` always throw at declaration, and get no diagnostic.
`Single().OneOf(1f, float.NaN)` is flagged. The page lists non-finite `OneOf` only for `Double` and
`Single`, so strictly this sits at the edge of the documented scope. It is classed here because the
page also claims to mirror the guards exactly (AN-D4). Domains: 09, 20. Repro:
[`repros/20-5`](repros/20-5). Source: `ArgumentGuardFacts.cs:109` (`AnyHalf` missing),
`RejectedConstantArgumentAnalyzer.cs` (no bound-finiteness case). Red test (JD014 suite): JD014 on
both the `NaN` bound and the `Half.NaN` element. Fix: add both checks, update the JD014 pages (EN/FR),
and extend the property-test chains.

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
A `[Fact, Reproducible]` test copied from `packages/justdummies-xunit.en.md` fails with `CS0246` on
`Reproducible`. No user page shows *any* `using JustDummies…` directive, nor does the NuGet readme.
**Root cause:** `JustDummies.Documentation.UnitTests/DocumentationSampleCompiler.cs:31-48` silently
adds `using JustDummies; using JustDummies.Diagnostics; using JustDummies.Xunit; using Xunit;` to
every sample. The samples therefore compile in CI, and the compiler's own remark ("if it binds, it
binds in a consumer's test project too") does not hold for namespaces.

| | |
|---|---|
| Domains | 03, 04, 05, 06, 07, 09, 11, 12, 13, 15, 16, 18, 19 |
| Repro | [`repros/docs-xunit-namespace`](repros/docs-xunit-namespace) |
| Red test | `JustDummies.Documentation.UnitTests`: compile the first full sample of `justdummies-xunit.{en,fr}.md` with a prelude that does not inject `JustDummies.Xunit`, and expect no `CS0246`. |
| Fix | Show the three `using` lines in the first sample of the xunit page (EN/FR), in `JustDummies.Xunit/README.nuget.md` and on the getting-started page. Name the namespace under "The whole surface". Stop the prelude from injecting `JustDummies.Xunit`, so the docs cannot drift again. A `buildTransitive` global `<Using>` would be a packaging decision, not this fix. |

No defect was found in the adapter's own behaviour. `[Reproducible]` printed a copy-pasteable
`[Reproducible(Seed = …)]` on failure in every domain that used it, and pinning that seed reproduced
the failing value each time it was tried.

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
| <a id="doc-1"></a>DOC-1 | `generators/enums-and-choices:157`, `guides/errors-and-conflicts:63` | `Any.Boolean().Except(true, false)` does not exist in the public API | `core` | Use an existing member, e.g. `Any.Boolean().True().DifferentFrom(true)` |
| <a id="doc-2"></a>DOC-2 | `generators/strings:56`, `analyzers/JD030:24` | "`NonEmpty()` leaves the ceiling where it was"; it gives 1 to 1025 (JD030's own sentence says so) | `core` | "raises floor and ceiling by one" |
| <a id="doc-3"></a>DOC-3 | `generators/collections:50`, `guides/design-principles:72` | Cite ADR-0029 for the million cap; superseded by ADR-0076 | `core` | Point at ADR-0076; add a link check for superseded ADRs |
| <a id="doc-4"></a>DOC-4 | `generators/strings` (regex tables) | `\xNN`/`\uNNNN` work but are unlisted; `\b`, `\B`, `\G`, `\p{…}`, `\k<…>` and class subtraction are refused but unlisted; `IgnorePatternWhitespace` throws `ArgumentException`, unmentioned; the 65,536-character limit is absent; every refusal ends with the same fixed list | `core` | Complete both tables, add the two notes; optionally make the message tail specific to the construct (`UnsupportedRegexException.cs:22`) |
| <a id="doc-5"></a>DOC-5 | `packages/justdummies-cli:254` | "A lambda never widens the base"; `_ => Any.Int32()` replaces it | `cli` | "One that builds on its argument never widens it; one that ignores it replaces the base" |
| <a id="doc-6"></a>DOC-6 | `guides/errors-and-conflicts:101-108` | `Seed` documented "inside a reproducible scope" only; it is also set outside | `core` | With [CORE-B1](#core-b1) |
| <a id="doc-7"></a>DOC-7 | `packages/justdummies-cli` | Silent on regex guards never being read (spec §5.3 decides it) | `cli` | Say so; point to `Any.StringMatching` and the regular subset |
| <a id="doc-8"></a>DOC-8 | `packages/justdummies-cli` | No section on where a cross-parameter invariant goes | `cli` | Idea (a) of [CLI-E1](#cli-e1), including the Luhn one-liner |
| <a id="doc-9"></a>DOC-9 | `packages/justdummies-cli:243-247` (refinement) | Does not say a once-only constraint (`OneOf`, families, `WithLength`, `MultipleOf`, `WithScale`) cannot be declared again in the lambda; `Except`/`DifferentFrom` narrow a pool; JD034 cannot see into the lambda | `cli` | One bullet plus a compiled `z => z.Except(...)` sample |
| <a id="doc-10"></a>DOC-10 | `packages/justdummies-cli` (refinement) | To reuse a prepared refinement, hold a `Func<AnyX, AnyX>`, not a built `IAny<T>` (ADR-0109; CS0266 does not teach it) | `cli` | One line |
| <a id="doc-11"></a>DOC-11 | `packages/justdummies-cli` | A scaffolded string row raises JD030 (Info) by design | `cli` | One line inviting the developer to declare the length |
| <a id="doc-12"></a>DOC-12 | `guides/errors-and-conflicts` | A transposed single call (`Between(10, 1)`) is an argument mistake, not a conflict | `core` | One line under "bounds that cross" |
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

1. **Issues.** Open one issue per root cause, grouped by scope, citing these IDs. Do so **only with
   the maintainer's approval**.
   * `cli`: CLI-B1–B6 (B7 is #296), CLI-D1–D5.
   * `core`: CORE-B1–B3, CORE-D1–D10.
   * `analyzers`: AN-D1–D5, AN-F1–F2.
   * `xunit`: XU-D1.
   * Documentation: DOC-1–13. Each row belongs to the train its component names.
   * Candidates (§8) go to the maintainer as decision requests, not as bugs. Most would end in an ADR
     or a documented refusal.
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
