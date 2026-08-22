# Changelog

All notable, user-facing changes to **`dum`** (the `JustDummies.Cli` package) are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
Releases are cut from the `cli` train (see [CONTRIBUTING.md](../CONTRIBUTING.md)).

## [Unreleased]

### Fixed

- **A guard the tool cannot place above every write to its parameter is no longer read as a bound on
  the drawn value.** `if (percent < 0) { throw … } percent = 100 - percent; if (percent < 0) { throw … }`
  read `.GreaterThanOrEqualTo(0)` over a real domain of 0 to 100 — the one shape where the tool was
  confidently wrong rather than blind, since it saw the guard, parsed it correctly, and attributed it
  to a value the constructor had already replaced. A draw of a million then threw inside that
  constructor, under a recap reporting the parameter inferred and nothing worth looking at. Only an
  assignment to a field or a property ended the guard scan; a write to the parameter itself now ends
  the reading of **that** parameter, and of no other, so the guards of the constructor's remaining
  parameters are untouched. Guards written *above* the write still stand, because they are true of
  the value the generator draws.

  Which writes count is asked of the compiler's own data-flow analysis rather than of a list of
  spellings, so `=`, `+=`, `++` and `--` are covered along with the ones a list misses: a
  deconstruction (`(percent, rate) = (100 - percent, rate)`), an `out` argument
  (`int.TryParse(text, out percent)`) and a `ref` local aliasing the parameter. Where they sit is a
  question about execution rather than about statements — a write and a guard share one statement in
  `else { percent = 100 - percent; ThrowIfNegative(percent); }`, which was measured emitting
  `.Between(0, 50)` for a domain whose real answer is 50 and above.

  The tool reads one order — statements run as written, and reaching either branch of an `if` means
  its condition ran first — and asks about every other construct **entire**. A loop runs its body
  again, a `finally` runs after a `try` that wrote, a `switch` evaluates its governing expression
  before the section it picked, a `using` its resource before the body it scopes; all four were
  measured reading a guard as a bound on a value the constructor had already replaced. A `: this(…)` or `: base(…)` counts too, running
  entire before the body's first statement — `: this(Normalize(ref value))` had already replaced the
  drawn value. Two writes are refused wherever they are written: one inside a local function or a
  lambda, which runs when called rather than where it is declared, and any write in a body carrying a
  `goto`. The cost of asking
  entire is precision — a guard inside a `try`, a `switch` or a `using` whose construct writes the
  parameter only *after* it is refused although it was readable — and that trade is deliberate.

  **This narrows what compiles:** such a parameter is now marked `unread guards`, so its scaffold
  blocks compilation until its author confirms the generator, where before it compiled over a
  constraint that was wrong.
- **A sign guard on an unsigned parameter no longer loses its constraint.** `if (size <= 0) { throw … }`
  on a `byte`, `ushort`, `uint`, `ulong` or `UInt128` was read as `.Positive()` — a member the unsigned
  generators do not carry, so the lookup dropped it and the parameter kept an unnarrowed draw under a
  file that compiled and reported nothing worth looking at. `Any.Byte()` then drew `0`, the one value
  the guard exists to refuse, on roughly one draw in two hundred. It is now `.NonZero()`, which is the
  same constraint rather than a looser one: zero is the floor of an unsigned type, so *above zero* and
  *not zero* admit exactly the same values. `if (size >= 0)` gets the opposite treatment — it rejects
  every value an unsigned type can hold, so nothing is written and the parameter is marked
  `unread guards`. Both spellings are affected, the `if` and the `ArgumentOutOfRangeException` helper.
