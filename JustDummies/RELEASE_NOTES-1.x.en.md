# Release notes — JustDummies, 1.x

What changed for you, release by release, in the `lib` train. For the full technical record — every constraint, every edge case, every ADR — see [CHANGELOG.md](https://github.com/Reefact/just-dummies/blob/main/JustDummies/CHANGELOG.md). Earlier: [0.x](https://github.com/Reefact/just-dummies/blob/main/JustDummies/RELEASE_NOTES-0.x.en.md).

## 1.0.0-rc.1 — November 16, 2026

_The release candidate for 1.0: new ways to declare what a value must satisfy, a floating-point draw that now reaches its type's limits, code fixes for the analyzers, and messages that name both sides of a conflict._

### ⚠️ Breaking changes

- **A seed now draws differently for three kinds of chain.** A floating-point range bounded on both sides is drawn whole, where every interval used to be narrowed to ±1 000 000; an open floating-point draw reaches the type's limits one draw in ten; and `OrNull()` yields `null` one draw in ten instead of one in two. A test that now fails on a limit value has either found code that does care about the magnitude, or was asserting on a value that was never a dummy ([ADR-0117](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0117-draw-a-limit-value-the-type-permits-one-draw-in-ten.md)).
- **`Any.UseSeed` returns a `SeedScope`**, whose `HasDrawn` says whether the seed served a draw. Source written against the `IDisposable` return still compiles; code compiled against a preview has to be recompiled ([ADR-0134](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0134-tell-the-opener-of-a-seed-scope-whether-it-was-drawn-from.md)).
- **`Any.DateTime()` refuses a `Local` bound**, naming the two ways out: `ToUniversalTime()`, or `Any.DateTimeOffset()` when the instant is what matters. A chain that passed `DateTime.Now` or `DateTime.Today` as a bound now throws ([ADR-0114](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0114-refuse-a-local-datetime-bound.md)).
- **No rule recognises a negative test any more.** A chain inside a lambda, such as `Check.ThatCode(() => Any.Int32().Between(10, 5))`, is now reported like any other code. A test whose subject is the refusal says so with a justified `[SuppressMessage]`; under warnings-as-errors, a build holding such tests turns red on upgrade until they do ([ADR-0112](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0112-report-a-test-asserting-a-refusal-like-any-other-code.md)).
- **Several refusal messages now read differently**: a date or a time prints in the round-trip `"O"` format, and a second `OneOf` ends with `; to narrow the first set, use Except(...)`. A script matching the old text needs the new wording.

### ✨ New

- **`Any.Pair(item1, item2)` draws two values of one ordered type**, each from a base of its own, and `Having` declares the order between them — `Having.Item1LessThanItem2()`, `Having.Item1BeforeItem2()` and their siblings. The order holds by construction, never by redraw ([ADR-0131](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0131-draw-a-pair-from-two-bases-and-an-optional-order.md)).
- **`.Then(...)` draws a value that depends on another draw**, such as a grid's cells inside its drawn dimensions, and the whole chain replays from one seed ([ADR-0119](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0119-draw-a-value-that-depends-on-another-draw.md)).
- **`Any.Fixed(value)` hands a value you chose to anything that takes a generator**: an operand of `Any.Combine` the test is about, a parameter pinned to its absent case. It consumes no draw, so the values around it replay unchanged ([ADR-0118](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0118-return-a-literal-through-any-fixed.md)).
- **Any `IAny<T>` takes `DifferentFrom(value)` and an `Except` over a set of values**, so a generator built by `As` or `Combine`, scaffolded, or your own can exclude values too, by a bounded redraw that fails with its seed ([ADR-0133](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0133-exclude-a-set-of-values-from-any-generator-by-redrawing.md)).
- **`Any.TimeOnly().Between(start, end)` crosses midnight when the start is after the end**: `Between(22:00, 06:00)` is the night, drawn uniformly over its two segments ([ADR-0125](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0125-let-a-time-of-day-window-cross-midnight.md)).
- **`Ordered()` and `OrderedBy(key)` arrange a list or an array in ascending order**, keeping the count, the contained values and distinctness.
- **`Any.PermutationOf(items)` draws a random order of exactly the items given**, duplicates included.
- **`DistinctBy(key)` on lists, arrays and sequences.**
- **`Any.Decimal().MultipleOf(step)` draws on a step's grid**, such as a tick size of `0.25`.
- **`Odd()` and `Even()` on every integer builder.**
- **`WithoutChars(chars)` narrows a string's alphabet at the call site**, so a scaffolded `WithChars("ACGTN")` becomes `WithoutChars("N")` where a test wants no unknown base.
- **Every `OneOf` and every `Except` accept a sequence as well as a list of values**, so a set you already hold passes as it is: `Any.Int32().OneOf(allowedPorts)`.
- **`Any.StringMatching(...)` accepts a value set**: the pattern becomes the test each supplied value passes or fails.
- **Rules come with a code fix**, offered by an IDE or `dotnet format` and shipped beside the analyzers: JD001, JD002, JD004, JD005, JD010, JD011, JD013, JD014, JD024, JD025, JD027, JD031, JD032 and JD035. A rule gets a fix only where exactly one rewrite keeps what you meant ([ADR-0130](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0130-ship-code-fixes-beside-the-analyzers.md)).
- **Six new rules.** JD034 reports a once-per-generator constraint redeclared with a different call, JD035 a constraint repeated identically, JD036 a `double`, `float` or `decimal` dummy that declares no range, JD037 a `Generate()` inside a lambda passed to `As` or `Combine`, JD038 a chain whose constraints admit exactly one value, and JD039 a generator passed to a parameter of the type it draws.

### 🙌 Improvements

- **An `Except` given an empty list excludes nothing** instead of throwing, so a fixture's set of values already used passes straight through on its first use.
- **A refused regex construct says what to do instead**, with the way out for its own family, and says so when no equivalent rewrite exists.
- **A conflict names both of its sides.** An allow-list, a character family or a `Char` pool emptied by what was declared before it now names the constraint that emptied it, and JD023 names both sides of the conflict as the run time writes them.
- **`Any.StringMatching(...)` accepts the whole-string anchors `\A`, `\z` and `\Z` at the pattern's edges**, so a production validator written with them can be reused through `StringMatching(Regex)`.
- **The analyzers read more of what throws on every run**: JD023 and JD024 now read `decimal` and the temporal generators, JD014 reads more guards and every numeric overload, JD015 more string chains, JD016 a typed generator's own value set, and JD007 and JD009 a `Generate()` on any `IAny<T>`.

### 🐛 Bug Fixes

- **Several declarations that are satisfiable no longer fail at random**: a distinct collection over an `OrNull()` element, a distinct collection with a comparer and a pinned value, a `decimal` interval within a unit of the type's own domain, and an exclusion sitting on a `decimal` interval's own boundary. The `decimal` fix shifts the last digits of some same-sign draws under a pinned seed.
- **A floating-point interval only a few values wide reaches every value in it.** This changes what a seed draws within such a span, and nowhere else.
- **A subtraction now narrows a `WithChars` pool**, where `WithChars("abc123").WithoutNumeric()` still drew digits.
- **Re-declaring the same value set no longer conflicts with itself**, whatever the order or the duplicates it was written with.
- **An IPv6 host without its brackets is refused when it is pinned**, instead of a raw `UriFormatException` at every `Generate()`.
- **`Any.Reproducibly` no longer reports a seed for a body that drew nothing.**
- **Failure messages no longer print control characters**, nor quote a floating-point strict bound in the engine's neighbouring value rather than as you wrote it.
- **Several rules no longer report correct code**: JD007 on a type xUnit does not instantiate, JD007 to JD009 on a lambda that is provably kept rather than run, JD012 on a pool that `Then` flattens into values, JD016 on a pinned value outside the element domain, JD017 on an excluded value the enum does not define, and JD011 beside JD005 on the same concatenation.

## 1.0.0-preview.6 — September 2, 2026

_A license change every consumer should read, a nullable-widening generator, and a fairer draw for `Half`._

### ⚠️ Breaking changes

- **JustDummies is now licensed under [PolyForm Internal Use 1.0.0](https://github.com/Reefact/just-dummies/blob/main/LICENSE), not Apache 2.0 — source-available, not open source.** You may read, build, modify and run the library (and its bundled analyzers) for your own or your company's internal business operations; you may not distribute the software. Versions already published on NuGet are untouched and keep the license they shipped with. Contributions are now governed by a [Contributor Agreement](https://github.com/Reefact/just-dummies/blob/main/CONTRIBUTOR_AGREEMENT.md).

### ✨ New

- New `generator.AsNullable()` — widens a generator's type to nullable without ever drawing an absent value, the opposite of `.OrNull()`. It keeps the wrapped generator's known value count, so `Any.SetOf(Any.Enum<T>().AsNullable())` sizes correctly against the enum's members ([ADR-0094](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0094-lift-a-nullable-value-type-rather-than-deriving-it.md)).

### 🙌 Improvements

- **`Any.Half()` now draws uniformly over the values a half can actually represent**, instead of over the reals and rounding — which produced almost nothing below 1. A seeded test drawing a `Half` will replay a different value than before ([ADR-0091](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0091-draw-a-half-from-the-values-it-can-represent.md)).

### 🐛 Bug Fixes

- `Any.Half()` now states how many distinct values it holds, so `Any.SetOf(Any.Half())` beyond that count is refused instead of exhausting a redraw budget.
- `JD016` now proves several more small element domains exactly (`Char`, `Byte`/`SByte`, `Int16`/`UInt16`, `Half`, and an enum's distinct values), and counts a caller-supplied `Any.Char().OneOf(...)` pool exactly.
- `JD015` now weighs a value set against every declared constraint together, so a chain refused only by the intersection of several constraints is reported.
- An element generator that admits nothing now names its own emptying constraint, even inside a distinct collection like `Any.SetOf(...)`.

## 1.0.0-preview.5 — August 25, 2026

_A guard against blank strings, and four chain-ordering fixes so a specification reads the same regardless of the order it was written in._

### ✨ New

- New `NotBlank()` on `Any.String()` — requires at least one non-whitespace character, the guard `NonEmpty()` alone never covered since an entirely-whitespace string is not empty ([ADR-0088](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0088-state-the-whitespace-guard-with-a-member-of-its-own.md)).

### 🐛 Bug Fixes

- `JD015` now measures a string's length budget the way the generator actually lays it out, so a re-declared prefix or a `NotBlank()` filler position is no longer double-counted or missed.
- An `Any.Enum<T>()` pool emptied by `Except(...)` now honours `AllowingCombinations()` regardless of which call was written first.
- A flag combination named in `OneOf` is now accepted wherever it is written, with no need for `AllowingCombinations()` at all.
- A distinct collection (`Any.SetOf(...)` and similar) is now judged on the whole finished chain, so `Containing`, `ContainingAny` and a finer `Distinct(comparer)` are honoured regardless of where `WithCount` sits in the chain.
- `JD030` now counts every anchored literal (`StartingWith`, `EndingWith`, `Containing`) when it reports the interval a chain draws.

## 1.0.0-preview.4 — August 24, 2026

_A rename that reads better, and a size ceiling analyzer and library now agree on._

### ⚠️ Breaking changes

- `AnyChar` and `AnyString` rename `LowerCase()`/`UpperCase()` to `InLowerCase()`/`InUpperCase()` — the bare names read like a state change, not a quality of the drawn value. No behaviour changes; only the two names do.

### 🐛 Bug Fixes

- JD014 now reports a size ceiling above the producible cap: `WithMaxLength` and `WithMaxCount` were declared uncapped while the library caps them, so a call the analyzer blessed could be refused at run time with nothing between the two to say so.

## 1.0.0-preview.3 — August 21, 2026

_A fixed-prefix, restricted-alphabet format — `ORD-` followed by alphanumerics — is finally one chain instead of a workaround._

### ✨ New

- A character family, a subtraction or a casing now governs only the characters the generator draws — never a prefix, suffix or contained value you wrote. `Any.String().StartingWith("ORD-").AlphaNumeric()` no longer throws at declaration, and yields `ORD-` followed by alphanumerics only ([ADR-0079](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0079-constrain-what-a-dummy-draws-never-the-literals-it-was-given.md)). No chain that worked before stops working, and no generated value changes shape.
- New rule JD033 — names an anchored literal the declared characters cannot draw, at the call site, without refusing the chain. Information only, on by default.
- New rule JD031 — points a chain declaring both inclusive bounds separately (`WithMinLength(8).WithMaxLength(20)`) at the range form the same generator exposes (`WithLengthBetween(8, 20)`). Information only.
- New rule JD032 — warns when a bound is declared twice and the looser call silently loses, whichever order the two were written in.

### 🙌 Improvements

- JD015 now reports, as a warning, a value set every declared constraint empties — until now this surfaced only as two information-level JD029 notes.
- JD024 and JD015 narrow to stay in step with the changes above: JD024 no longer reports a bound JD032 now owns, and JD015 keeps only its length-budget check, so neither refuses at build time what the library now honours at run time.

## 1.0.0-preview.2 — August 18, 2026

_Two breaking defaults, both in service of the same idea: an unconstrained dummy should certify something, not just look harmless._

### ⚠️ Breaking changes

- `Any.String()` and `Any.Char()` now draw from the whole of ASCII, control characters included, and an unconstrained string spans 0 to 1024 characters — narrow it with `NonEmpty()`, `WithMaxLength(n)` or `Printable()` ([ADR-0075](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0075-draw-characters-from-the-whole-of-ascii.md)).
- A declared maximum — `WithMaxLength`, `WithLengthBetween`, `WithMaxCount` — now steers the draw instead of composing with the old narrow spread, and a maximum above 1,000,000 is refused ([ADR-0076](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0076-let-a-declared-maximum-steer-the-size-draw.md)).

### ✨ New

- Five new character families — `Punctuation()`, `Printable()`, `NonPrintable()`, `Whitespaces()` and `Hexadecimal()` — plus `WithoutAlpha()`/`WithoutNumeric()` to subtract rather than pin, on both `Any.String()` and `Any.Char()`.
- **`IPoolInspection<T>` reports what your own constraints left of a pool you supplied** — `GetSurvivors()`, `GetRejections()` — on every generator that takes a value set ([ADR-0067](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0067-report-a-filtered-pool-through-an-explicit-interface.md), [ADR-0068](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0068-carry-the-pool-inspection-wherever-a-caller-supplies-the-values.md)).
- JD029 flags, at build time, a value written into a pool that your own constraints can never draw.
- JD030 flags an `Any.String()` chain that settles no length, as information.

### 🙌 Improvements

- The packaged readme now documents how to draw a `NaN` on purpose, since `Any.Double()`, `Any.Single()` and `Any.Half()` refuse one as an argument too.
- JD015 now validates against the new character families, and every value the library prints is escaped against control characters.

### 🐛 Bug Fixes

- A decimal exclusion declared twice no longer empties a satisfiable grid.
- A conflict on `Any.Enum<T>()` or `Any.Guid()` now names the exclusion that caused it.
- A `DateTimeOffset` pool holding two clocks for the same instant now reaches one verdict regardless of declaration order.
- JD023 and JD024 now read `UInt16`/`UInt32`/`UInt64` constants regardless of the literal's suffix.
- JD015, JD023, JD024 and JD029 now recognise a seeded chain written in one expression.

## 1.0.0-preview.1 — August 7, 2026

_Not a bigger surface than 0.1.0 — the same one, offered to an outside consumer for the first time, with one new promise attached: your seed._

### ✨ New

- **A seed now replays across patch and minor versions.** Pin one in a test, and it keeps drawing the same values through every upgrade within `1.x` ([ADR-0049](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0049-replay-a-seed-across-patch-and-minor-versions.md)).

### 🙌 Improvements

- The package now carries an icon, shared across every package this repository publishes.
- The packaged readme's links point at this repository instead of the one JustDummies was extracted from.
