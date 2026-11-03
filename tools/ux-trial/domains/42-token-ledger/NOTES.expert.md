# Expert notes — 42 token ledger
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did

**Pages read, in order:** `PROJECT-README.md`, `guides/getting-started.en.md`, `guides/core-concepts.en.md`,
`packages/justdummies-cli.en.md` (all of it, the guard table twice), `packages/justdummies-xunit.en.md`,
`generators/README.md`, `generators/numbers.en.md`, `guides/composition.en.md`, `generators/strings.en.md`
(patterns and `AnyPattern`), `generators/enums-and-choices.en.md` (first half), the severity section of
`packages/justdummies.en.md`. I read `MODEL.md` and all of `Domain/` before scaffolding anything.

**`any` commands run** (from `Tests.Expert`, after `dotnet restore`):

```
any generate Address TokenAmount ExactDecimal --output Generators --entry-point any
any generate TokenDefinition TokenPrice GasSchedule TransactionEnvelope Receipt TransferRecord --output Generators --entry-point any
any generate Ledger --output Generators --entry-point any
```

`Account`, `Enums`, `LedgerRefusedException` were not scaffolded (Account is built only by the ledger; the exception
carries a reason and a message, two literals).

**Generators completed or rewritten by hand** (`Tests.Expert/Generators/`):

| File | What I did |
| --- | --- |
| `AnyBigInteger.cs` | written from scratch: JustDummies has no `BigInteger` generator (see friction 3). Built on `Any.ArrayOf(Any.Byte())`, so it follows the seed. `Between`/`GreaterThan…`/`LessThan…`, an open side drawn within 10^40 of the other. |
| `AnyAddress` | `Any.String()` retyped to `Any.StringMatching("0x[0-9a-f]{40}")` (`AnyPattern` on 4 lines, as the TODO said). |
| `AnyTokenAmount` | base `Between(0, TokenAmount.MaxBaseUnits)`. |
| `AnyExactDecimal` | nothing but `AnyBigInteger` (it compiled as scaffolded once that existed). The scale guard (`ThrowIfNegative` + `ThrowIfGreaterThan`) was read correctly. |
| `AnyTokenDefinition` | name -> `StringMatching("[!-~]([ -~]{0,48}[!-~])?")` (retyped); symbol -> `StringMatching("[A-Z][A-Z0-9]{1,10}")` (retyped); supply cap: base of at least 10^36 base units, rounded down in `Generate()` to a whole number of tokens of the drawn decimals, unless the cap is pinned (hand-added `_supplyCapPinned` flag). |
| `AnyTokenPrice` | currency retyped to a pattern; price base `mantissa >= 1`, `scale <= 18`. |
| `AnyGasSchedule` | the five base costs `Between(IntrinsicGas, BlockGasLimit)`; surcharge bounded in `Generate()` by `BlockGasLimit - max(transfer, transferFrom, mint)` unless pinned. The `transfer <= transferFrom` tie had been turned into an `OrderedPair` by the tool. |
| `AnyTransactionEnvelope` / `AnyReceipt` | sender non-zero; `gasPrice <= Max / gasLimit` (resp. `gasUsed`) enforced in `Generate()` unless the price is pinned. |
| `AnyTransferRecord` | both ends non-zero by default (a mint/burn pins `Address.Zero`). |
| `AnyLedger` | minter/collector non-zero; `initialSupply <= cap` and `minimumGasPrice <= cap / transfer` enforced in `Generate()` unless pinned. |

**Test support:** `Tests/Chain.cs` is the one hand-written helper: a ledger plus its cast (minter, collector, Alice, Bob,
Carol: five *distinct* dummy addresses drawn with `Any.SetOf(...).WithCount(5)`), an `Envelope(sender)` method returning the
scaffolded `AnyTransactionEnvelope` pre-narrowed to "valid on this ledger now" (next nonce, gas limit 65 000..100 000, price 10,
deadline not passed), `Fund`, and `Refuses(reason, action)` which asserts the reason **and** that balances, nonces, allowances,
supply, block, receipts, transfers and account count are unchanged (the "nothing changes, nonce included" rule).

**Tests written** (`Tests/`, 525 cases in 370 methods; 4 skipped on purpose):

