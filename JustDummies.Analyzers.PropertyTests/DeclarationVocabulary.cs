namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     A generator a <see cref="DeclarationChain" /> is built on: its factory, as C# source and as the call the run time
///     executes, and the calls the chain draws from.
/// </summary>
internal sealed class DeclarationVocabulary {

    private readonly Func<AnyContext, object> _create;

    public DeclarationVocabulary(string factory, Func<AnyContext, object> create, params DeclarationCall[] calls) {
        Factory = factory;
        Calls   = calls;
        _create = create;
    }

    /// <summary>The factory as C# source — <c>Any.Uri().Web()</c>.</summary>
    public string Factory { get; }

    public IReadOnlyList<DeclarationCall> Calls { get; }

    /// <summary>The generator the factory builds, drawing from <paramref name="context" />.</summary>
    public object Create(AnyContext context) {
        return _create(context);
    }

    /// <inheritdoc />
    public override string ToString() {
        return Factory;
    }

}
