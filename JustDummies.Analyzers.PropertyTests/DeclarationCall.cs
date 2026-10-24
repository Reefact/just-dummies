namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     One call of a generator's DSL, carried twice: as the C# text the analyzer reads and as the call the run time
///     executes, written side by side as in <see cref="StringChainCall" />. The generator is held as an
///     <see cref="object" /> so one chain type serves every generator; <see cref="On{TGenerator}" /> restores the type.
/// </summary>
internal sealed class DeclarationCall {

    private readonly Func<object, object> _apply;

    private DeclarationCall(string source, Func<object, object> apply) {
        Source = source;
        _apply = apply;
    }

    /// <summary>The call as C# source — <c>WithPort(80)</c>.</summary>
    public string Source { get; }

    public static DeclarationCall On<TGenerator>(string source, Func<TGenerator, TGenerator> apply) where TGenerator : notnull {
        return new DeclarationCall(source, generator => apply((TGenerator)generator));
    }

    public object ApplyTo(object generator) {
        return _apply(generator);
    }

    /// <inheritdoc />
    public override string ToString() {
        return Source;
    }

}
