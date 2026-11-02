namespace JustDummies.Analyzers;

/// <summary>
///     A statically-known temporal value: its type, its ordinal — the ticks or the day number the run time compares
///     by — and the text the run time renders it with, so a message can quote the call as the exception would.
/// </summary>
internal sealed class TemporalConstant {

    internal TemporalConstant(TemporalKind kind, long ordinal, string rendered) {
        Kind     = kind;
        Ordinal  = ordinal;
        Rendered = rendered;
    }

    public TemporalKind Kind { get; }

    /// <summary>The ticks of a <c>DateTime</c>, a <c>TimeOnly</c> or a <c>TimeSpan</c>, the UTC ticks of a <c>DateTimeOffset</c>, the day number of a <c>DateOnly</c>.</summary>
    public long Ordinal { get; }

    /// <summary>The value as the run time writes it in a constraint call — the <c>O</c> form of the dates, the <c>c</c> form of a duration.</summary>
    public string Rendered { get; }

}
