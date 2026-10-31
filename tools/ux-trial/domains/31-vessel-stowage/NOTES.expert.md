# Vessel stowage — expert crafter notes

## What I did
- Scaffolded `ContainerId`, `PortCode`, `SlotPosition`, `VesselProfile`, `Rotation` and `Container`
  with `--entry-point any`, resolved every `TODO_verify_…`, then wrote a `Recipes.cs` partial layer
  that speaks stowage: `Any.VesselProfile().Roomy()`, `Any.SlotPosition().InRoomyVessel(size).AtFootOfHold()`,
  `Any.Container().SegregationNeutral().FitFor(slot).Travelling(from, to)`, `.Laden()`, `.StackableOn(lower)`.
- `ContainerId` = `Any.StringMatching("[A-Z]{3}[UJZ][0-9]{6}").As(body => body + ComputeCheckDigit(body))`.
- `AnyContainer` was rewritten by hand: three guards tie parameters (size -> tare -> gross, POL != POD,
  reefer -> hazard). Ties are drawn in dependency order inside `Generate()`.
- `Any.Combine(vessel, rotation, ...)` gives an empty plan; `Any.SetOf(Any.ContainerId()).WithCount(5)`
  gives distinct box numbers; `[assembly: Reproducible]`.
- 33 test methods (38 cases) on containers, value objects, every `StowageViolation` the plan raises,
  stability (displacement/VCG/TCG with literal limits), `UsableSlotsFor`, `Remove`, `DischargeSequence`.
  All pass; 15 back-to-back runs (570 cases) were green.

## Frictions, in the order met
1. `any generate Container` -> `✗ Container: several types answer to that name. System.ComponentModel.Container /
   VesselStowage.Domain.Container. Name the one you mean in full.` Clear and actionable (praise, but a
   surprise: an implicit using makes a BCL type collide with a domain name).
2. Plain range guards are not read: `if (bay < 1 || bay > 99) throw new ArgumentOutOfRangeException(...)`
   gives `Any.Int32()  to verify, unread guards` (`SlotPosition`, 4 `VesselProfile` ints, `row`). It is the
   most common guard shape in this domain; 15 of 19 parameters needed hand work. `lightshipTonnes <= 0`
   *was* read (`Positive()`), so the one-sided form works and the two-sided `||` form does not.
3. Tied guards force a rewrite of the scaffolded file. The CLI page suggests "derive one draw from another
   inside Generate()", but the generated shape (one `IAny<T>` field + `AnyValid…()` + `With…` per parameter)
   cannot express "tare's base depends on the drawn size": I had to change field types to
   `Func<…>`/nullable pins and hand-write every `With…`. `--force` would now destroy it.
4. Narrowing a scaffolded `OneOf` base: `t => t.OneOf(2, 4, 6, 8, 82, 84, 86, 88)` on a base
   `OneOf(2..20, 82..98)` threw
   `ConflictingAnyConstraintException : Cannot apply OneOf(2, 4, 6, 8, 82, 84, 86, 88) because OneOf(2, 4, ..., 98) is already defined.`
   Documented, but a subset is a pure narrowing; the message should at least suggest `Except(...)`,
   which is what I used.
5. "Last call wins" on a parameter makes recipes non-composable and silently lossy:
   `InRoomyVessel(size).WithRow(r => r.DifferentFrom(x))` drops the roomy row bound, so a slot can be drawn
   outside the vessel on some runs. I had to restate `LessThan(RoomyVessel.MinRows)`. A "refine the
   current recipe" overload (compose with what is set, not with the base) is what recipe layers need.
6. Joint draws between two parameters (a leg `from < to` on a rotation) cannot be expressed through `With…`;
   tests draw the rotation first and pin both ports. Fine, but `Travelling(…)` had to be hand-written.
7. Distinctness of aggregates: `Container` has reference equality, so "five distinct boxes" means drawing a
   `SetOf(ContainerId)` and pinning ids one by one. Workable, slightly noisy.
8. Scaffolded files land in the domain namespace (`VesselStowage.Domain`) inside the test assembly; unexpected
   but harmless.

## What worked well
- The `TODO_verify_…` non-compiling line: impossible to forget a guard, and the recap names the line.
- `StringMatching(...).As(...)` for the ISO 6346 check digit: one line, honest by construction.
- Disjoint ranges for tied pairs (lightship vs max displacement, VCG vs max VCG) kept the base valid without code.
- Lambda refinements read as domain language once wrapped in named recipes; immutability makes
  `static readonly` recipe fields safe.
- `[assembly: Reproducible]` + seed message on failure: zero ceremony.
- Conflict messages name both sides precisely.

## Fluency: 4 / 5
The core DSL, `As`, `Combine`, sets and reproducibility are fluent and trustworthy. Points lost on the
scaffolder: two-sided range guards unread, and no supported path for parameters tied by a guard other than
rewriting the generated class; plus last-call-wins silently dropping recipe constraints.
