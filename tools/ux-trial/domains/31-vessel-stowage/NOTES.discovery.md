# Vessel stowage: discovery notes

## What I did
- Read `guides/getting-started`, the `any` CLI page, `composition`, then the generator reference pages
  for numbers, strings/patterns, collections and enums, and the xUnit adapter page.
- Created `Tests.Discovery`, restored, then ran `any generate ... --entry-point any` for `ContainerId`,
  `PortCode`, `SlotPosition`, `VesselProfile`, `Rotation`, then `VesselStowage.Domain.Container`.
- Cleared 22 deliberate `TODO_verify_...` build errors by hand. Rewrote most of `AnyContainer`
  because four of its parameters depend on another one.
- Wrote 34 tests (value objects, `Container`, and the `StowagePlan` rules: support, weight, stack
  limit, overstow, hatch access, port order, explosives, segregation, list, displacement, remove,
  usable slots, discharge sequence). `[assembly: Reproducible]`. All green, 25 runs in a row.

## Frictions and surprises, in the order I met them
1. `any generate Container ...` refused the type name:
   `✗ Container: several types answer to that name. System.ComponentModel.Container / VesselStowage.Domain.Container. Name the one you mean in full.`
   The message was clear and the fix was immediate. In a multi-type run, though, it is the first line
   and is easy to scroll past while the other types succeed.
2. Two-sided range guards were not read. `if (bay < 1 || bay > 99)`, `row < 0 || row > 20`, and the
   four `VesselProfile` count guards all came back `Any.Int32()  to verify, unread guards`. A single
   comparison was read (`maxStackWeightTonnes <= 0` became `.Positive()`, `Length != 11` became
   `WithLength(11)`). The "or" range is the commonest guard in this domain and every one of them
   needed a manual `Between`. The user docs never list which guard shapes the scaffolder reads (they
   point to a maintainer specification), so I could not tell whether this is expected.
3. `to verify` was right every time it fired, and the line it quotes is excellent. For
   `Container.size` it quotes the *tare* guard: that is how I understood size/tare/gross form a chain.
4. Cross-parameter rules dominate this domain (tare range depends on size, gross is between the
   tare and the size maximum, discharge differs from loading, no explosives or radioactive cargo
   in a reefer). The doc section "When a guard ties two parameters" says to derive inside
   `Generate()`, but the scaffolded shape fights it:
   a `With...(Func<...>)` refiner has no base to refine when the base depends on another draw. I used
   nullable fields that mean "derive", kept the pins, and deleted four refiners. That is about 60
   hand-written lines, and `--force` would erase them. A pattern or sample for "derived unless
   pinned" would have saved me the design time.
5. For `VesselProfile` (`maxDisplacement > lightship`, `maxVcg > lightshipVcg`) I chose disjoint
   ranges so every pair is valid by construction. It is simple, but I had to come up with it myself.
6. Replacing `AnyString` with `Any.StringMatching(...)` (`AnyPattern`) and then with `.As(...)`
   (`IAny<string>`) went exactly as the doc's "When the replacement is another kind of generator"
   describes: retype the factory and the `Func<>`, or delete the refiner. The doc is accurate.
   `ContainerId.ComputeCheckDigit` with `.As(body => body + digit)` matched the Luhn sample.
7. Generated classes take the domain's namespace (`VesselStowage.Domain`) inside the test assembly.
   This is documented (`--namespace`) but surprised me the first time I read the file.
8. Hesitation: would `Any.Rotation().WithPorts(p => p.WithCount(3))` collide with the base's
   `WithCountBetween(2, 6)`? The "declared once" list names `OneOf/WithLength/MultipleOf/WithScale`
   only. It narrowed correctly. A line saying so for counts would remove the doubt.
9. No odd-number constraint: I wrote `Any.Int32().Between(0, 49).As(n => 2 * n + 1)` for an
   odd (20 ft) bay. That is fine, just a moment of searching.
10. Domain lesson the library made visible: a drawn `HazardClass` is *not* a dummy in plan tests
    (segregation, explosives under deck), and the drawn stability limits can refuse a box. So
    the tests pin `HazardClass.None` and use a named "roomy vessel" helper with generous limits.
    The "Arbitrary does not mean dummy" section helped me decide which values to pin.
11. Analyzer Info diagnostics (JD030 on `NotBlank()`, as the doc warns) never showed on a CLI
    `dotnet build`. No warning or error from the analyzers came up during normal work.

## What worked well
- The `TODO_verify_...` compile errors work as a precise to-do list. I resolved them one by one and
  nothing invalid slipped through: after that, every generator produced valid domain objects.
- `--entry-point any` gives `Any.Container()` next to `Any.Int32()`, so the tests read uniformly.
- Pin vs refine (`WithRow(3)` vs `WithBay(b => b.LessThan(99))`) is easy to grasp and reads well.
- `Distinct()` on `ListOf(Any.PortCode())`, `DifferentFrom` on an `AnyPattern`, `Except` on enums:
  all behaved as documented on the first try.
- `[assembly: Reproducible]` is a one-liner. No failure happened, so I never needed a seed.

## Fluency: 4 / 5
Once the first generator compiled, writing tests was fast and they read clearly. I lose a point
because two-sided range guards are not inferred, the user docs do not list the guard shapes the
tool reads, and the cross-parameter rules made me design my own "derived unless pinned" pattern in
the generator, which a re-scaffold would wipe.
