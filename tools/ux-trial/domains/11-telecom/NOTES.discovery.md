# Discovery notes — 11 telecom
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`, `docs/guides/getting-started.en.md`, `docs/packages/justdummies-cli.en.md`
(long; read it entirely because the scaffolder was my entry point), `docs/packages/justdummies-xunit.en.md`,
`docs/generators/README.md`, `docs/guides/composition.en.md`, then only the sections I needed in
`generators/dates-and-times.en.md`, `strings.en.md` (Patterns), `numbers.en.md` (Multiples and scale), `enums-and-choices.en.md`,
and `analyzers/JD036.en.md` after a warning showed up. I did not need `core-concepts`, `reproducibility`, `errors-and-conflicts`.
Then `MODEL.md` and the domain sources.

`any` commands run (from `Tests.Discovery`, after `dotnet restore`):
`any generate Msisdn Imei Iccid BillingPeriod DataBundle Plan UsageRecord MobileLine --entry-point any`
(eight types in one call, with entry points so the tests read `Any.Plan()`).

Generators completed by hand:
- `AnyMsisdn`, `AnyIccid`: regex guard -> `Any.StringMatching("[1-9][0-9]{7,14}")` / `"89[0-9]{17,18}"`, retyped `AnyString` -> `AnyPattern` on the 3 lines the TODO named.
- `AnyImei`: `Validate(value)` helper -> `Any.StringMatching("[0-9]{14}").As(body => body + CheckDigit(body))`, typed `IAny<string>`, refining overload deleted
  (as the CLI page says). The Luhn digit is computed in the generator, independently of the domain's `Imei.LuhnCheckDigit`.
- `AnyBillingPeriod`: the scaffold paired start/end with `Any.OrderedPair(strict: true)` but the 31-day cap ties them; I rewrote it as a start plus a length in days (1..31),
  keeping `WithStart` / `WithLengthDays` (pin and lambda).
- `AnyDataBundle`: `.MultipleOf(100)` added to megabytes (the `% 100` guard was left to verify).
- `AnyPlan`: removed the sentinel on `tier` (an enum draw only yields declared members).
- `AnyUsageRecord`: sentinels removed (`Any.DateTime()` draws Utc, per the dates page); Sms/quantity tie handled in `Generate()` (`quantity = kind == Sms ? 1 : drawn`).
- `AnyMobileLine`: `activatedOn` -> `Any.DateOnly().Between(2000-01-01, 2100-12-31)`.

Tests written (132 methods, 202 cases; 4 skipped): `MsisdnTests`, `ImeiTests`, `IccidTests`, `BillingPeriodTests`, `DataBundleTests`, `PlanTests`,
`UsageRecordTests`, `MobileLineTests` (construction, recording, status transitions as theories over `LineStatus`, handset swap, plan change, billing:
fee alone, allowance edge 2048/2049 MB, started gigabytes, voice/SMS ignored, period half-open boundaries, unlimited, suspended/terminated still billed, current plan only).
`[assembly: Reproducible]` in `AssemblyInfo.cs`. Dummies are used where the test does not care (tails of refused strings, the line's number/handset/plan when only
the status matters, tier on non-unlimited billing, the period start); fee, allowance, rate and quantities that appear in assertions stay literals.

Result: 198 passed, 4 skipped (domain defects below), 0 failed. The first run had 1 failure (my own wrong assumption, friction 7); after fixing it: 12, then 30 more
runs with fresh seeds, all green (42 consecutive green runs).

## Frictions and surprises, in order
1. **`any generate` before restore.** `any generate Msisdn ...` -> `✗ Msisdn: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   Clear and actionable; I expected the tool to restore itself, but the message is fine. (An empty `bin/Debug` and `obj/Debug` had appeared by then, apparently from the tool's own attempt.)
2. **`dotnet test --project Tests.Discovery` fails on the .NET 10 SDK.** `MSBUILD : error MSB1001: Unknown switch ... --project`. Without `--project`, `dotnet test Tests.Discovery` gives
   `Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later ... opt-in to the new dotnet test experience`.
   Neither the xUnit adapter page nor getting-started says how to run the tests; the xunit.v3 template needs a `global.json` with `"test": {"runner": "Microsoft.Testing.Platform"}`.
   I added it at the workspace root and the command worked. Not the library's fault, but the docs install `xunit.v3` and never say how to run it (first useful command for a newcomer).
3. **The scaffolded file does not compile on purpose, eight times.** With 8 types the first build gave a wall of `TODO_verify_the_generator_for_*` errors. I understood the mechanism from the CLI page
   (it is explained well there), and the recap's "to verify, unread guards" column told me where to look. Still, for a first run the page that explains this is the CLI page, not getting-started, which only says
   "scaffold a generator" with no mention that the output will not build.
4. **Regex guard retype is manual and spread over 3 lines.** The TODO text says exactly which ("here, on the recipe field, in the recipe constructor and in the Func of WithDigits"); a global `AnyString` -> `AnyPattern` replace did it. Fine, but
   an easy place to make a mistake by hand on larger types.
5. **`Imei`: the guard is in `Validate(value);` (a void helper)** -> marked `unread guard ... Validate(value);` with the base `Any.String()`. No help possible, the recipe is mine. The CLI page's "Draw the body, then derive the check digit" sample was
   exactly my case and I adapted it; the `IAny<string>` retype and deletion of the refining overload were also documented.
6. **`BillingPeriod`: tool-drawn `OrderedPair` would still violate the 31-day cap.** The recap marked both parameters `to verify, guard, unread guards ... ties end to start`. Had I only deleted the sentinels, the generator would draw
   two dates up to 9999 years apart and fail nearly every time. The block is a good thing; I followed the "draw a start and a duration" recipe from the CLI page. I lost `WithEnd(...)` in the process (nothing in the docs says how to keep a pin of the end).
7. **My own wrong assumption (not a library issue):** `Plan` name-length refusal has `ParamName == "name.Length"` (the `CallerArgumentExpression` text), not `"Length"` nor `"name"`. First run: 1 failed. I stopped asserting on it and recorded it as a domain wart.
8. **`UsageRecord`: the Sms/quantity tie.** The tool marked `kind` and `quantity` (`ties kind to quantity`). I derived quantity in `Generate()`. Consequence (the CLI page warns about it): `Any.UsageRecord().WithKind(UsageKind.Sms).WithQuantity(5)`
   silently draws quantity 1, so a pin is ignored. I left a comment in the file; nothing flags it.
9. **JD036 only visible after raising severity.** Default build is silent. After putting `dotnet_diagnostic.JD0xx.severity = warning` in an `.editorconfig` for all 38 rules (the docs say how in `justdummies.en.md#the-analyzers-travel-inside-the-package`; I used the JD036 page's hint)
   I got `AnyPlan.cs(51,16): warning JD036: This floating-point dummy declares no range: one draw in ten spans the whole type` and the same at line 59: both lines are **generated by the tool** (`Any.Decimal().GreaterThanOrEqualTo(0m).WithScale(2)` and `Any.Decimal().GreaterThanOrEqualTo(0m)`).
   So the scaffolder emits code its own analyzer flags. Defensible (the tool "will not invent a range the domain never declared"), but not stated in the CLI page, and one draw in ten of `monthlyFee`/`overagePerGb` is near `decimal.MaxValue`. I left it: `BillFor` tests pin fee and rate.
10. **Missed hesitation about `Any.Char().OneOf(...)`:** I used it without reading a page for `Any.Char()` (the generators index lists it under "Strings and characters"); it compiled and worked on the first try.
11. **Assembly-wide `[Reproducible]`** vs per test: the docs show the three levels clearly; I chose the assembly. A failure would print `[JustDummies] These arbitrary values were seeded with ... Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = ...)]` (seen once in the run of friction 7, exactly as documented).
12. **Docs bundle links:** `PROJECT-README.md` links to `doc/handwritten/for-users/...`, which do not exist in the `docs/` folder I was given (only `README.md` etc.). Probably a packaging artefact of the trial; I navigated via `docs/README.md`.

## What worked well
- The recap table from `any generate` (inferred / `guard` / `to verify` / `AnyX`) is honest and reads at a glance; it correctly read `ThrowIfLessThan(validity, TimeSpan.FromDays(1))`, the price range, the name length (`NotBlank().WithMaxLength(40)`), the decimal scale (`WithScale(2)`), the positive allowance.
- Composing aggregates: `MobileLine` drew `new AnyMsisdn()`, `new AnyImei()`, `new AnyPlan()` by itself, and `--entry-point any` made `Any.MobileLine().WithPlan(...)` read naturally.
- `With...` pin/lambda pairs made the tests very readable (`Any.Plan().WithTier(PlanTier.Postpaid).WithDataAllowanceGb(2).WithOveragePerGb(3.5m)`).
- `Any.StringMatching` as the tool for "any string with this shape" (tails of refused values), `Any.Enum<T>().Except(...)`, `Any.DateOnly().Between(...)` and `Any.Int64().GreaterThan(1)` all worked without surprise.
- Fail-loud design: contradictory/unsupported things stop the build at a line, never silently. The seed message appeared once, exactly like the docs.
- Four real domain defects were found just by writing refusal cases (below); the 202-case suite stayed green 42 times.

Skipped tests (kept, with reason): `MsisdnTests.Refuses_digits_followed_by_a_line_feed`, `IccidTests.Refuses_digits_followed_by_a_line_feed` (trailing `\n` satisfies `$`),
`UsageRecordTests.Refuses_a_kind_that_is_not_declared` (undeclared `UsageKind` accepted, `Plan` refuses undeclared tier), `MobileLineTests.Cannot_have_its_usage_changed_from_outside`
(`Usage` is the internal `List<UsageRecord>`; a cast to `IList<UsageRecord>` is writable). I verified in a throw-away copy that all four fail once unskipped (4 failed / 198 passed). They are literal cases, so there is no seed.
Other domain remarks: `Plan` name-length `ParamName` is `"name.Length"`; `DataBundle` price has no scale check although the type says "priced in cents"; `BillFor` bills a terminated line (documented).

## Fluency: 4 / 5
Once past two hurdles outside the library (running `dotnet test --project` on the .NET 10 SDK, and understanding that a freshly scaffolded file is supposed not to build), the flow was smooth: scaffold, read the recap, resolve each
sentinel following the CLI page's samples (regex, check digit, tied parameters, derived draws), then write tests that read like the domain. The documentation answered each hesitation in the page I opened first; the
price was a long CLI page and a few manual edits per type, and a half-dozen places where the tool cannot help (helper validation, ties, regex) and the generator is mine to write.
