# Changelog

All notable, user-facing changes to **JustDummies** are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
Releases are cut from the `lib` train (see [CONTRIBUTING.md](../CONTRIBUTING.md)).

## [Unreleased]

### Fixed

- **JD014 now reports a size *ceiling* above the producible cap.** `WithMaxLength` and `WithMaxCount` were
  declared uncapped while the library caps them, a leftover from the per-generator caps of ADR-0029 that
  ADR-0076 replaced with one rule for every size argument. The consequence was the worst kind of gap: a call
  the analyzer blessed and the library refused at run time, with nothing between the two to say so.
  `WithPathSegments` keeps its silence — verified rather than assumed, `UriSpec.RequireSegmentCount` refuses a
  negative count and nothing else.

### Changed

- **BREAKING — `AnyChar` and `AnyString` rename `LowerCase()`/`UpperCase()` to `InLowerCase()`/`InUpperCase()`.**
  The bare names read like a state change rather than a quality of the drawn value — unlike `Alpha()`, `Numeric()`
  or `Hexadecimal()`, which are adjectives on their own. The new names read as a chained clause instead:
  `Any.String().AlphaNumeric().InUpperCase()`. No behaviour changes; only the two names do.

## [1.0.0-preview.3] - 2026-08-21

### Added

- **JD032 — *A bound declared twice, where only the tighter one survives*** (category `JustDummies.Constraints`,
  🟠 Warning, on by default). Bounds fold silently and monotonically — a minimum keeps the larger of the two values,
  a maximum the smaller — and the losing call returns the generator unchanged, so on a chain that declares the same
  bound twice one of the two calls is always dead, the looser one, whichever order it was written in. Nothing threw
  and nothing reported it
  ([ADR-0078](../doc/handwritten/for-maintainers/adr/0078-own-a-bound-declared-twice-as-one-rule.md)).

  A warning rather than information, on JD025's precedent: a duplicate that collapses silently is already a warning
  in this rule set. It is matched on the constraint's **name**, which leaves the aliases alone — `NonEmpty()` and
  `Positive()` reach a minimum too, and choosing them says something the explicit bound does not. It never follows a
  generator held under a name, and there that is soundness rather than scope: a generator is an immutable recipe, so
  a named intermediate still draws and its bound is not dead.

### Changed

- **JD024 no longer reports a bound already implied by a bound of the same name** — that shape is JD032's now, in
  both writing orders and in every generator family, so one mistake draws one diagnostic. JD024 keeps what its
  message describes: an inert `Except`/`DifferentFrom`, a bound implied by a *different* one such as `Positive()`
  after `GreaterThan(5)`, and a range declared twice to no effect. Its pages state the boundary.

### Added

- **JD031 — *Two inclusive bounds the library also names as one range*** (category `JustDummies.Constraints`,
  🔵 Info, on by default). A chain that declares both inclusive bounds separately — `WithMinLength(8).WithMaxLength(20)`,
  `GreaterThanOrEqualTo(1).LessThanOrEqualTo(50)` — is pointed at the range form the same generator exposes.
  Nothing is wrong at such a call site and nothing has to change: the two spellings behave identically, and
  declaring the bounds separately stays a documented, supported way to write a range. The rule closes a
  discoverability gap, which is why it is information and not a warning
  ([ADR-0077](../doc/handwritten/for-maintainers/adr/0077-admit-a-rule-that-reports-a-correct-spelling.md)).

  It reports **inclusive pairs only**: `GreaterThan(5).LessThan(10)` is `Between(6, 9)` on an integral type and
  has no range form at all on a floating-point one, so every strict and mixed pair is silent. It is also silent
  when the bounds are not in the same chain, when a bound is declared twice, when a bound is reached through an
  alias such as `NonEmpty()`, and when the chain also settles an exact size. A pair of equal bounds is named as
  `WithLengthBetween(8, 8)` and never as `WithLength(8)`, which settles the length without drawing and would
  therefore move every later value on a seeded run.

### Changed

