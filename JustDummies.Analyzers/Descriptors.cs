using JustDummies.Diagnostics;

using Microsoft.CodeAnalysis;

namespace JustDummies.Analyzers;

/// <summary>
///     The <see cref="DiagnosticDescriptor" /> for every JustDummies rule. One field per JDxxx.
/// </summary>
internal static class Descriptors {

    public static readonly DiagnosticDescriptor AsyncBodyPassedToReproducibly = new(
        id: JustDummiesRule.JD001.Id,
        title: JustDummiesRule.JD001.Title,
        messageFormat: "Pass the asynchronous body to Any.ReproduciblyAsync and await it: Any.Reproducibly takes an Action, so an async lambda runs as 'async void' and its failures never reach the test",
        category: JustDummiesRule.JD001.Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Any.Reproducibly takes a synchronous Action. An async lambda bound to it becomes 'async void', whose exceptions escape the reproducible scope entirely and never fail the test. Use Any.ReproduciblyAsync(Func<Task>) and await it.",
        helpLinkUri: JustDummiesRule.JD001.HelpLinkUri);

    public static readonly DiagnosticDescriptor DiscardedReproduciblyAsyncResult = new(
        id: JustDummiesRule.JD002.Id,
        title: JustDummiesRule.JD002.Title,
        messageFormat: "Await the task returned by Any.ReproduciblyAsync; discarding it silently drops the body's failures",
        category: JustDummiesRule.JD002.Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Any.ReproduciblyAsync returns a Task that faults with the body's exception. Discarding it (as a bare statement or via '_ =') lets a failing test pass green. Await it.",
        helpLinkUri: JustDummiesRule.JD002.HelpLinkUri);

    public static readonly DiagnosticDescriptor AwaitableBodyPassedToReproducibly = new(
        id: JustDummiesRule.JD003.Id,
        title: JustDummiesRule.JD003.Title,
        messageFormat: "Pass the asynchronous body to Any.ReproduciblyAsync and await it: bound to Any.Reproducibly's Action the body is never awaited, so the scope returns before the assertions run and their failures never reach the test",
        category: JustDummiesRule.JD003.Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Any.Reproducibly takes a synchronous Action. A synchronous lambda whose body produces a task drops that task, and an 'async void' method group bound to the Action raises its failures outside the scope entirely. Neither is reported by the compiler — CS4014 does not fire when the enclosing lambda is not itself async. Use Any.ReproduciblyAsync(Func<Task>) and await it.",
        helpLinkUri: JustDummiesRule.JD003.HelpLinkUri);

    public static readonly DiagnosticDescriptor DiscardedSeedingResult = new(
        id: JustDummiesRule.JD004.Id,
        title: JustDummiesRule.JD004.Title,
        messageFormat: "Do not discard the result of Any.{0}: {1}",
        category: JustDummiesRule.JD004.Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Any.UseSeed returns the handle that closes the scope it opened; dropping it leaves the seed pinned for whatever runs next in the same execution context, silently making later tests replay one fixed sequence. Any.WithSeed returns an isolated context and pins nothing, so discarding it is dead code at a call site that reads as if the run had been seeded.",
        helpLinkUri: JustDummiesRule.JD004.HelpLinkUri);

    public static readonly DiagnosticDescriptor GeneratorRenderedAsText = new(
        id: JustDummiesRule.JD005.Id,
        title: JustDummiesRule.JD005.Title,
        messageFormat: "Call Generate() on the {0}: rendered as text a generator yields its own type name, not an arbitrary value",
        category: JustDummiesRule.JD005.Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A generator is an immutable recipe, and no JustDummies generator overrides ToString(). Interpolating, concatenating or calling ToString() on one therefore produces the builder's type name — a non-empty, plausible, run-invariant string that flows into the code under test as if it were an arbitrary value. Materialize the value with Generate().",
        helpLinkUri: JustDummiesRule.JD005.HelpLinkUri);

