namespace JustDummies;

/// <summary>
///     The justifications shared by several <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute" />
///     declarations, one nested class per analyzer rule. A justification lives here <b>only when it is duplicated</b>
///     — the same fact suppressed at several sites — so the reasoning has one home and cannot drift into fourteen
///     diverging copies; a justification used once stays inline at its site, unless its author wants the short
///     attribute value / detailed <c>summary</c> split this shape provides. The rule ids themselves are always the
///     catalogue constants (ADR-0050); these are only the texts.
/// </summary>
internal static class SuppressionJustification {

    /// <summary>Justifications for CA1822 — "Mark members as static".</summary>
    internal static class CA1822 {

        /// <summary>
        ///     <c>Validated</c> is the uniform validation hook of the fluent builders: every <c>With*</c> method routes
        ///     its candidate through it, and all seven engines declare it with the same signature. It reads the
        ///     CANDIDATE's state rather than this instance's — which is what the rule notices — but that is a builder
        ///     validating its own successor, not an oversight. Making it static across seven types would break a family
        ///     resemblance the reader relies on, for no measurable gain on a path that runs once per declared
        ///     constraint. <see cref="S2325" /> flags the same fact through Sonar's eyes and shares this text.
        /// </summary>
        internal const string UniformValidatedHook = "Validated reads the CANDIDATE's state by design — a builder validating its own successor, uniformly across the seven engines. See the constant's summary.";

    }

    /// <summary>Justifications for CA2208 — "Instantiate argument exceptions correctly".</summary>
    internal static class CA2208 {

        /// <summary>
        ///     The same fact as <see cref="S3928.PatternIsTheCallersArgument" />, noticed by the .NET analyzers instead of
        ///     Sonar: defined there once, referenced here so the two rules cannot drift apart.
        /// </summary>
        internal const string PatternIsTheCallersArgument = S3928.PatternIsTheCallersArgument;

    }

    /// <summary>Justifications for CA2249 — "Consider using String.Contains instead of String.IndexOf".</summary>
    internal static class CA2249 {

        /// <summary>
        ///     <c>string.Contains(string, StringComparison)</c> does not exist on netstandard2.0, which this library
        ///     targets (ADR-0007). <c>IndexOf</c> with <c>StringComparison.Ordinal</c> is the same comparison and the only
        ///     spelling that compiles on the shipped asset. Same downlevel wall as CA1510 (ADR-0037).
        /// </summary>
        internal const string NoContainsWithComparisonDownlevel = "string.Contains(string, StringComparison) is absent from the netstandard2.0 asset this library ships (ADR-0007); IndexOf with StringComparison.Ordinal is the same comparison. See the constant's summary.";

    }

    /// <summary>Justifications for S107 — "Methods should not have too many parameters".</summary>
    internal static class S107 {

        /// <summary>
        ///     The private constructor carries the engine's whole immutable state: the "constrain once, draw many"
        ///     design rebuilds the spec on every <c>With*</c> call, so every field has to be threaded through it. A
        ///     parameter object would only rename the same list, and the constructor is private — no caller ever
        ///     writes this argument list.
        /// </summary>
        internal const string EngineImmutableState = "The private constructor threads the engine's whole immutable state, and no caller ever writes this argument list. See the constant's summary.";

        /// <summary>
        ///     Heterogeneous composition needs one generator parameter per part; the arity-8 ceiling is a deliberate
        ///     ergonomic decision (ADR-0005), and a flat parameter list reads better at the call site than nested
        ///     <c>Combine</c> calls.
        /// </summary>
        internal const string HeterogeneousCombine = "One generator parameter per composed part; the arity-8 ceiling is deliberate (ADR-0005). See the constant's summary.";

    }

    /// <summary>Justifications for S125 — "Sections of code should not be commented out".</summary>
    internal static class S125 {

        /// <summary>
        ///     The flagged lines are prose, not disabled code: the heuristic reads an equation, a bracketed range or a
        ///     semicolon inside an explanatory sentence as a statement. These comments carry the reasoning this
        ///     codebase asks every comment to carry, so the finding is recorded rather than the comment deleted.
        /// </summary>
        internal const string ProseNotDisabledCode = "The flagged lines are prose carrying reasoning, not disabled code. See the constant's summary.";

    }