| File | Methods | What |
| --- | --- | --- |
| `AddressTests` | 14 | canonical text, zero, 11 malformed forms (theory), null, `Parse` (EIP-55, whitespace, `0X`, no prefix, null), equality |
| `TokenAmountTests` | 31 | range edges, refusals, `Add/Subtract/MultiplyBy/CanAdd` at the 2^256-1 edge (exact landing and one past), comparison, equality, text |
| `ExactDecimalTests` | 49 | normalisation (theory), scale bounds, equality/order, sign, `Parse` valid (theory) and 20 invalid forms, 80/81 digits, arithmetic, `Multiply` overflow at 80/81 digits and the trailing-zero rescue, `Divide` toward zero (theory, 12 rows) and refusals, `Truncate`, `ToScaledInteger` |
| `TokenDefinitionTests` | 35 | name/symbol/decimals/cap boundaries, whole-token cap, human <-> base units (rounding refused, never done), `Format`, equality |
| `TokenPriceTests` | 25 | positive/18-digit price, currency, `ValueOf` exact, `TokensFor` rounded down and overflow at exactly 2^256-1 |
| `GasScheduleTests` | 20 | Standard, every base-cost bound, the three ties (theory), `GasFor` for 7 kinds x holder flag (theory), equality |
| `TransactionEnvelopeTests` | 15 | fields, gas limit/price bounds, `MaxFee`, the 256-bit product edge, equality |
| `ReceiptTests` | 15 | idem for `Fee`, undefined kind |
| `TransferRecordTests` | 11 | mint/burn flags, zero-to-zero, zero amount legal, equality |
| `AccountAndRefusalTests` | 8 | account start state, refusal reasons |
| `LedgerConstructionTests` | 15 | initial supply, genesis record, roles, cap, price x transfer edge (exact and +1), nulls |
| `LedgerQueryTests` | 13 | unseen addresses, allowance direction, human balance, valuation, market cap, `AdvanceBlocks` (edges at `ulong.MaxValue`) |
| `LedgerTransferTests` | 38 | effects, receipt, log order, surcharge (3 cases), fee = gas used not limit, zero price, collector as sender, self-transfer, balance + max-fee edges (1 unit either side), every refusal reason, nonce/deadline edges, 6 precedence pairs |
| `LedgerAllowanceTests` | 25 | Approve/Increase/Decrease: replace, add, subtract, unlimited, overflow at exactly 2^256-1, zero spender, fee edges |
| `LedgerTransferFromTests` | 21 | spend, unlimited not decreased, fee paid by the spender, allowance vs balance precedence, "credits do not count" for the spender, spender == owner |
| `LedgerMintAndBurnTests` | 27 | mint effects/log/fee/surcharge, `NotMinter`, cap edge, minter minting to himself, burn effects and fee edges |
| `LedgerInvariantTests` | 8 | after every kind of transaction: sum of balances = supply, collector = sum of fees, receipt order, nonces, no account created by a refusal |

**Results.** First full run: green except the 4 domain defects below, which I confirmed red one by one before skipping them
(each skipped test keeps its body and carries the seed in `Skip = "domain defect: …, seed …"`). Then 75 further runs with fresh
seeds (15 + 60): all green (525 total, 521 passed, 4 skipped, 0 failed each time). All 38 analyzers raised to `warning` in an
`.editorconfig` (removed afterwards): zero diagnostics on the test project.

## Frictions and surprises, in order

1. **`dotnet test --project Tests.Expert` does not work out of the box on the .NET 10 SDK.**
   `MSBUILD : error MSB1001: Unknown switch ... --project`; without `--project`:
   `Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.`
   Needed a `global.json` with `{"test":{"runner":"Microsoft.Testing.Platform"}}` next to the solution. Nothing in the JustDummies
   or JustDummies.Xunit page mentions it, although the xunit adapter page says "xunit.v3 4.0.2 or later" and every command in the
   repo docs is `dotnet test`. Not a library defect; a one-line note in the xunit page would save a new user ten minutes.

