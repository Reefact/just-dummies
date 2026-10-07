namespace JustDummies;

/// <summary>
///     Makes a reference-type generator optionally <c>null</c> — the sibling of
///     <see cref="NullableExtensions.OrNull{T}" /> for the reference-type case (a nullable string, or an optional
///     value object produced through <c>As</c>). It lives in its own class because a single overloaded
///     <c>OrNull</c> constrained once to <c>struct</c> and once to <c>class</c> would collide.
/// </summary>
public static class NullableReferenceExtensions {

    /// <summary>
    ///     Derives a generator that yields <c>null</c> about half the time and, otherwise, a value drawn from
    ///     <paramref name="generator" /> — the dummy for an optional reference-type field.
    /// </summary>
    /// <remarks>
    ///     The null-versus-value decision draws from the same random context as the wrapped generator, so a
    ///     reproducible run replays it exactly; a <c>null</c> draw does not consume a value from the wrapped generator.
    ///     <para>
    ///         The result counts <c>null</c> as one additional value in the wrapped generator's domain, so a distinct
    ///         collection over it gates its size on that domain: <c>Any.SetOf(Any.String().OneOf("EUR", "USD").OrNull())</c>
    ///         holds at most the two codes and <c>null</c>, and never asks for more.
    ///     </para>
    /// </remarks>
    /// <param name="generator">The generator of the non-null values.</param>
    /// <typeparam name="T">The underlying reference type.</typeparam>
    /// <returns>A generator that is sometimes <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="generator" /> is <c>null</c>.</exception>
    public static IAny<T?> OrNull<T>(this IAny<T> generator)
        where T : class {
        if (generator is null) { throw new ArgumentNullException(nameof(generator)); }

        return new OrNullReferenceAny<T>(generator);
    }

}
