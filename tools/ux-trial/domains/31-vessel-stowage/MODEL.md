# Vessel Stowage — model

Namespace `VesselStowage.Domain`. A loading plan for one vessel over one rotation.

## Value objects
- `ContainerId` — ISO 6346: 3 upper-case letters, category `U`/`J`/`Z`, 6 digits, valid check digit (e.g. `CSQU3054383`).
- `PortCode` — UN/LOCODE: 2 letters + 3 of `A-Z`/`2-9` (e.g. `NLRTM`).
- `Rotation` — at least 2 ports, no null, each port once; `IndexOf` gives call order.
- `SlotPosition(bay, row, tier)` — bay 1-99 (odd = 20 ft, even = 40 ft); row 0-20 (0 centreline, odd starboard, even port);
  tier even, 02-20 in the hold or 82-98 on deck. Gives level, transverse offset (2.44 m/row), centre height (2.59 m/tier), adjacency.
- `ContainerSize`, `HazardClass` (IMDG 1-9 or `None`), `StowageViolation` — enums.

## Entities
- `Container` — id, size, tare (20 ft: 1.8-3.2 t, 40 ft: 3.4-4.9 t), gross mass (tare..30.48 t / 32.5 t),
  port of loading != port of discharge, hazard, reefer flag (no explosives/radioactive in reefers).
  `CanNeighbour`: explosives and radioactive touch no other DG; flammables (3, 4) never touch oxidizers (5).
- `VesselProfile` — name, bays 1-99, rows 1-21, hold tiers 1-10, deck tiers 0-9, stack weight limit > 0,
  lightship > 0 with VCG > 0, max displacement > lightship, max VCG > lightship VCG, max TCG > 0.
- `StowageAssignment(container, slot)` — the slot's bay must accept the container's size.

## Aggregate: `StowagePlan(vessel, rotation)`
- `Assign(container, slot)` — refused with `StowageRefusedException` (carrying a `StowageViolation`), checked in order:
  slot outside vessel; bay/size mismatch; container already stowed; slot occupied; loading/discharge not called in order;
  explosives under deck; no box in the tier below (lowest hold and deck tiers need none); heavier than the box below;
  stack (bay, row, section) over the weight limit; overstow (a box above must load no earlier and discharge no later);
  hatch access (deck boxes of a bay must load no earlier and discharge no later than its hold boxes);
  DG segregation with adjacent slots (same bay and section, one row or one tier apart);
  displacement, VCG or |TCG| over the vessel limits, computed with every planned box aboard.
- `Check(container, slot)` — the same evaluation, returning the violation (`None` when allowed).
- `UsableSlotsFor(container)` — every slot `Check` allows now: a free slot can be unusable through weight, neighbours or access.
- `Remove(id)` — refused when not stowed or when a box stands on it.
- `DischargeSequence(port)` — boxes discharged there, crane order: by bay, deck before hold, top tier first.
- `DisplacementTonnes`, `VcgMetres`, `TcgMetres` — lightship plus cargo moments.
