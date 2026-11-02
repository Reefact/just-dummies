# Expert notes — 16 aviation
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did

**Pages read, in order:** `PROJECT-README.md`, `guides/getting-started`, `guides/core-concepts`, `packages/justdummies-cli` (whole page, it is the one that matters for the scaffolded files), `packages/justdummies-xunit`, `generators/README`, `generators/strings` (patterns), `generators/dates-and-times`, `generators/numbers`, `guides/composition`, `generators/enums-and-choices` (up to booleans), the first part of `guides/reproducibility`. I did not open the analyzer pages: I turned every `JD0xx` into a warning instead and let the build speak.

**Project:** `Tests.Expert/Aviation.Domain.Tests.Expert.csproj` from the template, unchanged. Beside it: `Usings.cs` (`global using Xunit;`), `Reproducibility.cs` (`[assembly: Reproducible]`), `.editorconfig` (all of JD001..JD038 at `warning`).

**`any` commands run** (from `Tests.Expert/`, absolute path to the tool):

```
any generate IataAirportCode IcaoAirportCode FlightNumber AircraftRegistration Route Slot Aircraft Flight --entry-point any --output Generators
any generate <same eight> --entry-point any --output Generators --namespace Aviation.Domain.Tests.Expert.Generators
```

The first run put the generators in the domain's own namespace (`Aviation.Domain`); I deleted `Generators/` and re-ran with `--namespace` so test code does not live in the production namespace. Result: 16 files (8 generators + 8 `.Entry.cs`), none compiling until completed: 6 of the 8 types had at least one `to verify`.

**Generators completed by hand** (every `TODO(any)` / `_ = TODO_verify_…` pair read, resolved, deleted):

