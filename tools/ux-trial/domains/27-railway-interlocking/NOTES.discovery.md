# Discovery notes — 27 railway interlocking

## What I did
- Read MODEL.md and the domain, then getting-started, the CLI page, the xUnit page, and the
  strings / numbers / collections / dates generator pages as I needed them.
- Created `Tests.Discovery`, restored, ran
  `any generate SectionId TrainId PointSetting TrackSection Point Route --entry-point any`.
- Resolved the build-blocking `TODO(any)` lines, wrote a drawn `Junction` layout fixture
  (approach, junction with one point, main, branch, an independent siding; three routes) and
  32 test methods (35 cases with theory rows): route setting and its refusals, cancellation,
  sequential release incl. out-of-order clears, point unlocking, control-table conflicts,
  interlocking construction, value objects. Every class is `[Reproducible]`.
- `dotnet test --project Tests.Discovery`: 35/35 green, re-run 8 times, all green.

## Frictions and surprises, in order
1. `any generate ... Point` refused: `✗ Point: several types answer to that name.
   RailwayInterlocking.Domain.Point / System.Drawing.Point  Name the one you mean in full.`
   Clear and actionable; `any generate RailwayInterlocking.Domain.Point` worked. Mildly
   surprising that a BCL type nobody imports competes with the referenced domain's own type.
2. `TrackSection(id, lengthMetres, lineSpeedKmh)`: the guards
   `if (double.IsNaN(x) || x < MinimumLengthMetres || x > MaximumLengthMetres) throw` and
   `if (double.IsNaN(x) || x <= 0 || x > 300) throw` were both `to verify, unread guards`, with a
   fallback of `Any.Double()`. That is a very ordinary range guard; I expected `.Between(...)`.
   The block-the-build behaviour meant I could not miss it — that part worked as designed.
3. `SectionId` / `TrainId` regex guards: `to verify`, as the docs say. The docs' section
   "When the replacement is another kind of generator" told me exactly the three lines to retype
   (`AnyString` -> `AnyPattern`, factory + `Func<...>`). No hesitation thanks to that page.
4. `Route`: entry != exit is a rule between two parameters. The docs suggest deriving one draw
   from the other in `Generate()`, but the scaffolded fields are opaque `IAny<string>`, so I
   could not say "exit different from entry" declaratively; I wrote a bounded redraw loop in
   `Generate()`. Works, but it is the "succeeds by luck" shape the docs warn against.
5. `Route`: "no point set twice" means distinct by *name*, not by `PointSetting` equality. I had
   to write an `IEqualityComparer<PointSetting>` for `.Distinct(comparer)`; a key-selector
   overload (`DistinctBy(p => p.PointName)`) would have been a one-liner. The recap only showed
   the *first* unread guard per parameter (null check), so the duplicate rules on `sections` and
   `pointSettings` were found by reading the constructor myself — the docs do say "first".
6. `RouteLock` refuses `default(DateTimeOffset)`; `Any.DateTimeOffset()` spans Min..Max, so I
   declared `.DifferentFrom(default(DateTimeOffset))`. Found it under "Membership and exclusion".
7. JD030 (Info) fires 11 times on `Any.String().NotBlank()` without a length — invisible in a
   normal build log; I only saw it in a SARIF log. By design per the CLI page.
8. Domain note (not a library issue): `RefusalReason.PointLocked` is unreachable through a valid
   interlocking — a route setting a point must include the point's section, so a shared section
   is refused first as `ConflictingRoute`. I did not write a test for it.

## What worked well
- The `TODO_verify_...` line that stops the build at the exact guard, with the guard quoted.
- The `With...(value)` / `With...(lambda)` pair: pinning sections and point settings of a
  route while everything else stays drawn read naturally (`Any.Route().WithSections([...])`).
- `Any.ListOf(Any.SectionId()).WithCount(5).Distinct()` for a set of distinct ids: obvious,
  and the value-equality of `SectionId` made `Distinct()` meaningful.
- `--entry-point any` gave `Any.Route()` beside `Any.Int32()`; no setup.
- `[Reproducible]` on the class: nothing else to do.

## Fluency: 4 / 5
I was productive within minutes and every blocking point named its own fix. One point off for
the unread numeric range guard (most common guard shape in this domain) and for the two
cross-element rules (entry != exit, distinct by name) that needed hand-written plumbing.
