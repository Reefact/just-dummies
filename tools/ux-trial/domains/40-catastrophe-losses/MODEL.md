# Catastrophe loss aggregation — domain model

Namespace `CatastropheLosses.Domain`. Money is `decimal`, hazard physics is `double`.

## Types and invariants

- `Peril` (enum): TropicalCyclone, Earthquake, Flood, SevereConvectiveStorm.
- `GeoPoint(latitude, longitude)`: latitude in [-90, 90], longitude in [-180, 180], no NaN. `DistanceKmTo` (haversine).
- `Footprint(center, radiusKm, peakIntensity)`: center non-null; radius in (0, 2000] km; peak finite and > 0.
  `IntensityAt(location)` decays linearly from the peak at the centre to 0 at the radius; `Covers(location)`.
- `VulnerabilityCurve(name, intensities, damageRatios)`: name not blank; same count, at least 2 points;
  intensities finite, >= 0, strictly increasing; ratios in [0, 1], non-decreasing, the first one exactly 0.
  `DamageRatioAt(intensity)`: 0 below the first point, linear interpolation, last ratio above the last point.
- `InsuredProperty(id, location, insuredValue, vulnerability)`: id `PRP-` + 6 digits (else `FormatException`);
  insured value > 0. `GroundUpLoss(intensity)` = value x damage ratio, rounded to cents.
- `PolicyTerms(deductible, limit)`: deductible >= 0, limit > 0. `Apply(groundUp)` = min(max(groundUp - deductible, 0), limit).
- `Exposure(property, terms)`: cross-field — deductible < insured value, limit <= insured value.
  `InsuredLoss(footprint, severityFactor)`: intensity at the property x severity, through the curve, then the terms.
- `CatastropheEvent(id, peril, footprint, annualRate)`: id `EV-` + 4 to 8 digits; defined peril; rate in (0, 5].
  `ReturnPeriodYears` = 1 / rate.
- `Portfolio(name, currency)` (aggregate): name not blank, <= 100 chars; currency 3 upper-case letters.
  State: open -> frozen. `Add(exposure)` refuses a duplicate property id and any add once frozen
  (`InvalidOperationException`); `Freeze()` refuses an empty portfolio or a second freeze.
  `EventLoss(event, severityFactor)` sums every exposure's insured loss under the same occurrence.
  `TotalInsuredValue`, `TotalLimit`.
- `LossDistribution(annualLosses)`: at least one year, no negative loss. `Mean` (AAL), `Maximum`,
  `StandardDeviation`, `Quantile(p)` (VaR, p in (0, 1)), `TailValueAtRisk(p)`, `ReturnPeriodLoss(years > 1)`,
  `ExceedanceProbability(threshold >= 0)`.
- `CatastropheModel(catalogue, severityVolatility)`: catalogue non-empty, no null, unique event ids;
  volatility in [0, 2].

## Behaviour

`CatastropheModel.Simulate(portfolio, years, seed)` refuses an unfrozen portfolio and years outside
[1, 1 000 000]. For each simulated year and each catalogue event it draws a Poisson number of occurrences
(rate = annual rate); each occurrence draws ONE lognormal severity factor (mean 1) shared by every exposure
in the footprint — this shared draw is the correlation between damages. Terms apply per exposure and per
occurrence; occurrences sum into the annual loss. The years form a `LossDistribution`. Same seed, same result.
