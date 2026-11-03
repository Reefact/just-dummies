# Expert notes — 34 manuscript reconstruction
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did

**Pages read, in order:** `PROJECT-README.md`, `guides/getting-started`, `guides/core-concepts`, `guides/composition`,
`packages/justdummies-xunit`, `packages/justdummies-cli` (all of it: guards table, `to verify`, "Adjusting a
parameter", "When a guard ties two parameters"), `generators/README`, `strings`, `collections`, `enums-and-choices`
(first half), `numbers` (first half), `guides/reproducibility`, `guides/errors-and-conflicts` (first half),
`packages/justdummies.en.md` (analyzer severity section) and the `Domain/` sources plus `MODEL.md`.

**`any` commands run** (from `Tests.Expert`, after a `dotnet restore`):

```
any generate WitnessSiglum PassageReference Variant Lacuna CopyRelation AttestedReading Witness EditorialHypothesis --entry-point any --output Generators
any generate Tradition --entry-point any --output Generators
```

Result: 8 + 1 generators and their `Any.X()` entry points. Recap: 3 types fully inferred (`PassageReference`,
`Witness` — start/end years drawn as an `Any.OrderedPair` by the tool —, `Tradition`), 6 stopped the build with `to verify`
(`WitnessSiglum` regex, `Variant` x3 parameters, `Lacuna` x2, `CopyRelation` x2, `AttestedReading`, `EditorialHypothesis` x2).

**Generators completed by hand** (in `Tests.Expert/Generators/`):

| Generator | What I wrote |
| --- | --- |
| `AnyWitnessSiglum` | `Any.StringMatching("[A-Z][A-Za-z0-9.]{0,11}")`, retyped `AnyString` -> `AnyPattern` on the four places the tool listed |
| `AnyVariant` | removed the three sentinels; `Generate()` draws kind, text and hand together: an omission has no text, any other kind has some, a correction is by hand 2+. A pinned value is detected with `ReferenceEquals(_x, _xRecipe)` and the others follow it |
| `AnyLacuna` | `from <= to` drawn as `Any.OrderedPair` over the passage's position in the work (`PassageReference` is not a number, so I map it to an `int` and back); `WithFrom`/`WithTo` became plain pins, the lambda overloads were removed |
| `AnyCopyRelation` | `_copy.DifferentFrom(exemplar)` |
| `AnyAttestedReading`, `AnyEditorialHypothesis` | `ListOf(...).WithCountBetween(1, 6).Distinct()`; the hypothesis draws "conjecture or witnesses" together and honours a pin of either |

Plus `Support.cs` (`Scene`: `Reads`, `Omits`, `Corrects`, `Gap`, `Hypothesis`, `TraditionOf`, `DistinctSigla`,
`SiglumOtherThan`, `AnyPassageOtherThan`) and `AssemblyInfo.cs` (`[assembly: Reproducible]`), and a `global.json`
in the domain folder (see friction 1).

**Tests written:** 240 test methods, 379 cases (376 run, 3 skipped), in 17 files:
value objects (`WitnessSiglum`, `PassageReference`, `Variant`, `Lacuna`, `CopyRelation`, `AttestedReading`),
entities (`Witness`, `EditorialHypothesis`), the aggregate split by topic (witnesses, lacunae, variants and
corrections, alignment, proposals, accept/withdraw/reject decisions, shared errors, stemma), and `GeneratorsTests`
which holds the hand-written ties to the constructors they feed. Every refusal named in `MODEL.md` has a test and
the boundary on both sides (`-800/-801`, `1600/1601`, `500/501`, `2000/2001`, hand `0/1/9/10`, closed ranges of
lacunae, strict `<` of "certainly older", ...).

**Result:** green (0 failed, 3 skipped), then 27 further runs with fresh seeds, all green. One draw-dependent failure
happened *during development*, in a test of mine (friction 6), before those 27 runs; the seed replayed it at once and the test was fixed.
The 3 skipped tests are domain defects, kept and marked (see below).

**Domain defects found (kept as skipped tests):**
- `WitnessSiglumTests.A_siglum_cannot_end_with_a_line_feed`: `new WitnessSiglum("A\n")` and `"Vat.3225\n"` are accepted (the
  pattern's `$` matches before a final `\n`); the MODEL says letters, digits or dots only. Deterministic.
- `AttestedReadingTests.A_column_cannot_be_rewritten_through_its_witness_list`: `Witnesses` is the internal array behind an
  `IReadOnlyList`, `((IList<WitnessSiglum>)reading.Witnesses)[0] = x` succeeds and breaks "sorted, distinct". Same shape, by reading the code, in
  `EditorialHypothesis.SupportingWitnesses` (not tested).
- `TraditionVariantsTests.The_recorded_variants_cannot_be_rewritten_through_their_list`: `Tradition.Variants` (and `Lacunae`,
  `CopyRelations`, `Hypotheses`) is the aggregate's own `List<>`; `((IList<Variant>)tradition.Variants).Add(v)` bypasses every
  `RecordVariant` rule.

**Behaviours I did not pin as correct (ambiguous in MODEL.md), recorded for the maintainers of the domain:**
- `Variant`'s 500-character limit is checked on the raw text before normalisation: 501 characters that collapse to 333 are refused.
- `PassageReference.Parse("100.1.1")` throws `ArgumentOutOfRangeException` while `Parse("1.1.99999999999")` throws `FormatException`.
  Leading zeros are accepted (`"03.012.004"`).
- A hypothesis stays valid when a later corrector changes what a cited witness reads: the "every cited witness reads the text" rule
  is checked only at `Propose`.

## Frictions and surprises, in order

1. **`dotnet test --project Tests.Expert` does not run.** Exact output:
   `MSBUILD : error MSB1001: Unknown switch. ... --project Tests.Expert ... Switch: --project`. The command in the brief and in the project README (`dotnet test --solution ...`) is the Microsoft.Testing.Platform form, which the .NET 10 SDK only
   honours with `{"test":{"runner":"Microsoft.Testing.Platform"}}` in a `global.json`. No page of `docs/` says so (the xUnit adapter page
   talks about `xunit.v3` 4.0.2 only). I added a `global.json` next to the domain; after that everything worked. Expected: the adapter page
   to list the one-line prerequisite for `dotnet test --project/--solution`.
2. **`any generate` before `dotnet restore`.** `✗ WitnessSiglum: this project has not been restored, so its package references cannot be
   read. Restore it, then run the command again: dotnet restore`. Clear and actionable; I ran the restore and re-ran. Good message, small
   surprise that the tool does not do it itself.
3. **The recap tells the truth, and it is enough to plan the work.** `to verify, guard, unread guards` on 6 of 9 types, with the line of
   the guard, and "ties kind to hand" for the cross-parameter ones. I appreciated that `Witness` came out with an `OrderedPair` already
   and that `WitnessSiglum` named the exact retype to `AnyPattern`. No guess was hidden.
4. **Everything that ties two parameters is mine to write, and the tie plus a pin is the hard part.** `Variant` (kind / text / hand), `CopyRelation`,
   `EditorialHypothesis` (conjecture / witnesses). The doc's advice ("derive one draw from another inside `Generate()`") warns that
   `WithX` pins then stop reaching the draw, but gives no way to *know* whether a parameter was pinned. I used
   `ReferenceEquals(_hand, _handRecipe)`, which works because a lambda refinement stores the recipe in both fields and a pin stores
   `Any.Fixed(...)` in one only. It is an implementation detail of the scaffold I am relying on, and ~30 lines of resolution logic per
   such type. Wish: a scaffolded `IsPinned(...)` or a documented pattern for "a tie that respects pins".
5. **`PassageReference` is ordered but is not a number, so no `OrderedPair`.** `Lacuna(from <= to)` is the textbook case for
   `Any.OrderedPair`, but it exists only on ordered builders. I mapped the passage to its position in the work (`((book-1)*999+(chapter-1))*9999+(line-1)`)
   and back, 15 lines of arithmetic that duplicate the domain's ordering. Wish: an ordered pair over any `IComparable<T>` generator, or
   a `Then`-based recipe in the docs for "a value at or after another".
6. **A drawn kind broke one of my own tests, and the seed made it a 30-second job.** In a run of the whole suite while I was still
   adding tests, `A_reading_of_a_known_witness_is_recorded` failed: `Any.Variant().WithWitness(w).Generate()` had drawn a `Correction`, which the tradition
   refuses without an earlier hand (`A correction in AlYj at 72.316.7297 needs an earlier hand's reading to correct.`). The kind was not a dummy
   there, it was a prerequisite: I replaced it by `Scene.ReadsSomething(...)` which pins `Substitution`. I temporarily put
   `[Reproducible(Seed = -372754612)]` on the test and it failed identically at once. This is the library doing its job; the friction is
   only that a constructor-valid `Any.Variant()` is not an aggregate-valid one, so a helper per "kind of reading" is needed for every aggregate test.
7. **`JD030` on every scaffolded free text.** Raising `JD030` to a warning shows 11 hits, 6 in the scaffolded generators themselves
   (`Any.String().NotBlank()` for `cause`, `editor`, `repository`, `workTitle`, ...) and 5 in my tests. The domain declares no length for
   them, so none is invented, which is the documented stance; but the only way to say "yes, unbounded on purpose" is a suppression. At
   Info it never shows in `dotnet build`, so a team that never raises it never learns of it. Low.
8. **A refinement that implies the kind is invisible to the tie.** `Any.Variant().WithText(t => t.Alpha().WithLength(500))` with the kind
   left drawn can draw an `Omission` and my `Generate()` then forces the text to empty, silently ignoring the refinement (a failing test
   *at the assertion*, not at the call). A pin is understood (non-blank pinned text excludes `Omission`), a lambda refinement is not. I kept
   `.WithKind(Substitution)` in that single test. This is a limitation of my tie code, and of the shape "recipes refined independently, then
   tied at `Generate()`".
9. **`xUnit1025` on `[InlineData(0.0)]` and `[InlineData(-0.0)]`** ("has InlineData duplicate(s)"): xUnit's analyzer, not JustDummies', but it hits
   exactly the tests a boundary-minded user writes. I moved negative zero to its own `[Fact]`.
10. **Seed printed for tests that draw nothing.** With `[assembly: Reproducible]`, the deterministic `WitnessSiglum("A\n")` theory also prints
    `[JustDummies] These arbitrary values were seeded with 267367196 ...` on failure. Harmless, slightly noisy; the brief asks for a seed in the
    `Skip` reason of a defect, which is meaningless for a draw-free test, so I wrote that it fails on every run.
11. **Refusing in the generator hides the refusal of the domain.** My `AnyLacuna` refuses crossed pins itself (`ConflictingAnyConstraintException`
    from `OrderedPair`, naming both sides), so the tests of `Lacuna`'s own guard had to call `new Lacuna(...)`. Correct and by design
    (documented in Composition), but worth knowing when a generator is "smarter" than the constructor.

## What worked well

- **Intent at the call site.** `Any.Variant().WithKind(VariantKind.Correction).WithHand(2)`, `Scene.Gap(witness, "2.5.1", "2.7.10")`: the
  literals left in a test are exactly the ones its assertion is about, and everything else (titles, repositories, causes, rationales, the
  siglum of a witness that must merely exist) is drawn and invisible. Reading a refusal test, the one pinned value is the cause.
- **`Any.SetOf(new AnyWitnessSiglum()).WithCount(n)` and `.DifferentFrom(...)` / `.Except(...)`** gave me sigla that are *guaranteed* distinct or
  absent, so no test of mine can fail on a one-in-thousands collision. `AnyPattern.Except(sequence)` took a LINQ projection as documented.
- **Replay.** A failing drawn value was reproduced with one attribute, the first time, with the exact failing message.
- **The `to verify` build stop.** Not a single scaffolded recipe was silently wrong; the ones I had not read were exactly the ones that would
  have drawn invalid values (a `Correction` by hand 1 in about one draw in thirty-six, a conjecture citing witnesses or a non-conjecture citing none in about one in two, a siglum the pattern rejects).
- **`Witness` came out right**: the tool read `datedFrom > datedTo` and emitted an ordered pair with working `WithDatedFrom`/`WithDatedTo` pins
  (`Witness(…).WithDatedFrom(-800).WithDatedTo(-800)` is exactly the boundary test I wanted).
- **Analyzers stayed quiet** on a 5,000-line test project (no JD0xx at default severity), and the one Info rule that fired was justified.
- **`[assembly: Reproducible]`** in one file: zero ceremony in 240 tests.

## Fluency: 4 / 5
The library and the scaffolder carried the first two thirds of the work with almost no friction: the entry points read well, the
constraints are named after the domain's own words, the recap is honest, and a seed replay turned the one surprising failure into a
one-minute diagnosis. I lose a point on the part every real aggregate has: rules that tie parameters (kind/text/hand, conjecture/witnesses,
from/to over a non-numeric ordered type) are entirely hand-written, the pin-awareness has no supported idiom, and the first command of the
workflow (`dotnet test --project`) fails with an opaque MSBuild error that no documented page explains.