- **A character constraint now governs what is drawn, not the literals you wrote.** `Alpha()`, `Numeric()`,
  `AlphaNumeric()`, `Punctuation()`, `Printable()`, `NonPrintable()`, `Whitespaces()`, `Hexadecimal()`,
  `WithChars(...)`, the subtractive `WithoutAlpha()`/`WithoutNumeric()` and the casings `InLowerCase()`/`InUpperCase()`
  used to be checked against every anchored fragment as well, so `Any.String().AlphaNumeric().StartingWith("ORD-")`
  threw at declaration: the separator you wrote is not alphanumeric. It never was going to be drawn. A prefix, a
  suffix and a contained value are now kept exactly as written, and the declared alphabet governs the characters
  the generator draws beside them — so that chain yields `ORD-` followed by alphanumerics only, the hyphen
  appearing in the prefix and nowhere else. A very ordinary format finally says what it means, with each of its
  rules a named call: `Any.String().StartingWith("ORD-").AlphaNumeric().InUpperCase().WithLengthBetween(8, 20)`.
  The workaround this replaces — a custom pool holding the separator — made the separator drawable *everywhere*,
  yielding `ORD-SWLTLFk-` and `ORD-7-tVsCQNj61I17`, and cost the casing too, since a pool occupies the family slot
  and refuses to combine with one. Two consequences worth knowing: `InUpperCase()` now means "every letter I draw",
  so a lowercase literal beside it is kept rather than refused; and a value set is unchanged, so
  `OneOf("abc").InUpperCase()` still rejects `"abc"` — there the values are yours and the constraints filter them
  rather than shape a string
  ([ADR-0077](../doc/handwritten/for-maintainers/adr/0079-constrain-what-a-dummy-draws-never-the-literals-it-was-given.md)).
  This is a relaxation: no chain that worked stops working, and no generated value changes shape. Only code
  asserting on the removed conflict is affected.

- **New rule `JD033` — an anchored literal the declared characters cannot draw.** 🔵 Info, on by default. It
  names an ambiguity rather than a fault. `Any.String().AlphaNumeric().StartingWith("ORD-")` says two things
  about its characters — only alphanumerics, and then a hyphen — and the change above resolves that one specific
  way: the hyphen appears in the prefix and nowhere else. The rule tells you which reading applies, at the call
  site, without refusing what is the simple way to write `ORD-pDc8`. Silent
  once `OneOf(...)` is declared: nothing is drawn beside a pooled value, and a pooled value a constraint refuses
  is `JD029`'s to report. Silence it with `dotnet_diagnostic.JD033.severity = none` if your codebase writes
  fixed-prefix formats everywhere.

- **`JD015` now reports a value set a constraint empties.** `Any.String().AlphaNumeric().OneOf("ORD-1", "ORD-2")`
  throws at declaration — the family allows neither value — and until now the build said so only through two
  `JD029` notes at 🔵 Info, a severity that reads as "this still works". It is a 🟠 Warning now, once, about the
  chain, like every other *admits no value* case. This is the counterpart of the exemption above and the reason
  the two read differently: an anchored literal claims its own region of a shaped string while the family claims
  the rest, so the two never meet; a value set claims the **whole** string, so the family's region is that
  supplied value itself and the two must agree. The remedy the message points at is to drop the constraint — the
  values are yours, and beside them it contributes nothing. Reported only when **every** value is refused; a pool
  one value survives is a narrowing, still `JD029`'s, which now stays quiet on the emptied case.

- **`JD015` narrows to the length budget.** The analyzer mirrored the rule above at build time, so leaving it
  would have refused at compile time a chain the run time now honours. It keeps the one check that is still a
  contradiction — anchored fragments that cannot fit the declared length, `WithLength(3).StartingWith("ORD-")` —
  with its message unchanged. Its id, title, category and severity are unchanged.

## [1.0.0-preview.2] - 2026-08-18

### Changed

- **BREAKING — an unconstrained dummy now certifies something.** `Any.String()` used to yield 0 to 16 ASCII
  letters and digits, and `Any.Char()` one of those 62 characters. Both now draw from the **whole of ASCII**
  (0x00–0x7F), control characters included, and a string spans **0 to 1024** characters. That is deliberately
  inconvenient, and it is the point: a dummy is a value the code under test had no say in, so restricting it in
  advance to short, tame text removes exactly the evidence the draw exists to produce. A test that passed with
  `abc123` had shown nothing about a `\r`, a NUL, or 300 characters. Narrow it with the invariants your code
  actually has — `NonEmpty()`, `WithMaxLength(50)`, `Printable()` — each of which is a fact about the surrounding
  code, written where it belongs
  ([ADR-0075](../doc/handwritten/for-maintainers/adr/0075-draw-characters-from-the-whole-of-ascii.md)).

