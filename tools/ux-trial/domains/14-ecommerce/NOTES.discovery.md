# Discovery notes — 14 E-commerce
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`, `docs/README.md` (index), `guides/getting-started.en.md`, `guides/core-concepts.en.md`,
`packages/justdummies-cli.en.md` (all of it, it is the long one), `packages/justdummies-xunit.en.md`, `guides/composition.en.md`,
`generators/enums-and-choices.en.md`, the first 260 lines of `generators/strings.en.md`. I did not read the analyzers pages (no diagnostic ever appeared).

Commands:
- `dotnet restore` (needed before `any`, see friction 2), then
  `any generate Money DiscountPercentage Quantity Sku ShippingAddress GiftCard PromoCode OrderLine Order --entry-point any` (one call, nine types, 18 files).
- `dotnet test --project Tests.Discovery` (needed a `global.json`, see friction 1).

Generators completed by hand (every `TODO(any)` / `to verify` line deleted):
- `AnyMoney.currency` -> `Any.String().OneOf("EUR", "USD", "GBP")`; `amount` left as scaffolded (`GreaterThanOrEqualTo(0m).WithScale(2)`).
- `AnyDiscountPercentage.value` -> `Any.Decimal().Between(0m, 100m).WithScale(1)`.
- `AnySku.value` -> `Any.StringMatching("[A-Z]{3}-[0-9]{4}")`, with the `AnyString` -> `AnyPattern` retype on every line (done as a blunt find/replace in the file).
- `AnyGiftCard.number` -> `Any.String().Numeric().WithLength(16)`; `pin` -> `Between(0, 9999)`.
- `AnyPromoCode.maxUses` -> `Any.Int32().Positive()` (the tool already paired `validFrom`/`validUntil` through `Any.OrderedPair`).
- `AnyShippingAddress`: country `OneOf("FR","BE","DE")`, postal code 5 digits by default, and `Generate()` edited so that a Belgian address draws 4 digits.
- `AnyOrder.lines`: retyped to `IAny<IReadOnlyList<OrderLine>>`, base = draw a currency, then `.Then(currency => Any.ListOf(new AnyOrderLine().WithUnitPrice(p => p.WithCurrency(currency))).NonEmpty())`; the refining `WithLines(Func<...>)` overload deleted.

Tests written (13 files, 157 methods, 295 cases with theory rows; xUnit v3 + `[assembly: Reproducible]`):
- `MoneyTests` (currency/amount refusals, equality, ToString, Plus, Times, Less rounding), `DiscountPercentageTests`, `QuantityTests`, `SkuTests`,
  `ShippingAddressTests` (including check order), `GiftCardTests` (Redeem), `PromoCodeTests` (bounds, check order, `AppliesTo` boundaries), `OrderLineTests`,
  `OrderTests` (constructor checks and their order, copy of lines, derived amounts, whole lifecycle), `ShippingFeePolicyTests` (every method x country, threshold on both sides, after-discount threshold, undefined method).
- Literals for everything an assertion is about (prices, quantities, percentages, boundaries); dummies (`Any.Sku()`, `Any.ShippingAddress()`, `Any.Order()`, `Any.Money().Generate().Currency`, ...) for everything the test only needs to exist.

Result: green, 286 passed, 9 skipped (domain defects, see below), 0 failed. Run 3 times at the end plus 12 times in a loop with `--no-build`: 15 repeated runs, all green with fresh seeds.
Skipped as `domain defect`: Sku and postal code accept a trailing line feed and non-ASCII digits (`$` and `\d`), `Money.ToString()` is culture-dependent, `GiftCard.Redeem(-10)` raises the balance,
`Money` overflows (`Times`/`Plus`, seed 1306754213 kept on a drawn case), a null element in `lines` gives a `NullReferenceException`.

## Frictions and surprises, in order
1. **`dotnet test` does not run on the .NET 10 SDK until you opt in, and the docs never say so.** The brief's command `dotnet test --project Tests.Discovery` first gave
   `MSBUILD : error MSB1001: Unknown switch ... Switch: --project`; plain `dotnet test` in the project folder gave
   `Microsoft.Testing.Platform.MSBuild.targets(395,5): error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.`
   I expected the getting-started page to tell me how to run the first test. The only `dotnet test` in the documentation is the repository's own `dotnet test --solution JustDummies.sln` (PROJECT-README, contributing section, which is for maintainers), and the xunit page says nothing about it.
   I created `W/global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }`, after which `--project` worked. Not strictly a library defect (it is xUnit v3 on .NET 10), but a newcomer following "install `JustDummies.Xunit`" hits it on the very first run.
2. **`any generate` needs a prior restore, and says so well.** First call: `✗ Money: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`. Clear and actionable; I ran `dotnet restore` and it worked. I would only have liked the CLI page to list restore as a prerequisite next to "Run it from your test project".
3. **Batch scaffolding of nine types worked first time and the recap was genuinely useful** (see "What worked well"), but the output is long: 80 lines for nine types. I only needed the `to verify` rows; `--format json` exists for that, which I did not try.
4. **The tool could not read a switch expression nor `is not ("FR" or "BE" or "DE")`.** Both `Money.currency` and `ShippingAddress.countryCode` came out `Any.String()  to verify, unread guards`, with
   `currency: unread guard at Money.cs, line 14: Currency = currency switch { ...` and `countryCode: first unread guard at ShippingAddress.cs, line 14: if (countryCode is not ("FR" or "BE" or "DE")) { ..., and 1 more`.
   The "what the tool reads" table lists `p == E.Member` and `p is E.Member` for enums but no constant-pattern or `switch` on a string. A closed set of string constants is very common in a domain (currencies, countries), and I expected `Any.String().OneOf("EUR","USD","GBP")` to be inferred. I wrote it by hand (one line each), so the cost was low. Idea, low.
5. **The cross-parameter rule (postal code depends on country) is a hand-edit of `Generate()`, with a silent consequence.** The tool marked `postalCode: ... — ties postalCode to countryCode`, and the CLI page's "Derive one draw from another inside `Generate()`" section told me exactly what to do and what I would lose.
   I edited `Generate()` so that `BE` draws four digits; consequence, as documented: `Any.ShippingAddress().WithCountryCode("BE").WithPostalCode("1000")` silently ignores the pinned postal code (it is only honoured for FR/DE). I noticed it only because I reread my edit; nothing warns. Friction, low.
6. **A rule across the elements of a list (all lines in one currency) cost four edits and a lost overload.** The tool: `lines ... Any.ListOf(new AnyOrderLine()).NonEmpty()  to verify, guard, unread guards` with `unread guard at Order.cs, line 42: if (lines.Select(line => line.UnitPrice.Currency).Distinct().Count() > 1)`.
   Drawing lines independently would mix currencies in about two orders out of three, so the generated file was wrong by construction. The docs have a section "When the replacement is another kind of generator" (for `AnyString` -> `AnyPattern`) and "A collection's elements, drawn another way" (an overload, but for the element recipe), and the `Then` page in Composition, but nothing that combines them: I had to work out that a `.Then(...)` result is an `IAny<...>`, retype the recipe field, the AnyValid method and the recipe constructor parameter to `IAny<IReadOnlyList<OrderLine>>`, and delete the `WithLines(Func<AnyList<OrderLine>, AnyList<OrderLine>>)` overload. It compiled the first time (`IAny<AnyList<OrderLine>>` is accepted where `IAny<IReadOnlyList<OrderLine>>` is expected), and the doc's last paragraph of that section ("A replacement that ends in `.As(...)` is an `IAny<T>`, not a builder ... type ... `IAny<string>` and delete the refining overload") was the hint that let me do it. A sentence extending it to `.Then` would have saved me ten minutes. Docs, low. Side effect: tests can no longer say `.WithLines(l => l.WithCount(3))`; they pin literal lines, which is what they wanted anyway.
7. **Pinning a `Local` DateTime into the scaffolded ordered pair is refused, at the pin.** `Any.Order().WithPlacedAt(new DateTime(2026,5,1,12,0,0, DateTimeKind.Local)).WithDeliveryDeadline(...)` threw
   `System.ArgumentException : The instant (...) has DateTimeKind.Local, which may represent a local clock time or an instant; the library cannot determine the intended interpretation. Pass instant.ToUniversalTime(), a Utc or Unspecified value, or use Any.DateTimeOffset() ...` with a stack through `AnyDateTime.RefuseLocal`, `Any.OrderedPair`, `AnyOrderedPair.WithFirst`, `AnyOrder.PlacedAtAndDeliveryDeadline`.
   The message is excellent and tells exactly what to do. What surprised me: the domain explicitly accepts any `DateTime.Kind`, and the CLI page says "A pinned value is a literal ... it is checked against the other base, not its own", so I did not expect a pin to be refused for a property of the value itself. I built the `Order` through its constructor for that one test. Low.
8. **An unconstrained `Money.amount` draws values that overflow the domain's own arithmetic.** The tool's `Any.Decimal().GreaterThanOrEqualTo(0m).WithScale(2)` is a faithful reading of `Money`'s guards, and a throw-away probe over 2000 draws showed `Times(99)` throwing `OverflowException` on 189 of them (max drawn amount 78980241216123978791657934959), and `Any.Order().Generate().Total` throwing on 205 of 500. This is the draw doing its job (the domain has no upper bound), not a library problem; it is logged as a domain finding below. What I did with the generator: left it as scaffolded, because every test that computes an amount pins literal prices, and added one skipped test with a replaying seed (`Reproducible(Seed = 1306754213)` reproduces the overflow, verified).
9. **`[Reproducible]` report is clear.** On every failure the output carried `[JustDummies] These arbitrary values were seeded with 999371606. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 999371606)].` Nothing to complain about; the replay of seed 1306754213 reproduced the overflow exactly.
10. **My own mistake, and what it taught me about "literals vs dummies".** My first run had one failure that was a wrong expectation of mine (`33.33 - 10 % = 30.00`, I had written `29.99`). Not a library issue; recorded because the failing row printed the theory arguments, which made it obvious.

## What worked well
- The getting-started page's "what is a dummy" argument is the best part of the documentation: it told me, before I wrote a line, that prices, quantities and percentages had to stay literals and that SKU, address and ids were the dummies. My tests came out that way without effort.
- `any generate` handled nine interdependent types in one command, wired `OrderLine` -> `AnySku/AnyQuantity/AnyMoney` and `Order` -> the others by name, and found by itself the `placedAt >= deadline` rule (`Any.OrderedPair`) and the `validUntil < validFrom` rule in `PromoCode`. It also named the Quantity constant (`LessThanOrEqualTo(Quantity.Maximum)`).
- The `TODO` / "does not compile until you resolve it" design: every unresolved guard was a compile error at the exact line, so I could not forget one. After my edits `dotnet build` was clean (no analyzer warning at all on the test project).
- `With…` pins and lambdas on the scaffolded generators read well in tests: `Any.ShippingAddress().WithCountryCode("FR").Generate().IsDomestic`, `Lines.OrderOf(...).WithDiscount(...)`.
- Seeds: reported on failure, replayable, and `[assembly: Reproducible]` is a one-liner.
- Error messages from the library are exemplary (friction 7's message names the cause and three ways out).

## Fluency: 4 / 5
The documentation and the tool got me from zero to a 295-case green suite in a single sitting, and the philosophy page changed how I wrote the tests. Points lost for the first-run `dotnet test` wall on .NET 10 that nothing in the docs prepares you for, for the effort of encoding two cross-field/cross-element rules (postal code vs country, one currency across a list) which the tool rightly refuses to guess but whose by-hand recipe is spread over three doc sections, and for a pinned `Local` date being refused by a scaffolded generator when the domain accepts it.

## Domain findings (not library)
- `Sku` and `ShippingAddress.postalCode`: `$` matches before a final line feed (`"TSH-0042\n"`, `"75001\n"` accepted) and `\d` matches non-ASCII digits (`"TSH-٠٠٤٢"`, `"٧٥٠٠١"`). The MODEL states this as intended pattern behaviour; whether it is desired is a business call, I think it is a defect. Tests skipped.
- `Money.ToString()` uses the current culture: `"12,50 EUR"` under fr-FR, while the model specifies the `"0.00 EUR"` form. Test skipped.
- `GiftCard.Redeem` with a negative amount raises the balance (50 -> 60) and returns 0 (the model says "no check on `amount`"). Test skipped.
- `Money` has no upper bound: `Times`/`Plus` throw `OverflowException` for valid amounts (~1 in 10 draws of an unconstrained amount, seed 1306754213). Tests skipped.
- `Order` with a `null` element in `lines` throws `NullReferenceException` rather than `ArgumentNullException`/`ArgumentException`. Test skipped.
- `PromoCode.AppliesTo` ignores the basket currency (documented) — tested as documented, not as a defect.
