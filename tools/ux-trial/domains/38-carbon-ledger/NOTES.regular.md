# 38 carbon ledger — notes from the regular developer

## What I did
- Created `Tests.Regular`, restored, then one command:
  `any generate CreditSerial Holder Account RetirementClaim --entry-point static:Arbitrary`.
- Resolved the 7 `to verify` rows: registry `[A-Z]{2,5}`, holder id `[A-Z0-9]{4,12}`, country `[A-Z]{2}`
  (all `Any.StringMatching`, so factory + refining overload retyped `AnyString` -> `AnyPattern`),
  claim texts `NotBlank().WithMaxLength(RetirementClaim.MaxTextLength)`.
- `Account` has a cross-parameter rule (id prefixed by the holder's country). I followed the CLI page's
  "derive one draw from another inside `Generate()`": the factory draws only the six digits, `Generate()`
  writes `{holder.CountryCode}-{digits}`; renamed `WithAccountId` to `WithAccountNumber`.
- A small test-side `RegistryScenario` (open ledger, open accounts before a fixed first posting, units
  of the ledger's registry, forward-moving posting moments, a claim whose year covers the vintages).
- 32 tests: value objects (7), journal entry rules (6), opening (3), issuance (4), transfers (5),
  retirements (4), derived state / history / consistency (3). `[assembly: Reproducible]`.
- `dotnet test --project .`: 32/32 passed, and 15 further runs with fresh seeds all passed.

## Frictions and surprises, in order
1. Scaffolder: `RetirementClaim` guards `if (string.IsNullOrWhiteSpace(x) || x.Length > MaxTextLength)`
   came out `to verify, unread guards`, while `Holder` writes the very same two checks as two `if`s and
   gets `Any.String().NotBlank().WithMaxLength(200)  guard`. A disjunction of two guards the tool reads
   one by one is not read. Harmless (it blocks the build, as designed) but surprising, and the CLI page
   does not list this shape among what it cannot read.
2. Scaffolder inlines constants: `Any.Int32().Positive().LessThanOrEqualTo(999999)` instead of
   `CreditSerial.MaxProjectId`, `WithMaxLength(200)` instead of `Holder.MaxLegalNameLength`, and
   `999999999999` without digit separators. If the domain lowers the constant, the generator keeps
   drawing values the constructor now rejects, on some runs only. I would rather keep the symbol.
3. Five `Any.StringMatching` replacements = five times the documented retype dance (factory return type
   + `Func<AnyString, AnyString>` -> `Func<AnyPattern, AnyPattern>`). Documented and the compiler
   points at it, but it is the most mechanical edit of the session; for a guard the tool already
   located as a regex, emitting the `AnyPattern` shape with the TODO would save it.
4. Cross-parameter `Account` rule: the documented fix works, but it is a hand-written `Generate()` plus
   renamed `With…` methods in the generated file, so `any generate Account --force` would wipe it.
   The page warns about this; still, my only real customisation lives in the file I must never re-scaffold.
5. Aggregate tests use drawn entities as handles in assertions (`Assert.Same(anyReceiver,
   ledger.HolderOf(anyUnit))`, `Assert.Equal(anyUnits, ledger.UnitsHeldBy(...))`). The "an `any` name
   never reaches the assertion" rule reads as forbidding this, yet the value is not what is verified —
   only its identity is. The docs never say whether identity-as-handle is fine; I decided it was.
6. Minor: generated files land in the domain's namespace (`CarbonLedger.Domain`) by default; fine,
   but my tests live in `CarbonLedger.Tests.Regular` and an `any.json` with `namespace` is the fix.

## What worked well
- The recap is excellent: each unread guard printed with file, line and the exact `if`, and the build
  then stops at the very line. The `holder` row was flagged because of `accountId.StartsWith(holder.CountryCode)`
  — exactly the cross-parameter rule I had to handle.
- `--entry-point static:Arbitrary` for four types in one call; `Arbitrary.Account().WithKind(...)
  .WithOpenedOn(o => o.Before(...))` reads like the domain.
- Refinements compose naturally: `WithRegistry(r => r.DifferentFrom(registry))` for a foreign unit,
  `WithReportingYear(y => y.GreaterThanOrEqualTo(latestVintage))` for a valid claim.
- `Any.ListOf(...).WithCount(3).Distinct()` over a value-equal `CreditSerial` just worked.
- `[assembly: Reproducible]`: zero ceremony. No analyzer noise at all on the final code.

## Fluency: 4 / 5
Scaffold-then-refine took minutes and every test reads as Arrange / Act / Assert with clear dummies.
Lost a point on the repetitive pattern retyping, inlined constants, and having the one real
cross-parameter customisation live in a file the tool overwrites.
