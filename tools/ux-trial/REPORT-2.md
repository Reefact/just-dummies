# Developer-UX trial 2 — JustDummies and `any`: report

## 1. Summary

**Overall verdict on fluency.** Both the library and the scaffolder are fluent enough for real work.
- Twenty bounded contexts (21–40) were each modelled once and tested by three profiles, which makes 60 sessions.
- 58 sessions rated fluency 4/5. Two rated it 3/5: the regular profile on 28-satellite-conjunction and the discovery profile on 33-music-score.
- Every suite passed, and every suite stayed green over repeated runs with fresh seeds (5 to 150 runs per suite). In 32-grid-load-flow the expert's suite has one test skipped on purpose, because a drawn value exposed a real defect in the domain.

What worked:
- The core DSL, composition through scaffolded generators, pin and refine `With…` pairs, and `[Reproducible]` worked across every domain.
- Drawn dummies exposed real hidden dependencies several times, which is the reason the library exists. Examples: a growth-rate ceiling in 21, a solver that diverges at acceleration ≥ ~1.87 in 32, an assumption about a single milkable teat in 26, and arithmetic slips in several suites.

What slowed people down:
- Almost all friction sits in the scaffolder's guard reading and in rules that tie parameters together. It does not sit in the library surface.
- An unread guard stopped the build, as designed, with one exception: a guard written through a value-returning helper is silent, and the generator looks finished but throws on most draws ([CLI-14](#cli-14)).

### Fluency and test counts

D = discovery, R = regular, E = expert. A cell reads "fluency / tests". The test count is the figure each session reported (cases where the session gave cases, otherwise methods).

| Context | D | R | E |
|---|---|---|---|
| 21-orbital-microbiology | 4 / 40 | 4 / 42 | 4 / 44 |
| 22-pulmonary-nodules | 4 / 31 | 4 / 36 | 4 / 31 |
| 23-captive-breeding | 4 / 38 | 4 / 34 | 4 / 29 |
| 24-radiocarbon-chronology | 4 / 33 | 4 / 39 | 4 / 38 |
| 25-adaptive-video | 4 / 38 | 4 / 37 | 4 / 34 |
| 26-robotic-milking | 4 / 36 | 4 / 44 | 4 / 48 |
| 27-railway-interlocking | 4 / 35 | 4 / 32 | 4 / 36 |
| 28-satellite-conjunction | 4 / 41 | **3** / 40 | 4 / 38 |
| 29-parimutuel-betting | 4 / 32 | 4 / 34 | 4 / 32 |
| 30-liquidity-gap | 4 / 40 | 4 / 40 | 4 / 41 |
| 31-vessel-stowage | 4 / 34 | 4 / 33 | 4 / 38 |
| 32-grid-load-flow | 4 / 39 | 4 / 38 | 4 / 41 |
| 33-music-score | **3** / 34 | 4 / 40 | 4 / 42 |
| 34-manuscript-reconstruction | 4 / 31 | 4 / 35 | 4 / 35 |
| 35-dna-variants | 4 / 34 | 4 / 42 | 4 / 35 |
| 36-knitting-patterns | 4 / 41 | 4 / 37 | 4 / 38 |
| 37-batch-genealogy | 4 / 31 | 4 / 34 | 4 / 36 |
| 38-carbon-ledger | 4 / 30 | 4 / 32 | 4 / 33 |
| 39-geothermal-reservoir | 4 / 41 | 4 / 40 | 4 / 37 |
| 40-catastrophe-losses | 4 / 40 | 4 / 39 | 4 / 35 |
| **Total** | **719** | **748** | **741** |

That is 2,208 tests across 60 suites, all passing. Mean fluency is 3.97.

### The findings that matter most

1. **[CLI-11](#cli-11) — a real bug, high severity.** With `--entry-point any`, the entry class for a type `Voice` is named `AnyVoiceEntry`. That is also the generator name of a domain type `VoiceEntry`. The run exits 0 and the project no longer compiles.
2. **[CLI-1](#cli-1) — `x < a || x > b` range guards are never read.** This hit 18 of the 20 contexts, at every profile. It is a specified, deliberate limit (spec §5.3, ADR-0060), but it was the largest cost of the trial. The user documentation never says so ([DOCS-1](#docs-1)). This trial is the kind of field report ADR-0085 asks for before the guard reader may be widened.
3. **[CLI-3](#cli-3) — only the first unread guard of a parameter is named.** A cross-parameter rule that comes later stays hidden. A user who fixes only what is named gets a generator that compiles and throws on some draws. This is documented ("names the first guard"), but it is a trap.
4. **[CLI-8](#cli-8) — a parameter derived from another has no supported shape.** The documented "derive inside `Generate()`" route turns the derived parameter's `With…` pins into silent no-ops. The edit is also lost on `--force`. This was met in 15 contexts.
5. **[CLI-9](#cli-9) — refining a composed part restarts from the part's own base.** This silently drops a rule the outer base set. One verifier rated it a defect against spec D13 (29-12). The others rated it friction caused by a misleading "never widens the base" sentence in the docs.
6. **[DOCS-2](#docs-2) — the CLI page's suggested layout does not compile.** A root `Arbitrary` placed in a namespace ending in `.Arbitrary` fails with CS0234, and the tool does not warn.
7. **Core gaps** came up repeatedly, all of them meaning a test had to write its own helper:
   - [CORE-1](#core-1): `Except` with an empty list throws.
   - [CORE-4](#core-4): no idiom for "B differs from A".
   - [CORE-5](#core-5): no combinator for a draw that depends on another draw.
   - [CORE-3](#core-3): no `DistinctBy`.

---

## 2. By profile

### Discovery: what trips a developer new to the library

- **The scaffolder ignores ordinary guards, and nothing in the user docs explains why.** `x < lo || x > hi`, `is < a or > b` and the `double.IsNaN(x) || …` prefix all come back as `to verify, unread guards`. The user page shows one example of a guard it reads and never lists the shapes it reads ([CLI-1](#cli-1), [CLI-2](#cli-2), [DOCS-1](#docs-1)). Newcomers could not tell whether rewriting a guard would help. It would: splitting it into separate `if`s gets it read.
- **Only the first unread guard is named.** So a cross-field rule (min < opt < max, centrifuge volume, start ≤ end) is found only by reading the constructor ([CLI-3](#cli-3)).
- **Compile errors that the docs explain, but only one or two clicks away:**
  - CS0029 after switching to `Any.StringMatching` ([CLI-13](#cli-13)).
  - `AnyIRowElement` not found, followed by `any generate IRowElement` refusing because the type is abstract ([CLI-10](#cli-10)).
  - The entry-point class-name clash in 33 ([CLI-11](#cli-11)).
- **Unbounded dates.** `Any.DateOnly()` and `Any.DateTimeOffset()` span the whole type, and nothing says so for those two types ([DOCS-3](#docs-3)).
- **Doubt about what a dummy is.** Is a drawn id that appears in an assertion only as a handle still a dummy? ([DOCS-5](#docs-5)).
- **Smaller points:**
  - Getting-started never mentions `any` ([DOCS-8](#docs-8)).
  - Info-level analyzer hints are invisible in `dotnet build` ([AN-1](#an-1)).

### Regular: what slows down a habitual user

- **Hand work on numeric bases.** In most domains 9 to 23 parameters needed a hand-written `Between` ([CLI-1](#cli-1)).
- **Rules that tie parameters.** These need a hand-reshaped `Generate()`. It leaves the derived parameter's pins meaningless and is erased by `--force` ([CLI-8](#cli-8)).
- **Repeated mechanical edits:**
  - Retyping `AnyString` to `AnyPattern` for every regex-guarded value object ([CLI-13](#cli-13)).
  - Comparer classes, because there is no `DistinctBy` ([CORE-3](#core-3)).
  - A conditional around every computed `Except` list, because an empty list throws ([CORE-1](#core-1)).
- **Surprises:**
  - A nested `With…(lambda)` drops what the outer base set ([CLI-9](#cli-9)).
  - A pinned mutable aggregate is shared between draws ([CLI-23](#cli-23)).
  - A parameter that appears only in an exception message is flagged ([CLI-5](#cli-5)).
  - The `Arbitrary`-in-`.Arbitrary` layout from the docs fails to compile ([DOCS-2](#docs-2)).
- **Conventions to learn:**
  - Generators land in the domain namespace by default ([CLI-18](#cli-18)).
  - The `AnyValid…()` bases are private ([CLI-17](#cli-17)).
  - Pinning a nullable parameter to null needs a cast ([CLI-19](#cli-19)).

### Expert: what limits a crafter

- **No dependent draw.** There is no bind or `SelectMany`. Grids whose cells depend on drawn dimensions, or wells that must sit inside a drawn grid, were rewritten by hand. A working but undocumented idiom exists: `.As(x => inner.Generate())` ([CORE-5](#core-5)).
- **Missing composition primitives:**
  - A public constant generator ([CORE-6](#core-6)).
  - A choice among several generators ([CORE-12](#core-12)).
  - A permutation or subset of a known list ([CORE-7](#core-7)).
  - Ordered collections ([CORE-13](#core-13)).
  - Narrowing a `WithChars` pool or an `AnyPattern` ([CORE-8](#core-8), [CORE-9](#core-9)).
- **The ordinary-magnitude window.** A pressure valid up to 100 MPa is always drawn below 1 MPa, and a permeability over 11 decades lands almost always in the top decade ([CORE-14](#core-14)).
- **Small sources of noise:**
  - CA1822 on single-parameter scaffolds ([CLI-21](#cli-21)).
  - Info-level hints that a terminal build never shows ([AN-1](#an-1)).
  - JD009 does not see draws made through scaffolded generators ([AN-2](#an-2)).
  - The pasted replay attribute needs a `using` it does not mention ([XU-1](#xu-1)).
  - A failure reports the seed but never the drawn values ([XU-2](#xu-2)).

---

## 3. By component

Abbreviations: D/R/E = discovery/regular/expert. "Met in" lists each context with the profiles that hit the item, then the original finding ids.

### Core

#### CORE-1 — `Except` with an empty list throws instead of excluding nothing
<a id="core-1"></a>
- **Kind / severity:** friction, medium
- **Met in:** 23 (E), 24 (E), 37 (E), 38 (E). Findings 23-7, 24-3, 37-6, 38-4.
- **Minimal repro:**
  ```csharp
  string[] taken = [];
  Any.StringMatching("[A-Z]{2,4}-[0-9]{4,6}").Except(taken).Generate();
  // same on Any.String().Except(...) and Any.Int32().Except(new int[0])
  ```
- **Expected:** An empty exclusion list leaves the domain unchanged, the same way `Except(double.NaN)` is a documented no-op.
- **Actual:** `System.ArgumentException : At least one value is required. (Parameter 'values')`. A fixture whose "already used" set starts empty needs a conditional at every call site.
- **Verdict:** confirmed in 23. Partly in 24, 37 and 38.
  - The refusal is deliberate, uniform across every `Except`, and documented in the XML doc (`<exception … when values is empty>`).
  - JD014 does flag a constant empty `Except()`.
  - What remains: the JD014 page and the strings and enums-and-choices pages list only `OneOf()`. No ADR justifies refusing an empty exclusion, which is the identity, not an empty domain. ADR-0033 already criticises this message.

#### CORE-2 — No way to keep values distinct across separate draws; `Except` has no `IEnumerable<T>` overload
<a id="core-2"></a>
- **Kind / severity:** gap, low
- **Met in:** 37 (E). Finding 37-7.
- **Minimal repro:**
  ```csharp
  HashSet<int> used = new();
  Any.Int32().Between(1, 6).Except(used).Generate(); // CS1503
  ```
- **Expected:** An `Except(IEnumerable<T>)` overload (`OneOf` already has one), or a scoped "not yet used" draw.
- **Actual:** Only `params T[]` exists, so the used set is kept by hand and passed with `.ToArray()` at every call.
- **Verdict:** confirmed. The `IEnumerable<T>` overload is a small symmetric addition. A scope that tracks used values would widen the generator's ambition and needs a decision under ADR-0046.

#### CORE-3 — No key-based distinctness (`DistinctBy`)
<a id="core-3"></a>
- **Kind / severity:** friction / gap, low
- **Met in:** 22 (R, E), 24 (D), 27 (D), 31 (E), 40 (D). Findings 22-4, 24-8, 27-7, 31-10, 40-8.
- **Minimal repro:**
  ```csharp
  Any.ListOf(Any.StratigraphicLayer()).WithCount(2).DistinctBy(l => l.Code); // CS1061
  ```
- **Expected:** A key-selector overload. In 22, it should also gate the count on the key's cardinality, as `SetOf(Enum)` does.
- **Actual:** Only `Distinct()` and `Distinct(IEqualityComparer<T>)` exist. In 22, `Distinct(comparer)` with no maximum count fails after the bounded redraw: "Could not generate a distinct collection of 9 elements … only 8 distinct value(s)". The failure is loud and reports its seed.
- **Verdict:**
  - Confirmed in 22, 24, 27 and 40.
  - Partly in 31: the comparer route is documented (collections page, JD028 page) and works, so what is missing is convenience, not capability.
  - Under nullable, `EqualityComparer<T>.Create` needs `a!`/`b!`.

#### CORE-4 — Two values of the same type that must differ: no `DifferentFrom` on `IAny<T>`, no documented recipe
<a id="core-4"></a>
- **Kind / severity:** gap, medium
- **Met in:** 23 (D, R), 27 (D, R, E), 32 (D, R, E), 33 (R), 34 (R), 35 (D, R, E). Findings 23-5, 27-4, 27-5, 32-6, 33-9, 34-8, 35-3, 35-7.
- **Minimal repro:** `any generate Variant` on a constructor that throws when `reference.Equals(alternate)`. Then:
  ```csharp
  new AnyAllele().DifferentFrom(a); // CS1061
  // Pitch: Arbitrary.Pitch().DifferentFrom(anyPitch) — CS1061
  ```
- **Expected:** A way to declare the tie, such as `DifferentFrom` on `IAny<T>` or a distinct pair, or a documented recipe for "B differs from A" that keeps both `With…` pins.
- **Actual:** Two independent draws. They collide on rare seeds (about 1 in 315 for Pitch, 71 in 2000 for one-letter bus ids), so tests become flaky. All three 35 users hand-wrote a bounded redraw loop inside the scaffolded `Generate()`.
- **Verdict:**
  - Confirmed in 23, 27-4, 33, 35-3 and 35-7. The docs section "When a guard ties two parameters" covers ordering, derivation and check digits, but not inequality. `IAny<T>` exposes only `Generate()`, `As` and `OrNull`.
  - Partly in 27-5, 32 and 34. Documented building blocks already do the job:
    - `new AnyX().WithComponent(p => p.DifferentFrom(first.Component))` inside `Generate()` keeps the base.
    - `Any.ArrayOf(g).WithCount(2).Distinct()` also works, but ignores pins.
  - No page shows either building block used for this case.

#### CORE-5 — No combinator for a draw that depends on another draw
<a id="core-5"></a>
- **Kind / severity:** gap, medium
- **Met in:** 28 (D), 33 (D, R, E), 36 (D, E), 39 (E). Findings 28-4, 33-6, 36-6, 39-4.
- **Minimal repro:** Draw a `ReservoirSimulation` whose wells sit in distinct cells of the drawn grid. Or draw a `Measure` whose voices fill the drawn time signature.
- **Expected:** A bind-style operator, e.g. `anyGrid.Then(grid => wellsInside(grid))`, or a documented idiom.
- **Actual:** `Any.Combine` takes independent operands only. Users wrote custom `IAny<T>` classes, or used `Func<TDep, IAny<T>>` fields that `--force` erases.
- **Verdict:**
  - Confirmed in 33 and 36: the public API has no `SelectMany`, `Bind` or `Then`.
  - Partly in 28 and 39:
    - `design-principles.en.md` names this case as one to assemble by hand.
    - `anyGrid.As(grid => inner(grid).Generate())` already works and replays under the seed (39-4, 200 draws).
    - The docs never show that idiom, never say a nested `Generate()` inside a lambda is supported, and never warn that it draws from the ambient source rather than from an `Any.WithSeed` context.

#### CORE-6 — No public constant generator
<a id="core-6"></a>
- **Kind / severity:** gap, low
- **Met in:** 28 (D, E), 36 (E), 40 (E). Findings 28-5, 36-8, 40-9.
- **Minimal repro:** Scaffold any two types and compare them. Each carries a byte-identical private nested `FixedValue<TValue>`. Hand-written recipes need `Any.OneOf<IEnumerable<T>>(array)` with an explicit type argument.
- **Expected:** Something like `Any.Fixed(value)` / `Any.Just(value)`.
- **Actual:** No public constant `IAny<T>`. `Any.OneOf(value)` rejects null and consumes a draw.
- **Verdict:**
  - Confirmed. Spec §4.2 and §15 already note that `Any.Fixed<T>` would let the emitter drop the helper (deferred, not rejected).
  - Partly in 28 on one point: a `Generate()` can honour both pins of a tied enum/bool pair with a bounded joint redraw, without detecting pins. No page shows that.

#### CORE-7 — No permutation or subset of a known collection
<a id="core-7"></a>
- **Kind / severity:** gap, low
- **Met in:** 25 (E), 26 (E), 29 (E), 38 (E), 40 (E). Findings 25-10, 26-13, 29-10, 38-9, 40-9.
- **Minimal repro:**
  ```csharp
  List<UdderQuarter> order = Any.ListOf(Any.ElementOf(all)).WithCount(all.Length).Distinct().Generate();
  ```
- **Expected:** `Any.PermutationOf(items)`, `Any.SubsetOf(items).WithCount(n)`, or a documented idiom.
- **Actual:** The indirect form above. It works, because collections are shuffled internally (Fisher-Yates) and ADR-0113 gates cardinality, but the intent stays hidden.
- **Verdict:**
  - Confirmed in 26, 29 and 40.
  - Partly in 25 and 38 (rated friction there):
    - `Any.ListOf(Any.ElementOf(rungs)).Distinct()` works even without value equality, because existing instances are returned.
    - `Any.SetOf(Any.ElementOf(issued)).WithCount(2)` needs no `ToList()`.
    - None of this is documented as a recipe.

#### CORE-8 — A `WithChars` pool cannot be narrowed, and there is no `WithoutChars`
<a id="core-8"></a>
- **Kind / severity:** gap, medium
- **Met in:** 35 (D, R, E). Finding 35-1. The related documentation gap is [DOCS-4](#docs-4).
- **Minimal repro:**
  ```csharp
  Any.String().WithChars("ACGTN").WithLengthBetween(1, 200).WithChars("ACGT").Generate();
  ```
- **Expected:** Subset pools intersect, or `WithoutChars("N")` exists, or the error names a way forward.
- **Actual:** `ConflictingAnyConstraintException : Cannot apply WithChars("ACGT") because WithChars("ACGTN") is already defined.` The only subtractions are `WithoutAlpha()` and `WithoutNumeric()`.
- **Verdict:** confirmed. Declare-once is documented, but a strict-subset pool is a pure narrowing. The strings page also says the universe is narrowed by "every constraint below … no exception".

#### CORE-9 — An `AnyPattern` cannot be narrowed to a smaller pattern
<a id="core-9"></a>
- **Kind / severity:** friction, low
- **Met in:** 26 (R), 37 (E). Findings 26-8, 37-8.
- **Minimal repro:**
  ```csharp
  Arbitrary.AnimalId().WithValue(v => v.StartingWith("FR")); // CS1061 on AnyPattern
  ```
- **Expected:** A way to narrow, or guidance for this case.
- **Actual:** Only `OneOf`, `Except` and `DifferentFrom` exist. The call site falls back to `_ => Any.StringMatching("FR[0-9]{12}")`, which is the base-replacing form the CLI page warns against. Alternatively the user writes a hand method that restates part of the pattern.
- **Verdict:**
  - Confirmed in 37 and partly in 26.
  - The limit is deliberate: strings.en.md, "What you can still constrain", follows ADR-0008 and ADR-0046.
  - What remains is that no page covers the "sub-language at one call site" case.

#### CORE-10 — A second `OneOf` is refused, and the message does not point to `Except`
<a id="core-10"></a>
- **Kind / severity:** friction, low
- **Met in:** 31 (E). Finding 31-7.
- **Minimal repro:**
  ```csharp
  Any.Int32().OneOf(2,4,…,98).OneOf(2,4,6,8,82,84,86,88)
  ```
- **Expected:** An intersection, or a message that suggests `Except(...)`.
- **Actual:** `ConflictingAnyConstraintException: Cannot apply OneOf(…) because OneOf(…) is already defined.`
- **Verdict:** confirmed. The refusal is documented (CLI page, JD034). The narrowing route appears only on the CLI page, not in the message.

#### CORE-11 — Integer shapes: a union of ranges and odd numbers
<a id="core-11"></a>
- **Kind / severity:** friction, low
- **Met in:** 31 (D, R). Findings 31-8, 31-9.
- **Minimal repro:** Draw an even tier in 2–20 or 82–98. Draw an odd bay in 1..99. `Between(1, 99).MultipleOf(2, 1)` gives CS1501.
- **Expected:** A concise union form and an offset multiple, or documented idioms for both.
- **Actual:**
  - Union: `OneOf(Enumerable.Range(2,19).Concat(Enumerable.Range(82,17))).MultipleOf(2)` works in one line, because `OneOf(IEnumerable<int>)` exists, but it is not documented for ranges.
  - Odd numbers need `Between(0, 49).As(n => 2*n+1)`.
- **Verdict:**
  - Partly for the union: the claimed 30-literal list was not needed.
  - Confirmed for odd numbers: the internal lattice already has an anchor, but the public `MultipleOf` always anchors at 0.

#### CORE-12 — No choice among several generators
<a id="core-12"></a>
- **Kind / severity:** gap, low
- **Met in:** 36 (D, R, E). Finding 36-5.
- **Minimal repro:**
  ```csharp
  Any.Combine(Any.Boolean(), new AnyStitchRun(), new AnyStitchRepeat(),
              (isRepeat, run, repeat) => isRepeat ? (IRowElement)repeat : run);
  ```
- **Expected:** A documented union that draws only from the chosen generator.
- **Actual:** `Combine` draws both branches every time (10 of 10 calls each). `OneOf` takes values, and JD012 rightly warns against passing generators to it, but no page names an alternative.
- **Verdict:** confirmed.

#### CORE-13 — No ordered collections or rules between elements
<a id="core-13"></a>
- **Kind / severity:** gap, low
- **Met in:** 25 (R), 40 (D, R). Findings 25-9, 40-3.
- **Minimal repro:** Four distinct doubles in [0, 100] in ascending order. Or ladder rungs whose heights never decrease as bitrate rises.
- **Expected:** `.Ascending()` or a documented recipe.
- **Actual:** Users wrote `.As(l => l.Order()…)`, which loses the builder's refining methods, or a roughly 30-line custom `IAny`.
- **Verdict:**
  - Confirmed in 40.
  - Partly in 25: a six-line `.As(...)` expression does the job. What is missing is the documentation. The 2026-07-20 design audit lists a first-class `Sorted()` as deferred.

#### CORE-14 — Bounded doubles cover only a corner of the declared range
<a id="core-14"></a>
- **Kind / severity:** friction, medium
- **Met in:** 39 (D, E). Finding 39-5.
- **Minimal repro:**
  ```csharp
  Any.Double().GreaterThan(0.0).LessThanOrEqualTo(100e6) // 2000 draws
  Any.Double().Between(1e-20, 1e-9)
  ```
- **Expected:** A documented way to draw across the whole declared range or on a log scale.
- **Actual:**
  - Pressure: the maximum over 2000 draws was 999,823, never above 1e6.
  - Permeability: 1,811 of 2,000 draws were ≥ 1e-10, and none was ≤ 1e-17.
- **Verdict:** confirmed. The behaviour is intended and documented (ADR-0097, numbers page). No ADR discusses a window scaled to the bounds or a log spread, and changing this needs a decision.

### CLI (`any`)

#### CLI-1 — Range guards joined with `||` are never read (including `IsNaN`/`IsFinite` prefixes)
<a id="cli-1"></a>
- **Kind / severity:** gap / friction (verifiers split; the behaviour is specified), medium. Reported high in 32.
- **Met in:** 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 34, 36, 38, 39 and 40 (D, R, E each), and 33 (R for 33-3; D, R, E for 33-5). Findings 21-1, 22-2, 23-1, 24-1, 25-1, 25-2, 26-1, 27-1, 28-1, 29-1, 30-3, 31-1, 32-1, 33-3, 33-5, 34-5, 36-1, 38-1, 39-1, 40-1.
- **Minimal repro:**
  ```csharp
  public sealed class P { public P(int a) { if (a < 1 || a > 100) throw new ArgumentOutOfRangeException(nameof(a)); } }
  public sealed class Q { public Q(int a) { if (a < 1) throw new ArgumentOutOfRangeException(nameof(a)); if (a > 100) throw new ArgumentOutOfRangeException(nameof(a)); } }
  // any generate P Q --dry-run
  ```
- **Expected:** `P` is read like `Q`. The `double.IsNaN(x) ||` prefix is ignored, because NaN is never drawn. Failing that, the user docs say the shape is unread.
- **Actual:**
  - `P`: `a int Any.Int32() to verify, unread guards`.
  - `Q`: `Any.Int32().Positive().LessThanOrEqualTo(100) guard`.
  - The same happens for `double`, `decimal` and `int`, with literal or named-constant bounds.
  - `IsNullOrWhiteSpace(x) || x.Length > n` falls back to `NonEmpty()`.
  - `Count == 0 || Any(x => x is null)` loses `NonEmpty()`.
  - Impact per domain: 13 of 22, 15 of 17, 23 of 30, and "every numeric parameter".
- **Verdict:** partly overall. The facts were reproduced in every context.
  - The exclusion is specified: spec §5.3 line 639 says a guard's condition "contains no `&&` or `||`". ADR-0060 says "no boolean composition". `GuardBoundaryTests.cs:1259` pins the behaviour, and ADR-0085 freezes the reader pending field reports.
  - Several verifiers re-rated it friction (27, 28, 29, 31, 33-3, 34, 36, 40).
  - The ThrowIf helpers and split `if`s are read, and that workaround is undocumented for users.
  - Several verifiers note that a throwing `if (A || B)` is exactly `if (A) throw; if (B) throw;`, which the engine already reads. Reading a single-parameter disjunction would therefore work by construction (consistent with ADR-0046), but it changes a spec rule.
  - A standalone `double.IsNaN(x)` guard is also unread, because the closed set has a `!double.IsFinite` row but no `IsNaN` row ([CLI-15](#cli-15)).
  - The documentation half is [DOCS-1](#docs-1).

#### CLI-2 — Relational-pattern guards (`is < a or > b`, `is > n`) are not read
<a id="cli-2"></a>
- **Kind / severity:** gap, medium
- **Met in:** 23 (D, E), 30 (D, R, E), 33 (D, R, E), 34 (D, R, E). Findings 23-1, 30-2, 33-2, 34-4.
- **Minimal repro:**
  ```csharp
  public P(int a, int b, int c) {
      if (a is < -2 or > 2) throw …;
      if (b < -2) throw …; if (b > 2) throw …;
      ThrowIfLessThan(c, -2); ThrowIfGreaterThan(c, 2);
  }
  ```
- **Expected:** `a` → `Any.Int32().Between(-2, 2) guard`.
- **Actual:** `a int Any.Int32() to verify, unread guards`, while `b` and `c` → `Between(-2, 2) guard`. Even a single `value.Length is > 12` is unread, though `value.Length > 12` is read as `.WithMaxLength(12)`.
- **Verdict:**
  - Confirmed in 30 and 33.
  - Partly in 34: a combined `is 0 or > 12` is the pattern spelling of the documented `||` exclusion.
  - The real gap: `Guards.TryRecognise` accepts an `is` pattern only for `is null`. Neither the spec nor the user page names relational patterns as out of scope. This is a candidate for an ADR-0085 field report.

#### CLI-3 — Only the first unread guard per parameter is named; later and cross-parameter guards stay hidden
<a id="cli-3"></a>
- **Kind / severity:** friction (several filed as defect, re-rated), medium
- **Met in:** 21 (D, R), 22 (R, E), 24 (R, E), 25 (D, R, E), 26 (R, E), 27 (R, E), 28 (E), 29 (R, E), 30 (D, E), 32 (D), 34 (D, E), 37 (D, E), 39 (D, R, E), 40 (D, R, E). Findings 21-2, 22-5, 24-2, 24-11, 25-12, 26-2, 27-2, 28-7, 29-3, 30-1, 32-3, 34-2, 37-2, 39-9, 40-2.
- **Minimal repro:**
  ```csharp
  public BehaviouralAssumption(decimal s, int volatileRunOffDays, int coreMaturityMonths) {
      if (volatileRunOffDays is < 1 or > 365) throw …;        // line 16
      if (coreMaturityMonths is < 1 or > 120) throw …;        // line 21
      if (volatileRunOffDays >= coreMaturityMonths * 30) throw …; // line 26
  }
  ```
- **Expected:** Every unread guard that touches the parameter is listed, or at least "and N more", with a cross-parameter guard called out.
- **Actual:** Only line 16 and line 21 are named. After fixing those ranges and deleting the TODO lines, the generator compiles and throws on some draws:
  - (204 days, 3 months) here.
  - 15 of 2,000 draws on MilkingSettings (26).
  - About half the draws on CalendarInterval (24).
  - Route's distinctness guards and WitnessSiglum's regex are never mentioned (27, 34).
- **Verdict:** facts confirmed everywhere. The behaviour is specified and documented:
  - spec §5.6 and §6.1: "the earliest such place".
  - CLI page around line 94: "names the first guard".
  - The comment itself says "first at".
  - Every touched parameter is still marked and the build still stops, so ADR-0083's promise holds.
  - What remains: the recap line itself does not say "first", nothing counts the rest, and the ADR-0083 failure comes back as soon as the user deletes the TODO line after checking only the named guard.

#### CLI-4 — The guard excerpt is truncated, and a multi-line condition is cut with no ellipsis
<a id="cli-4"></a>
- **Kind / severity:** friction, low
- **Met in:** 22 (R, E), 39 (D), 40 (D). Findings 22-5, 39-6, 40-6.
- **Minimal repro:**
  ```csharp
  if (double.IsNaN(k)
      || k < 1.0
      || k > 5.0) throw …;
  // any generate Cell
  ```
- **Expected:** The full guard text in the file comment, or at least "..." when the quote is cut.
- **Actual:**
  - A multi-line guard is quoted as `if (double.IsNaN(k)`, with no marker.
  - A long single-line guard is cut at 100 characters with "...". In 40 that cut fell on the very constant the user had to copy.
- **Verdict:** partly. It is specified (spec §5.6, "the first line of the statement … shortened past a hundred characters"; `UnreadGuard.ExcerptLength`). The inconsistency stands: a line cut is not marked while a length cut is, and the file comment shares the console's width limit.

#### CLI-5 — A parameter used only in an exception message is marked as guarded
<a id="cli-5"></a>
- **Kind / severity:** defect, low
- **Met in:** 33 (D, R, E), 36 (D, R, E). Findings 33-4, 36-3.
- **Minimal repro:**
  ```csharp
  public Row(int number, int? count) {
      if (number < 1) throw …;
      if (count is { } c) { if (c != 3) throw new ArgumentException($"Row {number} is wrong.", nameof(count)); }
  }
  ```
- **Expected:** `number int Any.Int32().Positive() guard`.
- **Actual:** `number … to verify, guard, unread guards` and `unread guard at … if (count is { } c)`. Removing `{number}` from the message clears the mark.
- **Verdict:** confirmed. `Guards.Mentioned` excludes only `nameof`, and `MarkIfItRejects` marks every parameter mentioned anywhere in the throwing statement. That contradicts the engine's own remarks ("a message rejects nothing") and spec §5.3.

#### CLI-6 — The unread-guard location points at a loop header or fallback statement, not the guard
<a id="cli-6"></a>
- **Kind / severity:** defect, low
- **Met in:** 32 (R, E), 39 (D, R, E). Findings 32-4, 39-7, 39-9.
- **Minimal repro:**
  ```csharp
  public Sim(Box box, IReadOnlyCollection<int> items) {
      foreach (int item in items) {                                  // line 7
          if (!box.Contains(item)) throw …;                          // line 9
      }
  }
  ```
- **Expected:** `box: unread guard at Sim.cs, line 9: if (!box.Contains(item)) …`
- **Actual:** `box: unread guard at Sim.cs, line 7: foreach (int item in items)`. In 32, `name`, whose own guard was read, is pointed at a `foreach` over `buses` by the ADR-0110 mark-everything fallback.
- **Verdict:**
  - Confirmed in 39: it contradicts the CLI page ("so you do not have to search the constructor") and spec §5.7.
  - Partly in 32: the citation follows ADR-0110 and the recap rule, but nothing tells the reader the mark comes from the fallback.

#### CLI-7 — A null-element check marks a list `to verify` although `ListOf` never yields null
<a id="cli-7"></a>
- **Kind / severity:** friction, low
- **Met in:** 33 (D, R, E), 36 (E). Findings 33-5, 36-4.
- **Minimal repro:**
  ```csharp
  if (runs.Count == 0) throw …;
  if (runs.Any(r => r is null)) throw …;
  // any generate StitchRepeat
  ```
- **Expected:** `Any.ListOf(new AnyRun()).NonEmpty() guard`.
- **Actual:** `… to verify, guard, unread guards`, pointing at the null-element line.
- **Verdict:** confirmed. No row in the closed set covers it (spec §5.3, §9). It is safe to read when the element recipe is composed. Adding the row is a deliberate widening (ADR-0082, ADR-0085).

#### CLI-8 — A parameter derived from another has no supported shape: pins become no-ops and the edit is lost on `--force`
<a id="cli-8"></a>
- **Kind / severity:** gap (docs) / friction (cli), medium
- **Met in:** 21 (E), 22 (D, R, E), 24 (D, R, E), 25 (D, R, E), 26 (D, R, E), 28 (R), 29 (D, R), 30 (D, R, E), 31 (D, R, E), 32 (D, R, E), 34 (D, R, E), 37 (D, R), 38 (D, R, E), 39 (D, R), 40 (R, E). Findings 21-4, 22-3, 24-4, 24-5, 25-5, 26-3, 28-6, 29-5, 29-6, 30-5, 31-2, 32-5, 34-1, 37-1, 38-2, 39-3, 40-4, 40-7.
- **Minimal repro (34):**
  ```csharp
  // Witness(…, int datedFrom, int datedTo, …) throws if datedFrom > datedTo
  // Option 1 — derive datedTo from datedFrom in Generate(), as the CLI page suggests:
  Any.Witness().WithDatedTo(1600).Generate().DatedTo; // -269: the pin is ignored
  // Option 2 — disjoint bands (from -800..800, to 800..1600):
  Any.Witness().WithDatedFrom(1500).Generate(); // throws on 173 of 200 seeds
  ```
- **Expected:** A documented pattern or scaffolded hook for a dependent parameter that keeps its `With…` pins meaningful and survives `--force`, or at least a warning that pinning one side alone depends on the seed.
- **Actual:**
  - Users reshaped fields, the private constructor, the `With…` methods and `Generate()` inside the generated file: about 15 places in 37, and 60 to 230 lines in 31.
  - They used ad hoc devices: nullable "derive unless pinned" fields, type tests such as `_x is AnyInt32`, and `Func<TDep, IAny<T>>` fields.
  - A partial file cannot replace `Generate()`, so `--force` erases all of this.
  - In 22, the documented derivation changed what the pin meant: `WithUncertaintyMm3(5.0)` became a fraction and the constructor threw.
- **Verdict:** confirmed in 21, 22, 24, 29, 30, 32, 34 and 39. Partly in 25, 26, 28, 31, 37, 38 and 40.
  - Overwrite on `--force` is a documented, deliberate boundary (ADR-0056; the CLI page warns; the file header says so).
  - The `Any.Combine` form kept in your own file does survive `--force`, but it drops the scaffolded `With…` surface.
  - In 26 the rule was a constructor guard, so `--force` brought back the blocking TODO: the loss was not silent.
  - A simple local derivation in `Generate()` is a two-line edit. Only keeping pins meaningful needs the large edit.
  - No page and no ADR addresses what happens to a derived parameter's pins. Most verifiers expect a documentation fix rather than a solver (ADR-0046, ADR-0060).

#### CLI-9 — Refining a composed part restarts from the part's own base, silently dropping the outer base's rule
<a id="cli-9"></a>
- **Kind / severity:** friction (one verifier: defect against spec D13), medium. Reported high in 28.
- **Met in:** 23 (D, R, E), 25 (R), 26 (E), 28 (R), 29 (D, R, E), 30 (R, E), 31 (E), 37 (R). Findings 23-2, 23-9, 25-7, 26-12, 28-3, 29-12, 30-10, 31-3, 37-5.
- **Minimal repro (23):**
  ```csharp
  // AnyKinshipPolicy.AnyValidMaximumPairKinship() => new AnyKinshipCoefficient().WithValue(v => v.LessThanOrEqualTo(0.25));
  new AnyKinshipPolicy().WithMaximumPairKinship(k => k.WithValue(v => v.GreaterThanOrEqualTo(0.125))).Generate();
  ```
- **Expected:** The refinement composes with the outer base (a draw in [0.125, 0.25]), or the docs or an analyzer say plainly that it restarts from the inner type's base.
- **Actual:** `ArgumentOutOfRangeException: A policy may not tolerate kinship above 0.25` (drew 0.72). Measured rates elsewhere:
  - 29: 142 of 2,000 pools had a field below the parent's ≥ 4, and 34 threw. A contradictory `LessThan(4)` was not reported.
  - 25: 710 of 2,000 draws threw.
  - 28: about 2 runs in 3 named the wrong parameter.
  - The same happens with "last call wins" across recipe methods (26, 31).
- **Verdict:** partly in most contexts.
  - Generated `With…(Func)` always calls `configure(AnyValidX())`. That is documented: "what you set replaces what the base set for that part", "The last call wins … starts again from the base" (ADR-0109).
  - But the next bullet, "One that builds on its argument never widens the base", is stated without conditions. It is false for nested parts.
  - The documented reuse route is to keep the refinement as a `Func` and compose it.
  - The 29-12 verifier rated it a defect against spec D13 ("it never replaces the base").
  - No analyzer warns, and JD034 cannot see inside lambdas.

#### CLI-10 — An interface-typed composed parameter becomes `new AnyIRowElement()`, which `any` refuses to scaffold
<a id="cli-10"></a>
- **Kind / severity:** gap, medium
- **Met in:** 36 (D, R, E). Finding 36-2.
- **Minimal repro:**
  ```csharp
  public interface IRowElement { int Produces { get; } }
  public sealed class RowInstruction { public RowInstruction(IReadOnlyList<IRowElement> elements) { … } }
  // any generate RowInstruction
  // dotnet build
  // any generate IRowElement
  ```
- **Expected:** A TODO naming the abstract element type, ideally with its concrete implementers, or a documented note next to the generic-type case.
- **Actual:** `error CS0246: 'AnyIRowElement' could not be found`. Following the documented "work list", `any generate IRowElement` then fails with: `✗ IRowElement is abstract, so Generate() has nothing to instantiate. Scaffold a concrete type that derives from it instead.` (exit 1).
- **Verdict:** partly. The blind `new Any{T}()` is intended (spec §5.4, ADR-0089), and a hand-written `AnyIRowElement : IAny<IRowElement>` resolves it. Neither the user page nor the spec covers a composed interface or abstract type, and the tool's refusal points away from the fix.

#### CLI-11 — Entry-point class `Any{T}Entry` collides with the generator of a type named `{T}Entry`
<a id="cli-11"></a>
- **Kind / severity:** bug, high
- **Met in:** 33 (D, E). Finding 33-1.
- **Minimal repro:**
  ```
  # domain: VoiceEntry and Voice(int n, IReadOnlyList<VoiceEntry> e) in one namespace
  any generate VoiceEntry Voice --entry-point any
  dotnet build
  ```
- **Expected:** An entry class name that cannot clash, or detection that refuses or warns and names both files.
- **Actual:** Exit 0 with `✓ AnyVoice.Entry.cs — entry point Any.Voice()`. Then `AnyVoice.Entry.cs(10,21): error CS0260: Missing partial modifier on declaration of type 'AnyVoiceEntry'`, followed by a cascade of CS0714, CS0708, CS0710 and CS0722 pointing at the generator. `--dry-run` with the clash already present also exits 0 with no warning.
- **Verdict:** confirmed. `EntryPointEmitter.cs:184` emits `{GeneratorName}Entry` with no collision check. The only reserved name is the root `Any`. This is not documented as a boundary. (The `--force` part of the report was not tested.)

#### CLI-12 — A short type name that clashes with a framework type refuses the type and fails the batch
<a id="cli-12"></a>
- **Kind / severity:** friction, low
- **Met in:** 27 (D, R, E), 28 (E), 31 (D, R, E). Findings 27-3, 28-8, 31-4.
- **Minimal repro:**
  ```
  any generate Point TrainId --dry-run
  # domain Point vs System.Drawing.Point; likewise Vector3, Container
  ```
- **Expected:** Types from referenced projects ranked above framework assemblies, and the failure repeated in a closing summary.
- **Actual:** `✗ Point: several types answer to that name. … Name the one you mean in full.` The other types are written and the batch exits 1. The refusal appears once, at the top of pages of output.
- **Verdict:** confirmed. It matches spec §3.2 and §7. `TypeLookup` prefers source types only from the analysed project, so a domain type in a referenced project ties with the BCL type. The workaround (the full name) is in the message, but the user page never mentions the refusal. Side observation (31): when the project reference did not resolve, `Container` silently scaffolded `System.ComponentModel.Container`.

#### CLI-13 — Regex-guarded parameters: the TODO omits the pattern, and switching to `StringMatching` needs a hand retype
<a id="cli-13"></a>
- **Kind / severity:** friction, low (medium in 37)
- **Met in:** 21 (R), 24 (R, E), 26 (D), 27 (R), 34 (R, E), 37 (D, R), 38 (D, R), 40 (R, E). Findings 21-6, 24-6, 26-7, 27-8, 34-7, 37-3, 37-4, 38-7, 40-5.
- **Minimal repro:**
  ```csharp
  // any generate CultureId, then replace the body:
  return Any.StringMatching("CH(0[1-9]|1[0-2])-C[0-9]{4}");
  ```
- **Expected:** A one-line edit, or a TODO that names the retype and quotes the `[GeneratedRegex]` pattern.
- **Actual:**
  - CS0029, then CS1503, until both the factory return type and `Func<AnyString, AnyString>` are retyped to `AnyPattern`. For `.As(...)` bases the error is CS0266 to `IAny<…>`.
  - The note quotes `if (!match.Success)` or `if (!Pattern().IsMatch(x))`, never the pattern.
  - Five times per domain in 38. In 40, a blind find-and-replace breaks a second string overload.
- **Verdict:** confirmed or partly everywhere.
  - Not reading the regex is deliberate: spec §5.3 says most validation regexes leave the supported subset; reading them is a v1.1 candidate.
  - The CLI page documents the exact two-line retype, and the TODO's landing section links to it.
  - "Emit `AnyPattern`" is refuted by design.
  - Two improvements stay compatible with the spec: quoting the `GeneratedRegex` pattern in the comment, and mentioning the retype in the TODO itself.

#### CLI-14 — A guard through a value-returning helper is silent and reported as fully inferred
<a id="cli-14"></a>
- **Kind / severity:** friction (filed as defect, high), medium
- **Met in:** 22 (D, R, E), 21 (D, R, E — the `ThrowIfOutside` helper). Findings 22-1, 21-1.
- **Minimal repro:**
  ```csharp
  public SpatialPoint(double xMm) { XMm = Guard(xMm, nameof(xMm)); }
  private static double Guard(double v, string n) { if (Math.Abs(v) > 2000.0) throw new ArgumentOutOfRangeException(n); return v; }
  // any generate SpatialPoint
  ```
- **Expected:** `to verify` plus the compile-breaking TODO, because a helper that receives the parameter decides whether it is accepted.
- **Actual:** `xMm double Any.Double()` with no mark, and "1 of 1 parameters inferred". The file compiles, and 100 of 100 draws throw. A void helper called for its effect is marked as expected.
- **Verdict:** partly, re-rated friction.
  - Documented as silent: CLI page "What the tool still cannot see stays silent"; spec §9 (a returning validator cannot be told apart from normalisation); ADR-0086's exception covers only named libraries.
  - This is the idiomatic value-object guard, and the result is a file that looks finished but is broken. It is a candidate ADR-0085 field report, e.g. for reading a same-type private helper whose body visibly throws.

#### CLI-15 — Guard shapes missing from the closed recognised set
<a id="cli-15"></a>
- **Kind / severity:** gap / friction, low to medium (per shape)
- **Met in:** 25, 26, 27, 29, 30, 31, 33, 34, 35. Findings 25-2, 26-1, 26-5, 27-6, 29-7, 30-4, 31-6, 33-3, 34-3, 35-5.
- **Repro, expected and actual per shape:**

| Shape | Contexts (profiles) | Expected | Actual | Verdict |
|---|---|---|---|---|
| TimeSpan bounds: `ThrowIfLessThan(e, TimeSpan.Zero)`, `t < TimeSpan.FromSeconds(5)` | 26 (D, R, E) | `Any.TimeSpan().GreaterThanOrEqualTo(TimeSpan.Zero)` | `Any.TimeSpan() to verify` even as separate guards | Partly → gap (medium): `TimeSpan` operands are not C# constants. The finding's decimal claim is wrong: split decimal guards are read. |
| `ThrowIfGreaterThan(text.Length, 500)` | 34 (D, R, E) | `.WithMaxLength(500)` | `Any.String().NonEmpty() to verify` | Confirmed gap (medium): the helper is read only when its first argument is the parameter itself. The `if (p.Length > N)` spelling is read. |
| `decimal.Round(v, 2) != v` | 29 (E), 30 (E) | `.WithScale(2)` | `to verify, guard, unread guards` | Confirmed gap (low): no row in the closed set. |
| `lockedAt == default` on `DateTimeOffset` | 27 (R, E) | `.DifferentFrom(default)` | `Any.DateTimeOffset() to verify` | Confirmed friction (low). |
| `IndexOfAnyExcept("ACGTN") >= 0` | 35 (R) | `.WithChars("ACGTN")` | `NonEmpty() to verify` | Confirmed friction (low): no character-set inference exists. |
| Standalone `double.IsNaN(x)`, `!(x > 0)`, `double.IsInfinity(x)` | 25 (D, R, E), 31 (R), 33 (R), 40 | no constraint (NaN and infinities are never drawn) / `.Positive()` | unread | Confirmed / partly: the set has a `!double.IsFinite` row only. |
| `quarter is { } q && !Enum.IsDefined(q)` on `Q?` | 26 (D) | `.AsNullable() guard` | `to verify` | Partly: excluded by the `&&` rule (spec §5.3). The spec names this "validate what is present" cost as deliberate. |

- **Verdict (overall):** every shape is outside spec §5.3's closed set and is refused loudly, so none is a bug. Adding rows is a deliberate widening that needs a decision (ADR-0082, ADR-0085). The TimeSpan and `.Length`-helper rows are the most costly.

#### CLI-16 — The default `NonEmpty()` drops a legal empty string, and the row is tagged `guard`
<a id="cli-16"></a>
- **Kind / severity:** friction (filed as defect), low
- **Met in:** 35 (D, R, E). Finding 35-4.
- **Minimal repro:**
  ```csharp
  public Allele(string bases) { ArgumentNullException.ThrowIfNull(bases); if (bases.Length > 10_000) throw …; }
  ```
- **Expected:** `Any.String().WithMaxLength(10000)`, or a recap that marks `NonEmpty()` as a default.
- **Actual:** `bases string Any.String().NonEmpty().WithMaxLength(10000) guard`, with no `to verify`. The empty allele, which is valid and common, is never drawn.
- **Verdict:** partly. `NonEmpty()` is the specified string default (spec §5.2), and the per-row `guard` tag follows §6. The real UX issue is that a per-row tag mixes the engine's default with what the guard added.

#### CLI-17 — A parameter's valid base `AnyValid…()` is private
<a id="cli-17"></a>
- **Kind / severity:** friction, low
- **Met in:** 21 (R), 24 (D), 29 (E), 30 (R), 40 (D). Findings 21-5, 24-7, 29-9, 30-7, 40-10.
- **Minimal repro:**
  ```csharp
  AnyExperimentalConditions.AnyValidTemperatureCelsius().Generate(); // CS0122
  ```
- **Expected:** A reachable base, or a documented idiom.
- **Actual:** Users drew the whole aggregate and read one property, restated the range, or edited the visibility by hand (which `--force` would revert).
- **Verdict:**
  - Confirmed in 21; partly elsewhere.
  - Private is specified (spec §4.2).
  - The class is `partial`, so a one-line `internal static` wrapper in a neighbouring file works, survives `--force`, and passed a test.
  - That use of the documented extension point is not shown anywhere.

#### CLI-18 — Generators default to the domain's production namespace
<a id="cli-18"></a>
- **Kind / severity:** friction, low
- **Met in:** 24 (R), 26 (R), 32 (D), 35 (R), 37 (E), 40 (E). Findings 24-9, 26-10, 32-9, 35-6, 37-10, 40-12.
- **Minimal repro:**
  ```
  cd Tests.Regular && any generate Animal --entry-point static:Arbitrary
  ```
- **Expected:** The test project's namespace, or a hint toward `any.json`.
- **Actual:** `namespace RoboticMilking.Domain;` in both files.
- **Verdict:**
  - Partly everywhere. The default is deliberate (ADR-0062, spec D8): call sites need no extra import, and the engine has no MSBuild knowledge (ADR-0065).
  - The overrides are documented: `--namespace`, the `any.json` `namespace` key, and `--entry-point-namespace`, which moves only the root (26-10).
  - The only valid part: the recap never names the namespace it wrote into.

#### CLI-19 — Nullable parameters: a never-null base, a null pin that needs a cast, a hand-written "sometimes null"
<a id="cli-19"></a>
- **Kind / severity:** friction, low (reported medium in 23)
- **Met in:** 23 (D, R, E), 26 (R, E). Findings 23-3, 26-11.
- **Minimal repro:**
  ```csharp
  Any.Animal().WithLastMilkedAt(null);               // CS0121
  Any.Animal().WithLastMilkedAt(t => t.OrNull());     // CS0029 / CS1662
  ```
- **Expected:** A readable null pin (e.g. `WithoutLastMilkedAt()`) and a generated "sometimes null". In 23, a TODO hint that the two parameters are tied all-or-nothing.
- **Actual:** `(DateTimeOffset?)null` is required. "Sometimes null" needs a hand-written partial overload. In 23, the default always draws both parents.
- **Verdict:**
  - Confirmed in 26; partly in 23.
  - All of this is deliberate and documented: ADR-0064 never draws null; CLI page "Sometimes null"; `.WithSireId((StudbookId?)null).WithDamId((StudbookId?)null)` works with no edits.
  - In 23 the default passes the constructor; "unregistrable" is a Pedigree rule.

#### CLI-20 — A prepared generator cannot be passed to `With…`, and the feedback is a bare compiler error
<a id="cli-20"></a>
- **Kind / severity:** friction, low
- **Met in:** 21 (D, R), 25 (E). Findings 21-3, 25-11.
- **Minimal repro:**
  ```csharp
  Arbitrary.Culture().WithConditions(g); // g is an AnyExperimentalConditions → CS1503
  Any.PlaybackSession().WithLadder(_ => Ladder); // compiles, silently replaces the base
  ```
- **Expected:** A hint in the generated XML doc or a diagnostic pointing to the `Func<AnyX, AnyX>` form, and one natural way to reuse a named recipe.
- **Actual:** Only the compiler's "cannot convert". The `_ => recipe` workaround is the very base replacement ADR-0109 set out to prevent, with no warning.
- **Verdict:** confirmed. By design (ADR-0109). The `Func` route is documented only on the CLI page.

#### CLI-21 — Single-parameter scaffolds raise CA1822 on their `With…` methods
<a id="cli-21"></a>
- **Kind / severity:** friction, low
- **Met in:** 21 (E), 29 (E), 32 (E). Findings 21-7, 29-8, 32-8.
- **Minimal repro:**
  ```
  any generate GrowthRate
  dotnet build -p:ErrorLog=out.sarif
  ```
- **Expected:** No analyzer noise on freshly scaffolded code.
- **Actual:** Two CA1822 notes per type, on both `WithX(value)` and `WithX(Func)`.
- **Verdict:** confirmed. Note level only, so the console shows 0 warnings. The emitter has no single-parameter case and no suppression.

#### CLI-22 — The scaffolder copies a named constant's value instead of referencing it
<a id="cli-22"></a>
- **Kind / severity:** friction, low
- **Met in:** 38 (R). Finding 38-6.
- **Minimal repro:**
  ```csharp
  ThrowIfGreaterThan(projectId, MaxProjectId); // public const int MaxProjectId = 999_999
  ```
- **Expected:** `.LessThanOrEqualTo(CreditSerial.MaxProjectId)`.
- **Actual:** `.LessThanOrEqualTo(999999)`. Because the file is scaffolded once (D1), the copy goes stale when the domain changes the constant.
- **Verdict:** confirmed. Folding is still needed for non-accessible constants.

#### CLI-23 — A pinned mutable aggregate is shared between `Generate()` calls
<a id="cli-23"></a>
- **Kind / severity:** friction, low
- **Met in:** 25 (R). Finding 25-8.
- **Minimal repro:**
  ```csharp
  AnyPlaybackSession g = Arbitrary.PlaybackSession().WithBuffer(new PlaybackBuffer(10, 60));
  PlaybackSession a = g.Generate(), b = g.Generate();
  a.Buffer.Drain(5); // b.Buffer changes too
  ```
- **Expected:** A note in the pin's doc comment or on the CLI page.
- **Actual:** Silent aliasing, because `FixedValue<T>` returns the same instance.
- **Verdict:** confirmed. The behaviour is intended (spec §4.2). The aliasing is undocumented.

#### CLI-24 — Accepting a `to verify` recipe leaves a stale five-line comment per parameter
<a id="cli-24"></a>
- **Kind / severity:** friction, low
- **Met in:** 25 (D), 33 (D). Findings 25-6, 33-7.
- **Minimal repro:**
  ```
  any generate … (9 types)
  ```
  This produces 17 to 21 blocks, each a 5-line comment plus `_ = TODO_verify_…;`.
- **Expected:** A shorter marker, or a way to accept recipes in bulk.
- **Actual:** Users scripted the deletion.
- **Verdict:** partly. Only the one `_ =` line has to go, as the comment, the CLI page and spec §5.6 say. The recap, `--format json` and the build errors already list every open guard. What remains is the leftover comment.

#### CLI-25 — `--dry-run` prints a ✓ before "Nothing written"
<a id="cli-25"></a>
- **Kind / severity:** friction, low
- **Met in:** 27 (E). Finding 27-10.
- **Minimal repro:**
  ```
  any generate Interlocking --dry-run
  ```
- **Expected:** Wording such as "would write AnyInterlocking.cs".
- **Actual:** `✓ AnyInterlocking.cs — 4 of 4 parameters inferred, 3 to verify. … Nothing written: --dry-run.`
- **Verdict:** partly. The tick reports the inference outcome, and the output follows spec §6.

#### CLI-26 — A collection parameter's element recipe cannot be refined through the lambda
<a id="cli-26"></a>
- **Kind / severity:** friction, low
- **Met in:** 29 (D, R). Finding 29-4.
- **Minimal repro:**
  ```csharp
  Any.Stake().WithCombination(c => c.WithHorses(l => /* elements Between(1, fieldSize)? */ l))
  ```
- **Expected:** Element refinement, or a doc example for this case.
- **Actual:** A partial-file overload was needed.
- **Verdict:** partly. Documented ("When the lambda is not enough", ADR-0109), with that exact route. The remaining wish is an example of an element rule that depends on another aggregate.

### Analyzers

#### AN-1 — Info-level JD diagnostics never appear in `dotnet build` output
<a id="an-1"></a>
- **Kind / severity:** friction, low
- **Met in:** 21 (E), 26 (E), 33 (E), 34 (D, E), 36 (E), 40 (D). Findings 21-8, 26-14, 33-10, 34-6, 36-9, 40-11. (38-8 was refuted on the same ground.)
- **Minimal repro:**
  ```csharp
  Any.String().NotBlank().Generate();
  // dotnet build
  ```
- **Expected:** A visible hint, or documentation of how to surface it.
- **Actual:** 0 warnings in the console. JD030 or JD031 appear only in a SARIF log or an IDE.
- **Verdict:** partly. This is MSBuild's handling of Info, and the default severities are deliberate.
  - `analyzers/README.md` (lines 19–30) and `packages/justdummies.en.md` (lines 51–60) already give the `.editorconfig` remedy.
  - What remains is discoverability: the packed nupkg readme, the CLI page (line 273, which cites JD030) and the JD030/JD031 pages do not mention or link to that remedy.
  - Side observation (36): the JD030 message says "1 to 1025 characters" while the README says 0 to 1024.

#### AN-2 — JD009 does not see a static-field draw made through a scaffolded generator
<a id="an-2"></a>
- **Kind / severity:** friction, low
- **Met in:** 26 (R). Finding 26-9.
- **Minimal repro:**
  ```csharp
  [assembly: Reproducible]
  private static readonly MilkingSettings S = Arbitrary.MilkingSettings().WithCleaningTime(TimeSpan.FromSeconds(10)).Generate(); // nothing
  private static readonly int C = Any.Int32().Generate(); // JD009
  ```
- **Expected:** A warning, or a documented mention.
- **Actual:** No diagnostic for scaffolded entry points.
- **Verdict:** partly. The finding named JD007, but the relevant rule is JD009. Its page describes this hazard and documents in general terms that generators "not written inline from Any" are not flagged. It does not name scaffolded generators, which carry most draws in an `any` project.

### xUnit adapter

#### XU-1 — The pasted replay attribute does not compile when `[Reproducible]` is assembly-level
<a id="xu-1"></a>
- **Kind / severity:** friction, low
- **Met in:** 28 (E). Findings 28-9 (also the caveat in 28-10).
- **Minimal repro:** `[assembly: Reproducible]` in `AssemblyInfo.cs`, and a failing test in a file without `using JustDummies.Xunit;`. Paste the reported `[Reproducible(Seed = N)]`.
- **Expected:** A snippet that compiles as pasted (fully qualified), or one that names the namespace.
- **Actual:** `error CS0246: The type or namespace name 'ReproducibleAttribute' could not be found`.
- **Verdict:** confirmed. `ReplaySnippet` prints the bare form, and the package adds no global using. The IDE quick-fix resolves it in seconds.

#### XU-2 — A failure report gives the seed but never the drawn values
<a id="xu-2"></a>
- **Kind / severity:** gap, low
- **Met in:** 32 (E). Finding 32-7.
- **Minimal repro:** A `[Fact, Reproducible]` that fails for some draws of `Any.Double().GreaterThanOrEqualTo(1.0).LessThan(2.0)`.
- **Expected:** An optional way to see what was drawn.
- **Actual:** Only the seed line.
- **Verdict:** confirmed. No ADR decides against it. The report that "a probe draw shifted the sequence" is expected behaviour of a sequential stream: logging the already-drawn variable avoids it.

### Documentation

#### DOCS-1 — The user docs do not list which guard shapes `any` reads, or say that `||`/`&&` conditions are unread
<a id="docs-1"></a>
- **Kind / severity:** defect / gap, low per item (high in aggregate, because it underlies [CLI-1](#cli-1))
- **Met in:** 23 (D, R), 25 (D, E), 28 (D, E), 29 (D, R, E), 31 (D), 33 (R), 36 (R), 39 (R), plus the documentation halves of 21-1, 24-1 and 32-1. Findings 23-4, 25-3, 28-2, 29-2, 31-5, 33-8, 36-7, 39-2.
- **Minimal repro:**
  ```
  grep -nE '\|\||&&' doc/handwritten/for-users/packages/justdummies-cli.en.md   # no match
  ```
- **Expected:** A short user-facing table of the shapes that are read (and what each becomes) and those that are not, with the tip to split a disjunction into separate `if`s.
- **Actual:**
  - "What the last column means" gives one positive example (`quantity <= 0` → `.Positive()`) and lists only shapes it cannot see.
  - It then sends readers to spec §9 (non-goals), while the recognised set is in §5.3.
  - The emitted TODO calls the section "What the scaffolder reads, and what it cannot".
- **Verdict:** confirmed in all eight. The rules (one parameter, no `&&`/`||`, constant operands, before the first write, the helper table) exist only in the maintainer spec §5.3.

#### DOCS-2 — The CLI page's suggested layout hides the `Arbitrary` root behind its own namespace
<a id="docs-2"></a>
- **Kind / severity:** defect, medium
- **Met in:** 25 (R). Finding 25-4.
- **Minimal repro:**
  ```
  any.json: { "entryPoint": "static:Arbitrary", "namespace": "AdaptiveVideo.Tests.Regular.Arbitrary" }
  ```
  Then, in a test in `AdaptiveVideo.Tests.Regular`: `Arbitrary.Bitrate().Generate()`. The page's own example is `--entry-point-namespace Shop.Tests.Arbitrary`.
- **Expected:** `Arbitrary.Bitrate()` resolves, or the docs and the tool steer away from this layout.
- **Actual:** `error CS0234: The type or namespace name 'Bitrate' does not exist in the namespace '…Arbitrary'`, and the tool gives no warning.
- **Verdict:** confirmed. The layout appears at lines 187 and 559 of the CLI page, and the French twin has the same two. C# name lookup finds the child namespace first. The tool already has a shadowing warning and a reserved-name refusal, but neither catches this case.

#### DOCS-3 — The default span of unconstrained `DateOnly`, `TimeOnly` and `DateTimeOffset` draws is not documented
<a id="docs-3"></a>
- **Kind / severity:** gap / friction, low (reported medium in 30 and 38)
- **Met in:** 23 (D, R), 26 (D), 30 (D, R, E), 37 (D), 38 (D). Findings 23-6, 26-6, 30-6, 37-9, 38-5.
- **Minimal repro:**
  ```csharp
  // 100,000 draws
  Any.DateOnly().Generate().AddYears(6);
  ```
- **Expected:** One sentence per type stating the span, plus a hint (on the page or in the scaffold recap) to bound domain instants before doing arithmetic.
- **Actual:**
  - `dates-and-times.en.md:89` states `MinValue..MaxValue` for `DateTime` only.
  - Measured overflow rates: `AddYears(6)` failed on 68 of 100,000 draws, `AddMonths(120)` on 104 of 100,000.
  - The scaffold emits a bare `Any.DateOnly()` / `Any.DateTimeOffset()` with no hint.
- **Verdict:**
  - Confirmed in 30 and 37; partly in 23, 26 and 38.
  - The `DateTime` sentence already exists.
  - In 26 and 38 the reported risk is overstated: `AddHours(1)` never failed in 1,000,000 draws. Only larger offsets bite.

#### DOCS-4 — The "declared once" list omits `WithChars` and `WithCount`
<a id="docs-4"></a>
- **Kind / severity:** defect / friction, low
- **Met in:** 35 (E), 31 (D). Findings 35-2, 31-11.
- **Minimal repro:**
  ```csharp
  Func<AnyString, AnyString> refine = b => b.WithChars("AC");
  refine(Any.String().WithChars("ACGT")); // throws ConflictingAnyConstraintException
  ```
- **Expected:** `WithChars` (and the alphabet families, casing and prefix/suffix) and `WithCount` listed with the other declare-once constraints.
- **Actual:** The list at `justdummies-cli.en.md:343-346` names only `OneOf`, `WithLength`, `MultipleOf` and `WithScale`. JD032's page does say `WithCount` is declared once.
- **Verdict:**
  - Confirmed in 35.
  - Partly in 31: a refining `WithCount(3)` over a base `WithCountBetween(2, 6)` does narrow, and that is documented. The gap is the incomplete list.
  - The doc fix should not promise an `Except`-style narrowing for alphabets, because none exists ([CORE-8](#core-8)).

#### DOCS-5 — No guidance on drawn values that reach the assertion only as identity handles or echoes
<a id="docs-5"></a>
- **Kind / severity:** gap, low
- **Met in:** 27 (E), 34 (D), 35 (D), 38 (R, E). Findings 27-9, 34-9, 35-8, 38-3.
- **Minimal repro:**
  ```csharp
  Assert.Same(anyBuyer, ledger.HolderOf(unit));
  Assert.Equal([anyJunction.Junction, anyJunction.Main], routeLock.RemainingSections);
  ```
- **Expected:** The guides say whether a drawn identity used as a handle, or echoed into the output, may appear in an assertion.
- **Actual:** Only the blanket rule ("If an anyXxxx reaches your assertion, it is not a dummy", getting-started:248).
- **Verdict:**
  - Confirmed in 27 and 34.
  - Partly in 35 and 38: core-concepts' test ("whether the outcome depends on it") settles the case implicitly. In 35 the echoed accession is not a dummy by that rule. In 38 the classification is genuinely ambiguous.
  - No page names the case.

#### DOCS-6 — No guidance on bounding a draw by a value computed from other draws
<a id="docs-6"></a>
- **Kind / severity:** gap, low
- **Met in:** 21 (D). Finding 21-9.
- **Minimal repro:**
  ```csharp
  WithCellsPerMillilitre(c => c.GreaterThanOrEqualTo(1).LessThan(capacity)) // capacity from drawn conditions
  ```
- **Expected:** composition or core-concepts shows the pattern and says where a dependent bound belongs.
- **Actual:** No page shows it.
- **Verdict:** partly. The CLI page section "When a guard ties two parameters" and design-principles already say where a dependent rule belongs. What is missing is whether a call-site computed bound is idiomatic, plus a cross-link from the composition pages.

#### DOCS-7 — The distribution of floating-point draws inside an interval is not stated
<a id="docs-7"></a>
- **Kind / severity:** gap, low
- **Met in:** 21 (D). Finding 21-10.
- **Minimal repro:** `Any.Double().Positive().LessThanOrEqualTo(50)` — how close to 0 can it get?
- **Expected:** The numbers page states the lower end, or that floats are uniform by value within the clipped interval.
- **Actual:** Only the upper magnitude is described.
- **Verdict:** partly. The 1e-300 risk is refuted. Draws are uniform by value: the minimum over 2,000,000 draws was 3.55e-05, and none fell below 1e-6. The remedy is to declare the bound.

#### DOCS-8 — Getting-started never mentions `any`
<a id="docs-8"></a>
- **Kind / severity:** friction, low
- **Met in:** 39 (D). Finding 39-8.
- **Minimal repro:** Start from `guides/getting-started.en.md` and try to scaffold a generator.
- **Expected:** A short path from getting-started to a first scaffold.
- **Actual:** No link. The CLI page is 28.7 KB (the finding said ~36 KB).
- **Verdict:** partly. The page's first 40 lines already give install and a first run. The user README links the CLI as step 3. The missing link from getting-started is real.

---

## 4. What works well

Verified praise, deduplicated:

- **Build-blocking `TODO_verify` lines make an honest, precise work list.** All 20 contexts, all profiles: 21-11, 22-7, 23-8, 24-11, 25-12, 26-16, 27-11, 28-11, 29-11, 30-8, 31-12, 32-11, 33-11, 34-10, 34-11, 35-9, 36-10, 37-11, 38-10, 39-9, 40-13.
  - Every guard the tool cannot vouch for becomes a CS0103 error at the exact factory.
  - Each comes with a comment quoting the guard, its file and its line, plus a link to the docs.
  - Readable guards are inferred correctly: `ThrowIfNullOrWhiteSpace` → `NotBlank()`, length → `WithMaxLength`, `Guid.Empty` → `NonEmpty()`, and the ThrowIf range helpers.
  - No session shipped a silently invalid generator.
  - Caveats: only the first guard per parameter is named ([CLI-3](#cli-3)); a missing `AnyX` dependency (CS0246) hides the TODO errors until that dependency is scaffolded.
- **One command scaffolds a whole graph that composes through itself.** 22-7, 24-10, 26-16, 30-8, 33-12, 34-10, 34-12, 39-10.
  - Composed parameters are drawn through their own generators (`new AnyPorosity()`, `Any.ListOf(new AnyRockCell())`), so each invariant has one home.
  - `--entry-point any` (C# 14 `extension(Any)`) and `static:Arbitrary` both read like the built-in generators.
  - Caveat: only `static:Arbitrary` produces a shared partial root (39-10).
- **Pin/refine `With…` pairs and nested lambdas read like the domain.** 21-12, 22-8, 24-10, 25-13, 26-17, 27-12, 30-10, 31-14, 33-13, 34-13, 35-10, 36-11, 37-13, 38-11, 39-11, 39-13, 40-14; 23-9 and 29-12 hold with caveats.
  - Load-bearing values stay literal and everything else stays drawn.
  - A refining `Between` narrows the scaffolded base. A contradiction fails fast with a message naming both sides.
  - Refinements are reusable as `Func<AnyX, AnyX>` or method groups. `IAny<T>` is covariant. `Any.Combine` composes aggregates.
  - Caveat: nested refinement restarts from the inner base ([CLI-9](#cli-9)).
- **`[Reproducible]` reports a paste-ready seed and replays exactly, with no setup.** 21-13, 22-10, 23-12, 24-13, 25-15, 26-15, 28-10, 29-14, 30-9, 31-15, 32-10, 33-14, 34-14, 36-12, 37-12, 39-12, 40-15.
  - One assembly or class attribute is enough. Each case gets its own seed. Green runs stay silent.
  - Replay holds through nested scaffolded graphs and hand-written `Generate()`/`Combine` code (36-12, 40-15).
  - Drawn dummies exposed real hidden dependencies, as the fundamentals promise:
    - the 5 h⁻¹ growth-rate ceiling (21);
    - a single-milkable-teat phase quirk (26);
    - a pinned miss vector parallel to a drawn velocity (28);
    - a solver that diverges at acceleration ≥ ~1.87, which `SolverSettings` accepts (32);
    - a test-setup stability error (39);
    - several assertion and arithmetic slips.
  - Side observation from 26-15: the expert's committed test `Brushing_a_first_teat_starts_the_preparation` carries the unconstrained single-teat assumption and fails about 1 run in 4.
- **The CLI page's retyping walkthrough is exact.** 22-9, 23-10, 24-12, 28-12, 31-13, 32-12, 34-15, 38-12. "When the replacement is another kind of generator" names exactly the two lines to retype for `AnyPattern`, and the `IAny<T>` case for `.As(...)`. The `StringMatching(...).As(body => body + checkDigit)` recipe fitted ISO 6346 in one line. The docs also warned about JD007/JD009 constructor draws in advance.
- **Partial companion files survive `--force`.** 23-11, 36-11. Hand-written recipes such as `AsFounder` and `WithParents` go through the private constructor and are left untouched by a re-scaffold. Caveat: edits inside the scaffolded `AnyValid…()` bodies are reverted.
- **Distinctness by construction.** 23-13, 24-14, 29-13, 30-11, 32-13.
  - `SetOf(...).WithCount(n)`, `ArrayOf(...).Distinct()` over value objects, and `SetOf(...).As(sort)` for strictly increasing collections work without retries in user code.
  - The library's internal redraws are bounded and fail explicitly when they run out.
  - `Enum.DifferentFrom` and `Distinct(comparer)` behave as documented.
  - Caveat (23-13): JD028 stays silent on scaffolded generators whatever their equality, so its silence is not evidence that `Equals` was checked.
- **The constraint vocabulary can be guessed.** 28-12, 30-11. `AfterOrEqualTo`, `WithOffset`, `Boolean().False()`, `WithCountBetween`, `NotBlank()`, `WithScale(2)` and `Alpha().InUpperCase().WithLength(3)` compiled on the first try from the guide pages.
- **The docs on tied parameters and on "Arbitrary does not mean dummy" answered real questions.** 25-14. Caveat: "disjoint bases" was the developer's own adaptation, not a documented form.

---

## 5. Refuted findings

- **22-6** — "`.As` into an interface-typed collection needs an explicit lambda return type." Refuted: `IAny<out T>` is covariant, and the exact repro builds and runs. The mismatch came from the scaffolded `AnyList<T>` factory type, and its retype is documented.
- **26-4** — "The unread-guard line points at a readable guard instead of the helper that caused the mark." Refuted: `ThrowIfLessThan` on a `TimeSpan` is itself unread (see [CLI-15](#cli-15)), and the static-helper mark does not spread to that parameter. The cited line is correct.
- **38-8** — "Info-level JD diagnostics are invisible, and nothing in the docs says so." Refuted: `analyzers/README.md` (lines 19–29, with its French twin) and `packages/justdummies.en.md` (lines 51–59) already state it and give the `.editorconfig` remedy. They were added before the trial.
