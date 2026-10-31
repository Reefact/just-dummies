# Discovery notes — 38 carbon ledger

## What I did
- Read `MODEL.md` and the domain, then `guides/getting-started`, the `any` CLI page, `composition`,
  and the strings / collections / dates generator pages.
- Created `Tests.Discovery`, restored, ran
  `any generate CreditSerial Holder Account RetirementClaim --entry-point any` (one command, four types).
- Resolved the 7 `to verify` markers by hand, wrote a small `RegistryScenario` helper (ledger + 4 accounts,
  built inside each test so `[assembly: Reproducible]` pins it) and 30 tests: serial format/parse/equality,
  holder trimming, account prefix rule, journal entry rules, open/issue/transfer/retire rules, double
  holding and double use, append-only time, ownership history, consistency of derived state.
- `dotnet test --project Tests.Discovery`: 30/30 green, re-run 8 more times, still green.

## Frictions and surprises, in the order I met them
1. **The scaffolder recap is excellent but long for four types**; I had to scroll back to collect the
   seven `to verify` rows. A final one-line "7 parameters to verify across 4 files" would help.
2. **Inconsistent guard reading on the same shape of rule.** `Holder.legalName` (`IsNullOrWhiteSpace`
   then a separate `Length > Max` guard) became `Any.String().NotBlank().WithMaxLength(200)` / `guard`.
   `RetirementClaim.beneficiary` and `purpose` say the same thing in one `if (... || ...)` and came out
   `Any.String().NonEmpty()` / `to verify, unread guards`. Not wrong (it blocks the build, as designed),
   but I hesitated: is the `||` form unsupported or a bug? The CLI page does not mention `||`.
3. **Regex guards via `[GeneratedRegex]`**: the TODO quotes `if (!RegistryPattern().IsMatch(registry))`
   but not the pattern itself, so I had to open the domain file to find `^[A-Z]{2,5}$`. Documented
   ("a regex guard is never read"), still a detour. I then chose `Alpha().InUpperCase().WithLength…`
   rather than `Any.StringMatching(...)` to avoid retyping the factory and its `Func<…>` (the doc warns
   that the type changes to `AnyPattern`). Knowing about that trap in advance was useful.
4. **Cross-parameter rule `accountId` starts with `holder.CountryCode`.** `any` flagged `holder` as
   `to verify` and quoted the right line — good. The doc section "When a guard ties two parameters"
   gave me the pattern (derive in `Generate()`). But the generated pin `WithAccountId(string)` no longer
   made sense once the id is derived from the holder, so I deleted it. The doc does not say what to do
   with the With… pair of a parameter that becomes derived; I improvised.
5. **`Any.DateTimeOffset()` spans `MinValue..MaxValue` by default.** Two places needed arithmetic on a
   drawn instant (`from.AddDays(-1)`, `openedOn.AddHours(n)`), so I had to bound the draw to avoid
   overflow (`After(MinValue.AddDays(1))`, `Between(2000, 2090)`). Logical, but the scaffolded
   `AnyValidOpenedOn()` is full-range, so the first test that adds time to an account's opening date can
   throw `ArgumentOutOfRangeException` on a rare seed. Nothing in the recap hints at it.
6. **Cross-object invariant (claim year >= latest vintage)** lives in `JournalEntry`, not in either
   constructor; I expressed it with `WithReportingYear(y => y.GreaterThanOrEqualTo(latestVintage))`.
   The refining lambda composed with the base `Between(2000, 2100)` exactly as the CLI page says —
   no surprise, which is the point.
7. Generated files land in the *domain's* namespace (`CarbonLedger.Domain`) inside the test project.
   Documented (`--namespace` default), slightly surprising the first time.
8. No error message was met at all during the session: the build compiled on the first try after the
   TODOs were cleared, and no test ever failed, so I never saw a seed report.

## What worked well
- `any` + `--entry-point any`: `Any.Account().WithKind(AccountKind.Holding).WithOpenedOn(t)` reads like
  the domain. Guards on ints/longs (`Positive().LessThanOrEqualTo(999999)`, `Between(1990, 2100)`) were
  inferred exactly. The deliberate non-compiling `TODO_verify_…` line is a great forcing function.
- `WithRegistry(r => r.DifferentFrom(scenario.Registry))` for a "foreign registry" serial: one line,
  intent obvious.
- `Any.ListOf(...).WithCount(n).Distinct()` on a type with value equality just worked.
- `[assembly: Reproducible]` and the JD007 note (do not draw in a constructor) shaped my helper early.

## Fluency: 4 / 5
I wrote 30 real tests in one sitting without reading library source and without a single runtime
surprise. One point off for the hand work around cross-parameter rules (pin semantics left to me),
the `||` guard not being read when its split form is, and the full-range `DateTimeOffset` default that
turns innocent date arithmetic into a latent seed-dependent overflow.
