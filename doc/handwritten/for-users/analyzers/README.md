# JustDummies analyzers

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./README.fr.md)

The `JustDummies` package ships 33 Roslyn rules (`JD001`–`JD033`) inside itself, under
`analyzers/dotnet/cs`. Any project that references the package picks them up automatically,
with no extra install. They run while your project compiles, turning mistakes the run time
would otherwise report late — or never — into build-time diagnostics.

They exist because the type system cannot reach where these mistakes live: a generator is an
immutable *recipe* and a drawn value is not, yet both satisfy the same signatures; a seed
pinned outside its scope still compiles; a constraint set that admits no value is a perfectly
well-typed chain. Each rule closes one of those gaps.

Each rule has a stable id. Errors are hard defects; warnings flag likely mistakes; the info
rules are conventions, and two are opt-in (see each page for how to enable them).

## Reproducibility

These rules keep an asynchronous test body from silently swallowing its own failures.

| Rule | Severity | Default | Description |
|------|----------|---------|-------------|
| [JD001 AsyncBodyPassedToReproducibly](JD001.en.md) | 🔴 Error | on | An async lambda is passed to the synchronous Any.Reproducibly(Action); bound to an Action it becomes async void and its failures never fail the test. Use Any.ReproduciblyAsync and await it. |
| [JD002 DiscardedReproduciblyAsyncResult](JD002.en.md) | 🔴 Error | on | The task returned by Any.ReproduciblyAsync is discarded (a bare statement, or `_ =`); the body's failures are lost. Await it. |
| [JD003 AwaitableBodyPassedToReproducibly](JD003.en.md) | 🔴 Error | on | A synchronous lambda whose body drops a task, or an async void method group, reaches Any.Reproducibly; the scope returns before the assertions run, and CS4014 does not fire. |
| [JD004 DiscardedSeedingResult](JD004.en.md) | 🔴 Error | on | The handle returned by Any.UseSeed is discarded, leaving the seed pinned for whatever runs next — or Any.WithSeed is called for effect, which pins nothing at all. |
| [JD007 DrawOutsideThePinnedScope](JD007.en.md) | 🟠 Warning | on | A value is drawn during a [Reproducible] test class's construction, which xUnit runs before the seed scope opens; the reported seed does not replay it. |
| [JD008 ArbitraryValueInTheoryData](JD008.en.md) | 🟠 Warning | on | A theory's data provider draws a value at discovery, before any seed is pinned; every case shares the one value. |
| [JD009 DrawInStaticInitializer](JD009.en.md) | 🟠 Warning | on | A static initializer draws once for the whole suite, under whichever test ran first, making the tests order-dependent and replayable from no seed. |
| [JD010 ReproducibleOnNonTestMethod](JD010.en.md) | 🟠 Warning | on | [Reproducible] on a method xUnit never treats as a test; it pins nothing, and looks exactly like the working form. |
| [JD018 NestedReproducibilityScope](JD018.en.md) | 🟠 Warning | on | A reproducibility scope nested inside another; the inner one draws a fresh seed, so the outer's reported seed replays nothing. |
| [JD021 BlankReplaySnippet](JD021.en.md) | 🟠 Warning | on | Any.UseSeed is given a blank replay snippet, which the guard rejects — from an adapter hook, failing the whole suite. |
| [JD019 CommittedReplaySeed](JD019.en.md) | 🔵 Info | opt-in | A constant replay seed is pinned in committed code, so the test stops varying between runs. |
| [JD020 SharedStaticAnyContext](JD020.en.md) | 🔵 Info | on | An AnyContext held in a static field; interleaved draws make neither the sequence nor the multiset stable. |
| [JD022 ParallelDrawWithoutPerItemSeed](JD022.en.md) | 🔵 Info | on | A parallel work item draws without its own seed scope, so the draws interleave and the run replays nothing. |

## Usage

A generator is an immutable *recipe*, and `Generate()` is the only thing that materializes a value from it. These rules close the two ways that distinction is lost silently.

| Rule | Severity | Default | Description |
|------|----------|---------|-------------|
| [JD005 GeneratorRenderedAsText](JD005.en.md) | 🔴 Error | on | A generator is interpolated, concatenated or ToString()'d instead of generated from; no generator overrides ToString(), so the text is the builder's type name. |
| [JD006 DiscardedGeneratorResult](JD006.en.md) | 🟠 Warning | on | The generator returned by a constraint is discarded as a bare statement; generators are immutable, so the declared invariant is silently lost. |
| [JD011 GeneratorWhereValueExpected](JD011.en.md) | 🟠 Warning | opt-in | A generator reaches an object, dynamic or params object[] position, so the recipe is stored, compared or asserted on instead of the value. |
| [JD012 GeneratorPooledAsValue](JD012.en.md) | 🟠 Warning | on | Any.OneOf is given generators, inferring a pool of recipes; drawing from it yields a recipe rather than a value. |
| [JD013 HeldCollectionPassedToOneOf](JD013.en.md) | 🟠 Warning | on | A held collection passed to Any.OneOf binds T to the collection type, making a pool of one; Any.ElementOf draws from its elements. |

