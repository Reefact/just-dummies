# Batch genealogy — expert crafter notes

## What I did
- Scaffolded `LotNumber MaterialCode Quantity LotQuantity Lot Transformation` in one call with
  `--entry-point any --output ./Arbitrary`; kept the first four useful ones, deleted `AnyLotQuantity` and
  `AnyTransformation` (no test needed them: every Transformation invariant is relational, and the
  quantities of a step are what its tests are about, so they stay literals).
- Resolved every `to verify`: `Any.StringMatching(...)` for `LotNumber` / `MaterialCode` (factory retyped
  to `AnyPattern`, the refining `Func<…>` with it), `new AnyQuantity().NonZero()` for a lot's quantity, and
  two **disjoint date windows** for `Lot` (manufacture 2024-2025, expiry 2026-2033: expiry > manufacture and
  shelf life <= 3652 days by construction, while each date stays pinnable on its own).
- Hand-written partials: `AnyLotNumber.OfAWholeLot()`, `AnyQuantity.Mass()/Volume()/NonZero()`, and a C# 14
  `extension(Any)` file for the two string identifiers (`Any.SiteCode()`, `Any.TransformationId()`).
- A `Plant` test fixture grows a genealogy: tests say *which lots feed which step and how much*
  (`plant.Blend(Kg(100m), Kg(25m).Of(anyApi), Kg(75m).Of(anyFiller))`); the plant draws lot numbers
  (distinct), transformation ids (distinct), materials, step dates (monotonic, inside every shelf life)
  and the extra a raw-material receipt holds beyond what the scenario consumes.
- 36 tests: Quantity (4), LotNumber (2), Lot lifecycle and consumption (10), Transformation (6),
  recording (6), tracing (4, including a split-then-merge diamond), containment (4).
  `[assembly: Reproducible]`. All pass; ran the suite 9 times in a row, green every time.

## Frictions and surprises, in order met
1. Scaffold recap was excellent, but the default namespace of the generators is the *domain's*
   (`BatchGenealogy.Domain`), so test helpers land in the production namespace. Defensible (no import),
   just surprising in a test project; I left it.
2. `LotNumber` grammar has two shapes (lot / sub-lot). A refinement on `AnyPattern` can only narrow by
   value set (`OneOf/Except/DifferentFrom`), so "a whole lot, never a sub-lot" had to be a hand-written
   second grammar in a partial. Documented as deliberate; still a small duplication of the format.
3. **`AnyPattern.Except` refuses an empty list** at run time:
   `System.ArgumentException : At least one value is required. (Parameter 'values')`.
   Met naturally: the fixture excludes the lot numbers / ids it already used, a set that starts empty.
   `OneOf()` empty is a real empty domain; `Except()` empty is a no-op, and refusing it forces
   `used.Count == 0 ? p : p.Except(used.ToArray())` at every growing-exclusion site. The analyzer page
   JD014 lists `OneOf()` but not `Except()`, and nothing in the strings/choices pages says it.
4. No way to say "distinct across successive draws" other than carrying my own exclusion set; a
   `Distinct()` exists only inside one collection draw. For a fixture that registers lots one at a time,
   a "fresh value" helper (or an `Except(IEnumerable<T>)` overload) would remove the bookkeeping.
5. A cross-parameter guard (`expiresOn <= manufacturedOn`) is reported on the first guard only; the
   shelf-life guard on the next line was not mentioned. I found it by reading the constructor, which the
   docs do say to do.

## What worked well
- `to verify` that refuses to compile is the right default: every unread guard was a real invariant.
- Nested refinement through composed generators is lovely:
  `Any.Lot().WithMaterial(m => m.WithValue(code => code.DifferentFrom(apiMaterial.Value)))`.
- `Any.ArrayOf(Any.LotNumber().OfAWholeLot()).WithCount(3).Distinct()` just works on a value object
  with value equality.
- `[assembly: Reproducible]` printed the replay seed on every failure, with a clean stack trace pointing
  at my fixture line. Zero analyzer warnings on the final code.
- The "retype the factory when the replacement is another kind of generator" section was exactly the
  edit I needed, line for line.

## What I would design differently
- Accept `Except()`/`Except(IEnumerable<T>)` with no values as a no-op.
- Offer a scaffold option to place generators in the test project's namespace by default.

## Fluency: 4/5
The scaffold + refine + compose loop is fast and honest, and the docs answered everything I hit. One
point off for the empty-`Except` refusal and the per-fixture uniqueness bookkeeping it compounds.