- **BREAKING — a declared maximum now steers the draw.** `WithMaxLength(50)` used to yield 0 to 16 characters and
  `WithLengthBetween(1000, 5000)` yielded 1000 to 1016 — two numbers written, 1.6 % of the interval drawn. A
  maximum now **replaces** the default spread instead of composing with it, so a written range is the range drawn,
  under either spelling. The same rule reaches `WithMaxCount` on collections, whose spread is unchanged. Because a
  steering maximum is one the generator has to produce, it is now refused above 1 000 000 like every other size:
  `WithMaxLength(4_000_000)`, legal before, is an `ArgumentOutOfRangeException` now
  ([ADR-0076](../doc/handwritten/for-maintainers/adr/0076-let-a-declared-maximum-steer-the-size-draw.md)).

### Added

- **Five more character families, and every one of them narrows.** `Punctuation()` (the 32 printable
  non-alphanumerics, POSIX `[:punct:]`), `Printable()` (0x20–0x7E), `NonPrintable()` (the C0 controls and `DEL`),
  `Whitespaces()` (the space and the tab) and `Hexadecimal()` (RFC 4648, both cases — chain a casing for the
  single-case form a hash needs). Each occupies the one family slot, so a second conflicts naming both sides;
  `WithoutAlpha()` and `WithoutNumeric()` subtract instead and accumulate, so
  `WithoutAlpha().WithoutNumeric()` leaves the punctuation, the whitespace and the controls. On `Any.String()` and
  `Any.Char()` alike. Two caveats worth knowing: `Punctuation()` is deliberately **broader** than
  `char.IsPunctuation`, which reads `+`, `<` and `$` as symbols, and it deliberately excludes the space — the one
  character a `Trim()` removes in silence, so a separator you can rely on must not be one. `Whitespaces()` names
  it instead. Nothing named reaches past ASCII: a pool following the runtime's Unicode version would draw
  differently on two target frameworks, against a guarantee this library checks byte for byte.

- **JD030 names the length you did not declare.** A raised default only teaches when something says what to
  write instead, and a wall of characters in a failure message does not say `WithMaxLength`. The new rule reports
  an `Any.String()` chain that settles no length, at the call site, where you can act on it — `NonEmpty()`
  included, since it raises the floor and leaves the ceiling where it was. Reported as **information**, never a
  warning: a length a test genuinely does not care about is a legitimate thing to leave unsaid
  ([JD030](../doc/handwritten/for-users/analyzers/JD030.en.md)).

- **JD015 reads the new families, and the library escapes what it prints.** The build-time rule validates anchored
  fragments against all nine families and reports a subtraction under its own name. And since a draw can now be a
  control character, every value the library renders into a conflict message or a pool inspection is escaped — an
  `ESC` reaching your terminal would open an ANSI sequence in the very output reporting the failure.