2. **`any generate` before `dotnet restore`.** `✗ Address: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   Clear and actionable. Expected the tool to restore, but the message is good.

3. **`BigInteger` is scaffolded as if JustDummies owned a generator for it.**
   Recap: `baseUnits  BigInteger  new AnyBigInteger()  to verify, AnyX, unread guards` (TokenAmount) and
   `mantissa  BigInteger  new AnyBigInteger()  AnyX` (ExactDecimal, no mark at all). `AnyX` is documented as "drawn through the
   generator that type owns", but `BigInteger` is a BCL type and no generator exists; nothing in the docs (`generators/numbers.en.md`
   lists 14 numeric factories, `grep -ri biginteger docs` finds nothing) says so. The consequence is `CS0246: AnyBigInteger could not be found`
   on the first build, with no `TODO` sentinel naming the gap, so the "your build names the gap" promise holds only by accident.
   I wrote `AnyBigInteger` myself (about 60 lines, composed from `Any.ArrayOf(Any.Byte())`); the scaffolded
   `WithMantissa(Func<AnyBigInteger, AnyBigInteger>)` lambdas then worked unchanged. Wish: either a first-class `Any.BigInteger()`, or
   the tool should emit a `TODO` for BCL types it has no generator for. The case is not exotic (any money/crypto domain).

4. **Regex guards cost a four-spot retype each, five times.** Address, symbol, currency (and name, by my own choice). The TODO text
   is exact ("retype AnyString to AnyPattern here, on the recipe field, in the recipe constructor and in the Func of WithSymbol"),
   and I followed it with a script; every one compiled first time. But the retype is the same mechanical edit every time, and
   `AnyPattern` loses `StartingWith`/`WithLength`. It is the documented design; I only record the cost: a regex-validated value
   object is the most common shape in a domain, and the tool could offer the retyped form directly when the regex is inside the subset
   (it can parse the pattern, it prints it in the comment).

5. **The ties are flagged precisely, but resolving each is hand-written plumbing that must re-invent "a pin still pins".**
   The tool did well: `transfer`/`transferFrom` became an `AnyOrderedPair` on its own, `ties gasPrice to gasLimit` was named on both
   sides. But for the rest (price x gas limit <= 2^256-1 in `TransactionEnvelope` and `Receipt`; surcharge vs the dearest credit in
   `GasSchedule`; supply cap multiple of 10^decimals; `initialSupply <= cap` and `minimumGasPrice x transfer <= cap` in `Ledger`)
   I edited `Generate()` as the CLI page suggests ("derive one draw from another inside `Generate()`"). The page warns that the pin of the derived
   parameter then no longer reaches the draw. Concretely I had to add a `_xxxPinned` flag, set in `WithXxx(value)` and copied in the copy
   constructor, to keep `WithGasPrice(x)` meaning "this exact price"; I did that six times, identically. The `.Then(...)` seam the docs
   offer would do the same without a flag, but nothing in the generated file shows how to combine it with a pin. Wish (idea): for a tie
   the tool names, generate the dependent recipe as a `Func<TheOtherValue, IAny<T>>` plus the pin, so that the tied parameter is bounded by
   construction and a pin still wins.

6. **A tied builder refuses before the domain does, so refusal tests of a tied parameter must bypass the builder.**
   `AnyGasSchedule` carries the `transfer <= transferFrom` order as an `OrderedPair`, so `Any.GasSchedule().WithTransferFrom(20_999)`
   never reaches the `GasSchedule` constructor: the pair is refused first, with
   `JustDummies.ConflictingAnyConstraintException: Cannot apply WithSecond(20999) because no first value lies at or below 20999: Between(21000, 30000000) already requires values greater than or equal to 21000.`
   (clear, but it speaks of `WithSecond`/`WithFirst`, the pair's vocabulary, while I called `WithTransferFrom` on the schedule's builder). My
   first draft of the "each base cost is bounded" tests used the builder with an out-of-range pin, and they failed with the
   generator's exception rather than `ArgumentOutOfRangeException`. I wrote those tests against the constructor with a tiny
   `Schedule(transfer: …)` helper whose defaults are the Standard numbers. It is the right behaviour for a generator (it must not hand
   out invalid values) but worth stating in the CLI page: "a pin on a paired parameter is checked by the pair, not by your constructor".

7. **`Skip` plus seed on a defect that has no randomness.** The 4 skipped tests are deterministic (`"abc\n"` is accepted by a `$`-ended
   regex whatever the draw). `[Reproducible]` still prints a seed, which is noise in the `Skip` text the brief asks for. Only the
   wording of my own skip reasons suffered; no library action.

8. **Analyzers: silence is the result, which I could only believe after probing.** With all 38 rules at `warning` the project built
   with zero diagnostics. I injected `$"{Any.Int32()}"` and `Any.String().Generate()` once to prove the analyzers were alive
   (JD005 and JD030 fired with a link to the page). Worth saying that `dotnet_analyzer_diagnostic.category-JustDummies.severity`
   (without the sub-category) silently does nothing; `category-JustDummies.Constraints.severity` works, as the page shows.

9. **`Any.Address().DifferentFrom(other)` just worked, and it changed how I wrote equality tests.** Not a friction, a surprise: because
   `DifferentFrom` exists on every `IAny<T>` (bounded redraw), "two different addresses are not equal" is expressed without a literal.

10. **Domain observations (not library frictions).**
    - *Defect, 4 places:* every regex of the domain ends with `$` (`Address` canonical form, `TokenDefinition` symbol, `TokenPrice` currency,
      `ExactDecimal.Parse` syntax). In .NET `$` also matches before a final `\n`, so `new Address(valid + "\n")`, a symbol `"ABC\n"`, a currency
      `"USD\n"` and `ExactDecimal.Parse("12.5\n")` are all accepted, and `Address.Value` keeps the newline, contradicting "exactly `0x` + 40
      lower-case hex digits". The four tests are kept and skipped with their seeds (`AddressTests.An_address_followed_by_a_line_break_is_refused`
      seed 640351663; `TokenDefinitionTests.A_symbol_followed_by_a_line_break_is_refused` seed 594591566;
      `TokenPriceTests.A_currency_followed_by_a_line_break_is_refused` seed -929908857;
      `ExactDecimalTests.A_number_followed_by_a_line_break_is_refused` seed -1112055859, literal input). Fix: `\z` instead of `$`.
    - *Untestable branch:* `RefusalReason.NonceExhausted` cannot be reached through the public API (it needs an account at `UInt128.MaxValue` nonces,
      2^128 transactions; `Account.NextNonce` has a private setter). An envelope carrying `UInt128.MaxValue` for a fresh account is
      `NonceTooHigh`. I left it untested and say so here rather than fake it.
    - *Everything else of `MODEL.md` held*: the order of the eight checks (six precedence pairs tested), atomicity (nonce included), fee on gas used,
      credits not counting for the fee funds, unlimited allowance, cap and minimum-price ties.

## What worked well

- **`[assembly: Reproducible]`.** One file, and every one of 525 cases has a seed; the only failures I met (the `$` newline defects)
  printed `Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = …)]` and I used the number in the `Skip` text. The report is
  in the standard output of the failed test only, as documented.
- **The scaffold gets the shape of the domain right**: guards through `ArgumentOutOfRangeException.ThrowIfNegative`/`ThrowIfGreaterThan`
  (scale), `Between(GasSchedule.IntrinsicGas, GasSchedule.BlockGasLimit)` copied from a public `const`, the `OrderedPair` for
  `transfer <= transferFrom`, `AnyX` composition (`new AnyAddress()` inside `AnyTransactionEnvelope`), nullable pins, the `With…(Func)`
  refinement. 10 of the 38 constructor parameters needed no edit at all; the other 28 were flagged `to verify`.
- **The sentinel stops the build where a guard was not understood** (`_ = TODO_verify_the_generator_for_symbol`): the first build after
  scaffolding told me exactly which parameters to look at (28 of 38), and each comment named the guard's file and line.
- **Refinement lambdas as the "state only what the test is about" device.** `Any.Ledger().WithToken(t => t.WithSupplyCap(...).WithDecimals(d => d.Between(0, 12)))`
  reads as intended: the cap is the fact (mint tests are about it), the decimals are a dummy kept in the range the cap supports.
- **Pins reach the domain's own guards**: `Any.TokenDefinition().WithSymbol("abc").Generate()` throws the real `ArgumentException`, so refusal
  tests need no object construction by hand and the other three parameters stay dummies.
- **`Any.SetOf(Any.Address()...).WithCount(5)`** for a cast of distinct addresses.
- The docs' rule "a constraint states an invariant, never the assertion" made me delete 7 of my own tests that drew a value and
  asserted a property of it (round trips, generator smoke tests); what remained either uses literals where the value is the point or
  draws values that never reach the assertion (addresses, gas limits, deadlines, token decimals, block numbers).

## Fluency: 4 / 5

The library itself was fluent: after the CLI page I wrote and completed nine generators in under an hour and the 525 cases stayed green over 75
seeded runs, with the analyzers and the seed report behaving exactly as documented. Points were lost on a domain that sits at the edge of what the
scaffolder anticipates: the missing `BigInteger` generator (friction 3, the only real stop), and the amount of hand-written plumbing needed to honour
cross-parameter ties while keeping pins meaningful (friction 5). Everything else (test runner opt-in, regex retyping) was cost, not confusion.