    public static readonly DiagnosticDescriptor DiscardedGeneratorResult = new(
        id: JustDummiesRule.JD006.Id,
        title: JustDummiesRule.JD006.Title,
        messageFormat: "Assign the result of {0} back: a generator is an immutable recipe, so a constraint whose result is discarded constrains nothing",
        category: JustDummiesRule.JD006.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Every constraint returns a new generator rather than mutating the receiver. A discarded result therefore silently drops the invariant the arrangement declared, and the generator keeps drawing from the wider domain — so the test passes on most runs and fails on the one that draws outside it, with a value nobody can reproduce.",
        helpLinkUri: JustDummiesRule.JD006.HelpLinkUri);

    public static readonly DiagnosticDescriptor DrawOutsideThePinnedScope = new(
        id: JustDummiesRule.JD007.Id,
        title: JustDummiesRule.JD007.Title,
        messageFormat: "Draw this value inside the test body: {0} runs before [Reproducible] opens the seed scope, so the seed the failure reports does not replay it",
        category: JustDummiesRule.JD007.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "xUnit constructs the test-class instance, and awaits IAsyncLifetime.InitializeAsync, before running the hooks the adapter pins the seed from. A value drawn there comes from the unseeded ambient source, so the test advertises full reproducibility while part of its arrangement is unpinned: pinning the reported seed does not bring the failure back.",
        helpLinkUri: JustDummiesRule.JD007.HelpLinkUri);

    public static readonly DiagnosticDescriptor ArbitraryValueInTheoryData = new(
        id: JustDummiesRule.JD008.Id,
        title: JustDummiesRule.JD008.Title,
        messageFormat: "Draw this value in the test body, or let the provider yield the generator: theory data is produced at discovery, before any seed is pinned, and every case shares the one value",
        category: JustDummiesRule.JD008.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "xUnit evaluates a theory's data provider at discovery time, once for the whole run and outside every seed scope. The drawn value is therefore shared by every case of the theory, replayable from no reported seed, and constant where the theory reads as if it enumerated arbitrary cases.",
        helpLinkUri: JustDummiesRule.JD008.HelpLinkUri);

    public static readonly DiagnosticDescriptor DrawInStaticInitializer = new(
        id: JustDummiesRule.JD009.Id,
        title: JustDummiesRule.JD009.Title,
        messageFormat: "Hold the generator rather than the value: a static initializer draws once for the whole suite, under whichever test happened to run first",
        category: JustDummiesRule.JD009.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A type initializer runs once, lazily, when the first test touches the type. The value is drawn under whatever ambient context that test had pinned, is shared by every other test in the class, and is replayable from none of their reported seeds — so the tests become order-dependent and stop varying between runs. Store the generator in the static field and call Generate() per test.",
        helpLinkUri: JustDummiesRule.JD009.HelpLinkUri);

    public static readonly DiagnosticDescriptor ReproducibleOnNonTestMethod = new(
        id: JustDummiesRule.JD010.Id,
        title: JustDummiesRule.JD010.Title,
        messageFormat: "Remove [Reproducible] from '{0}' or make it a test: xUnit collects the attribute from the test method, its class and the assembly only, so here it pins nothing",
        category: JustDummiesRule.JD010.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The adapter's hooks are collected from a test method, its declaring class and the assembly. On a helper — or on a method whose [Fact] was removed during a refactor — the attribute is never read: it pins no seed and reports none. Because a working [Reproducible] is silent on a passing test by design, nothing else distinguishes the inert form from the working one.",
        helpLinkUri: JustDummiesRule.JD010.HelpLinkUri);

    public static readonly DiagnosticDescriptor GeneratorWhereValueExpected = new(
        id: JustDummiesRule.JD011.Id,
        title: JustDummiesRule.JD011.Title,
        messageFormat: "Call Generate() on the {0}: passed where an object is expected, the recipe itself is stored, compared or asserted on, never the value it would draw",
        category: JustDummiesRule.JD011.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        // Opt-in, on the evidence ADR-0038's follow-up asked for rather than on intuition: dogfooded over this
        // repository's suites the rule found no true positive and two false ones, both in a convention test that
        // collects generators into a List<object> on purpose. That shape is indistinguishable from the theory-row
        // mistake this rule exists to catch, so it cannot be narrowed away. The rule earns its keep in a consumer
        // suite, where object-typed assertion helpers are common and reflection over generators is not.
        isEnabledByDefault: false,
        description: "Generators are reference types, so an object, dynamic or params object[] position accepts one with no conversion — the residue the removal of the implicit conversions could not close. An assertion helper taking object then inspects the recipe (Assert.NotNull(Any.String()) is green for ever), a theory row carries the recipe into the code under test, and Equals against a value is false for every run and every seed. Opt-in: a suite that manipulates generators as objects on purpose would see this fire on legitimate code.",
        helpLinkUri: JustDummiesRule.JD011.HelpLinkUri);

