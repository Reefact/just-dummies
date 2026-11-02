# Expert notes — 11 Telecom
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
- Pages read, in order: `PROJECT-README.md`, `guides/getting-started.en.md`, `guides/core-concepts.en.md`, `packages/justdummies-xunit.en.md`
  (first part), `packages/justdummies-cli.en.md` (the table of what is read, the `to verify` section, the ordered-pair / tie section),
  `guides/composition.en.md` (OrderedPair), `generators/numbers.en.md`, `dates-and-times.en.md`, `strings.en.md`, `enums-and-choices.en.md`,
  start of `guides/reproducibility.en.md`. Then `MODEL.md` and the whole `Domain/`.
- `any` commands: `dotnet restore`, then one run for all eight types:
  `any generate Msisdn Imei Iccid BillingPeriod DataBundle Plan UsageRecord MobileLine --output Generators --namespace Telecom.Tests.Expert.Generators --entry-point any`
  (a first run before the restore was refused, see friction 1).
- Generators completed by hand (all in `Tests.Expert/Generators/`):
  - `AnyMsisdn`, `AnyIccid`: base retyped `AnyString` -> `AnyPattern` with `Any.StringMatching("[1-9][0-9]{7,14}")` / `"89[0-9]{17,18}"` (the edit the sentinel comment describes).
  - `AnyImei`: base is `Any.String().Numeric().WithLength(14).As(body => body + Imei.LuhnCheckDigit(body))`, retyped to `IAny<string>`; refining overload deleted (nothing to refine on an `.As`).
  - `AnyDataBundle`: `.MultipleOf(100)` added to megabytes. `AnyPlan`, `AnyMobileLine`: sentinels removed; `activatedOn` got `AfterOrEqualTo(2000-01-01)`.
  - `AnyUsageRecord`: sentinels removed (`Any.DateTime()` is always Utc); the SMS tie written in `Generate()` (`kind == Sms ? 1 : quantity`).
  - `AnyBillingPeriod`: rewritten by hand (start, then a length of 1 to 31 days; `StartingOn`, `EndingOn`, `Lasting`), because the scaffold's `OrderedPair` cannot hold the 31-day rule.
- Tests (108 methods, 207 cases with theory rows; 5 skipped): `MsisdnTests`, `ImeiTests`, `IccidTests`, `BillingPeriodTests`, `DataBundleTests`,
  `PlanTests`, `UsageRecordTests`, `MobileLineTests` (creation, usage, life cycle, handset, plan, billing). `[assembly: Reproducible]` in `AssemblyInfo.cs`.
  Rules I held myself to: boundaries and formats are literals (an IMEI check digit, 32 days, 41 characters, 5 121 MB); everything the rule does not consult is drawn
  (period dates, plan name, line number, handset, validity, bundle size, instants); a value that reaches an assertion is built, not drawn.
- Result: green (202 passed, 5 skipped, 0 failed) at the first run after one fix of my own (friction 8), then 52 further runs green with fresh seeds (12 + 40).
- Skipped on purpose, each with `Skip = "domain defect: …"` (deterministic cases, so no seed): trailing line feed accepted by `Msisdn` and `Iccid` (the `$` in .NET),
  `DataBundle` price 0.015 accepted although documented as "in cents", `UsageRecord` with an undeclared `UsageKind` accepted, `MobileLine.Usage` castable back to `List<UsageRecord>`.

## Frictions and surprises, in order
1. **`any generate` on a project that was never restored.** `any generate Msisdn Imei ...` printed
   `✗ Msisdn: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`
   and nothing else (no file). Clear and actionable, but the tool could not do the restore itself; I ran `dotnet restore` and re-ran. Fine, low cost.
2. **`dotnet test --project Tests.Expert` fails on a .NET 10 SDK until the repository opts in to the new runner.** Command: `dotnet test --project Tests.Expert`.
   Output: `MSBUILD : error MSB1001: Unknown switch.` (`--project` is passed through to MSBuild); `dotnet test Tests.Expert` gives
   `Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later`. I had to add a `global.json` with
   `{ "test": { "runner": "Microsoft.Testing.Platform" } }` next to the solution. Not a JustDummies defect, but neither the xUnit adapter page nor Getting started
   says how to run an xunit.v3 test project on .NET 10 (they talk about the attribute only); a consumer meets this wall on minute one.
3. **The scaffold is excellent where the constructor is plain, and honest where it is not.** The recap column (`guard`, `to verify`, `unread guards`) matched the domain
   closely: `Plan.name` -> `NotBlank().WithMaxLength(40)`, `monthlyFee` -> `GreaterThanOrEqualTo(0m).WithScale(2)`, `DataBundle.price` -> `Between(0.01m, 500m)`,
   `validity` -> `GreaterThanOrEqualTo(TimeSpan.FromDays(1))`, `AnyMobileLine` composing `new AnyMsisdn()/AnyImei()/AnyPlan()`. The build-breaking sentinels did exactly
   their job: every `to verify` was a real rule (regex, Luhn through a `void` helper, `% 100`, `Kind != Utc`, `Year < 2000`, SMS tie).
4. **Sentinels on rules the draw already honours.** `UsageRecord.occurredAt` (`Kind != DateTimeKind.Utc`) and `MobileLine.activatedOn`/`Plan.tier` are marked `to verify`
   (build stops at `TODO_verify_the_generator_for_occurredAt`) although `Any.DateTime()` is documented as always Utc and `Any.Enum` never draws an undeclared member
   (the `tier` switch). The tool cannot know, and the cost is only reading the library page to confirm and deleting two lines; but for `Kind` a recognised
   `occurredAt.Kind != DateTimeKind.Utc` -> "nothing to add" row would remove a sentinel that is a pure false positive for this library.
   (`Year < 2000` is a real gap: the scaffold would draw years 1 to 1999 and the line would be refused on almost every run.)