- **See what your constraints left of a pool you supplied.** When a value set is declared beside constraints, a
  value the constraints refuse leaves the domain silently — only an emptied domain is reported. That is fine
  until the value set is a *catalogue* you maintain, at which point you cannot tell whether to widen the
  invariant or fix the catalogue. `IPoolInspection<T>` answers it: `GetSurvivors()` for the values still
  drawable, `GetRejections()` for the ones refused, each naming every `DeclaredConstraint` that refuses it — a
  name and its rendered arguments kept apart, so you can group and filter instead of parsing text. It is carried
  by **every** generator that admits a value set you supply — `Any.OneOf(...)`/`Any.ElementOf(...)`,
  `Any.String().OneOf(...)`, and all twenty-two families with a `OneOf`: the integers, `Any.Decimal()`, the
  floating-point builders, the dates and times, `Any.Char()`, `Any.Guid()` and `Any.Enum<T>()` — so a catalogue
  loaded from a file is answered whatever its element type. It is implemented **explicitly**, so it never appears
  among the constraints while you write them; reach it with a cast, and test the cast, since a generator with no
  pool of yours carries it not at all. `IsPooled` is not "this domain is countable": `Any.Int32().Between(1,
  1_000_000)` answers `false`, those values being the engine's rather than yours. Nothing here draws or consumes
  randomness, so an inspection between two draws leaves a seeded run replaying identically. The library reports
  and does not judge: it never warns that a pool was narrowed, because narrowing a shared catalogue at one call
  site is what the composition is for. New guide: *Inspecting a pool*
  ([ADR-0067](../doc/handwritten/for-maintainers/adr/0067-report-a-filtered-pool-through-an-explicit-interface.md),
  [ADR-0068](../doc/handwritten/for-maintainers/adr/0068-carry-the-pool-inspection-wherever-a-caller-supplies-the-values.md)).

- **JD029 tells you at build time when a value you wrote into a pool can never be drawn.** A value set composes
  with the constraints declared beside it, and a value they refuse leaves the domain in silence — so a pool can
  read as five values and draw from three. The new rule reports each such value where it is written, naming the
  constraint that refuses it. It is the dual of JD024: that one reports a constraint narrowing nothing, this one
  a value nothing lets through. **Info, not a warning**: narrowing a shared pool at one call site is what the
  composition is *for*, so this states a fact to weigh rather than a verdict. It covers the string value sets and
  the numeric ones whose constants fold exactly — every integer type and `decimal`; the binary floating-point
  families stay out, a `double` constant having no exact decimal to judge it by. It reads only what is written at
  the call site — a pool held in a variable, which is what a catalogue always is, is answered instead at run
  time by `IPoolInspection<T>`. The claim is deliberately one-sided: a constraint whose argument does not fold
  is skipped rather than guessed at, so the rule can under-report but never accuse a value it has not tested.

### Documentation

- **How to get a NaN when you actually need one.** `Any.Double()`, `Any.Single()` and `Any.Half()` refuse a
  non-finite value as an *argument* as well as a draw, so `Any.Double().Except(double.NaN)` throws — which read
  as a missing feature, because nothing said what to do instead. The packaged readme now carries the rule, its
  reason, and the exit (`Any.OneOf(double.NaN, ...)`, whose pool the library does not judge), plus when to use a
  literal rather than a pool, the `Equals`/`==` asymmetry that makes a pooled `NaN` deduplicate, and why
  `decimal` is not part of the subject at all. The refusal message names the exit too, and the three builders'
  XML docs carry it, so the answer is reachable from IntelliSense at the moment the caller is blocked. The
  recipe is locked in by tests: a documented exit that quietly stopped working would be worse than no
  documentation.

### Fixed

- **A decimal exclusion declared twice no longer empties a satisfiable grid.** On `Any.Decimal()` with
  `WithScale`, the engine counts excluded grid points to decide satisfiability and cardinality, and it counted a
  duplicated value as many times as it was declared — so `Except(0.01m).DifferentFrom(0.01m)` (restating an
  exclusion already in force) or `Except(0.01m, 0.01m)` could refuse a declaration as exhausted while values
  were still drawable, with a message claiming every grid value was forbidden. Excluded values are now
  deduplicated at construction, as the integer engines always did. A genuinely exhausted grid still conflicts
  eagerly; generation, draw counts and the public surface are unchanged.
- **A conflict caused by an exclusion now names that exclusion.** On `Any.Enum<T>()` and `Any.Guid()`, a
  constraint that emptied the domain reported the constraint it had emptied instead — so
  `Any.Enum<T>().OneOf(a, b).Except(a, b)` said *"no value OneOf(a, b) allows remains available"*, naming the
  victim and leaving the cause to be guessed, and an excluded pin said only *"which the exclusions forbid"*,
  naming none of them. Both now read like the interval generators, which were fixed first: *"it forbids every
  value OneOf(a, b) allows"*, and *"Empty() already pins the value to 00000000-… and NonEmpty() forbids it"*.
  Only exclusions that actually removed something are named, since one whose values were never drawable caused
  nothing. Generation, conflict detection and the public surface are unchanged — only the wording of the
  message a failing declaration carries.
