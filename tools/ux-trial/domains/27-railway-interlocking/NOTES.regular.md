# Railway interlocking — notes (regular developer)

## What I did
- Created `Tests.Regular`, restored, then `any generate SectionId TrainId TrackSection Point PointSetting Route RouteLock --entry-point static:Arbitrary`.
- Resolved every `to verify`: patterns for `SectionId`/`TrainId`, bounds for `TrackSection`, distinct
  sections and distinct point names for `Route`, a non-default `lockedAt` for `RouteLock`.
- Wrote a `Junction` fixture: the topology (which route shares what) is written out because it is what
  the tests are about; section ids, lengths, speeds, point start position and area name are drawn.
- 32 tests (route rules, conflicts, construction refusals, setting/locking, cancel, sequential release,
  out-of-order clears). All pass; re-ran 8 times with fresh seeds, no flake.

## Frictions, in the order I met them
1. `any generate ... Point` failed for that one type:
   `✗ Point: several types answer to that name. RailwayInterlocking.Domain.Point / System.Drawing.Point`.
   My project never imports System.Drawing; the framework type should not compete with a type from a
   project reference. Re-ran with the full name. Minor, but the batch exits 1.
2. `TrackSection(id, lengthMetres, lineSpeedKmh)`: both guards are the most ordinary double guard there is,
   `if (double.IsNaN(x) || x < MinimumLengthMetres || x > MaximumLengthMetres) throw`, and
   `if (double.IsNaN(x) || x <= 0 || x > 300) throw`. Both came out `to verify, unread guards` with
   `Any.Double()`. I expected `Between(10, 5000)` and `GreaterThan(0).LessThanOrEqualTo(300)`
   (NaN is never drawn anyway, per the numbers page). Hand-wrote both.
3. `RouteLock`: `if (lockedAt == default) throw` is unread too; would map to a `DifferentFrom(default)`.
4. `SectionId`/`TrainId` validate with a regex: as documented, `to verify`. Replacing
   `Any.String().NonEmpty()` by `Any.StringMatching(...)` means retyping the factory and the refining
   `Func<AnyString, AnyString>` (documented, CS0029 otherwise). Every value object with a format pays it.
5. `Route`: the `sections` TODO names only the null-element guard (line 26); the "section twice" guard
   right below it is not named and the scaffolded `Any.ListOf(new AnySectionId()).NonEmpty()` can
   draw duplicates. Same for "point set twice". I only caught it because I read the constructor.
6. `Route` refuses `entrySignal == exitSignal`. There is no way to say "different from the other
   parameter" in a scaffolded generator; I drew the two signals from disjoint patterns
   (`SE[0-9]{1,3}` vs `SX[0-9]{1,3}`) with a comment. Works, but it is a trick.
7. Two different trains (or two different section ids) in one test: the only honest spelling I found
   is `Any.ListOf(Arbitrary.TrainId()).WithCount(2).Distinct().Generate()` then `[0]`/`[1]`. Drawing
   twice could collide (rare, hence flaky). A scaffolded `AnyX` has no `DifferentFrom(...)`.
8. Distinct point settings needed a hand-written `IEqualityComparer<PointSetting>` (PointSetting has
   no value equality). `Distinct(comparer)` then worked fine, including inside a `With…` lambda with
   `WithCount(1).Containing(...)`.

## What worked really well
- `--entry-point static:Arbitrary` for several types in one command: `Arbitrary.Route()` reads well.
- The non-compiling `TODO_verify_...` lines are a great work list; nothing silently wrong slipped in.
- `NotBlank()` and `NonEmpty()` inferred from `ThrowIfNullOrWhiteSpace` and `Count == 0`.
- Composed generators (`new AnySectionId()` inside `AnyTrackSection`, `AnyRoute`) just work once the leaf is fixed.
- `WithSections([...])` pins with collection expressions, `WithPointSettings(p => ...)` refinements.
- `[Reproducible]` is invisible until needed. Everything passed first run after the fixes.

## Domain remarks (not library findings)
- `RefusalReason.SectionLocked` and `PointLocked` look unreachable: any route touching a locked
  section or a locked point (which must lie in its own sections) is caught as `ConflictingRoute` first.
  So I did not test them. `OutOfSequenceRelease` is internal-only.

## Fluency: 4 / 5
Fast and pleasant once the generators are fixed; about a third of my time went to hand-resolving
`to verify` rows the guards made obvious (the double ranges) and to working around cross-parameter
and "two different X" needs.