5. **`BillingPeriod`: the scaffold pairs `start`/`end` with `OrderedPair`, then marks both `to verify` for the 31-day rule — and a deleted sentinel would be wrong on almost every run.**
   Recap: `start DateOnly Any.DateOnly() to verify, guard, unread guards`, comment `… if (end.DayNumber - start.DayNumber > 31) { ... — ties end to start`.
   `Any.DateOnly()` spans 0001..9999, so two ordered draws are years apart and nearly every `Generate()` would throw. The sentinel is the correct stop; the fix the CLI page
   recommends ("draw a start and a duration") is right, but it meant discarding the whole scaffolded file (about 120 lines of pin/refine machinery for the pair) and writing a 50-line
   class by hand. That class is not `partial`, so a later `any generate BillingPeriod --force` would overwrite it silently. An expert reading the file understands, a newcomer
   might delete the two sentinel lines and get a generator that fails at run time. Idea: when a guard ties two parameters by arithmetic, say in the recap line that
   the paired recipe will not satisfy it ("the OrderedPair does not bound the distance").
6. **`UsageRecord`: a tie I could only express by overriding a pin.** `if (kind == Sms && quantity != 1)`. I wrote `kind == Sms ? 1 : quantity` in `Generate()`. It keeps every
   draw valid, but `Any.UsageRecord().WithKind(Sms).WithQuantity(5)` then returns a quantity of 1 without a word — a pinned value that lies, which is exactly what the library is
   built against. The CLI page names the trade-off for derived parameters ("its `With…` pin no longer reaches the draw"), so I knew; the alternatives
   (excluding `Sms` from the base, or a named recipe `.Sms()`) each cost something else. No clean answer for a discriminated-union-shaped constructor; I would welcome a documented idiom.
7. **A refinement lambda is not available when the base is an `.As(...)` chain.** For `Imei` the docs are right (type the recipe `IAny<string>`, delete the refining overload) and it
   worked, but it is a hand edit of five places in a scaffolded file for the most common value-object shape (checksum). A `--base` hook or a partial method returning the recipe would
   spare it. Low.
8. **My own mistake, found by the first red run (not a drawn value):** `A_name_of_41_characters_is_refused` asserted `ParamName == "name"` and got `"name.Length"`, because
   the guard `ArgumentOutOfRangeException.ThrowIfGreaterThan(name.Length, 40)` reports the expression. Seed was printed and the failure was deterministic. I relaxed the assertion
   (`StartsWith("name")`) and noted the oddity as a domain remark. The seed report format helped: `[JustDummies] These arbitrary values were seeded with -1783799329. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = -1783799329)].`
9. **Entry point `--entry-point any` needs C# 14: fine on net10.0.** `Any.Plan().WithTier(...)` reads very well. One cosmetic point: the scaffolded `With…(Func<AnyEnum<PlanTier>, AnyEnum<PlanTier>> configure)` gives `WithTier(tier => tier.Except(PlanTier.Unlimited))`, which is the exact way to say "a metered tier" without naming one; I used it in `PlanTests` and `MobileLineTests.Metered`.
10. **No analyzer noise at all**: `dotnet build` reported 0 warnings and 0 errors on the generators and the eight test classes (JD030 is Info, hidden by default).
11. **The scaffolded `Plan` is valid for the constructor but not for the computations downstream.** `overagePerGb` and `monthlyFee` are one-sided, so about one draw in ten may sit near `decimal.MaxValue`; the allowance reaches `int.MaxValue`. `Plan.OverageFor` multiplies `startedGb * OveragePerGb` unchecked-in-decimal and `BillFor` sums `long` quantities in a checked `Sum`, so they can throw `OverflowException`
    (I confirmed `new Plan("x", Postpaid, 0m, 1, decimal.MaxValue).OverageFor(3072)` and two data records of `long.MaxValue` both throw). The draw is right to reach there — see domain findings — and my billing tests pin fee, allowance, rate and quantities, as the subject, so they never meet it.

## What worked well
- The recap table is a good review tool: reading it was enough to decide, per parameter, what to keep, retype or rewrite.
- A scaffolded `AnyMobileLine` that composes the generators other types own made `Any.MobileLine().WithPlan(plan).Generate()` the whole arrange for any status test; `Any.Imei().DifferentFrom(line.Handset)` (docs: redraw on any generator) worked at first try and states the swap precondition in one line.
- Pin versus lambda is a real design aid for BillFor tests: the plan's name is drawn, its fee/allowance/rate pinned (they are what the assertion is about), the line's number and handset stay arbitrary, and the reader sees which is which.
- `[assembly: Reproducible]` and the seed in the failure output: the single red run of the whole trial (friction 8) printed the replay attribute ready to paste.
- The distinction "a value that reaches an assertion is not a dummy" is operational: in several tests I reached for `Any.UsageRecord()` and wrote `new UsageRecord(...)` instead as soon as the record appeared in `Assert.Equal`.
- Constructive generation: no retries observed; 52 repeated runs with 207 cases each stayed green and fast (about 1.8 s per run).

## Fluency: 4 / 5
Everything the domain needed was a documented one-liner, and the scaffold plus the build-breaking sentinels put me in the right place almost every time. Points lost for the three spots where a cross-parameter or
derived rule (BillingPeriod's 31 days, UsageRecord's SMS rule, Imei's check digit) forced me to leave the scaffold's shape for hand-written code, with no `partial`/`--force` protection and one pin that now lies; for the
`dotnet test --project` wall on .NET 10; and for two sentinels that were pure false positives for this library (`Kind != Utc`, enum `switch`).