- **A `DateTimeOffset` pool holding two clocks for one instant now reaches one verdict, however it is written.**
  London opens 08:00 GMT and Frankfurt 09:00 CET — the same instant on two venue clocks. A pool holding both was
  collapsed to whichever spelling came first in the array, and the declared offset then judged that arbitrary
  survivor. So `OneOf(venues).WithOffset(+01:00)` threw *"no pooled value carries an offset it admits"* with
  Frankfurt's own value in the list, while re-sorting the array made the same code work; declaring the offset
  before the pool gave a third answer, and the rejection count differed between the two. The supplied values are
  now held whole until both dimensions are known, and the offset is carried into the interval engine as an
  exclusion — so the two declaration orders agree, the rejection names the offset alongside every other constraint
  refusing the same instant, and re-ordering a catalogue can no longer turn a satisfiable pool into a conflict.
  This restores what [ADR-0030](../doc/handwritten/for-maintainers/adr/0030-filter-the-datetimeoffset-pool-by-the-declared-offset.md)
  recorded as a consequence. **A seeded run drawing from such a pool may now yield a different spelling of the same
  instant** — the draw sequence is not a versioned contract below 1.0, and the value's instant is unchanged.
- **A distinct collection no longer refuses a count its own comparer makes reachable.** `DateTimeOffset` equality
  compares the instant and ignores the offset, so one instant drawn across a declared offset range is a single
  value under the default comparer and hundreds under one built on `EqualsExact` — but the cardinality gate,
  measured in instants, refused the wider count outright:
  `ListOf(Any.DateTimeOffset().Between(t, t).WithOffsetBetween(-2h, +2h)).Distinct(bySpelling).WithCount(3)` was
  rejected at declaration as exceeding *"the 1 distinct value(s) the element generator can produce"*, while 208
  distinct spellings were drawable. A generator whose bound a finer comparer can exceed now answers for the
  comparer actually in force, and the collection re-asks when one is declared instead of trusting a bound taken
  before it existed. **No behaviour changes under the default comparer** — the same specification is still
  refused there, and correctly, because the three spellings really are one value by that equality.
- **JD023 and JD024 no longer depend on how you spell an unsigned literal.** Both rules declare `UInt16`,
  `UInt32` and `UInt64` in scope, and their constant reader handled none of them — so
  `Any.UInt32().GreaterThan(5).LessThan(3)` was reported while `GreaterThan(5u).LessThan(3u)`, the same
  unsatisfiable chain, was not: without a suffix the literal reaches the rule as an `int` and is judged, with one
  it fails to read and the whole chain is abandoned. The verdict turned on a keystroke rather than on any
  documented boundary. The three types are now read. One real limit remains, and both pages state it: the rules
  reason in `long`, so a `UInt64` bound above `long.MaxValue` is left unjudged rather than truncated into a bound
  meaning something else. **Expect new diagnostics on unsigned chains you already have** — both rules were always
  meant to report them, and neither is an error.
- **Four analyzer rules work again on a seeded chain written in one expression.** JD015, JD023 and JD024 — and
  JD029, which is new — read a chain by walking back to the factory that started it, and the walk descended into
  a call's receiver before asking whether the call was itself the factory. On `Any.WithSeed(1).Int32()...`,
  `Int32()`'s receiver is an invocation, so `WithSeed` was named as the factory and every rule gated on that
  name fell silent — on precisely the form this library recommends for reproducibility. The same chain routed
  through a local variable was analysed correctly, which is what kept it hidden. **Expect new diagnostics on
  seeded chains you already have**: they are ones these rules always meant to report, and none of them is an
  error. Generation and the public surface are unchanged.

## [1.0.0-preview.1] - 2026-08-07

**Why the jump from `0.1.0-preview.1`.** Not because the surface grew — it did not change at all
between the two, and `PublicAPI.Unshipped.txt` is still where it is declared. Because the number was
understating the intent. `0.1.0` reads as an early sketch inviting nobody; this library has been in
use inside another repository for its whole life, and what the preview is waiting for is an outside
consumer, not more design. A `1.0.0-preview` says what is actually true: this is the surface offered
for 1.0, and the preview exists so it can be contradicted before it freezes.

