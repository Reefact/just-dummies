namespace JustDummies.GenAny.UnitTests.Sweep;

/// <summary>
///     One shape's row in the report: the seven columns the August survey printed, plus the verdict.
/// </summary>
/// <remarks>
///     The seven are kept, in order and under their old names, so the two surveys can be joined line by line
///     — which is the only way to tell what a rebuilt bench changed from what the engine changed. The eighth
///     is what the old one did not have: a verdict computed from rules stated in advance, rather than left
///     for a reader to infer from a message.
/// </remarks>
internal sealed class SweepOutcome(string name,
                                   string family,
                                   string status,
                                   string provenance,
                                   string compiles,
                                   string rules,
                                   string draw,
                                   SweepVerdict verdict,
                                   string? reason = null) {

    internal string Name { get; } = name;

    internal string Family { get; } = family;

    /// <summary>What <c>Scaffolder.Scaffold</c> answered.</summary>
    internal string Status { get; } = status;

    /// <summary>Each parameter and the provenance the engine gave it — <c>items=Guard</c>.</summary>
    internal string Provenance { get; } = provenance;

    /// <summary><c>ok</c>, or the errors the emitted file produced.</summary>
    internal string Compiles { get; } = compiles;

    /// <summary><c>ok</c>, the rules raised, or <c>-</c> when compilation did not get that far.</summary>
    internal string Rules { get; } = rules;

    /// <summary><c>ok</c>, what stopped the draw, or <c>-</c>.</summary>
    internal string Draw { get; } = draw;

    internal SweepVerdict Verdict { get; } = verdict;

    /// <summary>Why the verdict is a finding or a sweep bug, in one sentence a reader can act on.</summary>
    internal string? Reason { get; } = reason;

    /// <summary>The row, tab separated, in the column order the August survey used.</summary>
    internal string ToRow() {
        return string.Join("\t", Name, Family, Status, Provenance, Compiles, Rules, Draw, Verdict, Reason ?? "-");
    }

    /// <inheritdoc />
    public override string ToString() {
        return Reason is null ? $"{Name}: {Verdict}" : $"{Name}: {Verdict} — {Reason}";
    }

}
