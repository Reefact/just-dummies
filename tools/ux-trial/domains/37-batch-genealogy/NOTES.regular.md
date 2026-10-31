# Batch genealogy — regular developer notes

## What I did
- Created `Tests.Regular`, restored, scaffolded in one call:
  `any generate LotNumber MaterialCode Quantity LotQuantity Lot Transformation --entry-point static:Arbitrary`.
- Refined the generators: `LotNumber` and `MaterialCode` to `Any.StringMatching(...)` (retyped the factory
  and the `Func<AnyPattern, AnyPattern>` as the CLI page says), `Lot` dates to two disjoint windows
  (manufactured in 2024, expiring in 2026: expiry > manufacture and shelf life <= 3653 days by construction),
  positive quantities on `Lot` and `LotQuantity`. Deleted `AnyTransformation` (see below).
- Hand-wrote `Arbitrary.Custom.cs` (TransformationId, SiteCode, ProductionDay) in the same partial root,
  and a small `Site` test helper that registers released lots and output lots.
- `[assembly: Reproducible]`. 34 tests: value objects, lot lifecycle, transformation rules, recording
  (all-or-nothing), upstream/downstream tracing on a split+merge "diamond", containment.
- `dotnet test --project Tests.Regular`: 34/34 green, then 15 more runs green (no flakiness).

## Frictions and surprises, in order
1. Scaffold recap was immediately useful: every regex-validated type flagged `to verify, unread guards` with
   the guard line (`LotNumber.cs, line 15: if (!match.Success)`). Expected (the doc says regexes are never
   read), but for a value-object-heavy domain it means *every* string type needs the same manual
   three-line retype (factory body, factory return type, `Func<AnyString,AnyString>` -> `AnyPattern`).
   Mechanical; a `--pattern` hint or recognising `[GeneratedRegex]` constants would save it.
2. `LotNumber`'s guard is `Pattern().Match(value)` then `if (!match.Success)`: the recap points at the `if`,
   not at the regex, so I had to open the domain to find the pattern. Fine, but the quoted line alone is
   not enough to fix the recipe.
3. `Lot(manufacturedOn, expiresOn)`: cross-parameter guard, both flagged `to verify` (good). The doc's
   options (start+duration in `Generate()`) get overwritten by `--force`, so I chose disjoint windows in the
   `AnyValid…()` factories instead — survives re-scaffolds, but encodes a calendar into the generator.
4. `Transformation`: 4 of 5 parameters `to verify` (arity by kind, dimension, totals, sub-lots). No
   per-parameter recipe can honour it; I deleted the generator and build transformations explicitly in
   tests (the quantities are the subject of the tests anyway). The scaffold cost nothing, but a hint like
   "this type's guards are mostly cross-parameter; consider not generating it" would have been welcome.
5. "Positive quantity" lives on `Lot` and `LotQuantity`, not on `Quantity` (which accepts zero), so I wrote
   `new AnyQuantity().WithAmount(a => a.Positive())` twice. And a test that refines it,
   `Arbitrary.Lot().WithInitialQuantity(q => q.WithAmount(a => a.LessThan(5m)))`, restarts the amount from
   `AnyQuantity`'s own base and silently drops `Positive()` (documented: "a lambda guarantees where it starts,
   not where you take it"). Probed 20,000 draws: zero never came out, so it is latent, not flaky — but it
   contradicts the intuition from the leaf case ("never widens the base").
6. Generated files land in `namespace BatchGenealogy.Domain` inside the test project (the target's
   namespace). Convenient, but my test namespace could not be `BatchGenealogy.*` anyway because the
   aggregate class is also called `BatchGenealogy` (a domain naming issue, not the library's).

## What worked really well
- One command scaffolded six types plus a shared `Arbitrary` root; the partial root let me add my own
  entry points beside the generated ones with zero friction.
- The non-compiling `TODO_verify_…` lines are the right design: the build was the to-do list.
- `Any.StringMatching` with `.Except(...)` on the pattern ("another material") just worked.
- Pins vs lambdas read well in tests: `Arbitrary.Lot().WithInitialQuantity(Kg(10m))` vs
  `WithInitialQuantity(q => q.WithUnit(QuantityUnit.Gram))`.
- `[assembly: Reproducible]`: one line, nothing else to think about.
- No analyzer noise on idiomatic code.

## Fluency: 4/5
Fast from scaffold to green (first full run passed). Lost a point to the repetitive regex retype across
value objects and to the composed-refinement subtlety in point 5, which a regular user would not expect.
