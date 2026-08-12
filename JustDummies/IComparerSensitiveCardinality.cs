namespace JustDummies;

/// <summary>
///     Declared in place of <see cref="ICardinalityHint{T}" />, which it extends, by the rare generator whose
///     <see cref="ICardinalityHint{T}.DistinctCardinality" /> a <b>finer</b> comparer can exceed, so a distinct
///     collection carrying its own <see cref="IEqualityComparer{T}" /> asks for a bound that holds under it rather
///     than trusting one measured under <see cref="EqualityComparer{T}.Default" />.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="ICardinalityHint{T}" /> treats its bound as surviving any comparer, on the reasoning that a
///         comparer can only <i>merge</i> values, never split them. That reasoning holds while the default comparer
///         is the finest equality the type admits — which is the ordinary case, and why 25 of this library's 26
///         hint-bearing generators need nothing from this interface.
///     </para>
///     <para>
///         It fails when the BCL defines an equality <b>coarser than the type's own representation</b>.
///         <see cref="DateTimeOffset" /> is such a type: <c>Equals</c> compares the instant and ignores the offset,
///         so two spellings of one instant are equal — and a comparer built on <c>EqualsExact</c> tells them apart
///         again. A generator drawing one instant across a range of offsets then yields hundreds of values a finer
///         comparer counts as distinct, while a bound counted in instants says one. <see cref="DateTime" />
///         (<c>Equals</c> ignores <c>Kind</c>) and <see cref="decimal" /> (<c>1.0m == 1.00m</c>) share the shape but
///         not the consequence: neither generator draws a <i>range</i> over the redundant dimension, so one drawable
///         value has one spelling and their bounds hold.
///     </para>
///     <para>
///         Decision: ADR-0069. An extension of <see cref="ICardinalityHint{T}" /> rather than a second member on it, which the compiler
///         would then hold every implementer to: the answer is the same for all but one of them, and 26 identical
///         restatements would bury the one that differs. The trade is real — a future generator over a
///         coarsely-compared type could carry a bound and forget this interface — and it is paid for by naming the
///         condition here rather than by a check no tool performs.
///     </para>
/// </remarks>
/// <typeparam name="T">The element type.</typeparam>
internal interface IComparerSensitiveCardinality<T> : ICardinalityHint<T> {

    /// <summary>
    ///     The bound that holds when the collection deduplicates under a comparer of its own, or <c>null</c> when the
    ///     generator cannot count that far — which sends the collection to the bounded dedup-draw rather than to a
    ///     refusal.
    /// </summary>
    long? CardinalityUnderACustomComparer { get; }

}