- **The arithmetic throw helpers are read as guards, not passed over as unread.**
  `ArgumentOutOfRangeException.ThrowIfNegative(quantity)` — the commonest guard on a quantity there
  is — used to count as a call the tool could not parse and block the developer's build, over an
  invariant that was perfectly readable. `ThrowIfNegative`, `ThrowIfNegativeOrZero`, `ThrowIfZero`,
  `ThrowIfLessThan`, `ThrowIfGreaterThan`, `ThrowIfLessThanOrEqual` and `ThrowIfGreaterThanOrEqual`
  now map to the same numeric rows a comparison already builds: `ThrowIfNegative(value)` throws on
  `value < 0`, so zero is admissible — `GreaterThanOrEqualTo(0)`, never `Positive()`, which is what
  `ThrowIfNegativeOrZero(value)` reads as instead. The same subject-identity and compile-time-constant
  discipline the comparison rows already keep applies here too.
- **A guard followed by an `else`, or an `else if` chain that throws throughout, is read rather than
  passed over.** `if (v < 0) { throw … } else { … }` used to stop guard reading the moment it saw the
  `else`, even though an `else` branch only says what happens when the guard's own condition is
  false and can never weaken what it rejects. An `else if` chain now reads one branch at a time, for
  as long as every branch before it throws unconditionally too: `if (a < 0) { throw … } else if
  (b > 100) { throw … }` now reads both — reaching `b`'s test presupposes only that `a`'s branch
  already rejected the value. The moment a branch does not throw unconditionally, reading stops
  there and that branch, with everything after it, is marked `unread guards` instead: reaching it
  would otherwise presuppose a fact about an earlier parameter, which is the cross-parameter rule
  this reading has always refused.
- **An enum exclusion guard is read as `AnyEnum<T>.DifferentFrom`.** `if (status == Status.None) {
  throw … }` — the commonest enum guard there is — used to read as `.NonZero()`, a member
  `AnyEnum<T>` does not carry: Roslyn reports a zero-valued enum member as a plain integer
  constant, so the condition fell into the numeric family's `p == 0` row and the member lookup
  dropped it silently. A non-zero excluded member matched no numeric row at all and read as an
  unguarded parameter. Both now read as `.DifferentFrom(Status.None)`, with the same
  subject-identity discipline `Enum.IsDefined` already keeps: the excluded member has to belong to
  the parameter's own enum type. The negation, `p != E.Member`, is a different invariant — a pin
  rather than an exclusion — and stays out of scope.

## [1.1.0-beta.2] - 2026-08-22

### Fixed

- **A guard is read only where the parameter is its subject.** `if (Math.Abs(degrees) > 90)` became
  `Any.Int32().LessThanOrEqualTo(90)` — a generator every draw of which that guard rejects, reported as
  `guard` rather than as unread. Two siblings did the same: a bound on a member of a non-numeric parameter
  (`duration.TotalMinutes < 5` on a `TimeSpan`) emitted an argument the constraint could not bind, failing
  the developer's build with `CS1503`; and a `Length` read off something *derived* from the parameter
  (`value.Split(',').Length`, `value[0].Length`) was taken for the parameter's own size, so an element's
  length became a collection's count. All three now leave the parameter with its neutral generator and mark
  it `unread guards`, which is what §9 always said an arithmetic condition gets.
- **An odd parameter name emits a file that compiles.** `@event` reached the emitted file as `event` —
  Roslyn drops the escape — so the file did not parse at all, under a recap claiming every parameter
  inferred. `_id` failed the other way: the field derived from it carried the same identifier as the
  constructor parameter, so the assignment was `_id = _id`, which compiles and leaves the field null, making
  every draw throw. Both names are ones §17 already promised worked.
- **A size the generator could not produce is no longer written down.** Two shapes reached the developer as a
  generator that throws the moment it is constructed. A bound above the library's producible cap —
  `if (body.Length > 1_048_576)`, an ordinary 1 MiB limit — was emitted verbatim and threw inside the emitted
  parameterless constructor, where no `With…` call can rescue it. And a count floor on a `ISet<T>` or a
  dictionary was written without asking the element row how many *distinct* values it can draw, so five over a
  three-member enum threw for exactly the reason `JD016` reports it. Both are now refused at reading time and
  the parameter is marked `unread guards`. A **ceiling** is unaffected by the second: it asks the generator not
  to exceed a size, never to produce one.
