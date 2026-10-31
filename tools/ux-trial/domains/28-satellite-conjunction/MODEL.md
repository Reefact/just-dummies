# Satellite Conjunction Risk Assessment — domain model

Namespace `SatelliteConjunction.Domain`. Units: km, km/s, km² (covariance), metres (hard-body radius). Frame: Earth-centred inertial.

## Value objects

- **Epoch** (`DateTimeOffset utc`): offset must be zero (UTC); not before 1957-10-04. `Add`, `SecondsSince`, ordered.
- **Vector3** (`x, y, z`): every component finite. `Magnitude`, `Plus`, `Minus`, `Scale`, `Dot`, `Cross`, `Normalized` (refuses the zero vector).
- **PositionCovariance** (`xx, yy, zz, xy, xz, yz`): variances in (0, 1000² km²]; cross terms finite with |correlation| < 1; the 3x3 matrix must be positive definite (determinant > 0). `Combine` (sum of two independent covariances), `Project`, `MeanSigmaKm`.
- **CollisionProbability** (`value`): in [0, 1], not NaN. `Level`: `High` at >= 1e-4, `Elevated` at >= 1e-5, else `Low` (enum **RiskLevel**).
- **RiskAssessment** (`timeOfClosestApproach, missDistanceKm, relativeSpeedKmPerSecond, combinedSigmaKm, probability`): miss distance finite and >= 0; relative speed in (0, 24] km/s; combined sigma > 0.

## Entities and aggregates

- **SpaceObject** (`catalogNumber, name, kind, hardBodyRadiusMeters, isActive`): catalogue number 1..999 999 999; name not blank, max 24 chars; `kind` a defined **ObjectKind** (Payload, RocketBody, Debris); radius in (0, 100] m; only a Payload may be active.
- **OrbitalState** (`epoch, positionKm, velocityKmPerSecond, covariance`): no nulls; position radius above the Earth's surface (6378.137 km) and at most 100 000 km altitude; speed in (0, 12] km/s.
- **Trajectory** (`spaceObject, states`): at least two states, none null, epochs strictly increasing, consecutive states at most 10 minutes apart. `Covers(epoch)`, `StateAt(epoch)` interpolates position and velocity linearly (keeps the nearer node's covariance) and refuses an epoch outside the span.
- **Conjunction** (aggregate; `id, primary, secondary, timeOfClosestApproach`): non-empty id; the primary's object must be active; primary and secondary must be different catalogue numbers; both trajectories must cover the TCA. Status (**ConjunctionStatus**) starts at `Screened`.

## Behaviour

- `Conjunction.Assess()`: interpolates both states at TCA, computes miss distance and relative speed, combines the two covariances, projects everything onto the encounter plane (normal to the relative velocity) and evaluates a 2D Gaussian Pc = R² / (2·sqrt(det C)) · exp(-d²/2), with R the summed hard-body radii and d the Mahalanobis miss distance, capped at 1. Same miss distance with a tighter or a more dispersed covariance gives very different Pc. Appends to `History`, moves `Screened` to `Assessed`; refused once `Closed`, and refused for co-moving objects.
- `PlanManeuver()`: only from `Assessed` and only when the latest level is `High`; moves to `ManeuverPlanned`.
- `Close(reason)`: reason not blank; refused when `Screened`, already `Closed`, or `Assessed` with a `High` risk (plan a manoeuvre first).
- Every refusal throws `ArgumentException` / `ArgumentOutOfRangeException` (construction) or `InvalidOperationException` (state transition).
