# Railway Interlocking Route Control — domain model

Namespace `RailwayInterlocking.Domain`, project `Domain/RailwayInterlocking.Domain.csproj`.

## Value objects
- `SectionId(string value)` — 1-2 capital letters then 1-4 digits (`T101`); value equality.
- `TrainId(string headcode)` — digit, capital letter, two digits (`1A23`); value equality.
- `PointPosition` — enum `Normal` / `Reverse`.
- `PointSetting(string pointName, PointPosition required)` — non-blank name, defined position.

## Entities
- `TrackSection(SectionId id, double lengthMetres, double lineSpeedKmh)` — length in [10, 5000] m,
  line speed in (0, 300] km/h. State: `IsOccupied`, `LockedByRoute`. `ClearanceTimeSeconds` = length / speed.
- `Point(string name, SectionId section, PointPosition initialPosition)` — non-blank name, lies in a section.
  State: `Position`, `LockedByRoute`. A locked point never moves.
- `Route(name, entrySignal, exitSignal, IReadOnlyList<SectionId> sections, IReadOnlyList<PointSetting> pointSettings)` —
  entry != exit signal, at least one section, no section twice, no null element, no point set twice.
  `ConflictsWith(other)`: shares a section, or needs a shared point in the opposite position.
- `RouteLock(Route route, TrainId train, DateTimeOffset lockedAt)` — a set route for one train; lock time set.
  Tracks `RemainingSections` (running order), visited sections, `TrainHasEntered`, `IsFullyReleased`.

## Aggregate: `Interlocking(areaName, sections, points, routes)`
Construction refuses duplicates, points in unknown sections, routes over unknown sections or points,
and routes setting a point outside their own sections.
- `RequestRoute(routeName, train, at)` — checks all, then throws and locks points and locks sections atomically.
  Refusals (`InterlockingRefusedException` with `RefusalReason`): `UnknownRoute`, `TrainAlreadyHasRoute`,
  `ConflictingRoute` (incl. same route already set), `SectionOccupied`, `SectionLocked`, `PointLocked`.
- `CancelRoute(routeName)` — `RouteNotSet` if not active; `ApproachLocked` once the train has entered.
- `OnSectionOccupied(id)` — asynchronous track-circuit event; records occupancy, marks the route entered.
- `OnSectionCleared(id)` — sequential release: frees from the rear every section the train occupied and
  left (catching up on out-of-order clears), unlocks points whose section is released, ends the route
  when fully released. A clear ahead of the train keeps the lock.
- Safety invariants: a section is locked into at most one route; no two active routes conflict;
  a locked point never moves; a route is set only over clear, unlocked sections.
