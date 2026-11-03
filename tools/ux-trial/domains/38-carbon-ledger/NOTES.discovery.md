# Discovery notes — 38 carbon ledger
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`, `docs/README.md`, `guides/getting-started.en.md`,
`guides/core-concepts.en.md` (the "Arbitrary does not mean dummy" part), `packages/justdummies-cli.en.md`
(whole), `packages/justdummies-xunit.en.md`, `guides/composition.en.md`, `generators/strings.en.md`,
`generators/dates-and-times.en.md`, `generators/collections.en.md` (first half). Then `MODEL.md` and the
domain sources.

`any` commands run (from `Tests.Discovery`):
- `any --help`, `any generate --help`
- `any generate CreditSerial Holder Account RetirementClaim HoldingPeriod JournalEntry --output Generators --namespace CarbonLedger.Tests.Discovery.Generators --entry-point any`

Generators completed by hand:
- `AnyCreditSerial`: `registry` retyped `AnyString` -> `AnyPattern` (`Any.StringMatching("[A-Z]{2,5}")`), 4 places, TODO lines deleted.
- `AnyHolder`: `holderId` -> `[A-Z0-9]{4,12}` and `countryCode` -> `[A-Z]{2}` retyped the same way (`legalName` kept as `AnyString`).
- `AnyAccount`: the id is tied to the holder's country. I made the recipe draw only the six digits (`[0-9]{6}`),
  renamed the parameter `accountNumber` (`WithAccountNumber`) and wrote the prefix in `Generate()`
  (`holder.CountryCode + "-" + number`).
- `AnyHoldingPeriod`: kept the draws, and in `Generate()` swapped `from`/`until` when they come out in the wrong order.
- `AnyJournalEntry` (5 params `to verify`): deleted; a journal entry is only ever produced by the ledger, or built by hand
  with literal accounts in the entry's own tests.

Support code: `Support/LedgerWorld.cs` (a ledger with a drawn registry and a drawn increasing clock, verbs to open
accounts, mint distinct serials of the registry, issue, transfer, retire, and a claim that covers some serials) and
`Support/Accounts.cs`. `[assembly: Reproducible]` in `AssemblyInfo.cs`.

Tests written (172 methods, 310 cases; 302 run, 8 skipped as domain defects):
- `CreditSerialTests` - parts, `ToString` padding, `Parse` canonical/non-canonical/out-of-range, round trip, every limit of projectId/vintage/unitNumber and registry, equality and hashing.
- `HolderTests` / `AccountTests` / `RetirementClaimTests` / `HoldingPeriodTests` - limits (200/201, 500/501, years 1999/2000/2100/2101), blanks, trimming, country prefix rule, undefined enum members, nulls, period ordering by instant.
- `JournalEntryTests` - the full 3x3x3 entry-kind x debit-kind x credit-kind matrix, sequence, same account (by id), empty/null/duplicate serials, claim rules (missing, forbidden, reporting year vs latest vintage), snapshot of serials.
- `LedgerFoundationTests`, `IssueTests`, `TransferTests`, `RetireTests`, `LedgerQueryTests` - founding, `OpenAccount`, issue/transfer/retire happy paths and balances, every refusal (foreign registry, double issue, wrong holder, retired unit locked, bad account kinds, time ordering vs last entry and vs `openedOn` of each account), all-or-nothing batches, `HistoryOf`, `HolderOf`/`IsRetired`/`UnitsHeldBy`, conservation and `IsConsistent`.

Result: green (`dotnet test --project Tests.Discovery`: 302 passed, 0 failed, 8 skipped), then 15 more runs with fresh seeds, all green (302 / 0 / 8 each time).
The 8 skipped tests were each un-skipped once to confirm they fail for the reason stated, then re-skipped.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Discovery` does not work out of the box.**
   `dotnet test --project Tests.Discovery` -> `MSBUILD : error MSB1001: Unknown switch ... Switch: --project`;
   `dotnet test Tests.Discovery` -> `Microsoft.Testing.Platform.MSBuild.targets(395,5): error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.`
   Expected: the xUnit adapter page (which names `xunit.v3` 4.0.2) to say how to run the tests. No page I read mentions `dotnet test`, `global.json` or the Microsoft.Testing.Platform runner. I found the fix from the error's hint: a `global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }` at the workspace root (I added it; it is outside the library but a newcomer on the .NET 10 SDK hits it in the first minute).
2. **A regex guard is never read, so three of my four value objects started with a non-compiling generator.**
   Recap: `registry string Any.String() to verify, unread guards`, and the file contains `_ = TODO_verify_the_generator_for_registry;`, build error CS0103 on each. I expected the tool to translate `^[A-Z]{2,5}$` (it is well inside the documented regular subset); the CLI page explains why it does not (it does not translate "some and drop others"). The comment gives the exact recipe, which is good, but the edit is four coordinated retypings per parameter (`AnyString` -> `AnyPattern` on the recipe field, the factory, the private constructor parameter, and the `Func<>` of the refining overload), plus deleting two lines. With `Holder` (two regex parameters, one plain string parameter in the same constructor) I scripted it. Mistakes there are caught by the compiler, so it is safe, but tedious.
