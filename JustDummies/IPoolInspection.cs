namespace JustDummies;

/// <summary>
///     What a generator's declared constraints left of a caller-supplied value set: the values still drawable, and
///     the ones the constraints refuse together with the constraint that refuses each. It answers the question a
///     narrowed catalogue leaves open — widen the invariant, or fix the catalogue? — and it is reached by an
///     explicit cast, never from the fluent surface (ADR-0067).
/// </summary>
/// <remarks>
///     <para>
///         Generators implement this <b>explicitly</b>, so no completion list shows it to a caller writing
///         constraints: inspecting a recipe is stepping outside the contract the rest of the surface teaches, which
///         is that a recipe's output is a value, and the cast is what states that intent at the call site.
///     </para>
///     <para>
///         The interface is <b>optional</b>. It is carried by the generators whose pool a caller supplies whole —
///         <see cref="AnyString" /> and <see cref="AnyOneOf{T}" /> — so a cast is written as a test
///         (<c>if (generator is IPoolInspection&lt;string&gt; pool)</c>) rather than assumed to succeed.
///     </para>
///     <para>
///         Nothing here draws. The domain is fixed the moment the constraints are declared, so every member returns
///         the same answer on every call and under every seed, and none of them consumes randomness or advances a
///         sequence — an inspection between two draws leaves a seeded run replaying exactly as it would have.
///     </para>
/// </remarks>
/// <typeparam name="T">The type of the pooled values.</typeparam>
public interface IPoolInspection<T> {

    /// <summary>
    ///     Whether a value set is in force — <c>false</c> for a generator that builds its value rather than picking
    ///     it from supplied values, which has a pool neither to keep nor to reject from.
    /// </summary>
    bool IsPooled { get; }

    /// <summary>
    ///     The supplied values that satisfy every declared constraint — the exact domain a draw picks from, in the
    ///     order they were supplied, with duplicates already collapsed. Empty when <see cref="IsPooled" /> is
    ///     <c>false</c>; never empty otherwise, since a value set left with nothing is refused at declaration.
    /// </summary>
    /// <returns>The surviving values.</returns>
    IReadOnlyList<T> GetSurvivors();

    /// <summary>
    ///     The supplied values no draw can ever yield, each with the declared constraints that refuse it. Empty when
    ///     <see cref="IsPooled" /> is <c>false</c>, and empty when every supplied value survived — which is what a
    ///     catalogue in step with its invariants looks like.
    /// </summary>
    /// <returns>The rejected values and their reasons.</returns>
    IReadOnlyList<PoolRejection<T>> GetRejections();

}