- **The recap no longer claims a guard it did not honour.** `if (status == Status.None) throw …` — the
  commonest enum guard there is — was read, understood, and then dropped because `Any.Enum<T>()` carries no
  member to say it with. That drop is right; reporting it as `guard` was not. The column now reads
  `constraint unavailable`, a new value distinct from `unavailable`: there the *generator* for the type is
  missing, here the generator is exactly right and one constraint cannot be expressed on it. `guard` itself is
  now computed from the constraints **applied** rather than the constraints read, which is what §6's word
  *tightened* meant all along. The JSON report carries the value like any other.
- **A bounded parameter is scaffolded as the range it is, once.** `dum generate Order` on a factory guarding
  `IsNullOrWhiteSpace`, `Length < 8` and `Length > 20` emitted
  `Any.String().NonEmpty().WithMinLength(8).WithMaxLength(20)` — a chain the tool's own package reports
  (`JD031`), so the scaffolded file was marked on its first run, before its author had touched it, and one whose
  `NonEmpty()` narrowed nothing beside a floor of eight. It is now
  `Any.String().WithLengthBetween(8, 20)`. `WithCountBetween` and `Between` fold the same way.
- **Constraints that cannot stand together are reconciled properly, or refused.** Composition read two of the
  six things a constraint can pin down, so three shapes escaped it and reached the developer as a generator that
  throws the moment it is constructed: an exact size beside a bound excluding it (`WithCount(2).WithMinCount(5)`),
  a sign against an opposing bound (`Positive().LessThanOrEqualTo(-5)`), and — the engine's own doing — the
  base table's `NonEmpty()` against a guard demanding a blank string. Two guards bounding the *same* side are no
  longer dropped either: they are a conjunction, and the tighter one is what they both mean, so an invariant the
  engine had read correctly is no longer thrown away. Where nothing survives, the parameter keeps its neutral
  generator and the recap says `guards not combined`, as before.
- **A guard constant the engine cannot carry no longer ends the run.** `if (value > 1e30)` on a `double`
  overflowed the conversion to `decimal` and the exception escaped `Scaffolder.Scaffold`: the types before it
  were on disk, the rest absent, `--format json` printed nothing, and the exit code said the command line had
  not been understood. NaN, the infinities and anything past `decimal` are now simply not read.
- **A size bound that is not an `int` no longer breaks the developer's build.** `if (text.Length > Budget / 2.0)`
  emitted `WithMaxLength(140.5)` — `CS1503`, from a scaffold reporting success. Every size member takes an
  `int` (§14.3), so a bound that does not render as one leaves the parameter neutral and `unread guards`.
- **An `Enum.IsDefined` guard is read only over the parameter's own type.** `!Enum.IsDefined(typeof(OrderStatus), statusCode)`
  on an `int` was accepted and added nothing, so the parameter drew the whole `int` range against two
  admissible values — with an empty provenance column, indistinguishable from a type that had no guard at all.
  The single-argument generic overload is read too, where the universe comes from the value itself.
- **A cross-parameter guard now marks the parameters it spans.** `Range(int min, int max)` guarding `min > max`
  was dropped in silence — the one rejection path in the reading that marked nothing, while the `&&` case
  excluded by the same rule marked correctly. Measured on that shape: 5008 throws in 10 000 draws, under a
  recap reading `2 of 2 parameters inferred`. A guard mentioning no parameter at all still marks nothing.
- **A type the emitted file could not construct is now refused, not scaffolded.** An **abstract** type
  (`CS0144`), a **generic** one or one nested in a generic (`CS0246`), and a type whose chosen constructor
  leaves a `required` member unset (`CS9035`) each declared a public constructor, so a file was written and
  the run exited `0`. Each is now a refusal naming the remedy. Required members stay deferred (§16) — this
  makes deferring them audible. **The exit codes do not move**: every one of these is §7's existing `1`.
