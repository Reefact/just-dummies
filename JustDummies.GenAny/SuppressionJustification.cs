using System.Diagnostics.CodeAnalysis;

namespace JustDummies.GenAny;

/// <summary>
///     The justifications shared by several <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute" />
///     declarations, one nested class per analyzer rule. A justification lives here <b>only when it is
///     duplicated</b> — the same fact suppressed at several sites — so the reasoning has one home and cannot
///     drift into diverging copies; a justification used once stays inline at its site. The rule ids themselves
///     are always the catalogue constants (ADR-0050); these are only the texts.
/// </summary>
internal static class SuppressionJustification {

    /// <summary>Justifications for S1135 — "Complete the task associated to this 'TODO' comment".</summary>
    /// <remarks>The rule reads its own name here, and the justification below covers that too.</remarks>
    [SuppressMessage(SonarRule.S1135.Category, SonarRule.S1135.Id,
                     Justification = S1135.DocumentsTheMarkerTheToolEmits)]
    internal static class S1135 {

        /// <summary>
        ///     The rule looks for unfinished work a developer left behind. These occurrences are the opposite:
        ///     documentation of a marker this tool <b>emits on purpose</b>. §5.5 has an unresolved parameter
        ///     become a <c>TODO(dum)</c> comment and an identifier that does not exist, so the developer's own
        ///     build reports it — that refusal to guess is the feature (ADR-0060), and naming it anything other
        ///     than what it emits would make the documentation harder to search than the code it describes.
        ///     <para>
        ///         Suppressed rather than reworded, and shared rather than repeated, because the vocabulary
        ///         recurs wherever the emitted marker is discussed: the plan that counts them, the file that
        ///         carries them, and the console recap that reports them.
        ///     </para>
        /// </summary>
        internal const string DocumentsTheMarkerTheToolEmits =
            "Documents the TODO marker the tool emits by design (§5.5), not unfinished work here. See the constant's summary.";

    }

}
