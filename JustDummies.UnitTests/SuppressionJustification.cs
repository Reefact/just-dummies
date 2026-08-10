namespace JustDummies.UnitTests;

/// <summary>
///     The justifications shared by several <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute" />
///     declarations of this suite, one nested class per analyzer rule — the same convention as the library's
///     <c>SuppressionJustification</c>: only duplicated texts live here, and the detailed reasoning sits in each
///     constant's summary.
/// </summary>
internal static class SuppressionJustification {

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

    /// <summary>Justifications for JD025 — the pool-duplicate collapse diagnostic.</summary>
    internal static class JD025 {

        /// <summary>
        ///     The duplicate IS the subject. The test pins the collapsing JD025 reports: without it there is nothing
        ///     to collapse and the test asserts nothing.
        /// </summary>
        internal const string DuplicateIsTheSubject = "The duplicate IS the subject: without it there is nothing to collapse and the test asserts nothing. See the constant's summary.";

    }

    /// <summary>Justifications for S2688 — "NaN should not be used in comparisons".</summary>
    internal static class S2688 {

        /// <summary>
        ///     The same fact as <see cref="CA2242.ComparisonIsTheAssertion" />, noticed by Sonar instead of the .NET
        ///     analyzers: defined there once, referenced here so the two rules cannot drift apart.
        /// </summary>
        internal const string ComparisonIsTheAssertion = CA2242.ComparisonIsTheAssertion;

    }

}
