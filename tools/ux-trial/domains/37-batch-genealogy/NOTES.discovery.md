# Discovery notes: 37 batch genealogy

## What I did
- Read MODEL.md and the domain, then `guides/getting-started`, `packages/justdummies-cli`, the
  strings/patterns, dates, numbers and collections references, and `packages/justdummies-xunit`.
- Scaffolded `Lot LotNumber MaterialCode Quantity LotQuantity Transformation --entry-point any` in a
  single `any generate` call (exit 0, clear recap, "will not compile until you resolve it").
- Resolved the TODOs, deleted `AnyTransformation`, and wrote 31 tests across quantities and lot numbers, lot
  lifecycle and consumption, transformation rules, and the genealogy aggregate (record, all-or-nothing
  refusal, split keeps material, upstream trace through blend+split, downstream through a diamond,
  containment threshold). All 31 pass, and passed again on 15 more runs (different seeds each time).

## Frictions and surprises, in order
1. **Regex guards (`[GeneratedRegex]`)**: `LotNumber` and `MaterialCode` were marked `to verify` with
   `Any.String().NonEmpty()`. The CLI page says plainly that a regex is never read, so I was not
   surprised. I copied the pattern from the attribute into `Any.StringMatching(...)`. I then had to retype
   the factory (`AnyString` -> `AnyPattern`) and the refining `Func<...>`. The page explains this exactly
   (CS0029 and the two lines to change), but it is still two edits per value object.
2. **`initialQuantity.IsZero` unread**: a guard on a property of a composed type cannot be read. That is
   fair, and the recap pointed at `Lot.cs, line 17`. The fix was one line:
   `new AnyQuantity().WithAmount(amount => amount.Positive())`. Refining a nested generator through a lambda
   felt natural.
3. **`expiresOn > manufacturedOn` plus shelf life <= 3653 days** (two parameters tied together). The docs
   offer "derive one draw from another inside `Generate()`". In practice, changing what the
   `expiresOn` parameter *is* (I made it a shelf life in days) meant editing the field, both constructors,
   every positional `new AnyLot(...)` in the ten `With...` methods, and `Generate()`. It is doable, but it is invasive.
   `any generate Lot --force` would now wipe that work, and the page warns about this. A pattern for "a
   derived parameter" in the scaffold (or a doc sample of the whole edit) would help.
4. **Unconstrained `Any.DateOnly()`**: the dates page states the default range for `DateTime`
   (MinValue..MaxValue) but not for `DateOnly`. Because I add up to 3653 days to the draw, I bounded it
   (2000-2099) as a precaution, without knowing whether that was needed.
5. **`Transformation`**: arity by kind, sub-lots of the input's base, one dimension, output <= input. These are
   all rules that span parameters. The recap honestly marked 4 of 5 parameters `to verify`.
   For `outputs` it names the *first* unread guard (the null-entry lambda on line 26), not the rules that
   matter (arity, totals). I deleted the generator: tests build transformations explicitly, because their
   quantities are what the assertions are about. Only the id is a dummy (`Any.StringMatching("TRF-[0-9]{6}")`).
6. **Distinct identities**: several lots in one test need distinct numbers. The collections page led me
   straight to `Any.ListOf(Any.LotNumber()).WithCount(n).Distinct()`, and it worked first time
   (`LotNumber` overrides `Equals`). `AnyPattern.DifferentFrom` gave me "another material" for the split
   test, through the scaffolded `WithValue(code => code.DifferentFrom(...))`.
7. Not a library issue: the domain's `BatchGenealogy` class shares its name with the root namespace, so tests
   need `using Genealogy = BatchGenealogy.Domain.BatchGenealogy;`.

## Error messages met
- No compile errors beyond the deliberate `TODO_verify_the_generator_for_*` identifiers. Each one pointed
  to the guard and the line.
- No analyzer diagnostic fired in the final code. No runtime `AnyGenerationException`.
- A deliberately broken assertion printed
  `[JustDummies] These arbitrary values were seeded with 1078259903. Reproduce this run with [Reproducible(Seed = 1078259903)].`
  It is directly usable, and the form matches the xUnit adapter (not `Any.Reproducibly`).

## What worked well
- The `any` recap: it separates inferred from guessed, names the guard and its line, and the TODO lines do not compile.
- `--entry-point any` with several types in one call: `Any.Lot()` and `Any.LotNumber()` read like `Any.Int32()`.
- `With...(value)` versus `With...(lambda)` is a clean split. The pin is what the test cares about; the lambda keeps the rest of the generator valid.
- `[Reproducible]` on the class: zero ceremony.

## Fluency: 4 / 5
I went from nothing to 31 meaningful domain tests in one sitting with no blocking error, guided by docs
that anticipated almost every step. I take one point off for the cost of the cross-parameter edit in a
scaffolded generator (item 3) and the small doc gap on `DateOnly` defaults.