    public static readonly DiagnosticDescriptor GeneratorPooledAsValue = new(
        id: JustDummiesRule.JD012.Id,
        title: JustDummiesRule.JD012.Title,
        messageFormat: "Call Generate() on each pooled generator: Any.{0} inferred a pool of recipes, so drawing from it yields a recipe rather than a value",
        category: JustDummiesRule.JD012.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Any.OneOf(Any.Int32(), Any.Int32()) compiles and infers the builder type as the pool's element type, so the pool holds recipes. What makes this a trap rather than an obvious mistake is that the surface is inconsistent about it: pooling generators of different types fails type inference and the compiler catches it, while two of the same type bind cleanly.",
        helpLinkUri: JustDummiesRule.JD012.HelpLinkUri);

    public static readonly DiagnosticDescriptor HeldCollectionPassedToOneOf = new(
        id: JustDummiesRule.JD013.Id,
        title: JustDummiesRule.JD013.Title,
        messageFormat: "Use Any.ElementOf to draw from the collection's elements: passed to OneOf it binds T to {0}, so the pool holds one item and every draw returns the same one",
        category: JustDummiesRule.JD013.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Any.OneOf takes params T[], so a single collection argument binds T to the collection type itself rather than to its elements. The call compiles, draws succeed, and every one of them returns the same collection — the arbitrary choice the test claims to make never varies. Any.ElementOf is the entry point that draws from a collection's elements; an explicit type argument states the opposite intent and is left alone.",
        helpLinkUri: JustDummiesRule.JD013.HelpLinkUri);

    public static readonly DiagnosticDescriptor RejectedConstantArgument = new(
        id: JustDummiesRule.JD014.Id,
        title: JustDummiesRule.JD014.Title,
        messageFormat: "{0} throws for this argument: {1}",
        category: JustDummiesRule.JD014.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The argument is a compile-time constant the generator's own guard refuses, so the call throws every time it runs. Nothing is decided at run time that is not already decided here, and the failure otherwise surfaces late — inside an arrange helper shared by many tests, where it reads as a library problem rather than as the transposition typo it usually is. The run-time guards stay for every argument this cannot see.",
        helpLinkUri: JustDummiesRule.JD014.HelpLinkUri);

