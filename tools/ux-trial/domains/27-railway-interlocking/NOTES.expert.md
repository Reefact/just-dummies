# Railway interlocking — expert crafter notes

## What I did
- `Tests.Expert`: 36 xUnit v3 tests, all green (15 consecutive runs, `[assembly: Reproducible]`).
- `any.json` (`output: ./Arbitrary`, `entryPoint: any`), then `any generate SectionId TrainId PointSetting
  TrackSection Point Route RouteLock`, so tests read `Any.Route()`, `Any.TrackSection()`, ...
- Refined bases: `SectionId`/`TrainId` -> `Any.StringMatching(...)` (retyped to `AnyPattern` as the CLI page
  says), track length/speed bounds, `lockedAt` -> `DifferentFrom(default)`, route sections `.Distinct()`,
  point settings `.Distinct(PointSettingByPointName)`, names given a max length.
- `AnyRoute` reworked by hand: entry/exit signals drawn together as two distinct names
  (`ListOf(...).WithCount(2).Distinct().As(...)`), pinned with `WithSignals(entry, exit)`.
- `AnyJunctionLayout : IAny<JunctionLayout>`: a fixed topology (approach, junction + point, main, branch,
  independent siding; three routes) whose ids, names, lengths, speeds and initial point lie are all drawn.
  Every interlocking test starts from it; `Recipes` holds route name, area name, instant, two distinct trains.
- Tests cover: route invariants and `ConflictsWith`, track clearance time, control-table validation at
  commissioning, route setting / refusals (unknown, conflicting, same route twice, train already routed,
  occupied section) with "state unchanged" checks, cancellation (before entry, approach locked, not set),
  sequential release (rear release, clear ahead ignored, out-of-order catch-up, point freed, route ends,
  next conflicting route settable).

## Frictions and surprises, in order met
1. `any generate ... Point ...`: `✗ Point: several types answer to that name. RailwayInterlocking.Domain.Point /
   System.Drawing.Point. Name the one you mean in full.` Clear, but System.Drawing is not imported anywhere
   in the test project; the domain project's own type could win. Fully-qualified name worked.
2. `TrackSection`: `if (double.IsNaN(x) || x < MinimumLengthMetres || x > MaximumLengthMetres) throw` is
   "unread guards" -> `Any.Double()`. A very ordinary guard shape (NaN || below const || above const).
   Same for `if (lockedAt == default)`. Fixing by hand was easy; the TODO compile-stop did its job.
3. `Route.sections`: recap says `guard, unread guards` and names only the first unread guard
   (`sections.Any(s => s is null)`). The *distinctness* guard two lines below
   (`sections.Distinct().Count() != sections.Count`) is named nowhere — neither recap, TODO nor JSON.
   Same for `pointSettings` (distinct by name). Someone who checks "ListOf never yields null" and deletes
   the TODO line gets routes that intermittently throw "traverses a section twice". I only caught it by
   reading the constructor. The scaffolded base is `NonEmpty()` without `Distinct()`.
4. Entry != exit signal: the CLI page's "When a guard ties two parameters" recipes (start + duration,
   derive in `Generate()`) do not fit a "differs from" rule while keeping both pins; there is no
   `DifferentFrom` on a plain `IAny<T>`. I restructured the generator around a distinct pair.
5. Disjoint section runs for two routes: no way to say "a list disjoint from that one"; I drew one distinct
   list and split it. Fine, but a recipe in the collections page would help.
6. Distinct pairs (two trains, two route names) come up constantly: `ListOf(g).WithCount(2).Distinct()` +
   indexing every time. A `Any.DistinctPairOf(g)` (or deconstruction on a 2-list) would read better.
7. Dummy-vs-assertion tension: interlocking tests assert on fixture *handles* (`anyJunction.Main`), drawn
   ids compared by identity, not by value. The "an any-name in the assertion is a property" heuristic
   flags this though the outcome cannot depend on the drawn value. A doc paragraph on fixture handles
   would settle it. Where the assertion is about a value (train, lock time, running order) I used literals.
8. `--dry-run` prints `✓ AnyInterlocking.cs — ...` then `Nothing written`; the tick reads as "written".
9. No analyzer diagnostic fired on my code (checked via a SARIF error log): nothing to report there.

## Domain observations (not library)
- `PointLocked` and `SectionLocked` are unreachable: a point lies in a section of every route setting it,
  so `ConflictingRoute` (checked on the full section list, released sections included) always wins first.
  I did not write tests for those two reasons.

## What worked well
- The TODO-that-does-not-compile: every unread guard was in front of me at build time, at the line.
- `Any.StringMatching` for regex-shaped value objects: one line, exact.
- Refining lambdas compose nicely: `WithSections(s => s.Containing(anyShared))` said exactly the test.
- `Distinct(IEqualityComparer)` handled "settings distinct by point name" with no ceremony.
- `any.json` + `--entry-point any`: `Any.Route()` beside `Any.Int32()` reads as one language.
- Composing scaffolded generators into a layout recipe (`IAny<JunctionLayout>`) felt natural.

## Fluency: 4/5
Everything I needed existed or had a documented workaround, and the result reads like the domain.
Minus one for the silent distinctness guards (finding 3), which is the kind of thing that ends as a
rare flaky test, and for the hand rework a cross-parameter "differs from" rule required.
