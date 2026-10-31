# Vessel stowage — regular developer notes

## What I did
- Created `Tests.Regular`, added `any.json` (`output: ./Arbitrary`, `entryPoint: static:Arbitrary`), scaffolded
  `ContainerId PortCode Rotation SlotPosition VesselProfile` in one call, then `VesselStowage.Domain.Container`.
- Resolved every `to verify` in the generated bases (≈20 TODO lines), then wrote 33 tests: every `Assign`
  refusal (outside vessel, bay/size, already stowed, occupied, port order, port outside rotation, explosives
  under deck, no support, heavier on top, stack weight, overstow, hatch access, DG segregation, displacement,
  VCG, TCG), `Check`, `UsableSlotsFor`, `Remove` (3), `DischargeSequence`, `DisplacementTonnes`, and the
  value objects (reefer/explosives, tare range, `CanNeighbour`, ISO 6346 check digit, slot adjacency).
- Shared arrangement in `Stowage.cs`: a "roomy vessel" made only of refinements
  (`WithBayCount(b => b.GreaterThanOrEqualTo(10))` …) so stability never bites unless a test pins a limit.
- `[assembly: Reproducible]`. All 33 pass, 15 consecutive runs green. Domain untouched.

## Frictions and surprises, in order
1. `any generate Container` → `✗ Container: several types answer to that name. System.ComponentModel.Container /
   VesselStowage.Domain.Container. Name the one you mean in full.` Clear and correct; slightly annoying for a
   common domain word, the other candidate comes from a framework assembly no one would scaffold.
2. **Two-sided range guards are not read.** `if (bay < 1 || bay > 99) throw …` gave `Any.Int32()` + `to verify,
   unread guards`, same for `row`, `bayCount`, `rowCount`, `holdTiers`, `deckTiers`, and
   `if (!(maxTcgMetres > 0) || double.IsInfinity(maxTcgMetres))`. Meanwhile `maxStackWeightTonnes <= 0` →
   `.Positive()` was read. 9 of my ~20 TODOs were plain `Between(min, max)` I had to type by hand. Safe (the
   build stops), but the most common guard shape in this domain earned nothing.
3. `ContainerId` (check digit): the docs' "draw the body, derive the check digit" recipe fitted exactly:
   `Any.StringMatching("[A-Z]{3}[UJZ][0-9]{6}").As(body => body + ContainerId.ComputeCheckDigit(body))`.
   Retyping the factory to `IAny<string>` and the refining `Func<…>` was documented and took a minute.
4. **Cross-parameter rules in `Container`** (tare range depends on size, gross ∈ [tare, max(size)], no reefer
   for explosives/radioactive, loading ≠ discharge). No per-parameter base can express them. I made the three
   dependent fields nullable ("derive unless pinned"), deleted their refining `With…(Func…)` overloads, and
   wrote `Generate()` by hand (incl. a bounded redraw for the discharge port). Works well in tests
   (`WithGrossMassTonnes(25m)` pins, the rest stays valid), but: the scaffold has no notion of
   "derived unless pinned", and `--force` will erase my `Generate()` — I have to remember never to re-scaffold.
5. `SlotPosition.tier` (even, 2-20 or 82-98): no way to say "union of two ranges" on `AnyInt32`; I wrote
   `Between(2, 98).MultipleOf(2).Except(22, 24, …, 80)` — 30 literals. Works, reads poorly.
6. `Rotation`: `Any.SequenceOf(new AnyPortCode()).WithCountBetween(2, 8).Distinct()` as base, then a test
   refines with `WithPorts(p => p.WithCount(3))` — narrowing the count inside the base just worked.
7. Minor: every generated file carries its own private `FixedValue<T>` copy; the generated entry-point/generator
   live in the *domain's* namespace inside the test project (documented, but surprised me the first time).

## What worked really well
- Build-breaking `TODO_verify_the_generator_for_x` with the guard line quoted: I never had to open the
  constructor to know what to fix, and nothing invalid could slip through.
- `With…` pin vs refine: tests read as the domain does — `WithMaxStackWeightTonnes(40m)` is a literal because the
  assertion is about it; the rest is drawn. Refinements compose with the base (`l => l.GreaterThanOrEqualTo(20_000m)`
  on top of `Between(1_000, 50_000)`).
- `AnyPattern.Except(string[])` to draw a port outside the rotation in one line.
- `Any.Boolean().False()` to keep the derived reefer recipe in the same shape as the others.
- `[assembly: Reproducible]`: a forced failure printed
  `[JustDummies] These arbitrary values were seeded with 1087910541. Reproduce this run with [Reproducible(Seed = 1087910541)].`
- All 18 first tests passed on the first run; no analyzer noise at warning level.

## Fluency: 4 / 5
Scaffold → fix bases → write tests is fast once the bases are right, and the tests stay short and honest.
One point off for the time spent hand-writing obvious ranges the tool did not read, and for the hand-written
`Generate()` that cross-parameter invariants force, which the next `--force` would silently discard.
