using System.Diagnostics.CodeAnalysis;

namespace JustDummies.GenAny;

/// <summary>
///     The justifications shared by several <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute" />
///     declarations, one nested class per analyzer rule. A justification lives here when it is <b>duplicated</b> —
///     the same fact suppressed at several sites, so the reasoning has one home and cannot drift into diverging
///     copies — or when it is long enough that leaving it inline would make the attribute unreadable: the
///     constant's value stays one crisp sentence while its <c>summary</c> carries the reasoning. The rule ids
///     themselves are always the catalogue constants (ADR-0050); these are only the texts.
/// </summary>
internal static class SuppressionJustification {

    /// <summary>Justifications for S1135 — "Complete the task associated to this 'TODO' comment".</summary>
    /// <remarks>The rule reads its own name here, and the justification below covers that too.</remarks>
    [SuppressMessage(SonarRule.S1135.Category, SonarRule.S1135.Id, Justification = S1135.DocumentsTheMarkerTheToolEmits)]
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

    /// <summary>Justifications for S2342 — "Enumeration types should comply with a naming convention".</summary>
    internal static class S2342 {

        /// <summary>
        ///     The specification names this concept in the singular (§6, "provenance"), and the property that carries
        ///     it is <c>Provenance</c>; a plural type name would make every use site read <c>Provenances Provenance</c>.
        /// </summary>
        internal const string TheSpecificationNamesItSingular = "The specification names the concept in the singular (§6), and a plural type would make every use site read `Provenances Provenance`. See the constant's summary.";

    }

}