A preview still promises nothing about the surface. What it does now promise is the seed.

### Added

- **A seed replays across patch and minor versions.** Within a major version, a given seed draws the
  same values; the mapping may change on a major
  ([ADR-0049](../doc/handwritten/for-maintainers/adr/0049-replay-a-seed-across-patch-and-minor-versions.md)).
  This matters because a pinned seed is usually committed: without the promise an upgrade would not
  break such a test, it would leave it green while it quietly stopped covering the case it was pinned
  for. The promise is enforced rather than stated — a golden master pins, for each factory at a fixed
  seed, both the values produced and the number of draws consumed, the latter because a single
  sequential stream is shared by the whole scope, so a generator that changes how much it consumes
  shifts every value drawn after it.

### Changed

- The package carries an icon, shared by every package this repository publishes.
- The packaged readme's links point at this repository rather than the one JustDummies was extracted
  from.

## [0.1.0-preview.1] - 2026-07-31

First published version. The library itself is not new — it was developed inside
[`Reefact/first-class-errors`](https://github.com/Reefact/first-class-errors) and
[extracted into this repository](../doc/handwritten/for-maintainers/adr/0044-extract-justdummies-into-its-own-repository.md)
with its full history on 2026-07-31. This is the first time it reaches nuget.org.

**A preview on purpose.** The public surface is large and has never been exercised by an outside
consumer. It is declared in `PublicAPI/<tfm>/PublicAPI.Unshipped.txt` rather than
`PublicAPI.Shipped.txt`, which is the honest state: nothing here is promised yet, and a stable
release is what will freeze it.

### Added

- **The `Any` generator surface** — a fluent DSL producing arbitrary yet valid test values.
  Constraints express the invariants a value must satisfy, never what the test asserts. Scalars,
  strings, collections, dictionaries, sets, enums, GUIDs, temporal types and URIs, plus composition
  through `As`, `Combine` and `OrNull`.
- **Fail-fast conflict detection.** Contradictory constraints are refused at declaration with a
  message naming both sides, rather than looping or silently drawing a value that satisfies neither.
- **Reproducibility.** `Any.Reproducibly` pins a seed for the run and reports it when the body
  throws, so a red test says how to replay itself; `Any.ReproduciblyAsync` covers `async` bodies,
  and `Any.UseSeed` opens an explicit scope.
- **28 first-party analyzers** (`JD001`–`JD028`), bundled in this package under
  `analyzers/dotnet/cs`. They guard the recipe-versus-value boundary where the type system cannot
  reach — a generator rendered as text, a discarded result, a draw outside the pinned scope,
  constraints that admit no value.
- **Two target frameworks.** `netstandard2.0` for the widest reach, and `net8.0` which additionally
  carries the generators for types that do not exist downlevel: `DateOnly`, `TimeOnly`, `Int128`,
  `UInt128` and `Half`. The supported .NET Framework floor is 4.7.2, and CI runs the suites on it.
- **Package hardening**: embedded SPDX SBOM, SourceLink, symbol package, deterministic build, and a
  build-provenance attestation on the release artifact.

### Notes

Commit messages older than 2026-07-31 cite issue and pull-request numbers of
`Reefact/first-class-errors`, and ADR numbers this repository has since renumbered. The mapping is
in [ADR-0045](../doc/handwritten/for-maintainers/adr/0045-renumber-the-decision-base.md); the full
migration record is under
[`doc/handwritten/for-maintainers/migration/`](../doc/handwritten/for-maintainers/migration/).

[Unreleased]: https://github.com/Reefact/just-dummies/compare/lib-v1.0.0-preview.2...HEAD
[1.0.0-preview.2]: https://github.com/Reefact/just-dummies/compare/lib-v1.0.0-preview.1...lib-v1.0.0-preview.2
[1.0.0-preview.1]: https://github.com/Reefact/just-dummies/compare/lib-v0.1.0-preview.1...lib-v1.0.0-preview.1
[0.1.0-preview.1]: https://github.com/Reefact/just-dummies/releases/tag/lib-v0.1.0-preview.1
