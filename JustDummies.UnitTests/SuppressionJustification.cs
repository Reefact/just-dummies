namespace JustDummies.UnitTests;

/// <summary>
///     The justifications shared by several <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute" />
///     declarations of this suite, one nested class per analyzer rule — the same convention as the library's
///     <c>SuppressionJustification</c>: only duplicated texts live here, and the detailed reasoning sits in each
///     constant's summary.
/// </summary>
internal static class SuppressionJustification {

    /// <summary>Justifications for JD025 — the pool-duplicate collapse diagnostic.</summary>
    internal static class JD025 {

        /// <summary>
        ///     The duplicate IS the subject. The test pins the collapsing JD025 reports: without it there is nothing
        ///     to collapse and the test asserts nothing.
        /// </summary>
        internal const string DuplicateIsTheSubject = "The duplicate IS the subject: without it there is nothing to collapse and the test asserts nothing. See the constant's summary.";

    }

}
