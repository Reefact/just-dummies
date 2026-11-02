namespace JustDummies.Analyzers;

/// <summary>
///     The keys a rule attaches to a diagnostic for the code fix that answers it (ADR-0130). A fix reads only what the
///     rule hands it this way, so the fix rewrites exactly what the rule established and guesses nothing.
/// </summary>
public static class DiagnosticProperties {

    /// <summary>Set on a JD014 that reports a transposed pair, whose fix swaps the two arguments.</summary>
    public const string Transposed = "Transposed";

    /// <summary>On a JD031, the span of the minimum's call, as <c>start,length</c>.</summary>
    public const string Minimum = "Minimum";

    /// <summary>On a JD031, the span of the maximum's call, as <c>start,length</c>.</summary>
    public const string Maximum = "Maximum";

    /// <summary>On a JD031, the name of the range form that replaces the pair.</summary>
    public const string Range = "Range";

    /// <summary>On a JD032, the span of the looser of the two calls, as <c>start,length</c>, when both arguments fold.</summary>
    public const string Looser = "Looser";

}