    /// <summary>Justifications for S1244 — "Floating point numbers should not be tested for equality".</summary>
    internal static class S1244 {

        /// <summary>
        ///     Exact equality is the question, not an approximation of it: <c>_min</c> and <c>_max</c> are not measured
        ///     quantities but the bounds the constraint chain validated, and the test asks whether they are the SAME
        ///     representable value. A tolerance would answer a different question, and answer it wrongly:
        ///     <c>[1.0, 1.0 + 1e-12]</c> holds millions of representable doubles, so reporting a cardinality of 1 would
        ///     make <c>ICardinalityHint</c> promise a distinct collection of one element over a range that can serve many.
        /// </summary>
        internal const string BoundsAreValidatedNotMeasured = "The bounds are validated constraint values rather than measurements; a tolerance would promise a cardinality of one over a range that serves millions. See the constant's summary.";

        /// <summary>
        ///     Exact equality detects the validated pin (the singleton domain <c>Cardinality</c> also reports) and returns
        ///     the only value the bounds leave; <c>IsSatisfiable</c> already proved that value is not excluded, which is
        ///     why this early return may skip the nudge walk. A tolerance would break both halves: it would collapse every
        ///     draw of a merely narrow interval such as <c>[1.0, 1.0 + 1e-12]</c> onto its lower bound instead of sampling
        ///     it, and it would take that exclusion-free shortcut for an interval whose lower bound IS excluded, returning
        ///     a value the constraints forbid.
        /// </summary>
        internal const string ExactEqualityDetectsThePin = "Exact equality detects the validated pin that licenses skipping the nudge walk; a tolerance would collapse narrow intervals and return a forbidden bound. See the constant's summary.";

        /// <summary>
        ///     Exclusion-list membership is exact by definition: <c>DifferentFrom(x)</c> forbids the value x, not a
        ///     neighbourhood of it. Widening it to a tolerance would carve a band out of the continuum that no constraint
        ///     asked for, and <c>Generate</c>'s nudge walk would have to step clear of that band, turning a measure-zero
        ///     collision into a systematic bias away from every excluded point. <c>Equals(double)</c> is
        ///     <c>'a == b || (IsNaN(a) &amp;&amp; IsNaN(b))'</c>; the NaN arm is unreachable because <c>EnsureFinite</c>
        ///     rejects NaN at every entry point, so this is plain exact equality with a defensive tail.
        /// </summary>
        internal const string ExclusionMembershipIsExact = "DifferentFrom(x) forbids x, not a neighbourhood of it; a tolerance would carve out a band no constraint asked for and bias every draw away from it. See the constant's summary.";

    }

    /// <summary>Justifications for S1694 — "An abstract class should have both abstract and concrete methods".</summary>
    internal static class S1694 {

        /// <summary>
        ///     The abstract class is the root of a closed, internal hierarchy and is deliberately not an interface: a
        ///     class cannot be implemented from outside the assembly, keeps the option of adding shared state without
        ///     breaking every subtype, and on the netstandard2.0 leg cannot be replaced by an interface with
        ///     non-public members.
        /// </summary>
        internal const string ClosedInternalHierarchyRoot = "The root of a closed, internal hierarchy — deliberately a class, not an interface. See the constant's summary.";

    }

    /// <summary>Justifications for S2245 — "Using pseudorandom number generators (PRNGs) is security-sensitive".</summary>
    internal static class S2245 {

        /// <summary>
        ///     S2245 is right that this generator is predictable; that predictability is the type's contract. A dummy is
        ///     worth generating only if the seed a failing run reports replays it, and a seeded <c>System.Random</c> is the
        ///     one BCL generator whose sequence a recorded seed reproduces. <c>RandomNumberGenerator</c> is seedless by
        ///     design, so adopting it would delete <c>Dummy.Reproducibly</c>, <c>Dummy.WithSeed</c> and <c>Dummy.UseSeed</c>
        ///     outright, along with the seed every generation failure reports. It would also break a checked contract:
        ///     <c>justdummies.yml</c> compares the SEEDBATCH banner that <c>tools/justdummies-check</c> draws from
        ///     <c>CrossTfmSeed</c> byte-for-byte between the lib/netstandard2.0 and lib/net8.0 assets. No draw in this
        ///     solution is security material: <c>SeededRandom</c> is internal and reachable only through the
        ///     <c>Dummy.*</c> test-data generators, and <c>README.nuget.md</c> tells consumers never to draw a secret, key,
        ///     token or nonce from <c>Dummy.*</c>.
        /// </summary>
        internal const string PredictabilityIsTheContract = "The predictability IS the contract: only a seeded System.Random replays the seed a failing run reports, and no draw here is security material. See the constant's summary.";

    }