## Constraints

These rules front-load, to build time, the subset of the library's run-time constraint checks that is decidable from compile-time constants. The run-time checks stay: they cover every argument these cannot see.

| Rule | Severity | Default | Description |
|------|----------|---------|-------------|
| [JD014 RejectedConstantArgument](JD014.en.md) | 🟠 Warning | on | A constraint argument is a compile-time constant the generator's own guard refuses, so the call throws every time it runs. |
| [JD015 StringConstraintsAdmitNoValue](JD015.en.md) | 🟠 Warning | on | An AnyString chain's constant constraints admit no value — a declared length the shape cannot fit under, or character constraints that together admit none of a value set's values. |
| [JD016 CollectionConstraintsAdmitNoValue](JD016.en.md) | 🟠 Warning | on | A collection chain's count constraints cannot all hold, or it asks for more distinct elements than its element generator can produce. |
| [JD017 EnumUniverseViolation](JD017.en.md) | 🟠 Warning | on | An enum constraint names a value the type does not define — an undeclared numeric value, or an exclusion that empties the universe. |
| [JD023 ScalarChainAdmitsNoValue](JD023.en.md) | 🟠 Warning | on | An integer chain's constant constraints narrow the domain to nothing — bounds, lattice or allow-list. |
| [JD024 ConstraintWithNoEffect](JD024.en.md) | 🔵 Info | on | A constraint narrows nothing: an exclusion of a value the domain could never produce, or a bound already implied. The only constraint family the run time never reports. |
| [JD025 DuplicatePoolValue](JD025.en.md) | 🟠 Warning | on | The same constant is listed twice in a pool; duplicates collapse, so the pool is one value smaller than it reads and the duplicate weights nothing. |
| [JD026 EmptyRelativeUri](JD026.en.md) | 🟠 Warning | on | A relative URI with zero path segments and no query, fragment or root is the empty reference — the one chain whose failure lands at act time rather than at the arrange line. |
| [JD029 PooledValueNeverDraws](JD029.en.md) | 🔵 Info | on | A value written into a string or numeric value set that a constraint on the same chain refuses, so no draw can yield it. The dual of JD024, and it sees only what is written at the call site. |
| [JD030 UndeclaredStringLength](JD030.en.md) | 🔵 Info | on | An `Any.String()` chain that declares no length, so it draws the whole default spread — 0 to 1024 characters. Names the remedy where you can act on it. |
| [JD031 PairedBoundsHaveARangeForm](JD031.en.md) | 🔵 Info | on | A chain declares both inclusive bounds of a range separately, where the same generator names that range in one call. Nothing is wrong — this closes a discoverability gap. Inclusive pairs only: a strict pair has no exact range form. |
| [JD032 BoundDeclaredTwice](JD032.en.md) | 🟠 Warning | on | A chain declares the same bound twice; bounds fold silently, so only the tighter one survives and the looser call is dead. Matched on the name, so the aliases stay silent, and a bound held under a name is never followed. |
| [JD033 AnchoredLiteralOutsideCharacterFamily](JD033.en.md) | 🔵 Info | on | An anchored literal holds a character the declared family, subtraction or casing cannot draw. Legal and deliberate in a fixed-prefix format, so it reports the consequence — that character appears only where you wrote it. |

## Composition

These rules are about assembling generators into bigger ones — `Combine`'s operands, and the element contract a collection generator relies on. What they share is that nothing goes wrong: the composed generator builds, draws and returns a value. It is simply not the value the call site describes.

| Rule | Severity | Default | Description |
|------|----------|---------|-------------|
| [JD027 UnusedCombineOperand](JD027.en.md) | 🟠 Warning | on | A Combine operand is drawn and thrown away because the composer never reads its parameter. Name the parameter `_` to say the draw is deliberate. |
| [JD028 InertDistinctness](JD028.en.md) | 🟠 Warning | on | Distinctness is declared over an element type with no value equality, so it is satisfied by construction and the collection can still hold the same value twice. |

## Configuring

Every rule's severity can be tuned in `.editorconfig`, for example:

```ini
# turn an opt-in rule on
dotnet_diagnostic.JD011.severity = warning

# or silence a rule you do not want
dotnet_diagnostic.JD024.severity = none
```

The rule set is also declared in
[`AnalyzerReleases.Shipped.md`](../../../../JustDummies.Analyzers/AnalyzerReleases.Shipped.md),
which is what the Roslyn release-tracking analyzers check the descriptors against.
