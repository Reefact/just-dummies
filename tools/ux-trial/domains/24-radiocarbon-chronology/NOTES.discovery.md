# Discovery notes: radiocarbon chronology

## What I did

- Read `guides/getting-started`, `core-concepts`, `composition`, the `any` CLI page, `generators/numbers`,
  `collections`, the `strings` patterns section, the enum `Except`/`DifferentFrom` lines and the xUnit page.
- Created `Tests.Discovery`, restored it, and ran
  `any generate RadiocarbonAge CalibrationPoint CalibrationCurve Sample StratigraphicLayer PrecedenceRelation --entry-point any`,
  then `any generate SiteChronology --entry-point any`.
- Resolved every `TODO_verify_…` line, deleted `AnyPrecedenceRelation` (see 5), then wrote 33 tests:
  value objects, calibration, samples and reservoir offsets, and the `SiteChronology` aggregate (duplicates,
  cycles, modelling order, stratigraphic truncation, contradicting dates, a multimodal layer, the
  modelled/reopened lifecycle). Every class that draws is `[Reproducible]`.
- `dotnet test --project Tests.Discovery`: 33/33 passed, and still passed on 8 more runs with different seeds.
  I broke one assertion on purpose once to see the replay message, then put it back.

## Frictions and surprises, in the order I met them

1. **Simple range guards come out as "unread".** `if (yearsBp < 0 || yearsBp > MaximumYearsBp) throw new ArgumentOutOfRangeException(...)`
   gave `Any.Int32()  to verify, unread guards` for both `RadiocarbonAge` parameters and for all three
   `CalibrationPoint` parameters. The CLI page shows `quantity <= 0` → `.Positive()`, so I expected
   `Between(0, MaximumYearsBp)`. I could not tell whether the `||`, the named constant or `double.IsNaN(..)`
   is what stops it, and the docs do not say. Five easy recipes ended up written by hand.
2. **The regex guards were handled as documented.** They were marked `to verify` and nothing more. The page
   names the exact three lines to change: the factory's return type `AnyString`→`AnyPattern`, the `Func<…>`
   of the refining overload, and the body. Following it took a minute. Without that section I would have
   been stuck on `CS0029`.
3. **A guard tying two parameters (`material` with `reservoirOffset`) marks both, plus `measurement`.**
   That was expected from "When a guard ties two parameters". I used "derive inside `Generate()`": a
   non-marine sample gets offset 0. One consequence surprised me later: `WithReservoirOffset(400)` on a
   drawn charcoal sample is now silently ignored by my `Generate()`. To test "a terrestrial sample cannot
   carry an offset" I had to call the constructor directly. The docs recommend this pattern but do not
   warn that it can turn a pin into a no-op.
4. **The curve's points (strictly increasing years, checked in a `for` loop) were marked `to verify`.**
   That is fine. My replacement, `ListOf(point).Distinct(sameYear).As(sort)`, is an `IAny<…>`, so I deleted
   the refining overload as the page says. `EqualityComparer<T>.Create` needed `a!`/`b!` under nullable.
5. **`AnyPrecedenceRelation` was not useful.** Its two codes have to differ, and in practice they always
   name existing layers, so tests pin both. I deleted it.
6. **The per-parameter recipe is private.** To get "a valid layer code" as a plain string (for a
   `PrecedenceRelation`, or for a lab code passed to a constructor) I wrote
   `Any.StratigraphicLayer().Generate().Code`. That draws a whole layer to get one string. A public
   `AnyStratigraphicLayer.Codes()`, or a documented idiom for it, would read better.
7. **"Two different layers" is verbose:**
   `Any.ListOf(Any.StratigraphicLayer()).WithCount(2).Distinct(SameCode)` plus a hand-written comparer.
   `WithCode(c => c.DifferentFrom(other.Code))` on the `AnyPattern` base worked, and reads well for the
   second-layer case.
8. **The generated files are put in the domain namespace (`RadiocarbonChronology.Domain`) inside the test
   project.** That was surprising at first but convenient, and `--namespace` exists.
9. The build printed no JD diagnostics at all. JD030 is Info, so I never saw it from `dotnet build`. That
   was not a problem.

## What worked well

- `any`'s recap names each unread guard with its file and line, and the `TODO_verify_…` identifiers broke
  the build exactly where I had to look. Every error I met (CS0103 on the TODO identifiers, CS0029 when I
  retyped a factory) pointed at a line the docs had already explained.
- `SiteChronology` scaffolded cleanly (`new AnyCalibrationCurve()`, `AnyX`), and
  `Any.SiteChronology().WithCurve(c => c.WithPoints(Curves.Linear()))` is exactly the shape I wanted: a
  curve the test reasons about, with an arbitrary name and site.
- `WithMeasurement(m => m.WithYearsBp(5000))`, which pins one part and keeps the rest valid, reads naturally.
- When a test failed, the seed came back as
  `[JustDummies] These arbitrary values were seeded with 815642649. Reproduce this run with [Reproducible(Seed = 815642649)].`
  It names the attribute form directly.
- Many domain tests here have no dummy at all (`CalendarInterval`, `CalibratedDate`). The core-concepts
  page made me comfortable leaving them as literals.

## Fluency: 4 / 5

I went from nothing to 33 green tests in one sitting, and the docs answered each question at the moment I
had it. I took one point off because the scaffolder could not read plain range guards, which left five
trivial recipes for me to write. A cross-parameter rule also pushed me into a hand-written `Generate()`,
and that quietly weakens the pins.
