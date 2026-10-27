namespace JustDummies.Xunit.UnitTests;

/// <summary>
///     The justifications carried by this suite's <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute" />
///     declarations, one nested class per analyzer rule — the same convention as the library's
///     <c>SuppressionJustification</c>: a text lives here when it is duplicated, or when it is long enough that leaving
///     it inline would make the attribute unreadable, and the detailed reasoning sits in each constant's summary.
/// </summary>
internal static class SuppressionJustification {

    /// <summary>Justifications for JD030 — the undeclared-string-length diagnostic.</summary>
    internal static class JD030 {

        /// <summary>
        ///     The length is not what these tests pin. The string only has to exist and be well-formed so that a batch of
        ///     draws can be compared across two runs of the same seed; declaring a length would add a number that looks as
        ///     though it mattered.
        /// </summary>
        internal const string LengthIsNotWhatThisPins = "The length is not what this test pins: the string is only one of the draws a replay compares. See the constant's summary.";

    }

}
