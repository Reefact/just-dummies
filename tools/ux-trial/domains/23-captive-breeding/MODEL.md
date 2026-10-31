# Captive Breeding Kinship Optimization — domain model

Namespace `CaptiveBreeding.Domain`. A conservation programme keeps a studbook (pedigree) and
plans each breeding season so as to limit inbreeding and preserve founder diversity.

## Value objects
- `StudbookId` — `^[A-Z]{2,4}-[0-9]{4,6}$` (e.g. `ZSL-01234`), else `FormatException`; null refused. `Institution` = prefix.
- `KinshipCoefficient` — double in [0, 1], NaN refused. `Exceeds(limit)`, `OffspringInbreeding` (= value), `Unrelated` (0).
- `GeneticContribution` — founder id (non-null) and proportion in [0, 1]; `IsLost` when 0.
- `BreedingPair` — sire id ≠ dam id, kinship ≤ 0.5 (distinct individuals). Equality on (sire, dam).
- `KinshipPolicy` — max pair kinship ≤ 0.25; pairings per individual in [1, 10];
  min breeding age in [0, 50]; max breeding age > min and ≤ 100. `IsOfBreedingAge(individual, date)`.
- `Sex` (Male, Female), `BreedingPlanStatus` (Draft, Approved).

## Entities and aggregates
- `Individual` — id, house name (1–40 non-blank chars), defined sex, birth date, optional sire/dam.
  Sire and dam are both present or both absent (founder); sire ≠ dam; never its own parent.
  `RecordDeath(date)`: once only, not before birth.
- `Pedigree` (aggregate) — programme name and species (non-blank). `Register` refuses a duplicate,
  unregistered parents, a non-male sire, a non-female dam, parents not strictly older, a dam dead
  before the birth (`InvalidOperationException`; unknown id → `KeyNotFoundException`).
  Calculations: `GenerationOf` (founder = 0), `AncestorsOf`, `CommonAncestorsOf`,
  `KinshipBetween` (tabular method: self = ½(1+F), else ½ of the younger's parents' kinships with
  the other; founders unrelated), `InbreedingOf` (= kinship of parents), `MeanKinshipOf` (vs. all
  living, self included), `FounderContributions` (expected founder shares in the living, sum 1),
  `FounderEquivalents` (1 / Σp²). Both throw when nobody is alive.
- `BreedingPlan` (aggregate) — pedigree, policy, season start; Draft → Approved.
  `Propose(sire, dam)` refuses with `PairingRefusedException` (wrong sex, dead, out of breeding
  age at season start, kinship above the policy limit, duplicate pair, an individual over its
  pairing quota). `Withdraw(pair)`, `Approve()` (needs ≥ 1 pair; afterwards any change throws
  `InvalidOperationException`). `ExpectedOffspringInbreeding` = mean pair kinship (0 if empty).
  `RankSiresFor(dam)` — eligible living males, lowest pair kinship first, then lowest mean kinship.
- `PairingRefusedException` — carries sire id, dam id and the reason.
