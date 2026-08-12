namespace JustDummies;

/// <summary>
///     Implemented by the library's own generators that draw from a <b>small, countable</b> domain, so a distinct
///     collection (<see cref="AnySet{T}" />, <c>ListOf(...).Distinct()</c>, a dictionary's keys) can tell — at
///     declaration time — whether a requested count, together with any values pinned through <c>Containing(...)</c>,
///     can be satisfied from the effective domain, and fail eagerly with a
///     <see cref="ConflictingAnyConstraintException" /> instead of only discovering it while drawing.
/// </summary>
/// <remarks>
///     The two members travel together on purpose — that is the whole point of putting them on one interface:
///     <see cref="DistinctCardinality" /> answers "<i>how many</i> distinct values can the generator produce" (a
///     conservative <b>upper</b> bound), and <see cref="Contains" /> answers "<i>is this one</i> of them". A distinct
///     collection needs both: the size to gate the count, and membership to tell a contained value that
///     <i>extends</i> the domain (one the generator could never draw) from one already inside it. Because they are a
///     single contract, a generator cannot advertise a cardinality without also answering membership — the compiler
///     keeps the promise, so no generator can drift out of the eager perimeter unnoticed.
///     <para>
///         A generator whose domain is unbounded, effectively unbounded, or simply unknown (a foreign
///         <see cref="IAny{T}" />, a derived generator) does not implement this interface; the collection then relies
///         on the bounded dedup-draw fallback, which surfaces a genuine shortfall as an
///         <see cref="AnyGenerationException" />.
///     </para>
///     <para>
///         Both answers are given under <see cref="EqualityComparer{T}.Default" />, and neither survives a collection
///         carrying its own <see cref="IEqualityComparer{T}" /> unaided. <see cref="Contains" /> does not — a comparer
///         <i>stricter</i> than the default one (reference equality over a type with value equality) keeps apart
///         values this membership calls the same, so a value it reports as inside the domain may be one the
///         collection would count as extending it. A collection carrying a custom comparer therefore treats every
///         pinned value as outside, which can only defer to the bounded dedup-draw.
///     </para>
///     <para>
///         <see cref="DistinctCardinality" /> survives <b>almost</b> always, and the exception is worth stating
///         because the obvious reasoning for it is wrong. That reasoning runs: a bound is an upper bound, and no
///         comparer can make a generator yield more distinct values than it has. It holds while the default comparer
///         is the finest equality the type admits — a comparer can then only merge values, never split them. It fails
///         when the BCL defines an equality <b>coarser than the type's own representation</b>: two
///         <see cref="DateTimeOffset" /> spellings of one instant are equal and hash alike, and a comparer built on
///         <c>EqualsExact</c> tells them apart again, so a generator drawing one instant across a range of offsets
///         yields values a finer comparer counts as distinct while a bound counted in instants says one. A generator
///         in that position declares <see cref="IComparerSensitiveCardinality{T}" /> and answers for the comparer in
///         force; every other one is asked, and answers, exactly as before. Either way the collection never refuses a
///         specification the comparer makes satisfiable.
///     </para>
/// </remarks>
/// <typeparam name="T">The element type.</typeparam>
internal interface ICardinalityHint<T> {

    /// <summary>The number of distinct values the generator can produce, or <c>null</c> when that is unbounded or unknown.</summary>
    long? DistinctCardinality { get; }

    /// <summary>Whether the generator, as constrained, could ever produce <paramref name="value" />.</summary>
    /// <param name="value">The candidate value.</param>
    /// <returns><c>true</c> when <paramref name="value" /> is within the generator's domain; otherwise <c>false</c>.</returns>
    bool Contains(T value);

}
