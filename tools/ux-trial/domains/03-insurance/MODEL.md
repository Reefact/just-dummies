# Insurance — domain model

Namespace `Insurance.Domain` (`Domain/`). A car policy with a bonus-malus coefficient, a coverage period of at most one year, claims
declared against it and an annual renewal.

Guard conventions used below: "null/blank" means `ArgumentException.ThrowIfNullOrWhiteSpace` (null throws `ArgumentNullException`,
blank throws `ArgumentException`). Range guards throw `ArgumentOutOfRangeException`.

## Value objects
- `PolicyNumber(value)` — null → `ArgumentNullException`; must match `^POL-\d{4}-[A-Z]{2}\d{6}$` (e.g. `POL-2026-FR123456`) else `ArgumentException`. The year is not validated. Equality by value.
- `BonusMalus(coefficient)` — a `double` in [`Floor` = 0.50, `Ceiling` = 3.50], both ends accepted. NaN → `ArgumentException`; below the floor or above the ceiling (infinities included) → `ArgumentOutOfRangeException`.
  `NewDriver` (static property) = 1.00.
  `AfterYear(responsibleClaims, partialClaims)`: both counts ≥ 0 (`ArgumentOutOfRangeException`). With no claim of either kind: `max(0.50, round(coefficient × 0.95, 2, toward zero))`.
  Otherwise: `min(3.50, round(coefficient × 1.25^responsible × 1.125^partial, 2, toward zero))`. A year with only partial claims therefore raises the coefficient (× 1.125 each).
- `Deductible(amount)` — amount in [0, `Ceiling` = 5 000] (`ArgumentOutOfRangeException`), at most two decimal places (`ArgumentException`). Zero is accepted.
- `CoveragePeriod(start, end)` — `end` strictly after `start` and `end <= start.AddYears(1)` (a period of exactly one calendar year is accepted); both `ArgumentException`. Both days are inclusive.
  `Days` = `end - start + 1`; `Contains(day)` = `start <= day <= end`; `Next()` = a new period starting the day after `end` and ending `Days` days after `end` (`End.AddDays(Days)`), built through the constructor, so the same checks apply to it.
  The new period spans the same number of days, but the one-year ceiling is measured from its new start, so `Next()` can throw `ArgumentException`: for example `2028-01-01 .. 2029-01-01` is accepted (`Days` = 367), while its `Next()` would run `2029-01-02 .. 2030-01-03`, past `2030-01-02`.
- `Driver(fullName, birthDate, yearsLicensed)` — fullName null/blank (stored trimmed); yearsLicensed in [0, 80] (`ArgumentOutOfRangeException`). `birthDate` is not checked and is not related to `yearsLicensed`.
  `IsYoungDriver` = `YearsLicensed < 3`.
- Enums `Responsibility` (`None, Partial, Full`), `VehicleCategory` (`City, Family, Utility, Sport`), `CoverageLevel` (`ThirdParty, ThirdPartyFireTheft, Comprehensive`).

## Entities
- `Claim(id, occurredOn, damage, responsibility)` — `id` not `Guid.Empty` (`ArgumentException`); damage > 0 (`ArgumentOutOfRangeException`; no upper bound or decimal-place rule);
  responsibility must be a defined value (`ArgumentOutOfRangeException`, evaluated last). `InsuredShare`: `None` → 1.00, `Partial` → 0.50, `Full` → 0.00 (share of the damage the third party's insurer reimburses, seen from the policyholder).
- `CarPolicy(number, driver, vehicle, level, period, deductible, bonusMalus, basePremium, claims)` — number, driver, period, deductible, bonusMalus, claims not null (`vehicle` and `level` are not checked for being defined values);
  `basePremium` in (0, 20 000] (`ArgumentOutOfRangeException`); every initial claim must have `OccurredOn` inside the period (`ArgumentException`). The claims are copied into the policy.
  - `AnnualPremium()` = `basePremium × (decimal) bonusMalus.Coefficient`, times `YoungDriverSurcharge` = 1.50 when `Driver.IsYoungDriver`, rounded to 2 decimals, midpoint away from zero.
  - `DeclareClaim(claim)`: null → `ArgumentNullException`; a claim dated outside the period → `InvalidOperationException`. A claim whose id is already present is not refused.
  - `OwnDamagePayout(claim)`: null → `ArgumentNullException`; 0 unless `Level` is `Comprehensive`; otherwise `max(0, claim.Damage × (1 - claim.InsuredShare) - Deductible.Amount)`. The claim need not belong to the policy.
  - `Renew()`: counts the policy's claims with `Responsibility.Full` and `Responsibility.Partial`, and returns a new policy with the same number, driver, vehicle, level, deductible and base premium,
    `Period.Next()`, `BonusMalus.AfterYear(full, partial)` and no claims. The driver (hence `YearsLicensed`) is carried over unchanged. Exceptions of `Next()` / `AfterYear` / the constructor propagate.
