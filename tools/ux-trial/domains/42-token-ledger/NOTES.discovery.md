# Discovery notes — 42 token ledger
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did

**Pages read, in order.** `docs/PROJECT-README.md`; `docs/README.md` (the index); `docs/guides/getting-started.en.md`;
`docs/guides/core-concepts.en.md` (skimmed: recipe versus value, the golden rule); `docs/packages/justdummies-cli.en.md`
(all of it, it is the longest page and the one I needed most); `docs/generators/README.md` and `numbers.en.md` (looking for a
`BigInteger` generator); `docs/packages/justdummies-xunit.en.md`; `docs/guides/composition.en.md`;
`docs/generators/strings.en.md` (the `StringMatching` section); `docs/guides/reproducibility.en.md`;
`docs/packages/justdummies.en.md` (the severity section, to see the analyzers' info-level rules).
Not read: errors-and-conflicts, collections, dates, enums, GUIDs, design principles, FAQ, the per-rule analyzer pages.

**Project.** `Tests.Discovery/TokenLedger.Tests.Discovery.csproj` from the template; a `global.json` in the domain folder
(see friction 1); an `.editorconfig` in the test project that turns all 38 `JDxxx` rules on as warnings.

**`any` commands run** (from `Tests.Discovery`, after `dotnet restore`; see friction 2):
- `any generate Address --output Generators --entry-point any` (first, alone, to see what the tool says)
- `any generate TokenAmount ExactDecimal TokenDefinition TokenPrice GasSchedule TransactionEnvelope Receipt TransferRecord Account Ledger --output Generators --entry-point any`
- Not scaffolded: the enums (`Any.Enum<T>()` was enough) and `LedgerRefusedException`.

**Generators completed by hand** (the 10 scaffolded files plus `AnyBigInteger`, its entry point and `AnyAddress.Custom.cs` in `Generators/`):
- `AnyBigInteger` (+ `.Entry.cs`): written from scratch, `Between` / `AtLeast` / `AtMost`; JustDummies has no `BigInteger`
  generator and every scaffolded row for `BigInteger` names `new AnyBigInteger()` (friction 3).
- `AnyAddress`: retyped `AnyString` to `AnyPattern` as the TODO said, `Any.StringMatching("0x[0-9a-f]{40}")`; a `.Custom.cs`
  partial adds `NonZero()` and `DifferentFromAddress(other)`.
- `AnyTokenAmount`: base `[0, 2^256 - 1]`.
- `AnyExactDecimal`: kept as scaffolded (it was the only one the tool resolved without a `TODO`).
- `AnyTokenDefinition`: name `[!-~]([ -~]{0,48}[!-~])?`, symbol `[A-Z][A-Z0-9]{1,10}`; the supply cap must be a whole
  number of tokens of the drawn decimals, so `Generate()` was rewritten to draw the decimals first and round the cap down
  to whole tokens (a pinned cap is used as given).
- `AnyTokenPrice`: perToken positive with at most 18 digits, currency `[A-Z]{3}`.
- `AnyGasSchedule`: kept the `OrderedPair` the tool wrote for `transfer <= transferFrom`; bases bounded to
  `[21 000, 30 000 000 - 1 000 000]` so that "crediting op + surcharge fits a block" holds by construction.
- `AnyTransactionEnvelope`, `AnyReceipt`: the price is folded under `Max / gas` at draw time, and a pin of the price is
  tracked separately so that an impossible pinned pair is still refused loudly.
- `AnyTransferRecord`, `AnyAccount`: addresses non-zero (only `to` for the record, so the pair is never zero/zero).
- `AnyLedger`: initial supply folded under the drawn cap, minimum gas price folded under `cap / transfer gas`
  (base `[0, 1000]`), pins tracked.

**Tests written** (`Tests/`, 361 test methods, 452 test cases once the theories expand; 5 skipped, see below):
- `GeneratorTests` (9): every scaffolded or completed generator only draws what its constructor accepts (300 draws each).
- `AddressTests` (19), `TokenAmountTests` (23), `ExactDecimalTests` (45 methods incl. a 13-row and an 18-row theory),
  `TokenDefinitionTests` (36), `TokenPriceTests` (20), `GasScheduleTests` (18), `TransactionEnvelopeTests` (14),
  `ReceiptTests` (14), `TransferRecordTests` (10), `AccountTests` (5): construction, every refusal with its exact
  exception type and parameter name (`Assert.Throws` is exact, so `ArgumentException` versus
  `ArgumentOutOfRangeException` is pinned), edges (21 000 / 30 000 000 gas, scale 0 / 80, 18 / 19 price digits, 2^256 - 1),
  normalisation, truncation toward zero, equality and hash, `ToString`.
- `LedgerConstructionTests` (14), `LedgerQueryTests` (12), `LedgerTransferTests` (25), `LedgerAllowanceTests` (38),
  `LedgerMintAndBurnTests` (25): the aggregate, one operation at a time, with the fee arithmetic (gas used, not the limit),
  the new-holder surcharge in each place it applies and each place it does not, allowances (replace, increase, decrease,
  unlimited), delegated transfers, mint under the cap, burn, and the "credits to the sender do not count towards the fee" rule.
- `LedgerRefusalTests` (19 methods, 4 theories run over the 7 kinds): the 8 checks in their documented order (one test per
  adjacent pair of checks) and "a refusal changes nothing" (balances, nonces, logs, supply, block; checked by a helper that
  snapshots the ledger around every expected refusal).
- `LedgerScenarioTests` (4): a scripted history with literal figures that checks every balance, the collector's total
  against the receipts, and an indexer-style replay of the transfer log into the balances.
- Not testable through the public API: `RefusalReason.NonceExhausted` (a nonce reaches `UInt128.MaxValue` only after 2^128 - 1
  transactions).

**Dummy or literal.** Participants (addresses), nonces taken from the ledger, deadlines "still ahead", gas limits "ample", the
malformed values of a refusal test, the drawn token definition behind a test that does not read it, the message of an exception:
dummies. Amounts, gas prices, tight gas limits, balances at the edge of the fee, the expected figures: literals.

**Skipped, all domain defects** (5): see findings 10 and 11 below and the `Skip` strings in
`AddressTests`, `ExactDecimalTests`, `TokenDefinitionTests`, `TokenPriceTests`, `LedgerScenarioTests`.

**Result.** `dotnet test --project Tests.Discovery`: 452 cases, 447 passed, 0 failed, 5 skipped. After the last edit it stayed
green on 12 further runs (fresh seeds each time) on top of the ~15 runs made while writing; no flaky run. I also replayed one
seed twice with `Any.Reproducibly(seed, ...)` over `Any.Ledger()`, `Any.TokenDefinition()` and `Any.ExactDecimal()`
(including my hand-written `AnyBigInteger`) and got identical values.
The 38 analyzers, all on as warnings, reported nothing on the test project; I checked they were live by adding a throw-away
`Any.String().Generate()` (JD030) and `$"{Any.Int32()}"` (JD005), which both fired.

## Frictions and surprises, in order

1. **`dotnet test --project Tests.Discovery` does not run on this SDK as is.**
   `dotnet test --project Tests.Discovery` -> `MSBUILD : error MSB1001: Unknown switch ... Switch: --project`.
   `dotnet test Tests.Discovery` -> `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on
   .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.`
   Neither the getting-started page nor the xUnit adapter page says anything about the runner; they only name `xunit.v3 4.0.2`.
   I expected `dotnet test` to work after "install the two packages". I found the fix from the error's own wording: a
   `global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }` in the domain folder, after which
   `--project` works. Low, since it is xUnit v3 on .NET 10 rather than JustDummies, but the adapter page is where a newcomer
   would look, and it already mentions the `NU1608` crash for a version mismatch.
2. **`any generate` before restore.** `any generate Address ...` -> `✗ Address: this project has not been restored, so its package
   references cannot be read. Restore it, then run the command again: dotnet restore`. Clear and actionable (I ran `dotnet restore`
   and it worked); the getting-started page just never mentions the order. The first line of the CLI page's "Requires" would be a
   natural place.
3. **No `BigInteger` generator, and the tool's row for it is misleading.** `TokenAmount(BigInteger baseUnits)` is the heart of this
   domain. The recap says `baseUnits  BigInteger  new AnyBigInteger()  to verify, AnyX, unread guards`, i.e. "drawn through the
   generator that type owns" (the `AnyX` word in the table of the CLI page), but `BigInteger` is a framework struct and nobody owns
   an `AnyBigInteger`: the file would not compile (`CS0246`, as the page itself says for a not-yet-scaffolded type; I wrote the
   class straight away and did not watch the error). `numbers.en.md` lists fourteen numeric types and `BigInteger` is not
   one of them, and no page says so; I only learnt it by grepping the docs for the word. What the hand-written class must
   offer (`Func<AnyBigInteger, AnyBigInteger>` is in the generated signatures, so a builder with `Between`, `AtLeast`, `AtMost`)
   is something I had to infer from the generated file. A `no generator` word in the last column (like `unavailable`, but for a
   type JustDummies has no factory for at all) and a sentence in the numbers page would have saved the detour.
4. **Guards that tie parameters by arithmetic are all `to verify`, and the page's four forms do not cover them.** The domain has five
   of them: `gasLimit * gasPrice <= 2^256 - 1`, `gasUsed * gasPrice <= 2^256 - 1` (receipt), `supplyCap % 10^decimals == 0`,
   `initialSupply <= token.SupplyCap`, `minimumGasPrice * transfer <= cap`. The recap said exactly what it could not do
   (`gasLimit: unread guard at TransactionEnvelope.cs, line 27: ... — ties gasPrice to gasLimit`) and the build stopped at
   `CS0103: The name 'TODO_verify_the_generator_for_gasLimit' does not exist`, which is the documented design and worked as
   advertised. The cost is what comes next: the page offers start+duration, deriving inside `Generate()`, a check digit and
   `DifferentFrom`; my cases are "a ceiling that depends on another draw". I wrote `price % (Max / limit + 1)` inside
   `Generate()`, and then hit the consequence the page warns about (a pin on the price no longer reaches the draw). To keep a
   pinned impossible pair refused instead of silently folded, I added a `_gasPricePin` field, set it in `WithGasPrice(value)`,
   cleared it in the lambda overload, and copied it in the private copy constructor — in five generators. It works, but it is
   boilerplate the tool does not help with, and `--force` would erase it. An `idea` (a documented pattern, or a hook in the
   generated `Generate()`), not a bug.
5. **A guard on a member of the parameter (`sender.IsZero`, `address.IsZero`, `minter.IsZero`) marks the parameter `to verify` in seven
   places**, every one an `Address`. Documented (a comparison through a member), and the "tip" (move it into the value's own
   constructor) does not apply because the zero address is a valid `Address`. I added `AnyAddress.NonZero()` in a partial file and
   edited each `AnyValid...()` by hand. Fine, but the same one-line edit seven times.
6. **The regex retype was exactly as documented, and still four edits.** `Address` and `TokenDefinition.Symbol` and
   `TokenPrice.QuoteCurrency` all have a regex guard; the TODO line says to retype `AnyString` to `AnyPattern` "here, on the recipe
   field, in the recipe constructor and in the Func of WithX". A search-and-replace of `AnyString` by `AnyPattern` in the file did
   it in each case. Worth saying that this works because the file had no other `AnyString`; in `AnyTokenDefinition` the name
   (`Any.String()...`) was also retyped, which is what I wanted, but a file with a second string parameter that should stay an
   `AnyString` would need care.
7. **The gas limit is not a dummy when funds are tight.** My first helper drew the limit in `[65 000, 1 000 000]`; two tests then failed with
   `LedgerRefusedException : ... keeps 1500000 after the operation; the maximum fee is 1640856.` (seed -295207223). Not a
   defect: the check is on the *maximum* fee, so the draw reached the outcome. I bounded the limit to `[65 000, 100 000]` in the helper
   (the scenario's balances are sized for that) and pinned the limit in the tests that sit at the edge. Recorded because it is a nice
   case of the draw teaching what the test is about; the seed in the failure output made the diagnosis quick.
8. **Scaffolded `With…` methods use `Any.Fixed(value)` for a pin; my own derived rules need the pin.** Same as 4: the pin machinery of
   the generated file is exactly what you want in a plain generator and exactly what you must bypass in a tied one. Mentioned
   once more because it is where I spent most of the generator time (about a third).
9. **Nothing else in the library got in the way**, which is itself a result: `Any.Ledger().WithToken(t => t.WithSupplyCap(...).WithDecimals(d => d.Between(0, 12)))`
   composed three levels deep on the first try, `Any.ListOf(Any.Address().NonZero()).WithCount(n).Distinct()` drew distinct
   addresses, `AnyPattern.DifferentFrom` worked, and the `[assembly: Reproducible]` seed was printed in the "Standard output" of every
   failing test (and only those).

Domain findings (kept as skipped tests, `Skip = "domain defect: ..."`):

10. **`$` also matches before a final line feed, in four patterns.** `new Address(validAddress + "\n")` is accepted and kept (the
    `Value` then ends with a line feed); `new TokenDefinition("Example Token", "ABC\n", 4, cap)` accepts the symbol `ABC\n`;
    `new TokenPrice(ExactDecimal.One, "USD\n")` accepts the currency; `ExactDecimal.Parse("12\n")` and `ExactDecimal.Parse("1.5\n")`
    parse. All four contradict "exactly `0x` + 40 lower-case hex digits", "`^[A-Z][A-Z0-9]{1,10}$`", "`^[A-Z]{3}$`" and "`-?digits(.digits)?` only"
    of MODEL.md; the cause is `[GeneratedRegex("^...$")]` without `RegexOptions` that anchor with `\z`. Seeds of the first failing
    runs: Address 1324976036, ExactDecimal 852058983, symbol 1639000521, currency 1608270057 (the inputs are literals plus a
    drawn valid address, so any seed shows it).
11. **`Ledger.Receipts` / `Ledger.Transfers` are the backing `List<T>`s returned as `IReadOnlyList<T>`.**
    `((List<Receipt>)ledger.Receipts).Clear()` empties the log. Low (needs a deliberate downcast), no balance can be altered this
    way, but the "read-only" claim is only by type.

## What worked well

- **The recap's last column is a good idea executed well.** Every `TODO`, `to verify`, `guard` and `AnyX` told me what to look at; the
  line number and the quoted guard (`ties gasPrice to gasLimit`) pointed at the right statement. The build failing on
  `TODO_verify_the_generator_for_...` made the work list impossible to forget: I did not have to remember which of the ten files
  were finished.
- **It inferred what it could and was right every time:** `scale` -> `Between(0, ExactDecimal.MaxScale)` (reading a
  `ThrowIfGreaterThan` helper), the gas-limit window with the domain's own constants, and above all `transfer <= transferFrom` as an
  `Any.OrderedPair`, which saved me a real piece of reasoning.
- **Refining lambdas across levels.** `Any.Ledger().WithToken(t => t.WithSupplyCap(...).WithDecimals(d => d.Between(0, 12)))` and the
  `Chain.Next(...)` helper (`WithGasLimit(l => l.Between(...))`, `WithDeadlineBlock(d => d.GreaterThanOrEqualTo(ledger.CurrentBlock))`)
  read as what they mean; pinning is `WithX(value)`, refining is `WithX(x => ...)`, and I never mixed them up.
- **`--entry-point any`** gave `Any.Ledger()` next to `Any.Int32()` with no ceremony on .NET 10 (C# 14).
- **Reproducibility is quiet and useful.** Green runs print nothing; a failure prints one line with the seed and the attribute to
  copy. `[assembly: Reproducible]` was one line and every one of the 452 cases got its own seed. Replay worked, including through my
  own `AnyBigInteger` (it draws through `Any.UInt64()`, so it inherits the seed for free).
- **The golden rule shaped the tests in a way I could feel.** Each time an `any...` value reached an assertion I stopped and made it a
  literal (the first drafts of the amount tests round-tripped drawn values); each time a refusal test needed "some malformed value",
  the draw said so (`Any.StringMatching("0x[0-9a-f]{39}")` for "one digit short").
- **Analyzers stayed silent on 450 tests with all 38 rules on**, and fired when I tried them: no false positive to argue with.
- **`Any.StringMatching` for formats** (addresses, symbols, currencies) made both the generators and the malformed-input tests
  one-liners.

## Fluency: 4 / 5
A newcomer's path was short — one page of getting-started, one page of CLI, and the first `any generate` gave a working shape — and
the tool's honesty about what it could not infer turned an otherwise opaque domain (256-bit amounts, ties between gas, price and
supply) into a checklist. The score is not five because the two places where this domain pushed on the edge cost real time: a
`BigInteger` the library has no generator for and the tool labels as if it had (friction 3), and parameters tied by arithmetic
(friction 4/8), where the documented forms do not apply and the generated pin machinery has to be bypassed by hand in five files. The
test-runner setup on .NET 10 (friction 1) cost minutes more than it should have, though not through the library's fault.
