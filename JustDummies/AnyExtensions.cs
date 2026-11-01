namespace JustDummies;

/// <summary>
///     Composition over <see cref="IAny{T}" />. These extensions are the bridge between constrained primitives and
///     domain types: a generator of raw values becomes a generator of value objects by going through the type's own
///     factory — no reflection, and the domain's validation stays the single gatekeeper.
/// </summary>
public static class AnyExtensions {

    /// <summary>
    ///     Derives a generator of <typeparamref name="TResult" /> by passing each generated
    ///     <typeparamref name="TSource" /> through <paramref name="factory" /> — typically a value object's own
    ///     factory method, so the constraints declared upstream express the invariant that factory enforces.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the factory throws, the failure is wrapped in an <see cref="AnyGenerationException" /> naming the
    ///         generated value and, when known, the seed that replays the run — the usual cause is constraints weaker
    ///         than the invariant the factory enforces, and the fix is to tighten them.
    ///     </para>
    ///     <example>
    ///         <code>
    ///         IAny&lt;OrderReference&gt; reference = Any.String()
    ///             .StartingWith("ORD-")
    ///             .WithLength(12)
    ///             .As(OrderReference.Create);
    ///         </code>
    ///     </example>
    /// </remarks>
    /// <param name="generator">The generator of the raw values.</param>
    /// <param name="factory">The factory turning a raw value into a <typeparamref name="TResult" />.</param>
    /// <typeparam name="TSource">The type of the raw generated values.</typeparam>
    /// <typeparam name="TResult">The type the factory produces.</typeparam>
    /// <returns>A generator of <typeparamref name="TResult" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="generator" /> or <paramref name="factory" /> is <c>null</c>.</exception>
    public static IAny<TResult> As<TSource, TResult>(this IAny<TSource> generator, Func<TSource, TResult> factory) {
        if (generator is null) { throw new ArgumentNullException(nameof(generator)); }
        if (factory is null) { throw new ArgumentNullException(nameof(factory)); }

        RandomSource? source       = AnyDerivation.SourceOf(generator);
        bool          reproducible = AnyDerivation.IsReproducible(generator);

        return new DerivedAny<TResult>(source, reproducible, () => {
            TSource value = generator.Generate();

            return AnyDerivation.Invoke(() => factory(value), source, reproducible, () => $"the factory passed to As(...) threw for the generated value {AnyDerivation.Display(value)}");
        });
    }


    /// <summary>
    ///     Derives a generator whose value depends on another draw: it draws a value from <paramref name="generator" />,
    ///     builds the generator <paramref name="next" /> returns for it, and draws from that one — a grid's cell drawn
    ///     inside the grid's drawn dimensions, a branch chosen by a drawn flag. Only the generator built for the drawn
    ///     value is asked for one, so choosing among generators is <c>Any.Boolean().Then(b =&gt; b ? first : second)</c>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Both draws come from the same source, so the whole chain replays from one seed. Under the ambient source
    ///         that is the ambient seed; under an explicit <see cref="Any.WithSeed" /> context, a generator built from the
    ///         static <see cref="Any" /> inside <paramref name="next" /> draws from that context too, for the length of
    ///         the dependent draw — the case a <c>Generate()</c> written inside <c>As(...)</c> would split across two
    ///         seeds. A generator built from another context, or one of your own drawing through neither, keeps its own
    ///         source.
    ///     </para>
    ///     <para>
    ///         It is a sequential draw, correct by construction: the second value is drawn knowing the first, never
    ///         searched for. A relation <paramref name="next" /> cannot state as a generator is still the test's to write
    ///         (ADR-0046, ADR-0119).
    ///     </para>
    /// </remarks>
    /// <param name="generator">The generator of the value the next draw depends on.</param>
    /// <param name="next">Builds, from that value, the generator the result is drawn from.</param>
    /// <typeparam name="TSource">The type of the value the next draw depends on.</typeparam>
    /// <typeparam name="TResult">The type of the generated values.</typeparam>
    /// <returns>A generator of <typeparamref name="TResult" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="generator" /> or <paramref name="next" /> is <c>null</c>.</exception>
    public static IAny<TResult> Then<TSource, TResult>(this IAny<TSource> generator, Func<TSource, IAny<TResult>> next) {
        if (generator is null) { throw new ArgumentNullException(nameof(generator)); }
        if (next is null) { throw new ArgumentNullException(nameof(next)); }

        RandomSource? source       = AnyDerivation.SourceForADrawOver(generator);
        bool          reproducible = AnyDerivation.IsReproducible(generator);

        return new DerivedAny<TResult>(source, reproducible, () => {
            TSource       value = generator.Generate();
            IAny<TResult> built = AnyDerivation.Invoke(() => next(value) ?? throw new InvalidOperationException("Then(...) returned no generator."),
                                                       source, reproducible,
                                                       () => $"the function passed to Then(...) threw for the generated value {AnyDerivation.Display(value)}");

            if (source is not FixedRandomSource context) { return built.Generate(); }

            using (AmbientRandomSource.Borrow(context.Current, context.ReplaySnippet)) { return built.Generate(); }
        });
    }

    /// <summary>
    ///     Derives a generator that never yields <paramref name="value" />: it redraws while <paramref name="generator" />
    ///     produces it — a second value of a type that must differ from a first, on any generator, a scaffolded or
    ///     hand-written one included.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is the <b>rejective</b> exclusion, and it differs from the <c>DifferentFrom</c> the typed builders
    ///         carry. Theirs is constructive: an exclusion in the builder's own specification, checked when it is
    ///         declared, so <c>Any.Int32().OneOf(5).DifferentFrom(5)</c> is refused at once, naming both constraints,
    ///         and the analyzers see it at build time. This one sees nothing before <see cref="IAny{T}.Generate" />,
    ///         then redraws a bounded number of times and fails with its seed when every draw produced the excluded
    ///         value — at once for a generator pinned to it (ADR-0121).
    ///     </para>
    ///     <para>
    ///         C# picks between the two from the receiver's <b>static</b> type: on a typed builder the builder's own wins,
    ///         and on an expression typed <see cref="IAny{T}" /> this one is called. Values are compared with
    ///         <see cref="EqualityComparer{T}.Default" />. It returns an <see cref="IAny{T}" />, so it goes last in a chain,
    ///         like <c>As</c> and <c>OrNull</c>.
    ///     </para>
    /// </remarks>
    /// <param name="generator">The generator whose draws must differ from <paramref name="value" />.</param>
    /// <param name="value">The value never yielded; <c>null</c> is accepted.</param>
    /// <typeparam name="T">The type of the generated values.</typeparam>
    /// <returns>A generator of values different from <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="generator" /> is <c>null</c>.</exception>
    public static IAny<T> DifferentFrom<T>(this IAny<T> generator, T value) {
        if (generator is null) { throw new ArgumentNullException(nameof(generator)); }

        return new DifferentFromAny<T>(generator, value);
    }
}
