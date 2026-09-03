namespace JustDummies;

/// <summary>
///     A recipe for an arbitrary value of type <typeparamref name="T" /> that satisfies the constraints declared on
///     it. This is the composition seam of the library: every generator — built-in or derived through
///     <see cref="DummyExtensions.As{TSource,TResult}" /> and <see cref="Dummy.Combine{T1,T2,TResult}" /> — implements it,
///     so a constrained primitive, a value object built from one, and an object assembled from several all flow
///     through the same contract.
/// </summary>
/// <remarks>
///     <para>
///         A generator is an <b>immutable recipe</b>, not a value: each fluent constraint returns a new generator, and
///         randomness is drawn only when <see cref="Generate" /> runs, from the random context the generator was
///         created with — the ambient context for the static <see cref="Dummy" /> entry points (see
///         <see cref="Dummy.Reproducibly(Action, Action{string})" />), or the isolated context of
///         <see cref="Dummy.WithSeed" />. The same recipe can therefore be generated from several times, yielding a
///         fresh value each time.
///     </para>
///     <para>
///         <see cref="Generate" /> is the single operation that materializes a value: the concrete generators expose
///         no implicit conversion to their generated type, so a value is produced only by an explicit
///         <see cref="Generate" /> call — directly, or through the composition seams
///         <see cref="DummyExtensions.As{TSource,TResult}" /> and <see cref="Dummy.Combine{T1,T2,TResult}" />, which call
///         it internally. Generic inference likewise flows through this interface — <c>Materialize(Dummy.String().NonEmpty())</c>
///         infers <c>T = string</c>.
///     </para>
/// </remarks>
/// <typeparam name="T">The type of the generated values.</typeparam>
public interface IDummy<out T> {

    /// <summary>
    ///     Produces one arbitrary value satisfying every constraint declared on this generator.
    /// </summary>
    /// <remarks>
    ///     A built-in generator is safe to call concurrently: its draw on a random context is serialized, so no
    ///     amount of parallelism can corrupt the source or produce a value outside the declared constraints. That
    ///     covers the library's own draw only — a call may also run caller-supplied code (a factory passed to
    ///     <see cref="DummyExtensions.As{TSource,TResult}" />, a composer passed to
    ///     <see cref="Dummy.Combine{T1,T2,TResult}" />, an element generator, a comparer) or a foreign
    ///     <see cref="IDummy{T}" /> implementation, whose thread-safety is the caller's own responsibility.
    ///     Reproducibility is the separate cost of parallelism: concurrent draws interleave, so a seed replays a
    ///     run only while its draws are taken one at a time. To keep a parallel run reproducible, open a scope per
    ///     unit of work with <see cref="Dummy.UseSeed(int)" /> and derive its seed from the run's own.
    /// </remarks>
    /// <returns>A value that satisfies the declared constraints.</returns>
    /// <exception cref="DummyGenerationException">
    ///     Thrown when the value cannot be produced even though the declared constraints were accepted — for example
    ///     when a factory passed to <see cref="DummyExtensions.As{TSource,TResult}" /> rejects a generated value.
    /// </exception>
    T Generate();

}