- **A guard delegated to a helper is no longer read as no guard at all.** `Ensure.NotBlank(name);` — a call by
  itself, with no `if` in the constructor for §5.3 to parse — used to pass over in silence: the parameter read
  with an empty provenance column, indistinguishable from one with no guard on it, and the neutral generator
  it kept could draw a value the helper rejects on every real construction. A leading statement that hands a
  parameter to a call **made for its effect alone** is now marked `unread guards`, the same word already used
  for a condition it fails to recognise. `nameof(...)` inside a throw's own message is exempted: it names the
  rejected parameter rather than calling anything. A call whose result is *used* is production, not a guard —
  `_name = value.Trim();` and `_tags = tags.ToList();` say nothing about which values are admissible, and
  reading them as doubt would block the compilation of constructors carrying no guard at all. The cost of
  drawing the line there is named in §9: a guard helper that returns the value it checked
  (`_name = Ensure.NotBlank(value);`) reads as production and is missed.
- **A throwing guard the tool cannot parse is no longer read as no guard at all.** An `else if` chain, a block
  that logs before it throws, a condition outside the recognised set — each fell past guard reading entirely
  and, carrying no call naming the parameter, past the helper rule too, so `if (v < 0) { throw … } else if
  (v > 100) { throw … }` reported nothing whatsoever. A `throw` before the first assignment to state cannot be
  ordinary logic — it refuses to build the object — so the parameters such a statement names are now marked
  `unread guards`. A parameter named only inside the `nameof` of the throw's own message does not count.
- **A guard the tool already knows is read in either spelling.** `ArgumentNullException.ThrowIfNull(value)` and
  `if (value is null) { throw … }` state one invariant, and so do
  `ArgumentException.ThrowIfNullOrEmpty` / `ThrowIfNullOrWhiteSpace` and the `string.IsNullOr…` conditions.
  Only the older spelling was read, so the modern one counted as a guard the tool could not read and blocked
  the build — over a chain that was already exactly right, since a null check adds nothing and an emptiness
  check is the string row's own `NonEmpty()`. Both spellings now produce the same expression, the same
  provenance, and a file that compiles. The arithmetic helpers (`ArgumentOutOfRangeException.ThrowIfNegative`
  and its siblings) are deliberately not read: that would widen the recognised set rather than admit a second
  spelling of what is in it.

### Changed

- **Each parameter's recipe now lives in its own factory.** The public constructor's initializer used to build
  every chain inline; it now names one `private static` factory per parameter — `CustomerFactory()` beside
  `ReferenceFactory()` — and calls it. The constructor reads as a list of names, and whatever a parameter has
  to say for itself is said inside the method that owns it rather than crowding the call site. An unresolved
  parameter's TODO moves with it: the non-existent identifier is now the factory's `return`, not the
  constructor argument. Every emitted file's shape changes; `Generate()`, the fields and the `With…` methods
  do not.
- **A guard the engine cannot vouch for now blocks compilation, the same as one it cannot infer a generator
  for at all.** A parameter marked `unread guards` used to keep its neutral generator and say nothing more —
  compiling, looking finished, and drawing a value the real constructor could reject on some later run,
  indistinguishable from a flaky test to whoever hits it. Its factory now carries the same non-existent-
  identifier mechanism ADR-0060 already uses for an unresolved parameter, with one difference: the generator
  dum inferred stays as the working base underneath the line that blocks it, for the developer to verify or
  replace rather than write from nothing. The recap counts this separately — `1 TODO, 1 to verify` — since a
  generator *was* inferred here. Reaches every existing `unread guards` case, including a size past the
  library's producible cap and a count past what an element row can draw, not only the helper case above.
- **The recap no longer says two things about one parameter.** A parameter to verify read `TODO` in its own
  row while the closing line counted it under `1 to verify` and *not* under TODO — the table and the footer
  disagreeing about the same parameter. The row now reads `to verify` too.