3. **A guard that ties two parameters (`accountId.StartsWith(holder.CountryCode)`) leaves nothing usable.**
   `Account`: `accountId ... to verify, unread guards` and `holder ... to verify, AnyX, unread guards ... — ties accountId to holder`. The docs list four forms for a tie; none matches "a parameter is a function of another". I derived the id in `Generate()`. Consequence the docs do warn about: the `WithAccountId` pin no longer reaches the draw. I renamed the parameter to `WithAccountNumber` (digits only) and set the country through `Any.Account().WithHolder(h => h.WithCountryCode("FR"))`. It reads well at the call site, but it is a hand-edit of generated code that `--force` would erase (I did not put it in a separate partial file; given how much of `Generate()` changed, I am not sure what could live in one).
4. **`Any.DateTimeOffset()` scaffolded unconstrained spans year 1 to 9999, and my first ledger test failed because of it.**
   `LedgerQueryTests.A_ledger_works_with_a_registry_code_of_two_letters_and_one_of_five` failed with `LedgerRuleViolationException : An entry cannot be recorded before both its accounts were opened.`: I recorded an entry at a literal 2030 instant while the issuance account's `openedOn` was drawn anywhere. This is documented ("An unconstrained draw spans the whole type"), and the failure was legitimate (the instant was not a dummy for that account), but the scaffold gives no hint on a domain whose accounts are ordered in time. I solved it once, in `LedgerWorld`, by drawing account opening dates in 2000-2030 and the ledger clock after 2031.
5. **`HoldingPeriod`: the scaffolder's `from`/`until` tie is read but not drawn.**
   Recap: `from ... to verify, unread guards ... — ties until to from`, `until ... DateTimeOffset?.OrNull()`. `Any.OrderedPair` exists, but with a nullable second element I did not see how to apply it; I swapped in `Generate()` instead. That silently turns a pinned wrong order into a valid pair (the swap would hide a test mistake). Low cost, but I wished the tie recipes covered a nullable end.
6. **`JournalEntry`: 5 of 7 parameters `to verify`, all because of cross-parameter rules** (kind vs account kinds, debit != credit, claim iff retirement, year vs vintage of the serials). That is a rule between six parameters; none of the four documented forms helps, so I did not use a generator for it at all and built entries through the ledger or with literal accounts. A reasonable outcome, but the scaffold run cost a file I deleted.
7. **Distinct, valid serial lists were easy; unit-of-work helpers were not generator work.** `Any.ListOf(Any.CreditSerial().WithRegistry(r)).WithCount(n).Distinct()` worked first time (`CreditSerial` overrides `Equals`, so `Distinct()` is meaningful). No friction, noted because the "distinct needs value equality" paragraph made me check that first.
8. **Domain defects found by hand-picked literals, not by draws.** Five pattern-based validators (`CreditSerial.Registry`, `Holder.HolderId`, `Holder.CountryCode`, `Account.AccountId`, ledger registry) accept a trailing `\n` (`$` matches before a final line feed) and the account id (and `Parse`) uses `\d`, accepting non-ASCII digits. The draws could not have found these (`Any.StringMatching("[A-Z]{2,5}")` never produces a newline, `Any.String()` rarely ends in exactly `\n` and matches the shape). See findings. This is not a library problem, but it is a reminder that a constructor-valid generator proves nothing about the constructor's negative space.
9. **Mild naming trap:** `WithRegistry(r => r.OneOf(...))` on a pattern-typed parameter works (`AnyPattern.OneOf`), and `Any.StringMatching(...).DifferentFrom(x)` is exactly what I needed for "another registry". Found in the strings page, no friction.
10. **Analyzers were silent.** `dotnet build --no-incremental` printed 0 warnings; the only rule that would have applied is JD030 (Info) on the scaffolded `Any.String()`, which the CLI page says never prints. I did not raise its severity.

## What worked well
- The getting-started page's rule ("if an `anyXxx` reaches your assertion it is not a dummy") changed how I wrote the tests: every pass-through assertion (constructor exposes parts, `ToString`) uses literals, and the dummies are the things the test does not care about (a holder's legal name, serial numbers, registry codes, instants). I kept one honest exception: the `Parse(ToString(x)) == x` test, which the model states as a rule; it is named for a single draw.
- The scaffolder's recap with its last column is genuinely informative: `guard` with named constants (`CreditSerial.MaxProjectId`) meant the project id/vintage/unit-number generators were right the first time, and `AnyX` composition (`Account` uses `new AnyHolder()`, `HoldingPeriod` uses `new AnyCreditSerial()`) produced the "work list" the docs promise.
- Build-stopping `TODO_verify_...` identifiers: I could not forget one. Each comment names the exact guard and line.
- Fluent refinements on a generated generator (`Any.Account().WithKind(...).WithOpenedOn(o => o.Between(...))`, `WithRegistry(...)`, `WithReportingYear(y => y.GreaterThanOrEqualTo(latestVintage))`) made the ledger helper short and kept the domain rules in the generators.
- `[assembly: Reproducible]` plus a seed printed on failure: every failure I met printed `These arbitrary values were seeded with ...`; repeated 15 times, no failure appeared.
- Contradictory-constraint errors never came up; the one conflict-shaped surprise was mine (friction 4).

## Fluency: 4 / 5
Getting from zero to a green, repeated suite of 310 cases took one reading pass and no dead end, and the library itself never surprised me: every `With...` I reached for existed and did what the page said. Points lost on the edges around it: the test command does not work on a .NET 10 SDK without an undocumented `global.json`, the regex-guard retyping and the cross-parameter ties (Account, HoldingPeriod, JournalEntry) are still hand-work in generated files, and a domain with many validated identifiers takes four or five such hand-edits before the first test can compile.
