# Regular notes — 31 vessel stowage
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
- Read, as a refresher and in this order: `MODEL.md` and the whole `Domain/` (10 files); `docs/PROJECT-README.md`;
  `guides/getting-started`, `guides/core-concepts`, `guides/composition`; `packages/justdummies-cli` (what the last column
  means, guards read, adjusting a parameter, when a guard ties two parameters); `packages/justdummies-xunit`;
  `generators/README`, `numbers`, `enums-and-choices`, `collections`, and the Patterns section of `strings`;
  `packages/justdummies` (analyzer severities).
- Project `Tests.Regular/VesselStowage.Domain.Tests.Regular.csproj` from the template. `dotnet restore`, then
  `any generate ContainerId PortCode Rotation SlotPosition Container VesselProfile StowageAssignment --output Generators --entry-point any`.
  Recap: 7 files + 7 entry points; 15 parameters marked `to verify`: `ContainerId.value`, `PortCode.value`, `Rotation.ports`,
  `SlotPosition.tier`, 7 of the 8 `Container` parameters, 2 `VesselProfile` doubles, both `StowageAssignment` parameters.
- Generators completed by hand (all `TODO(any)` sentinels removed, `--force` never used):
  - `AnyPortCode`: `Any.StringMatching("[A-Z]{2}[A-Z2-9]{3}")`; retyped `AnyString` to `AnyPattern` on the three lines the CLI page names.
  - `AnyContainerId`: `Any.StringMatching("[A-Z]{3}[UJZ][0-9]{6}").As(body => body + ContainerId.ComputeCheckDigit(body))`;
    factory / field / ctor parameter retyped `IAny<string>`, refining overload deleted (as the CLI page says for `.As`).
  - `AnyRotation`: `Any.SequenceOf(new AnyPortCode()).WithCountBetween(2, 8).Distinct()`.
  - `AnySlotPosition`: tier `Any.Int32().OneOf(Enumerable.Range(2, 19).Concat(Enumerable.Range(82, 17))).Even()`.
  - `AnyContainer`: the tare/gross pair the tool had built depends on the size, so I removed the stored pair and rebuild it in
    `Generate()` once the size is drawn (tare in the size's range, gross between tare and `Container.MaxGrossMass(size)`);
    ports drawn with `DifferentFrom` unless the discharge port is pinned; reefer/hazard tie resolved in `Generate()` honouring pins.
  - `AnyVesselProfile`: realistic bounds on the numeric recipes; `maxVcgMetres` drawn after `lightshipVcgMetres` with
    `GreaterThan(lightshipVcg)` (the tool did pair the two decimals, not the two doubles).
  - `AnyStowageAssignment`: slot and container drawn so that the bay takes the size, whichever side is pinned.
  - Support code: `Support/PlanScenario.cs` (roomy vessel whose limits never bind, 4-port rotation, `Teu()/Feu()/DangerousBox()`
    helpers, `ForeignPort()`, `VesselOf(...)` for limit tests); `AssemblyInfo.cs` with `[assembly: Reproducible]`.
- Tests: 196 test methods, 364 cases, in 11 classes.
  - `ContainerIdTests` (known number, check digit, three categories, each refusal, null, equality).
  - `PortCodeTests`, `RotationTests` (order, `IndexOf`, `Calls`, each refusal, defensive copy of the input list; one skipped domain-defect test),
    `SlotPositionTests` (every bound, level/section, accepted size, transverse offset and index, centre height hold and deck,
    `Below`, 12 adjacency cases, equality, `ToString`), `ContainerTests` (tare and gross limits per size, port, reefer rule, `CanNeighbour` matrix),
    `VesselProfileTests` (each guard at its edge, NaN/infinity, `Contains`, `AllSlots`), `StowageAssignmentTests`.
  - `StowagePlanTests` (stow, `At`/`Find`, `Check`, atomic refusal, `Remove` four ways), `StowagePlanRefusalTests` (every one of the
    15 `Assign` violations accepted/refused at its edge, then 11 "reported before" ordering tests), `StowagePlanStabilityTests`
    (displacement, VCG, TCG as hand-computed numbers and at their limits), `StowagePlanQueryTests` (`DischargeSequence`,
    `UsableSlotsFor`, and one consistency test that fills a plan only through `UsableSlotsFor` and checks the rules independently).
- Result: green, 363 passed + 1 skipped (domain defect below). 27 repeated `dotnet test --project Tests.Regular --no-build` runs
  (15 on the 363-case suite, then 12 on the final 364-case suite, fresh seeds each), all green. `bin/` and `obj/` deleted at the end.
  Build: 0 warnings, including with `category-JustDummies.Constraints` raised to `warning` (see friction 8).

## Frictions and surprises, in order
1. `dotnet test --project Tests.Regular` (the command of the brief) fails out of the box on the .NET 10 SDK:
   `MSBUILD : error MSB1001: Unknown switch. ... --project Tests.Regular`. `dotnet run` in the folder works (OutputType Exe), and
   `dotnet test --project` only works after adding a `global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }`.
   The docs mention `dotnet test --solution` (PROJECT-README, contributing section) and "pinned in global.json" but nowhere
   say that an xunit.v3 consumer needs that file. I put one in the domain folder.
2. `any generate ...` before restore: `✗ ContainerId: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   Clear and actionable; I just had not restored yet. Fine, but a first-time run would stop here.
3. Seven `to verify` parameters on `Container` and a tied-pair recap are right, but what the tool offers for a rule that depends on
   another parameter's *value* (tare range per size, reefer/hazard, size per bay) is nothing: the CLI page says "derive in `Generate()`",
   and then "its `With…` pin no longer reaches the draw". In practice I had to write ~25 lines in each of `AnyContainer.Generate()`
   and `AnyStowageAssignment.Generate()` to keep the pins honoured (detecting a pin with `ReferenceEquals(_x, _xRecipe)`, a trick I
   found because the generated code makes it possible, not because a doc suggests it). This is the daily cost of this tool on a
   non-trivial aggregate; I would like a documented pattern for "pinned or drawn" for tied parameters.
4. The recap paired `lightshipTonnes`/`maxDisplacementTonnes` (`if (maxDisplacementTonnes <= lightshipTonnes)`) into an
   `AnyOrderedPair<decimal>`, but not `lightshipVcgMetres`/`maxVcgMetres`, guarded by `if (!(maxVcgMetres > lightshipVcgMetres) || double.IsInfinity(maxVcgMetres))`:
   `... lightshipVcgMetres: unread guard at VesselProfile.cs, line 60: ... — ties maxVcgMetres to lightshipVcgMetres`.
   The CLI page lists `!(p > N)` as read for constants, but a negated order between two parameters, and an order written with `||`
   alongside another test, apparently are not. I had expected the same treatment as the decimals and wrote the dependent draw myself.
5. Retyping after replacing a base is documented and worked (`AnyString` to `AnyPattern`, `.As` gives `IAny<string>` and the refining overload goes),
   but it is hand-surgery in a 76-line file for a one-line change, in three of my seven generators. I followed the CLI page to the letter and
   it compiled the first time, so no complaint about the doc; it is just the most repeated chore of the day.
6. `Any.VesselProfile().WithName(null!)` is ambiguous: `error CS0121: The call is ambiguous between 'AnyVesselProfile.WithName(string)' and 'AnyVesselProfile.WithName(Func<AnyString, AnyString>)'`.
   Expected from the docs (cast to tell the value from a lambda), so I wrote `(string)null!`. Mentioned because tests of null guards hit it every time.
7. Parameters paired by the tool (`lightshipTonnes`/`maxDisplacementTonnes`) are checked by the *generator* when pinned in the wrong order
   (the CLI page: "two pins in the wrong order are refused at the second call, naming both"). I did not try it, I read it and went straight to
   `new VesselProfile(...)` for the constructor-guard tests of the tied parameters (equal displacement, VCG not above lightship), since the generated
   `With…` would have refused before the constructor was reached. Sensible, but it means a refusal test of a tied rule can never go through the scaffold.
8. JD036 and JD030 only appear after raising the severity in `.editorconfig`, as the docs say. With the Constraints category raised to `warning` I got:
   `AnyContainer.cs(61,16): warning JD036: This floating-point dummy declares no range: one draw in ten spans the whole type` (twice, on my `Any.Decimal().WithScale(1)` and
   `.WithScale(2)` bases, although `Generate()` adds the `Between` right after) and `AnyVesselProfile.cs(63,16): warning JD030: This string dummy declares no length: it draws 1 to 1025 characters`
   on the scaffolded `Any.String().NotBlank()`. I wrote the union range (`Between(1.8m, 4.9m)`) in the tare base and `.WithMaxLength(40)` on the name; both are better documentation anyway.
9. The scaffolded vessel (`Any.Decimal().Positive()` for the lightship and stack limit, `Any.Double()` for the max VCG) is valid for the constructor but, per the numbers page,
   would reach `decimal.MaxValue` or `double.MaxValue` one draw in ten, which no ship has and which makes `DisplacementTonnes` a question about overflow. I bounded every numeric recipe from
   the start by what a ship is. Not a library problem (the doc says so), but the recap's `guard` word reads as "valid" while meaning "valid for the constructor only".
10. Smaller: `Container resolved to VesselStowage.Domain.Container; System.ComponentModel.Container also answers to that name.` appears in the recap — useful and quiet. The entry-point
    extension (`--entry-point any`) needed no extra step and `Any.Container()` read well in every test; C# 14 was on by default with net10.0.
11. Domain observations (not library): `Rotation.Ports` returns the internal `List<PortCode>` typed as `IReadOnlyList<PortCode>`; a downcast lets a caller add a duplicate or
    a `null` port after the constructor's guards. Test `RotationTests.The_ports_a_rotation_exposes_cannot_be_used_to_change_it` fails (`Assert.False() Failure Expected: False Actual: True`),
    kept and marked `Skip = "domain defect: ..."` (not seed-dependent). Also `StowagePlan.Assignments` is a live view of the dictionary's values, so a collection kept
    before an `Assign` grows afterwards; I did not test it, no contract says otherwise.

## What worked well
- `[assembly: Reproducible]` once, and every failure printed its seed in the test output
  (`[JustDummies] These arbitrary values were seeded with 1040987772. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 1040987772)].`).
  While I was writing, three failures were my own mistakes (a pinned 31 t on a 20 ft box, a tare invalid for the size, a hatch case with the wrong direction) and the
  messages named the argument, the value and the limit every time.
- The pinning vocabulary of the scaffolded generators (`WithSize(...)`, `WithGrossMassTonnes(...)`, `WithHazard(...)`) made the plan scenarios read like
  the stowage plan itself: only the literal that matters (a slot, a mass, a port index) is spelled, the rest is drawn.
- `Any.OrderedPair` for tare/gross, `Any.Int32().OneOf(range ∪ range).Even()` for the tiers (the numbers page even uses container tiers as its example), `DifferentFrom` on a plain `IAny<PortCode>`:
  each fitted the first time.
- Refinements that accumulate: `WithBayCount(b => b.GreaterThanOrEqualTo(4))` on a base already `<= 99` in the roomy vessel was exactly what I wanted.
- Errors when a pin contradicts a base are loud and early (`AnyGenerationException: Could not draw a value different from HV4FM: 1000 draws in a row produced it. ...` when I pinned both ports to the same value through my own `DifferentFrom`), which led me to only apply it when the discharge port is not pinned.
- Zero analyzer noise on the finished tests, even with the Constraints category raised to warning, once the two scaffold recipes had a range.

## Fluency: 4 / 5
Everyday use is smooth: scaffold, complete, write tests, seeds on every failure, analyzers silent. What costs time, on an aggregate like this one, is the part the tool
openly does not do: rules that tie a parameter to another one's value force hand-written `Generate()` bodies that must re-implement pin handling, the one-line replacements
of a base require retyping fields in several places, and the `dotnet test --project` command needs an undocumented `global.json`. Nothing blocked me, nothing was surprising
once read in the CLI page, but the cost of tied parameters is real.
