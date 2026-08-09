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

}
