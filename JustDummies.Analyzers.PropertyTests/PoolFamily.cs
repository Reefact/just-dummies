namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     A numeric generator a <see cref="PoolChain" /> is built on: its factory, the pooled values it may write, and the
///     constraints it may declare beside them — each carried as C# source and as what the run time executes.
/// </summary>
internal sealed class PoolFamily {

    private readonly Func<AnyContext, object>       _create;
    private readonly Func<object, object[], object> _oneOf;
    private readonly Action<object>                 _generate;

    private PoolFamily(string factory, Func<AnyContext, object> create, Func<object, object[], object> oneOf, Action<object> generate, IReadOnlyList<PoolElement> elements, IReadOnlyList<DeclarationCall> constraints) {
        Factory     = factory;
        Elements    = elements;
        Constraints = constraints;
        _create     = create;
        _oneOf      = oneOf;
        _generate   = generate;
    }

    /// <summary>The factory as C# source — <c>Any.UInt64()</c>.</summary>
    public string Factory { get; }

    public IReadOnlyList<PoolElement> Elements { get; }

    public IReadOnlyList<DeclarationCall> Constraints { get; }

    public static PoolFamily Of<TGenerator, TValue>(string factory,
                                                    Func<AnyContext, TGenerator> create,
                                                    Func<TGenerator, TValue[], TGenerator> oneOf,
                                                    (string Source, TValue Value)[] elements,
                                                    params DeclarationCall[] constraints) where TGenerator : IAny<TValue> where TValue : notnull {
        return new PoolFamily(factory,
                              context => create(context),
                              (generator, values) => oneOf((TGenerator)generator, values.Cast<TValue>().ToArray()),
                              generator => ((TGenerator)generator).Generate(),
                              elements.Select(element => new PoolElement(element.Source, element.Value)).ToList(),
                              constraints);
    }

    public object Create(AnyContext context) {
        return _create(context);
    }

    public object OneOf(object generator, IEnumerable<PoolElement> elements) {
        return _oneOf(generator, elements.Select(element => element.Value).ToArray());
    }

    /// <summary>Draws once, which is where a refusal the declarations deferred surfaces.</summary>
    public void Generate(object generator) {
        _generate(generator);
    }

    /// <inheritdoc />
    public override string ToString() {
        return Factory;
    }

}

/// <summary>One value a pool may hold, as C# source — <c>ulong.MaxValue</c> — and as the value the run time pools.</summary>
internal sealed record PoolElement(string Source, object Value) {

    /// <inheritdoc />
    public override string ToString() {
        return Source;
    }

}
