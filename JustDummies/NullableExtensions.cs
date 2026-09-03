namespace JustDummies;

/// <summary>
///     The two ways a value-type generator becomes a generator of <see cref="Nullable{T}" />, and they are not the
///     same thing. <see cref="OrNull{T}" /> yields <c>null</c> on an even coin flip and, otherwise, a value
///     satisfying the constraints declared upstream — the dummy for an optional value-type field (<c>int?</c>,
///     <c>DateTime?</c>, <c>Guid?</c>, an enum, ...). <see cref="AsNullable{T}" /> never yields <c>null</c>: it
///     widens the type and leaves the values alone, for a parameter that is spelled nullable and still has to be
///     given one.
/// </summary>
public static class NullableExtensions {

    /// <summary>
    ///     How many equiprobable outcomes the null-versus-value draw picks between — two, which is what makes
    ///     <c>null</c> come up about half the time. Shared with
    ///     <see cref="NullableReferenceExtensions.OrNull{T}" /> so the two siblings cannot drift to different rates.
    /// </summary>
    internal const int NullDrawOutcomes = 2;

    /// <summary>
    ///     Derives a generator that yields <c>null</c> about half the time and, otherwise, a value drawn from
    ///     <paramref name="generator" /> — so a test exercises both the present and the absent case without pinning
    ///     either. Reproducible under a seed, like every other draw.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The null-versus-value decision draws from the same random context as the wrapped generator, so an
    ///         <c>Dummy.Reproducibly(...)</c> run replays it exactly. A <c>null</c> draw does not consume a value from
    ///         the wrapped generator.
    ///     </para>
    ///     <example>
    ///         <code>
    ///         int? discount = Dummy.Int32().Between(0, 100).OrNull().Generate();
    ///         </code>
    ///     </example>
    /// </remarks>
    /// <param name="generator">The generator of the non-null values.</param>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <returns>A generator of <see cref="Nullable{T}" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="generator" /> is <c>null</c>.</exception>
    public static IDummy<T?> OrNull<T>(this IDummy<T> generator)
        where T : struct {
        if (generator is null) { throw new ArgumentNullException(nameof(generator)); }

        RandomSource? source       = DummyDerivation.SourceOf(generator);
        bool          reproducible = DummyDerivation.IsReproducible(generator);

        return new DerivedDummy<T?>(source, reproducible, () => {
            RandomSource working = source ?? AmbientRandomSource.Instance;

            return working.Current.Next(NullDrawOutcomes) == 0 ? (T?)null : generator.Generate();
        });
    }

    /// <summary>
    ///     Widens <paramref name="generator" /> to <see cref="Nullable{T}" /> without changing what it draws — the
    ///     same values, the wider type, and never <c>null</c>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The sibling of <see cref="OrNull{T}" /> and its opposite: <c>OrNull</c> is for a value that may be
    ///         absent, this is for one that is merely spelled nullable. It is what a scaffolded generator writes
    ///         for a nullable parameter, since a dummy the code under test needs is never absent (ADR-0064).
    ///     </para>
    ///     <para>
    ///         Unlike the general <c>As(value =&gt; (T?)value)</c> this replaces, the result keeps whatever the
    ///         wrapped generator knows about how many distinct values it can produce. A distinct collection —
    ///         <c>Dummy.SetOf(...)</c>, a dictionary's keys — therefore gates its size on the underlying domain
    ///         instead of drawing a count that domain cannot fill: <c>Dummy.SetOf(Dummy.Enum&lt;Slot&gt;().AsNullable())</c>
    ///         behaves exactly as <c>Dummy.SetOf(Dummy.Enum&lt;Slot&gt;())</c> does.
    ///     </para>
    ///     <example>
    ///         <code>
    ///         ISet&lt;Slot?&gt; slots = Dummy.SetOf(Dummy.Enum&lt;Slot&gt;().AsNullable()).NonEmpty().Generate();
    ///         </code>
    ///     </example>
    /// </remarks>
    /// <param name="generator">The generator of the values.</param>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <returns>A generator of <see cref="Nullable{T}" /> that never yields <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="generator" /> is <c>null</c>.</exception>
    public static IDummy<T?> AsNullable<T>(this IDummy<T> generator)
        where T : struct {
        if (generator is null) { throw new ArgumentNullException(nameof(generator)); }

        return new NullableDummy<T>(generator);
    }

}
