// The catalogue of JustDummies' own diagnostics: every JDxxx rule as compile-checked constants, so a
// consumer's [SuppressMessage] can name a rule the compiler resolves instead of a string nobody verifies
// (decision: ADR-0052).
//
// NOT generated at build time, and not a mirror. JustDummies.Analyzers reads its DiagnosticDescriptor
// arguments FROM these constants, which is the loop only a first-party catalogue can close: the rule the
// analyzer reports and the rule a consumer silences are then the same value by construction rather than two
// transcriptions of one string. Adding a rule starts here.

using DiagnosticCatalog;

namespace JustDummies.Diagnostics;

/// <summary>
///     The categories JustDummies groups its diagnostics under, declared once each.
/// </summary>
/// <remarks>
///     Reached only through the rule that carries it — write <c>JustDummiesRule.JD001.Category</c>, never the
///     category constant directly. The two spellings fold to the same string today and stop agreeing the day a
///     rule moves category: the rule member follows, a category named on its own does not, and the suppression
///     is left asserting a category the rule no longer carries.
/// </remarks>
[DiagnosticCategory]
public static class JustDummiesCategory {

    /// <summary>Rules about a run being replayable from the seed it reported.</summary>
    public const string Reproducibility = "JustDummies.Reproducibility";

    /// <summary>Rules about the recipe-versus-value distinction the library teaches: a generator is an immutable
    ///     recipe, and <c>Generate()</c> is the only thing that materializes a value from it.</summary>
    public const string Usage = "JustDummies.Usage";

    /// <summary>Rules that front-load, to build time, the subset of the library's run-time constraint checks that is
    ///     decidable from compile-time constants.</summary>
    public const string Constraints = "JustDummies.Constraints";

    /// <summary>Rules about assembling generators into bigger ones — what they share is that nothing goes wrong: the
    ///     composed generator builds, draws and returns a value. It is simply not the value the call site
    ///     describes.</summary>
    public const string Composition = "JustDummies.Composition";

}