    public static readonly DiagnosticDescriptor StringConstraintsAdmitNoValue = new(
        id: JustDummiesRule.JD015.Id,
        title: JustDummiesRule.JD015.Title,
        messageFormat: "No string satisfies this chain: {0}",
        category: JustDummiesRule.JD015.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The constraints contradict each other for the constants written at the call site, so the chain throws a ConflictingAnyConstraintException the moment the arrange line runs. This is the case ADR-0014 names as the one an analyzer should carry: Numeric().StartingWith(\"ORD-\") conflicts while Numeric().StartingWith(\"123\") does not, from identical call sites and identical static types — only the argument value tells them apart.",
        helpLinkUri: JustDummiesRule.JD015.HelpLinkUri);

    public static readonly DiagnosticDescriptor CollectionConstraintsAdmitNoValue = new(
        id: JustDummiesRule.JD016.Id,
        title: JustDummiesRule.JD016.Title,
        messageFormat: "No collection satisfies this chain: {0}",
        category: JustDummiesRule.JD016.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The count constraints contradict each other for the constants written at the call site, or the chain asks for more distinct elements than its element generator can produce — the cardinality gate ADR-0004 records. Both throw at declaration time, so the value here is a build-time red rather than an arrange-time one: the chain usually sits in a helper several call frames away from the test that dies on it.",
        helpLinkUri: JustDummiesRule.JD016.HelpLinkUri);

    public static readonly DiagnosticDescriptor EnumUniverseViolation = new(
        id: JustDummiesRule.JD017.Id,
        title: JustDummiesRule.JD017.Title,
        messageFormat: "Any.Enum draws only declared members: {0}",
        category: JustDummiesRule.JD017.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Any.Enum<T>() draws uniformly across T's declared members and never an undeclared numeric value. That is deliberate and surprising: on a [Flags] enum, writing a combination in OneOf is the natural thing to do and the generator refuses it unless AllowingCombinations() is declared. An exclusion that removes every declared member is the same category error from the other side.",
        helpLinkUri: JustDummiesRule.JD017.HelpLinkUri);

    public static readonly DiagnosticDescriptor NestedReproducibilityScope = new(
        id: JustDummiesRule.JD018.Id,
        title: JustDummiesRule.JD018.Title,
        messageFormat: "This Any.Reproducibly runs inside {0}, whose reported seed then replays nothing: the inner scope draws a fresh seed on every run",
        category: JustDummiesRule.JD018.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Any.Reproducibly takes its seed from Guid.NewGuid().GetHashCode(), not from the ambient source, so an inner scope ignores whatever the outer one pinned and draws afresh every run. The outer mechanism still reports its own seed, so the failure names a seed that reproduces nothing — a wrong instruction rather than a wrong result. The seeded overload is left alone: pinning a chosen seed inside is deliberate.",
        helpLinkUri: JustDummiesRule.JD018.HelpLinkUri);

    public static readonly DiagnosticDescriptor CommittedReplaySeed = new(
        id: JustDummiesRule.JD019.Id,
        title: JustDummiesRule.JD019.Title,
        messageFormat: "Seed {0} is pinned: the values stop varying between runs, so the test no longer surfaces a dependency on one particular value",
        category: JustDummiesRule.JD019.Category,
        defaultSeverity: DiagnosticSeverity.Info,
        // Opt-in, and it must be: this repository's own maintainer guide instructs the opposite for a whole class of
        // tests ("Pin a seed for anything statistical"), so a rule enabled by default would fight documented practice.
        isEnabledByDefault: false,
        description: "The seeded overloads exist to replay a run a failure reported — correct while reproducing, wrong once committed, because the test then draws the same values for ever and stops surfacing the coupling the library exists to reveal. Opt-in: a statistical test legitimately pins a seed, and this repository's maintainer guide says so, which makes the rule a pre-release sweep rather than a standing check.",
        helpLinkUri: JustDummiesRule.JD019.HelpLinkUri);

    public static readonly DiagnosticDescriptor SharedStaticAnyContext = new(
        id: JustDummiesRule.JD020.Id,
        title: JustDummiesRule.JD020.Title,
        messageFormat: "Give each unit of work its own context: '{0}' is shared, and interleaved draws make neither the sequence nor the multiset stable across runs",
        category: JustDummiesRule.JD020.Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "AnyContext's own documentation states the hazard: a context is safe to draw from concurrently, but sharing one across threads costs the replay rather than the values. A static context looks maximally deterministic — a literal seed, right there in the source — while a parallel suite gets a different value per test per run from it.",
        helpLinkUri: JustDummiesRule.JD020.HelpLinkUri);

    public static readonly DiagnosticDescriptor BlankReplaySnippet = new(
        id: JustDummiesRule.JD021.Id,
        title: JustDummiesRule.JD021.Title,
        messageFormat: "Pass the code a reader copies to replay the run, or drop the argument: a blank snippet is rejected at run time",
        category: JustDummiesRule.JD021.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Any.UseSeed(int, string) rejects a blank snippet. Because that scope is normally opened from a test-framework adapter's hook, the throw surfaces as an infrastructure failure on every test in the suite rather than as one failing assertion — a disproportionately expensive way to learn about a typo the compiler can already see.",
        helpLinkUri: JustDummiesRule.JD021.HelpLinkUri);

    public static readonly DiagnosticDescriptor ParallelDrawWithoutPerItemSeed = new(
        id: JustDummiesRule.JD022.Id,
        title: JustDummiesRule.JD022.Title,
        messageFormat: "Open an Any.UseSeed scope inside the work item: the ambient scope reaches every worker, so the draws interleave and the run replays nothing",
        category: JustDummiesRule.JD022.Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The ambient seed scope flows with the execution context, so a scope opened around a parallel loop reaches every worker and their draws interleave: neither the sequence nor the multiset is stable across runs. A scope opened inside the loop body gives each unit of work its own sequence, and the whole run replays — the shape the library's documentation names.",
        helpLinkUri: JustDummiesRule.JD022.HelpLinkUri);

    public static readonly DiagnosticDescriptor ScalarChainAdmitsNoValue = new(
        id: JustDummiesRule.JD023.Id,
        title: JustDummiesRule.JD023.Title,
        messageFormat: "No value satisfies this chain once {0} is applied",
        category: JustDummiesRule.JD023.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The constant constraints narrow the domain to nothing, so the chain throws a ConflictingAnyConstraintException the moment the arrange line runs. The library computes this with one emptiness test over bounds, lattice and allow-list; this rule runs the same test over the constants written at the call site, and stays silent for every argument it cannot fold.",
        helpLinkUri: JustDummiesRule.JD023.HelpLinkUri);

    public static readonly DiagnosticDescriptor ConstraintWithNoEffect = new(
        id: JustDummiesRule.JD024.Id,
        title: JustDummiesRule.JD024.Title,
        messageFormat: "This constraint changes nothing: {0}",
        category: JustDummiesRule.JD024.Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The constraint is legal and inert: the domain it produces is the one that already existed. This is the only member of the constraint family the run time NEVER reports — every other contradiction throws eventually and loudly, while an inert constraint leaves the test green and exercising a domain the author did not write. The dangerous case is an exclusion of a sentinel the generator could never draw: it silently misses, and starts mattering the day someone widens the range.",
        helpLinkUri: JustDummiesRule.JD024.HelpLinkUri);

    public static readonly DiagnosticDescriptor DuplicatePoolValue = new(
        id: JustDummiesRule.JD025.Id,
        title: JustDummiesRule.JD025.Title,
        messageFormat: "This value is already in the pool; a duplicate neither weights it nor widens the domain",
        category: JustDummiesRule.JD025.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A pool is deduplicated under the default equality when it is built, so a value listed twice contributes exactly once. The library declines to weight a pool on purpose — writing a value twice therefore cannot mean 'draw this more often', and the pool is one value smaller than it reads. That gap surfaces far from here, when a distinct collection over the pool gates against the real distinct count and reports a number the author cannot find in their source.",
        helpLinkUri: JustDummiesRule.JD025.HelpLinkUri);

    public static readonly DiagnosticDescriptor PooledValueNeverDraws = new(
        id: JustDummiesRule.JD029.Id,
        title: JustDummiesRule.JD029.Title,
        messageFormat: "This value never draws: {0} refuses it",
        category: JustDummiesRule.JD029.Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "A value set composes with the other constraints, so each value passes or fails them and the domain is the values that pass. A value that fails leaves the domain in silence — only an EMPTIED domain is reported, at declaration. This rule is the dual of JD024: where that one reports a constraint narrowing nothing, this one reports a value nothing lets through. It reads the string families and the numeric ones whose constants fold exactly -- every integer type and decimal; the binary floating-point families are out, since their constants have no exact decimal to judge them by. It sees only what is written at the call site, since a pool held in a variable is not knowable here; a catalogue loaded at run time is answered instead by IPoolInspection<T>, which reports the same fact against the values actually supplied. Reported as information, not as a warning: narrowing a shared pool at one call site is what the composition is for, so this is a fact to weigh, never a verdict.",
        helpLinkUri: JustDummiesRule.JD029.HelpLinkUri);

    public static readonly DiagnosticDescriptor UndeclaredStringLength = new(
        id: JustDummiesRule.JD030.Id,
        title: JustDummiesRule.JD030.Title,
        messageFormat: "This string dummy declares no length: it draws {0} characters",
        category: JustDummiesRule.JD030.Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "An unconstrained Any.String() draws 0 to 1024 characters from the whole of ASCII, and NonEmpty() alone only moves that to 1 to 1025 -- it raises the floor and leaves the ceiling where it was. That default is deliberately inconvenient (ADR-0076), because a dummy short enough to be comfortable is one no length invariant is ever exercised against -- but an inconvenient default only teaches when something names the remedy, and a wall of characters in a failure message does not say WithMaxLength. This rule says it, at the call site. Declare the length the surrounding code actually allows: a column width, a contract bound, an exact size. Reported as information, not as a warning: a length a test genuinely does not care about is a legitimate thing to leave unsaid, so this is a fact to weigh, never a verdict.",
        helpLinkUri: JustDummiesRule.JD030.HelpLinkUri);

    public static readonly DiagnosticDescriptor PairedBoundsHaveARangeForm = new(
        id: JustDummiesRule.JD031.Id,
        title: JustDummiesRule.JD031.Title,
        messageFormat: "These two bounds are the range {0}",
        category: JustDummiesRule.JD031.Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Both inclusive bounds of a range are declared separately, and the same generator names that range in a single call. Nothing is wrong here, and nothing has to change: the two spellings behave identically -- the range method IS the two bounds -- and the decomposed form is decomposable on purpose, so a shared helper can set a floor and a call site add a ceiling. This is a discoverability rule and nothing more. The range form is easy to miss, a reader who writes the bounds separately never learns it exists, and it reads closer to how the rule is usually stated out loud. It also carries one consequence the pair does not: a conflict raised later names the range the author wrote rather than one of its halves. Reported as information, never as a verdict. Only INCLUSIVE pairs are reported, because only they have an exact range form: a strict pair has none at all on a floating-point type, and a different one on an integral type.",
        helpLinkUri: JustDummiesRule.JD031.HelpLinkUri);

    public static readonly DiagnosticDescriptor EmptyRelativeUri = new(
        id: JustDummiesRule.JD026.Id,
        title: JustDummiesRule.JD026.Title,
        messageFormat: "A relative URI with exactly 0 path segments and no query, fragment or root is empty, which is not a valid URI reference",
        category: JustDummiesRule.JD026.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The chain describes the empty reference, which no URI can be. The library reports it, but only at Generate() — this is the one constraint family member whose failure lands at act time rather than at the arrange line, so the stack points at the code under test instead of at the declaration that is wrong. Add WithQuery(), WithFragment(), Rooted(), or a positive segment count.",
        helpLinkUri: JustDummiesRule.JD026.HelpLinkUri);

    public static readonly DiagnosticDescriptor UnusedCombineOperand = new(
        id: JustDummiesRule.JD027.Id,
        title: JustDummiesRule.JD027.Title,
        messageFormat: "This generator is drawn and thrown away: the composer never reads its parameter '{0}'",
        category: JustDummiesRule.JD027.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Combine draws every operand before calling the composer, so an operand the composer ignores is still generated — constraints, conflict checks and all — and then dropped. Nothing fails: the composed value is well-formed, and simply does not carry the part the call site says it carries. The usual causes are a constructor argument forgotten during a refactor and a composer whose parameters no longer line up with its operands. Rename the parameter to '_' to say the draw is deliberate.",
        helpLinkUri: JustDummiesRule.JD027.HelpLinkUri);

    public static readonly DiagnosticDescriptor InertDistinctness = new(
        id: JustDummiesRule.JD028.Id,
        title: JustDummiesRule.JD028.Title,
        messageFormat: "Distinctness cannot bind here: '{0}' inherits reference equality, so every freshly generated element already counts as distinct",
        category: JustDummiesRule.JD028.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The element type neither overrides Equals nor implements IEquatable, so the default comparer falls back to reference equality — and every element the generator produces is a new instance. Distinctness is therefore satisfied by construction and constrains nothing: the collection can hold the same value several times, which is precisely what the declaration asks it not to. The library cannot report this, because from its side the requirement is met. Give the type value equality, or pass an explicit comparer.",
        helpLinkUri: JustDummiesRule.JD028.HelpLinkUri);

}
