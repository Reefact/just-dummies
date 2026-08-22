namespace JustDummies;

/// <summary>
///     Makes a value-type generator optionally <c>null</c>: <see cref="OrNull{T}" /> turns an
///     <see cref="IAny{T}" /> into an <see cref="IAny{T}" /> of <see cref="Nullable{T}" /> that yields
///     <c>null</c> on an even coin flip and, otherwise, a value satisfying the constraints declared upstream — the
///     dummy for an optional value-type field (<c>int?</c>, <c>DateTime?</c>, <c>Guid?</c>, an enum, ...).
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
    ///         <c>Any.Reproducibly(...)</c> run replays it exactly. A <c>null</c> draw does not consume a value from
    ///         the wrapped generator.
    ///     </para>
    ///     <example>
    ///         <code>
    ///         int? discount = Any.Int32().Between(0, 100).OrNull().Generate();
    ///         </code>
    ///     </example>
    /// </remarks>
    /// <param name="generator">The generator of the non-null values.</param>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <returns>A generator of <see cref="Nullable{T}" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="generator" /> is <c>null</c>.</exception>
    public static IAny<T?> OrNull<T>(this IAny<T> generator)
        where T : struct {
        if (generator is null) { throw new ArgumentNullException(nameof(generator)); }

        RandomSource? source       = AnyDerivation.SourceOf(generator);
        bool          reproducible = AnyDerivation.IsReproducible(generator);

        return new DerivedAny<T?>(source, reproducible, () => {
            RandomSource working = source ?? AmbientRandomSource.Instance;

            return working.Current.Next(NullDrawOutcomes) == 0 ? (T?)null : generator.Generate();
        });
    }

}
