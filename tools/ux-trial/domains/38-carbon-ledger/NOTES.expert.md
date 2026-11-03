# Expert notes — 38 Carbon ledger
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `PROJECT-README.md`, `guides/getting-started`, `guides/composition`, `packages/justdummies-xunit`,
`packages/justdummies-cli` (whole page, including "Adjusting a parameter" and "When a guard ties two parameters"),
`generators/strings` (patterns), `generators/collections`, `generators/dates-and-times`, `generators/enums-and-choices`,
`guides/core-concepts` ("Arbitrary does not mean dummy"), `guides/reproducibility` (first half), `guides/errors-and-conflicts` (first half),
`packages/justdummies` (analyzer severity).

`any` commands run (from `Tests.Expert/`):
`any generate CreditSerial Holder Account RetirementClaim HoldingPeriod JournalEntry --output Generators --namespace CarbonLedger.Tests.Expert.Generators --entry-point any`
Result: 1286 lines in 12 files, 5 of 6 types "to verify", 9 `TODO_verify_the_generator_for_*` sentinels; first build: 9 x CS0103, as designed.

Generators completed by hand (all in `Generators/`):
* `AnyCreditSerial`: registry -> `Any.String().Alpha().InUpperCase().WithLengthBetween(2, 5)` (not `StringMatching`, to avoid the 4-place retype);
  `AnyCreditSerial.Custom.cs` exposes `ValidRegistry()` (the docs' "one line in a neighbouring file" tip).
* `AnyHolder`: id -> `AlphaNumeric().InUpperCase().WithLengthBetween(4, 12)`, country -> `Alpha().InUpperCase().WithLength(2)`.
* `AnyAccount`: the account number is tied to the holder's country, so the recipe now draws only the six digits and `Generate()` prefixes the
  holder's country (docs form "derive one draw from another inside `Generate()`"); `WithAccountId` renamed `WithAccountNumber`.
* `AnyHoldingPeriod`: `until` before `from` handled in `Generate()` (start moves to the end); `accountId` retyped to `AnyPattern` over
  `[A-Z]{2}-[0-9]{6}` (the scaffold's `Any.String().NotBlank()` raised JD030 once I turned the rules on). The retype recipe from the docs worked
  first time with a `sed` on `AnyString`.
* `AnyJournalEntry`: `Generate()` rewritten: kind first, then debit/credit accounts of the kinds that suit it, distinct numbers,
  a claim only for a retirement with a reporting year at least the latest vintage; `.Distinct()` added to the serials; the `WithDebit/WithCredit/
  WithClaim(value)/WithoutClaim` pins deleted because the derived `Generate()` would ignore them.

Support code: `Support/RegistryScene.cs` (a ledger with issuance, three holding and one retirement account, all opened at one drawn instant;
`At(days)`, `NewSerials(n)`, `IssueTo`, `RetireFrom`, `ClaimFor`, `Stranger()`, `OpenHoldingAccountOn(days)`), `TestAccounts.Pair`, `TestClaims`,
`GeneratorTests` (100 draws of each generator, and the account/entry coherence of my own generators). `AssemblyInfo.cs` has
`[assembly: Reproducible]`. `.editorconfig` raises JD001..JD038 to warning so Info rules show in the build.

Tests (187 methods, 298 cases, 10 skipped as domain defects):
* `Values/CreditSerialTests` (canonical text, Parse refusals, registry/project/vintage/unit limits both sides, equality part by part)
* `Values/HolderTests`, `AccountTests`, `RetirementClaimTests`, `HoldingPeriodTests`
* `Values/JournalEntryTests` (every kind/account-kind combination: 3 accepted, 24 refused in a `MemberData` theory; claim rules; vintage vs reporting year in both list orders; defensive copy)
* `Ledger/LedgerFoundationTests`, `LedgerIssueTests`, `LedgerTransferTests`, `LedgerRetireTests`, `LedgerQueryTests` (incl. `HistoryOf`), `LedgerConsistencyTests`
  (balances sum to zero over an arbitrary activity run; atomicity of refused batches; ledger consistent after refusals).

Final result: `dotnet test --project Tests.Expert` green: 298 total, 288 succeeded, 10 skipped, 0 failed. Repeated: 12 + 30 further runs
(fresh seeds each), all green. Before that, one probe copy of the project with every `Skip` stripped (to prove each skipped test really fails) found
that one non-skipped test of mine (`An_entry_moves_at_least_one_unit`) failed on some seeds: see friction 7.

## Frictions and surprises, in order
1. **`dotnet test --project` does not work out of the box.** `dotnet test --project Tests.Expert` (SDK 10.0.114) ->
   `MSBUILD : error MSB1001: Unknown switch. ... --project Tests.Expert`. It needs a `global.json` with
   `{ "test": { "runner": "Microsoft.Testing.Platform" } }` beside the solution. The user docs only show `dotnet test --solution` (in the
   project README's contributing block), nothing about how a consumer runs an xunit.v3 project. I added `global.json` in W.
2. **The first build is red by design, and the work list is good.** 9 CS0103 `TODO_verify_the_generator_for_*`. The recap names the guard line
   and the tie (`ties accountId to holder`, `ties debit to credit`). Nothing was a surprise. But for a domain this regex-heavy (5 anchored
   patterns) 5 of 6 types start red.
3. **Trivial anchored regexes are not translated.** `^[A-Z]{2,5}$`, `^[A-Z0-9]{4,12}$`, `^[A-Z]{2}$` are `to verify` with the advice
   `Any.StringMatching(...)` + "retype AnyString to AnyPattern in four places". I avoided that by writing the equivalent
   `Any.String().Alpha().InUpperCase().WithLengthBetween(2, 5)`, which keeps `AnyString` and keeps `WithRegistry(Func<AnyString,...>)`
   refinable with `DifferentFrom`, `Except`... A refinable `AnyPattern` has only `OneOf/Except/DifferentFrom` (docs), so for a test that wants
   "another registry" (`registry => registry.DifferentFrom(scene.Registry)`) the `AnyString` route is the more useful one. The TODO comment
   only mentions the `StringMatching` route; it could say the Alpha/InUpperCase/length route exists for simple classes.
4. **A rule tying a parameter to another one (account number prefixed by the holder's country) has no scaffold form.** Doc form used:
   derive inside `Generate()`. Consequence the docs do warn about: the pin `WithAccountId(string)` would silently stop reaching the draw, so I
   had to rename it by hand (`WithAccountNumber`, six digits). It works, but I now own a method whose name no longer matches the constructor
   parameter, and `--force` would erase all of it.
5. **A cross-parameter rule over five parameters (JournalEntry) means rewriting the generator.** Kind decides the two accounts' kinds, the
   claim's presence, and the claim's year depends on the units' vintages. All five parameters were flagged `to verify`; the fix was 25 lines in
   `Generate()` plus deleting four generated pins (`WithDebit`, `WithCredit`, `WithClaim(value)`, `WithoutClaim`) by hand. Again documented, but
   there is no way to tell `any` "these parameters are derived"; the generated surface then lies until I prune it.
6. **Pins are an excellent refusal-test idiom, as long as the parameter is independent.** `Any.Holder().WithLegalName("   ").Generate()` throwing
   `ArgumentException { ParamName: "legalName" }` while every other part stays a valid dummy reads very well, and the Theory tables
   (`CreditSerial` 4 parts x both sides of each limit) took minutes. For tied parameters (account id vs holder, entry kind vs accounts) the same
   idiom does not exist, so those tests construct with `new Account(...)` / `new JournalEntry(...)` and draw only the neighbours.
7. **A derived `Generate()` makes my own test flaky, and only repetition shows it.** `Any.JournalEntry().WithSerials(Array.Empty<CreditSerial>()).Generate()`
   expected `ArgumentException` but my generator drew kind = Retirement one time in three and computed `serials.Max(...)` -> `InvalidOperationException`
   on the empty array. It passed in the first runs by luck. The failure was mine (a generator that cannot represent an invalid batch), found by the
   probe run, not by the library, and the reported seed would have replayed it. Fixed by building the invalid entries with the constructor.
   Lesson recorded: once `Generate()` is derived, never use the generator to build the invalid case.
8. **`new AnyX()` pins and `DateTimeOffset` default span.** `Any.DateTimeOffset()` (scaffold default for `openedOn`) spans year 0001 to 9999, so
   `AddDays(+/-)` in `RegistryScene.At` would overflow; I bound the scene's opening instant to 2000..2050. Documented in the dates page; no
   surprise, but the scaffolder cannot know this domain wants a sane range.
9. **Mixing `Stranger()` draws.** Test bug of mine, not library: I called `scene.Stranger()` twice inside one expression and built an `Account`
   with the id of one draw and the holder of the other: `ArgumentException: Account 'BV-285879' must be prefixed with its holder's country code
   'FP'` with the seed printed. The seed line made it a 30-second diagnosis.
10. **Analyzer view.** Info rules are invisible in `dotnet build` (documented). With all 38 rules raised to warning in `.editorconfig`, the
    project produced exactly one diagnostic, and it was in scaffolded code:
    `Generators/AnyHoldingPeriod.cs(45,16): warning JD030: This string dummy declares no length: it draws 1 to 1025 characters`. No false positive
    anywhere in about 190 test methods.
11. **Nested refinement reads well but is long.** `Any.Account().WithKind(AccountKind.Holding).WithHolder(holder => holder.WithCountryCode(country => country.Except(openCountries)))`
    is the way to say "an account that cannot collide with the open ones" and it worked first time. `DistinctBy(account => account.AccountId)` and
    `Distinct()` on `ListOf` removed every collision my scene could have had, so no test can fail on a coincidence of drawn ids.

## What worked well
* `[assembly: Reproducible]` plus the seed line on every failure: the two failures I met (friction 7 and 9) were replayable and explained in seconds.
* The refusal-test idiom through `With…` pins on independent parameters (friction 6).
* `Any.ListOf(...).WithCount(n).DistinctBy(...)`, `Except`, `DifferentFrom` as builder-level calls: they let the scene guarantee distinct account
  numbers and unseen strangers *by construction*, without a retry in the test.
* The "bound from another draw" idiom (`Between(1, anyIssued)`, `Between(1, anyMoved)`) for the arbitrary-activity consistency test.
* The scaffolded files compile as ordinary code I could edit (retype, derive, delete pins) with `sed`; the TODO sentinel as a build-time work list.
* The generated entry point `Any.Account()`, `Any.Holder()`: test code reads like the domain.

## Fluency: 4 / 5
The library itself got out of the way: constraints, pins, collection builders and seeds did what the pages said, the one retype went mechanically
and the analyzers stayed quiet and correct. The score loses a point for the scaffolder's limits on exactly the shape this domain is made of:
anchored regexes and rules between parameters (account number vs holder, entry kind vs accounts vs claim vs vintage) leave you hand-editing 5 of
6 generated files, pruning generated pins that no longer mean anything, and being unable to use the generators to build the invalid case once
`Generate()` is derived (friction 7). The `global.json` detour for `dotnet test --project` (friction 1) cost the first minutes.

## Domain observations (not fixed, `Domain/` untouched)
Each one is covered by a skipped test with `Skip = "domain defect: ..."` unless stated; I verified every skipped test fails once un-skipped.
* D1 (medium) Every `^...$` regex of the domain accepts a trailing `\n` because .NET's `$` matches before a final newline: registry (ledger and
  `CreditSerial`), holder id, country code, account id, and `CreditSerial.Parse("VCS-1234-2019-000000000042\n")`. A serial built on registry
  `"VCS\n"` writes `VCS\n-...` which `Parse` refuses (round-trip broken). Tests: 6 skipped.
* D2 (low) `\d` in the account pattern also accepts non-ASCII digits (`FR-` + six Arabic-Indic digits). 1 skipped.
* D3 (low) `JournalEntry.Serials` returns the stored `CreditSerial[]` and `CarbonRegistryLedger.Journal` the live `List<JournalEntry>`: both are castable
  and mutable although the model says immutable / append-only. 2 skipped.
* D4 (low) `Holder` checks the 200-character limit before trimming: a 199-character name plus two spaces is refused though the stored name fits. 1 skipped.
* D5 (medium, design/doc) MODEL.md says `LedgerRuleViolationException` is "every refused registry operation", but empty batches, duplicate units in
  a batch, self-transfer, transfer into a retirement account, retiring into a holding account, issuing into the issuance/retirement account and a
  reporting year before the vintage all surface as `ArgumentException` / `ArgumentNullException` from `JournalEntry`. A caller catching the ledger
  exception misses registry rules such as "vintage after reporting year". Tests pin today's types.
* D6 (low) Message "An entry cannot be recorded before both its accounts were opened" while the condition refuses when *either* account is not yet open.
* D7 (low) `CreditSerial.Parse` throws `ArgumentOutOfRangeException` for a well-formed text whose part is out of range, `FormatException` otherwise.
