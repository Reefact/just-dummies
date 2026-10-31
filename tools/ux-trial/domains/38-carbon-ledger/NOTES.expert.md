# Carbon ledger: expert crafter notes

## What I did

- Scaffolded `CreditSerial Holder Account RetirementClaim` in one call (`--entry-point any --output ./Arbitrary`).
  All four files stopped the build on purpose (7 `to verify` rows), and I resolved each one in its `AnyValid…()` factory.
- I kept every string recipe an `AnyString` (`Alpha().InUpperCase().WithLengthBetween(2, 5)` for a registry,
  `AlphaNumeric().InUpperCase()…` for a holder id), so I never had to retype a factory to `AnyPattern`.
- `Account` has a rule that spans two parameters: its id is prefixed by the holder's country. I changed the
  parameter to `_accountNumber` (six digits), and `Generate()` writes `$"{holder.CountryCode}-{number}"`, so the
  rule holds by construction. `WithAccountId(string)` is gone. `--force` would wipe this, as the docs warn.
- `CarbonRecipes.RegistryCode()` is the single place the registry rule lives. The ledger tests and `AnyCreditSerial`
  both use it. `SameAccountId` is a comparer, needed because `Account` has reference equality.
- `AnyRetirementClaim.ValidFor(units)` sits in a partial file of my own. It is a refinement,
  `WithReportingYear(y => y.GreaterThanOrEqualTo(latestVintage))`, which carries the vintage ≤ reporting-year rule
  that lives between the claim and the units.
- `RegistryScenario` is a test fixture. It owns the bookkeeping no test cares about: distinct account ids, accounts
  opened before anything is posted to them, and a clock that only moves forward (`Later()`). It also offers
  `NewUnits(n)` (`Any.SetOf(serial).WithCount(n)`), `IssueTo` and `ClaimFor`.
- 31 test methods (33 cases) cover serials, holders and accounts, journal-entry invariants, opening, issuance,
  transfers (full and partial), retirement locking, append-only dating, history rebuild and conservation.
  `[assembly: Reproducible]` is on the whole suite. I ran it 25 extra times and it stayed green.

## Frictions and surprises, in the order I met them

1. **The scaffolder reads `IsNullOrWhiteSpace(x) || x.Length > Max` as unread.** In `RetirementClaim` it marked
   `beneficiary` and `purpose` `to verify` and fell back to `Any.String().NonEmpty()`. In `Holder`, the same two
   rules written as two separate `if`s were read: `NotBlank().WithMaxLength(200)`, `guard`. So the shape of the
   code changes what the tool reads, though both forms mean the same.
2. **A guard that ties two parameters** (`accountId.StartsWith(holder.CountryCode)`) was flagged on `holder`,
   which is good. Fixing it meant hand-editing `Generate()`, the private constructor and the `With…` pairs. The docs
   describe this pattern ("derive one draw from another"), but every option they offer is lost on `--force`
   except a separate partial file, and a separate file cannot replace `Generate()`.
3. **`AnyString.Except()` with an empty array throws** `System.ArgumentException : At least one value is required.
   (Parameter 'values')`. I hit it through the natural "exclude the account numbers already used" refinement:
   the first account of a registry has nothing to exclude. Excluding nothing is a well-defined no-op, unlike
   `OneOf()` with no values, and none of the user docs lists `Except` with no values among the refusals.
   Workaround: `count == 0 ? number : number.Except(…)`. It failed 18 of 33 cases on the first run, and every
   failure printed its seed, which helped.
4. **Info-level analyzers do not show on the command line.** To confirm there were no JD diagnostics I had to
   build with `-p:ErrorLog=…sarif`. The final code raises none.
5. **Picking a distinct subset** of units I had already issued takes
   `Any.ListOf(Any.ElementOf(issued.ToList())).WithCount(2).Distinct()`. It works and reads fine, but a
   first-class `SubsetOf(collection)` would say it directly.
6. **The doc rule "an `anyXxx` in the assertion is a property" is too broad for checking that something went to the
   right account.** `Assert.Same(anyBuyer, ledger.HolderOf(unit))` and `Contains($"held by {anyOtherOwner.AccountId}")`
   follow a drawn account through to the assertion, but the outcome does not depend on its value. I kept them, and the
   docs give no guidance on this case.
7. **A clock that only moves forward** (strictly increasing instants) had no recipe, so I wrote one: `_now +=
   Any.TimeSpan().Between(1 min, 30 d)`. This is minor, and probably correctly left to the user.

## What worked well

- The `to verify` TODO that stops compilation, naming the exact guard line, is excellent. Nothing could have
  silently drawn a bad registry code.
- `Any.SetOf(customGenerator).WithCount(n)` over a value-equal domain type, and `Distinct(comparer)` over a
  reference-equal one, did exactly what I needed for units and accounts.
- Refinement lambdas (`WithVintage(2022)`, `WithReportingYear(y => y.GreaterThanOrEqualTo(v))`,
  `WithRegistry(code => code.DifferentFrom(registry))`) keep the base rules and state only the scenario's
  precondition. "A foreign registry's unit" became one honest line.
- `Any.Holder()` and `Any.Account()` entry points through C# 14 extensions read naturally beside `Any.DateTimeOffset()`.
- `[assembly: Reproducible]` takes one line. A failure reports `[Reproducible(Seed = …)]` in the test output.

## Fluency: 4 / 5

Most of the surface did what I expected the first time. Scaffold-then-refine fits an aggregate with real
invariants well. I took a point off for three things: the guard-shape sensitivity (1), the empty-`Except` refusal (3),
and the cross-parameter rule that can only live in a hand-edited `Generate()` that `--force` destroys (2).
