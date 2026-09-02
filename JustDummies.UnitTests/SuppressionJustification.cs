namespace JustDummies.UnitTests;

/// <summary>
///     The justifications carried by this suite's <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute" />
///     declarations, one nested class per analyzer rule — the same convention as the library's
///     <c>SuppressionJustification</c>: a text lives here when it is duplicated, or when it is long enough that leaving
///     it inline would make the attribute unreadable, and the detailed reasoning sits in each constant's summary.
/// </summary>
internal static class SuppressionJustification {

    /// <summary>Justifications for CA1870 — "Use a cached 'SearchValues&lt;T&gt;' instance".</summary>
    internal static class CA1870 {

        /// <summary>
        ///     <c>SearchValues&lt;T&gt;</c> arrived in .NET 8 and this suite also runs on the .NET Framework 4.7.2 support
        ///     floor (ADR-0007), where the type does not exist. <c>IndexOfAny</c> over two characters, run once per cref in
        ///     a convention test, is not the cost this rule exists to remove.
        /// </summary>
        internal const string NoSearchValuesDownlevel = "SearchValues<T> arrived in .NET 8 and this suite also runs on the net472 support floor (ADR-0007); one IndexOfAny per cref is not the cost this rule removes. See the constant's summary.";

    }

    /// <summary>Justifications for CA2242 — "Test for NaN correctly".</summary>
    internal static class CA2242 {

        /// <summary>
        ///     The rule says to write <c>double.IsNaN(x)</c> rather than compare with <c>==</c>, which is right
        ///     everywhere except here: this test asserts that the two DISAGREE. Replacing the comparison with
        ///     <c>IsNaN</c> would delete the assertion and leave a test that proves nothing, on the exact trap the
        ///     README warns a user about. <see cref="S2688" /> flags the same fact through Sonar's eyes and shares
        ///     this text.
        /// </summary>
        internal const string ComparisonIsTheAssertion = "The == that disagrees with IsNaN IS the assertion; writing IsNaN would leave a test proving nothing. See the constant's summary.";

    }

    /// <summary>Justifications for CA2263 — "Prefer generic overload when type is known".</summary>
    internal static class CA2263 {

        /// <summary>
        ///     <c>Enum.IsDefined&lt;TEnum&gt;(TEnum)</c> arrived in .NET 5 and this suite also runs on the .NET Framework
        ///     4.7.2 support floor (ADR-0007, <c>build/Net472TestFloor.props</c>), where it does not exist. The non-generic
        ///     overload is the only spelling that compiles on both legs; the reason is restated at the call site so a
        ///     reader meets it there too.
        /// </summary>
        internal const string NoGenericIsDefinedDownlevel = "Enum.IsDefined<TEnum>(TEnum) arrived in .NET 5 and this suite also runs on the net472 support floor (ADR-0007), where only the non-generic overload compiles. See the constant's summary.";

    }

    /// <summary>Justifications for JD005 — the materialized-generator diagnostic.</summary>
    internal static class JD005 {

        /// <summary>
        ///     The rendered generator IS the subject. This pins the behaviour JD005 reports, and the deliberate absence of
        ///     a <c>ToString</c> override that would mask it — an override returning a drawn value would make this test
        ///     red, which is the point.
        /// </summary>
        internal const string RenderedGeneratorIsTheSubject = "The rendered generator IS the subject: an override that masked it would make this test red, which is the point. See the constant's summary.";

    }

    /// <summary>Justifications for JD013 — the one-member pool diagnostic.</summary>
    internal static class JD013 {

        /// <summary>
        ///     The one-member pool IS the subject. This pins the behaviour JD013 reports: inference binds <c>T</c> to the
        ///     collection, so the call is legal, the draw succeeds, and what comes back is the whole list.
        /// </summary>
        internal const string OneMemberPoolIsTheSubject = "The one-member pool IS the subject: inference binds T to the collection, so the draw returns the whole list. See the constant's summary.";

    }

    /// <summary>Justifications for JD025 — the pool-duplicate collapse diagnostic.</summary>
    internal static class JD025 {

        /// <summary>
        ///     The duplicate IS the subject. The test pins the collapsing JD025 reports: without it there is nothing
        ///     to collapse and the test asserts nothing.
        /// </summary>
        internal const string DuplicateIsTheSubject = "The duplicate IS the subject: without it there is nothing to collapse and the test asserts nothing. See the constant's summary.";

    }

    /// <summary>Justifications for JD027 — the ignored-operand diagnostic.</summary>
    internal static class JD027 {

        /// <summary>
        ///     The ignored operand IS the subject. This pins the behaviour JD027 reports: the operand is generated in full
        ///     — constraints built, conflict checks run — and then dropped, with nothing failing.
        /// </summary>
        internal const string IgnoredOperandIsTheSubject = "The ignored operand IS the subject: it is generated in full, then dropped, with nothing failing. See the constant's summary.";

    }

