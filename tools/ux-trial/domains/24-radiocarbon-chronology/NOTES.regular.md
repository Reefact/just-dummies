# Regular developer notes — 24 radiocarbon chronology

## What I did
- Created `Tests.Regular`, restored, scaffolded in one command:
  `any generate RadiocarbonAge CalendarInterval CalibrationPoint CalibrationCurve Sample StratigraphicLayer PrecedenceRelation SiteChronology --entry-point static:Arbitrary`.
- Kept 6 generators, deleted `AnyCalendarInterval` and `AnyPrecedenceRelation`: in every test I wrote, an
  interval or a succession is what the assertion is about, so it stays a literal.
- Refined the factories: `Between(...)` for ages, years and uncertainties; `StringMatching` for lab codes and
  layer codes; reservoir offset `Zero()` by default (valid for every material; a shell test pins it);
  curve points = `SetOf(years).WithCountBetween(2, 8).As(sort + map)` for strictly increasing years.
- Two hand-made curves (`Curves.Linear()`, `Curves.Wiggle()`) are literals: their shape decides the outcome.
- 34 test methods / 39 cases (one 4-row theory): age correction, sample invariants, calibration (incl. a
  multimodal date from a curve wiggle), `CalibratedDate` (HDR, truncation, mixture), site editing rules,
  and modelling (stratigraphic order, succession removing one mode of a bimodal layer in both directions,
  contradiction naming both layers, modelled/reopen state). `[assembly: Reproducible]`.
- `dotnet test --project Tests.Regular`: 39/39 green, and green on 8 further reruns (different seeds).

## Frictions and surprises, in order met
1. **Every numeric guard came back `to verify`.** All of the domain's range checks are written
   `if (x < Min || x > Max) throw ...`; the scaffolder reads none of them (9 TODOs across 4 types). Recap:
   `yearsBp: unread guard at RadiocarbonAge.cs, line 11: if (yearsBp < 0 || yearsBp > MaximumYearsBp)`.
   I checked with a throwaway type: the same bounds as two separate `if`s give `Any.Int32().Between(0, 100)  guard`;
   joined with `||`, even with literals, it gives `Any.Int32()  to verify, unread guards`. This is the most
   common way to write a range guard, so most of the scaffold's value went away in this domain.
   (It is safe, at least: the build stops, nothing wrong is drawn.)
2. **The relational guard of `CalendarInterval` is never named.** The TODOs for `startYear`/`endYear` name
   lines 11 and 16 (the range checks) only. `if (startYear > endYear)` at line 21 shows up nowhere: neither in
   the recap nor in the file. Fix the two ranges as the TODOs suggest, delete the TODO lines, and you have a
   generator that throws about half the time. The CLI page says a guard tying two parameters "stops the
   build at the first"; here the first *range* guard took that place and hid the tie. I only caught it
   because I read the constructor. (I ended up deleting that generator, for other reasons.)
3. Lab and layer codes: regex guards are not read (documented). Replacing with `Any.StringMatching(...)`
   means retyping the factory and the refining overload `AnyString` -> `AnyPattern` by hand, each time
   (documented too, but it's the same 2-line chore for every regex-guarded string; the scaffolder could
   just emit `AnyPattern` + a `StringMatching` TODO when it sees `Regex.IsMatch`/`[GeneratedRegex]`).
4. Curve points: "strictly increasing calendar years" is a cross-element rule. `SetOf(...).As(...)` did it
   cleanly, but the factory had to be retyped to `IAny<IReadOnlyList<CalibrationPoint>>` and the refining
   overload deleted, so the curve loses its `WithPoints(p => ...)`. Acceptable.
5. `Sample`'s offset/material tie: I chose a `Zero()` base instead of writing `Generate()` logic, because a
   hand-written `Generate()` cannot tell a pin from a draw. Consequence: `WithReservoirOffset(o => o.Between(...))`
   would contradict `Zero()`, so shell tests must pin the offset. Fine in practice, but it's a choice the
   generator model pushes on you.
6. Small: the generators and the `Arbitrary` root land in the domain's namespace
   (`RadiocarbonChronology.Domain`) inside the test assembly. Works with no import, but it surprises in a
   code review; I'd set `--namespace` / `any.json` next time.
7. To test a constructor refusal, the Act is `generator.Generate()` (e.g. `Arbitrary.Sample().WithLabCode("12345")`).
   Reads well enough; the alternative (`new Sample(...)` with dummies) needs a lab-code generator I can't
   reach because `AnyValidLabCode()` is private.

## What worked really well
- One command for 8 types with a shared `Arbitrary` root; recaps print the exact guard line that blocked
  each parameter, so fixing was quick.
- `any` composing `new AnyRadiocarbonAge()` inside `AnySample` and `new AnyCalibrationCurve()` inside
  `AnySiteChronology`: fix the leaf once, every aggregate is valid.
- Pins vs lambdas read exactly like the intent: `.WithMeasurement(m => m.WithYearsBp(300))`,
  `.WithMaterial(m => m.DifferentFrom(SampleMaterial.MarineShell))`, `.WithCurve(Curves.Linear())`.
- Drawn-then-pinned for "same code twice" tests: `WithCode(anyLayer.Code)`, `WithLabCode(anySample.LabCode)`.
- `[assembly: Reproducible]` one line, nothing else to think about. No analyzer noise, no warnings.

## Fluency: 4 / 5
Once the generators were fixed, writing 34 tests was fast and they read like the domain. The point lost is
the scaffold: the `||` range guard is unread everywhere in this domain, so I rewrote nearly every scalar
factory by hand, and the hidden `start <= end` guard is the kind of thing that would bite a less careful
teammate.