- **`--format json` counts a parameter to verify apart from an open one.** `summary.openParameters` had been
  widened to include it, which made that number disagree with the rows: a parameter to verify carries an
  expression, so its row reads `resolved: true`, and a script summing those rows and a script reading the
  count answered differently about one document. `openParameters` keeps its published meaning, the new
  `summary.parametersToVerify` carries the other count, and each row states both facts — `resolved` and the
  new `requiresVerification` — so the summary is checkable against the rows.

## [1.1.0-beta.1] - 2026-08-13

A minor release, and additive throughout: three new options, and not one existing behaviour changed.
`dum generate Order` writes exactly what it wrote in 1.0.0-beta.1, byte for byte, and the exit codes of
§7 keep the meanings they were published with.

Still a **beta**, for the reason 1.0.0-beta.1 gave: what a version commits to here is the command line
rather than a set of types, and that surface has still not been exercised by a project outside this
repository.

### Added

- **`--entry-point`** — a scaffold can now also write an entry point, so a generator is reached the way the
  library's own are. `--entry-point any` emits a C# 14 extension member and gives you `Any.Order()` beside
  `Any.Int32()`; `--entry-point static:<Name>` emits a `partial` root you own and gives you `Dummies.Order()`,
  with no language requirement at all. The default is `none`, and `new AnyOrder()` is unaffected
  ([ADR-0070](../doc/handwritten/for-maintainers/adr/0070-emit-an-entry-point-on-request-as-a-file-of-its-own.md)).
- **`--entry-point-namespace`** — declares the entry-point file somewhere other than beside the generator, so one
  root can gather types from several namespaces. It moves that file and nothing else: the generator stays in the
  namespace `--namespace` (or the target type) gives it, so no call site pays an import for it.
- **`--format json`** — a run reports itself as one JSON document on stdout instead of the recap, for the caller
  that is a script rather than a reader. It carries what the exit code cannot: `summary.openParameters`, and a row
  per parameter with its expression and provenance. §7 makes a file written with open parameters a success — right
  for a developer, and indistinguishable from a clean run for a script scaffolding forty types at once. **The exit
  codes do not move**: this adds a channel rather than redefining one
  ([ADR-0071](../doc/handwritten/for-maintainers/adr/0071-report-a-run-as-data-without-moving-the-exit-codes.md)).
  Under `json`, stdout carries the document alone and everything written for a person stays on stderr, so
  `2>/dev/null` leaves a clean pipe; `--dry-run` puts each file's text inside the document; and a run that stopped
  before its first scaffold still produces one, naming the refusal.
- **`dum.json`** — an optional file beside the project supplies defaults for the options that describe the project
  rather than the invocation: `output`, `namespace`, `entryPoint`, `entryPointNamespace`, `format`. **The command
  line always wins** over any of them, and it wins by simply already being there — the file fills blanks and
  overwrites nothing. A relative `output` is rooted at the project's directory, so it means the same thing wherever
  the tool is run from
  ([ADR-0072](../doc/handwritten/for-maintainers/adr/0072-read-project-defaults-from-a-file-the-command-line-overrides.md)).

### Changed

- The emitted generator file is **unchanged**, byte for byte, whichever entry point is asked for. The C# 7.3 floor
  of the scaffolded code is a property of that file; only the `--entry-point any` file needs anything newer.
- Where a scaffold writes two files, it writes both or neither: an existing `Any{Type}.Entry.cs` refuses the whole
  scaffold rather than leaving the generator behind it, and `--force` covers both.
- The console recap closes with a second line naming the call the entry point opened —
  `✓ AnyOrder.Entry.cs — entry point Dummies.Order()`.

### Fixed

- **`--namespace ""` and its four siblings no longer advise "omit it to take its default"**, which stopped being
  true the day a `dum.json` could set the same option: omitting it takes that file's value where there is one. The
  refusal now points at the file.
- **A parameter type outside any namespace no longer emits `using <global namespace>;`**, which does not parse.
  Two cases reached it: a domain type declared outside any namespace, and — the likelier one — an *error* type,
  since a parameter that failed to bind is reported as living in the global namespace. A project that opened with
  an unresolved reference therefore scaffolded a file broken on its fifth line, for every parameter it could not
  resolve.

