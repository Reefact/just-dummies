namespace JustDummies.PropertyTests;

/// <summary>
///     The justifications shared by several <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute" />
///     declarations of this suite, one nested class per analyzer rule — the same convention as the library's
///     <c>SuppressionJustification</c>: only duplicated texts live here, and the detailed reasoning sits in each
///     constant's summary.
/// </summary>
internal static class SuppressionJustification {

    /// <summary>Justifications for S1854 — "Unused assignments should be removed".</summary>
    internal static class S1854 {

        /// <summary>
        ///     The assignment is dead and the CALL is not. These builders exist to provoke the declaration-time
        ///     conflict, so what matters is that <c>Except()</c> runs; nothing reads the spec afterwards because the
        ///     verdict is the exception or its absence. Dropping <c>spec =</c> from the last line alone would break
        ///     the uniform chain that makes the sequence of constraints readable.
        /// </summary>
        internal const string CallIsTheSubject = "The assignment is dead and the CALL is not: the verdict is the exception or its absence. See the constant's summary.";

    }

}
