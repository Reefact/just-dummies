namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     One call of the <c>Any.String()</c> DSL, carried twice: as the C# text the analyzer reads and as the call the
///     run time executes. The two are written side by side so a call cannot mean one thing to the rule and another
///     to the oracle.
/// </summary>
internal sealed class StringChainCall {

    private readonly Func<AnyString, AnyString> _apply;

    public StringChainCall(string source, Func<AnyString, AnyString> apply, StringChainSlot slot) {
        Source = source;
        Slot   = slot;
        _apply = apply;
    }

    /// <summary>The call as C# source — <c>WithLength(3)</c>, its literals escaped.</summary>
    public string Source { get; }

    /// <summary>The once-per-generator slot the call occupies, if any.</summary>
    public StringChainSlot Slot { get; }

    public AnyString ApplyTo(AnyString generator) {
        return _apply(generator);
    }

    /// <inheritdoc />
    public override string ToString() {
        return Source;
    }

}