### Refused, on purpose

- `--entry-point static:Any` — a static class named `Any` in your own project hides `JustDummies.Any` for its whole
  namespace rather than extending it, and `Any.Int32()` stops compiling. The refusal points at `--entry-point any`,
  which is the mechanism that actually reaches that spelling.
- `--entry-point any` on a project below C# 14 — refused, naming the language version the project resolved, rather
  than silently downgraded to a static root a developer would only discover at the call site.
- `--format` given a value that is neither `human` nor `json` — refused at the command line, exit `2`, naming both.
- A `dum.json` key that is not read — refused, exit `2`, naming the key and listing the ones that are. A default
  someone believes is in force and is not is worse than no file at all, so §16's reserved `naming` key is refused
  too, until `--name` and `--pattern` exist to give it a meaning.

## [1.0.0-beta.1] - 2026-08-10

First published version: **`dum` had never reached nuget.org before this one.** It starts at the number
of the specification it implements rather than at `0.1.0`, because it implements that specification
entire — not an earlier sketch of it.

**A beta, not a preview**, and the difference is deliberate. `JustDummies` and `JustDummies.Xunit` say
`preview` to mean one precise thing: their surface is declared in `PublicAPI.Unshipped.txt`, so no API is
promised before 1.0. A tool takes no public-API baseline at all — it carries no compatibility promise, and
its public surface is the command line rather than a set of types
([specification §13.4](../doc/handwritten/for-maintainers/specifications/justdummies-tool.md)). `beta`
states what is true of *that* surface: complete against the specification, and not yet run against anyone
else's project.

### Added

- **`dum generate <Type>`** — writes the dummy generator for a type, once, as ordinary code the developer
  then owns. Not a source generator and not a build-time step: it reads the compilation, emits a file, and
  gets out of the way.
- **Resolution.** A constructor parameter becomes a generator through the base table, then the
  constructor's own guard clauses (`quantity <= 0` → `.Positive()`), then composition through a static
  factory or an already-scaffolded `Any{Type}`. Every candidate member is looked up in the developer's
  compilation before it is kept.
- **An open parameter is left open, loudly.** What could not be inferred is emitted as an identifier that
  does not exist, so the developer's own build reports it at the line, with the type in hand
  ([ADR-0060](../doc/handwritten/for-maintainers/adr/0060-seed-generators-from-constructor-guards.md)).
- **A console recap** that says where each expression came from — base table, guard, factory, a reused
  generator, or nothing — so "inferred, and here is why" is distinguishable from "gave up".
- **`--project`, `--output`, `--namespace`, `--force`, `--dry-run`**, and nothing else. Several types are
  processed independently; the exit code is the worst of them.
- **Package hardening**: embedded SPDX SBOM, SourceLink, symbol package, deterministic build, and a
  build-provenance attestation on the release artifact.

### Requires

The `JustDummies` package in the project being analyzed — without it nothing can be resolved, and the tool
says so rather than emitting anything.

**No dependency on `JustDummies` is declared, in either direction of the version graph.** The tool resolves
every library symbol by metadata name against the developer's compilation, exactly as the analyzers do
([ADR-0063](../doc/handwritten/for-maintainers/adr/0063-give-the-scaffolder-no-dependency-on-the-package.md)),
which is what makes version skew between tool and library impossible. `tools/packaging/pack.sh` asserts it
on the produced package — both that the nuspec declares no such dependency and that no `JustDummies.dll` is
bundled beside the tool, since a .NET tool ships its closure as files and the first check alone would pass
on an empty dependency list.

[Unreleased]: https://github.com/Reefact/just-dummies/compare/cli-v1.1.0-beta.1...HEAD
[1.1.0-beta.1]: https://github.com/Reefact/just-dummies/releases/tag/cli-v1.1.0-beta.1
[1.0.0-beta.1]: https://github.com/Reefact/just-dummies/releases/tag/cli-v1.0.0-beta.1
