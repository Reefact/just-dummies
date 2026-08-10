namespace JustDummies.Analyzers;

/// <summary>
///     The justifications carried by this project's <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute" />
///     declarations, one nested class per analyzer rule. A justification lives here when it is <b>duplicated</b> — the same
///     fact suppressed at several sites — or when it is long enough that leaving it inline would make the attribute
///     unreadable: the constant's value stays one crisp sentence while its <c>summary</c> carries the reasoning. The rule
///     ids themselves are always the catalogue constants (ADR-0050); these are only the texts.
/// </summary>
internal static class SuppressionJustification {

    /// <summary>Justifications for S3267 — "Loops should be simplified with LINQ expressions".</summary>
    internal static class S3267 {

        /// <summary>
        ///     Every <c>TryCheck*</c> member walks the argument list and reports the first offender through its out
        ///     parameters. The rule asks for <c>Select(argument => argument.Value)</c>, a projection that buys nothing and
        ///     renames the loop variable away from what it is: <c>argument</c> is an <c>IArgumentOperation</c>, and
        ///     <c>argument.Value</c> reads as the operation behind it. <c>TryCheckSize</c> cannot honour it at all — its
        ///     filter produces the <c>out int value</c> its body then reports on, so a projection would force a second
        ///     <c>TryGetInt32</c> call. The family reads the same way on purpose, so the suppression sits once on the type
        ///     rather than four times on its members.
        /// </summary>
        internal const string ArgumentIsTheOperation = "The loop variable IS the IArgumentOperation the body reports on, and TryCheckSize's filter produces the value its body reads. See the constant's summary.";

        /// <summary>
        ///     The rule asks for <c>Select(argument => argument.Value)</c>. The loop unwraps a delegate creation before
        ///     testing what it found and reports on the lambda body, so the projection would rename the loop variable away
        ///     from what it is without removing a single step: <c>argument</c> is an <c>IArgumentOperation</c>, and the
        ///     unwrapping still has to happen inside.
        /// </summary>
        internal const string UnwrappingHappensInsideTheLoop = "The loop unwraps a delegate creation before testing it, so a projection would rename the loop variable without removing a step. See the constant's summary.";

    }

}