    /// <summary>Justifications for S2325 — "Methods and properties that do not access instance data should be static".</summary>
    internal static class S2325 {

        /// <summary>
        ///     The same fact as <see cref="CA1822.UniformValidatedHook" />, noticed by Sonar instead of the .NET
        ///     analyzers: defined there once, referenced here so the two rules cannot drift apart.
        /// </summary>
        internal const string UniformValidatedHook = CA1822.UniformValidatedHook;

    }

    /// <summary>Justifications for S2436 — "Types and methods should not have too many generic parameters".</summary>
    internal static class S2436 {

        /// <summary>
        ///     Heterogeneous composition needs one type parameter per part plus the result; the arity-8 ceiling is a
        ///     deliberate ergonomic decision (ADR-0005), and nesting <c>Combine</c> calls to stay under three would
        ///     bury the shape of the value being composed.
        /// </summary>
        internal const string HeterogeneousCombine = "One type parameter per composed part plus the result; the arity-8 ceiling is deliberate (ADR-0005). See the constant's summary.";

        /// <summary>
        ///     <c>TItem</c> and <c>TResult</c> are the element type and the collection type they build; <c>TSelf</c> is the
        ///     CRTP self-type that lets every fluent method return the concrete generator instead of this base. Dropping it
        ///     would make each chained call return <c>DummyCollection</c> and force a cast at every step.
        /// </summary>
        internal const string CrtpSelfTypeKeepsTheChainConcrete = "TSelf is the CRTP self-type that keeps every chained call returning the concrete generator instead of this base. See the constant's summary.";

    }

    /// <summary>Justifications for S3267 — "Loops should be simplified with LINQ expressions".</summary>
    internal static class S3267 {

        /// <summary>
        ///     The loop body advances the very accumulator the condition tests — each iteration changes what the next
        ///     one compares against — so the filter cannot be lifted out of the loop. A <c>Where</c> clause would
        ///     evaluate every predicate against the value the accumulator held on entry and silently skip exclusions.
        /// </summary>
        internal const string AccumulatorAdvancesInLoop = "The loop advances the very accumulator its condition tests; a Where clause would test the entry value and skip exclusions. See the constant's summary.";

        /// <summary>
        ///     The condition reads the collection the body mutates. <c>Where</c> is lazily evaluated, so lifting the
        ///     filter out would run each predicate against a snapshot taken before the additions it is meant to see,
        ///     and let duplicates through.
        /// </summary>
        internal const string ConditionReadsMutatedCollection = "The condition reads the collection the body mutates; a lifted Where would test a stale snapshot and let duplicates through. See the constant's summary.";

        /// <summary>
        ///     The loop exists to name the FIRST offending element in the exception it throws. A <c>Where</c> clause
        ///     discards which element failed, so the message would have to re-find it, turning one pass into two and
        ///     one statement into three.
        /// </summary>
        internal const string LoopNamesFirstOffender = "The loop names the FIRST offending element in the exception it throws; Where discards which element failed. See the constant's summary.";

    }

    /// <summary>Justifications for S3928 — "Parameter names used into ArgumentException constructors should match an existing one".</summary>
    internal static class S3928 {

        /// <summary>
        ///     <c>pattern</c> is the public parameter the consumer passed to <c>Dummy.Pattern(...)</c>; this private factory
        ///     only assembles the exception the parser throws on its behalf. Its own <c>reason</c> parameter names the
        ///     diagnosis, not the argument at fault, so pointing the exception at it would send the caller to the wrong
        ///     place. <see cref="CA2208" /> flags the same fact through the .NET analyzers' eyes and shares this text.
        /// </summary>
        internal const string PatternIsTheCallersArgument = "pattern is the parameter the consumer passed to Dummy.Pattern(...); this private factory only assembles the exception on the parser's behalf. See the constant's summary.";

    }

}