    /// <summary>Justifications for JD028 — the inert-distinctness diagnostic.</summary>
    internal static class JD028 {

        /// <summary>
        ///     The inert distinctness IS the subject. This pins the silent behaviour JD028 reports, which the library
        ///     cannot report itself: from its side the requirement is met, because the draws really are pairwise unequal
        ///     under the comparer it was given.
        /// </summary>
        internal const string InertDistinctnessIsTheSubject = "The inert distinctness IS the subject: from the library's side the requirement is met, because the draws are pairwise unequal under the comparer it was given. See the constant's summary.";

    }

    /// <summary>Justifications for S125 — "Sections of code should not be commented out".</summary>
    internal static class S125 {

        /// <summary>
        ///     Prose, not code. The line explains what <c>obj/</c> and <c>bin/</c> contain and why scanning them would
        ///     double-count; the rule reads the slashes and the parenthetical as a commented-out statement.
        /// </summary>
        internal const string ProseNotDisabledCode = "Prose, not code: the rule reads the slashes of obj/ and bin/ and the parenthetical as a statement. See the constant's summary.";

    }

    /// <summary>Justifications for S2688 — "NaN should not be used in comparisons".</summary>
    internal static class S2688 {

        /// <summary>
        ///     The same fact as <see cref="CA2242.ComparisonIsTheAssertion" />, noticed by Sonar instead of the .NET
        ///     analyzers: defined there once, referenced here so the two rules cannot drift apart.
        /// </summary>
        internal const string ComparisonIsTheAssertion = CA2242.ComparisonIsTheAssertion;

    }

    // Conditioned like its only site: CrossEngineReachabilityTests is not compiled on the net472 support floor
    // (the <Compile Remove> list in this project file says why), and on that leg the constant would be dead —
    // which S1144 reports, as a warning the CI ratchet turns into an error.
#if !NET472
    /// <summary>Justifications for S2699 — "Tests should include assertions".</summary>
    internal static class S2699 {

        /// <summary>
        ///     Each theory is one line of dispatch to the per-type adapter; the NFluent <c>Check.That</c> and
        ///     <c>Check.ThatCode</c> calls live in the <c>IntervalCase&lt;T&gt;</c> overrides. The rule does follow
        ///     assertions into concrete helpers, but this call resolves statically to the abstract
        ///     <c>ReachabilityCase</c> declaration, which has no body, so it cannot see past the virtual dispatch. Lifting
        ///     the assertions into the test bodies would flatten the one-row-per-builder design and reduce the per-draw
        ///     scenarios to a single aggregated boolean.
        /// </summary>
        internal const string AssertionsLiveInTheAdapterOverrides = "The assertions live in the adapter overrides; the call resolves statically to an abstract declaration with no body, so the rule cannot see past the dispatch. See the constant's summary.";

    }
#endif

    /// <summary>Justifications for S3220 — "Method calls should not resolve ambiguously to overloads with 'params'".</summary>
    internal static class S3220 {

        /// <summary>
        ///     Passing a bare <c>null</c> to a <c>params</c> parameter is exactly what this test asserts about:
        ///     <c>OneOf(null!)</c> must be refused with <c>ArgumentNullException</c> rather than read as an empty list. The
        ///     ambiguity the rule warns about IS the input under test.
        /// </summary>
        internal const string AmbiguityIsTheInputUnderTest = "The ambiguity the rule warns about IS the input under test: OneOf(null!) must be refused rather than read as an empty list. See the constant's summary.";

        /// <summary>
        ///     Two separators passed to <c>Split</c>'s <c>params</c> overload, which is the only spelling that works on both
        ///     target frameworks. Wrapping them in an explicit array to disambiguate would immediately trip S3878, which
        ///     asks for that array to be removed again.
        /// </summary>
        internal const string TwoSeparatorsThroughParams = "The params overload is the only spelling that works on both target frameworks, and wrapping the separators in an array would trip S3878. See the constant's summary.";

    }

    /// <summary>Justifications for S3877 — "Exceptions should not be thrown from unexpected methods".</summary>
    internal static class S3877 {

        /// <summary>
        ///     Throwing from <c>ToString()</c> IS the fixture. The test proves diagnostics survive a domain object whose
        ///     rendering explodes, and that a successful draw never renders one — neither of which can be shown without a
        ///     type that throws exactly here.
        /// </summary>
        internal const string ThrowingToStringIsTheFixture = "Throwing from ToString() IS the fixture: neither half of the test can be shown without a type that throws exactly here. See the constant's summary.";

    }

}