| Generator | Completion |
|---|---|
| `AnyIataAirportCode` | `Any.String().Alpha().InUpperCase().WithLength(3)` (the tool's regex hint offered `AnyPattern`; I avoided the retype by building the format from named calls) |
| `AnyIcaoAirportCode` | scaffold had `NotBlank().WithLength(4)` plus a `to verify` on the per-character guard; now `Alpha().InUpperCase().WithLength(4)` |
| `AnyFlightNumber` | designator `AlphaNumeric().InUpperCase().WithLength(2)`, number `Between(1, 9999)` (both were hidden in a `Validate(...)` helper, so both only said `to verify`) |
| `AnyAircraftRegistration` | retyped `AnyString` -> `AnyPattern` (4 places, as the tool said); base is the union of the two forms as one pattern; two public constants for the halves; hand-written `AnyAircraftRegistration.Custom.cs` with named recipes `UnitedStates()` and `Foreign()` |
| `AnyRoute` | the origin/destination tie written in `Generate()` (see friction 6); distance guard was read by the tool (`Between(50, 20000)`) |
| `AnySlot` | `start`: `Between(2000, 2100)` UTC with `WithGranularity(5 min)`; the duration guard was read by the tool |
| `AnyAircraft` | type `Any.String().OneOf(Aircraft.KnownTypes)`, seats `Between(1, 78)` (78 is the smallest maximum of the fleet, so seats never depend on the type) |
| `AnyFlight` | block time `Positive().LessThanOrEqualTo(20 h)`; scheduled departure left at the scaffold's `Any.DateTime()` on purpose (the domain does not check it) |

`Generators/Helpers.cs` holds `Fixtures.AnyUtcMidnight` (a drawn UTC day the slot tests hang their times on) and `Fixtures.FlightIn(AnyFlight, status, delays…)` (walks a drawn flight to a status through the real transitions).

**Tests written: 114 methods, 323 cases** (65 `[Fact]`, 49 `[Theory]`), in `Value/` and `Entities/`:

- `IataAirportCodeTests` (value, refusals, null, equality/hash, ToString), `IcaoAirportCodeTests` (blank checked before length, length vs content refusal types, `IsEuropean` for E/L and six neighbours, reference equality characterised), `FlightNumberTests` (printing without padding, designator accept/refuse incl. null, number 1/9999 edges, check order), `AircraftRegistrationTests` (both forms, N-first rule, length cap, null, `IsUnitedStates` via the named recipes), `RouteTests` (reverse, same-code-different-instance refusal, null ends, distance 49/50/20000/20001), `SlotTests` (UTC kinds, 5-minute grid, seconds/ms, duration edges, `End`, `Overlaps` 10 cases symmetric both ways, different airports, same code in two instances, other day, null; `IsRespectedBy` 10 cases + tick beyond tolerance + other day).
- `AircraftTests` (6 types x max/max+1/1 seat, bad seats, unknown/case/space types, null type, check order, wide body, `KnownTypes`), `FlightTests` (construction, block time edges, estimated times, accumulation, 9 category boundaries in seconds, sum-not-each, refused delays leave state alone, delay allowed/refused per status, the full status matrix for `StartBoarding`/`Depart`/`Arrive`/`Cancel`), `CompensationPolicyTests` (null, 9 distance edges cancelled, 6 edges for 3 h late, exactly 3 h and 3 h minus 1 s across four statuses, summed delays, cancelled whatever the delay, re-read after a later delay).
- `GeneratorsTests`: 2 000 draws of every generator (they never draw what the constructor refuses), and a drawn route never joins an airport to itself when one end is pinned.

**Result:** green on the first full run (317 passed, 6 skipped = the six domain defects below, 0 failed), then 15 more full runs, all green with fresh seeds each time (16 green runs in total). Build: 0 warnings, 0 errors, with every JD rule raised to warning. I also un-skipped the six defect tests once to prove they fail for the stated reason (5 failed on the first pass; the sixth, `"A\n"`, was my own wrong literal: `"A\n"` does not match `^[A-Z0-9]{2}$`, `"AF\n"` does; fixed and re-checked).

## Frictions and surprises, in order

1. **`dotnet test --project Tests.Expert` does not run on this workspace.**
   `MSBUILD : error MSB1001: Unknown switch ... Switch: --project`. The `--project` form is the Microsoft.Testing.Platform mode of the .NET 10 SDK and needs a `global.json` with `"test": { "runner": "Microsoft.Testing.Platform" }`; the workspace had none, and a bare `dotnet test` answers `Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK` (that message is good and links to the fix). I wrote `global.json` at the root of W. Nothing in the JustDummies.Xunit page tells an xunit.v3 user on .NET 10 that this opt-in exists; the page only says "xunit.v3 4.0.2 or later". Low, docs.

2. **The template compiles nothing that uses `[Fact]` until a `using Xunit;` exists.** `error CS0246: The type or namespace name 'InlineData' could not be found` x many. Every doc sample carries its `using Xunit;`, but with `ImplicitUsings` on, one expects the test framework to be covered. Added `Usings.cs` with `global using Xunit;`. Trivial.

3. **`any generate` with no `--namespace` puts the generators in the domain's namespace.** The recap says "in namespace Aviation.Domain (--namespace to change)", so the tool is upfront about it, but for a test project it is the wrong default and I paid one delete-and-rerun. The `any.json` defaults file would have fixed it for good; I only noticed it after. A first-time reader of the CLI page meets `--namespace` in the options table last. Low, friction.

4. **A guard delegated to a `void` helper marks every parameter `to verify` with no hint at all.** `FlightNumber` calls `Validate(airlineDesignator, number);`. Output: `airlineDesignator  string  Any.String()  to verify, unread guards` and the same for `number`, with the generated comment only repeating the call. Documented ("a `void` helper called for its effect is marked as before"), and the right behaviour (a plausible `Any.Int32()` would have drawn 0 and negatives). But the tool had the helper's body in hand and could have *named what it saw*: a regex on the first parameter, a range check on the second. I read `FlightNumber.cs` myself. Compare with the regex case in `IataAirportCode`, where the comment quotes `^[A-Z]{3}$` and the retype to `AnyPattern`: that one was excellent. Low, idea.

5. **The same value format cannot be had from `Any.StringMatching` and a length cap.** `AircraftRegistration` has two forms plus a total length cap of 7 that is stricter than the N-number regex (`N12345AB` matches the regex and is refused by the length). `AnyPattern` offers no `WithMaxLength` (documented, and argued: intersecting two regular languages), so I had to fold the cap into the pattern by hand: `N[1-9]([0-9]{0,4}[A-Z]?|[0-9]{0,3}[A-Z]{2})`, and keep the hyphenated half off the letter N: `[A-MO-Z][A-Z]?-[A-Z]{3,4}`. It is correct and I wrote the 8+ `Foreign`/`UnitedStates` draws into a 2 000-draw test to be sure, but it is the least readable thing in the project, and it duplicates (and can drift from) the domain's two regexes. Medium-low, friction.

6. **Tying origin and destination: the recommended `DifferentFrom` inside `Generate()` is a trap once one side is pinned.** The CLI page says to derive the dependent draw in `Generate()` (`_destination.DifferentFrom(origin)`), and warns that a pin on one side of a tie only holds on the draws where it happens to. I wrote exactly that, then realised that `Any.Route().WithDestination(x)` fails with probability 1/17 576 per draw: confirmed with a probe, `AnyGenerationException: Could not draw a value different from CDG: 1000 draws in a row produced it. The redraw is bounded, so this is an exhausted budget rather than a proof — though the usual cause is a generator that yields nothing else, such as one pinned to that value.` (the message is clear, and it carries the seed in a test). A 2 000-draw loop in my own generator test would have hit it about once in nine runs. I replaced it with a bounded redraw of *both* sides on collision (a pinned side stays, the free side moves; two equal pins still reach the constructor, which refuses them with the domain's `ArgumentException`, what a refusal test wants). The docs describe the trap; they do not offer this form. Low, docs.

7. **A pin and a lambda are ambiguous for `null` on a non-nullable string parameter.** `Any.Aircraft().WithTypeDesignator(null!)` -> `error CS0121: The call is ambiguous between 'AnyAircraft.WithTypeDesignator(string)' and 'AnyAircraft.WithTypeDesignator(Func<AnyString, AnyString>)'`. The CLI page explains the cast for nullable reference parameters; here the parameter is not nullable and a refusal test still needs it. `(string)null!` fixes it. Low, docs.

8. **`JD005` and `JD011` fire together on the same expression.** On `Any.String().Generate() + Any.Int32().Between(1, 5)` the build printed both `warning JD005: Call Generate() on the AnyInt32 ...` and `warning JD011: Call Generate() on the AnyInt32 ...` at the same column, with the same remedy. Noise, not harm. (Seen on a scratch probe I deleted; my test code itself has zero diagnostics.) Low, defect.

9. **Info-level rules are invisible by default.** I only learnt that the project was clean after raising all 38 rules to `warning` in an `.editorconfig`. The CLI page does say "Info never prints in `dotnet build`", but nothing in getting-started tells an expert to do this on day one, and `JD030` (unbounded string) is exactly what a scaffolded generator trips. Low, docs.

10. **Domain-side, found by tests and not by the library** (all literal values, not draws; six skipped tests carry the reason): see the findings list of the report. In short, `$` accepting a trailing `\n` in `IataAirportCode`, `FlightNumber` and `AircraftRegistration` (both forms); `AircraftRegistration("")` throwing `IndexOutOfRangeException`; `Slot` accepting a start one tick past the 5-minute boundary. Two latent items I did not skip a test for: `Flight` accepts `DateTime.MaxValue` as scheduled departure and then `EstimatedArrival` throws `ArgumentOutOfRangeException` ("The added or subtracted value results in an un-representable DateTime"), same for `Slot.End` at 9999-12-31 and `Slot.IsRespectedBy` at the minimum; and `IcaoAirportCode` has no value equality while `IataAirportCode` does (documented in MODEL.md, characterised by a test).

## What worked well

- **The scaffolder's red build is genuinely a work list.** 16 files, 6 `to verify`, and every one pointed at a line of the domain I had to read anyway. Nothing compiled by accident. The recap column (`guard`, `AnyX`, `to verify`) told me in one glance which generators to trust (`Route.distanceKm` -> `Between(50, 20000)`, `Slot.duration` -> `Between(5 min, 1 h)` came out right from the guards).
- **`Any.Slot().WithStart(...).WithDuration(...)` and `Any.Flight().WithRoute(r => r.WithDistanceKm(2_000))`** make the arrange line say exactly the part the assertion depends on; the airport, aircraft and flight number vanish. The compensation tests read like the policy table.
- **Pins that break a rule go through the real constructor**, so refusal tests (`WithNumber(0)`, `WithBlockTime(20 h + 1 tick)`, `WithSeats(187)` on an A320) need no hand-built object and no dummy argument at all: only the offending value appears in the test.
- **`DifferentFrom` on a scaffolded generator** (`Any.IataAirportCode().WithValue(v => v.DifferentFrom(code.Value))`) and **`WithGranularity(TimeSpan.FromDays(1))`** for "any day, I hang times on it" were each one line and correct first time; `Kind` came back `Utc`, which the Slot constructor needs.
- **The named-recipe pattern** (`UnitedStates()`, `Foreign()` in a file beside the scaffold) is the right answer for the registration; the call site reads `Any.AircraftRegistration().UnitedStates()`.
- **`[assembly: Reproducible]`** and silence on green: zero ceremony in 114 methods.
- **Failure messages name the cause.** The `DifferentFrom` exhaustion message says what it means and what the usual reason is.

## Fluency: 4 / 5
Once the workspace could run a test (friction 1, an environment matter) and the `using Xunit` was in, the library got out of the way: the generators were quick to complete because the tool tells exactly where to look, and the test bodies came out reading as intended, with literals only where the assertion lives and dummies only for what the code must have and ignore. What costs a point is the cluster around ties and formats that the tool cannot express (friction 4, 5, 6): the right answers exist in the docs but each one cost a re-think, and the pattern-with-length case produced the one ugly line in the project.
