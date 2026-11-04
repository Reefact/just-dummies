namespace JustDummies;

/// <summary>
///     The relations an <see cref="AnyPair{T}" /> can declare through <see cref="AnyPair{T}.Having" />, one set per
///     ordered type and spelled in that type's builder vocabulary: <c>LessThan</c> and <c>GreaterThan</c> on the
///     numbers and durations, <c>Before</c> and <c>After</c> on the dates. Each is named from <c>Item1</c> to
///     <c>Item2</c>, so the subject of the relation is always written (ADR-0131).
/// </summary>
public static class AnyPairRelations {

    /// <summary>
    ///     Declares <c>Item1</c> strictly less than <c>Item2</c>: <c>Item1</c> is drawn from its base kept below the
    ///     largest value <c>Item2</c>'s base can produce, then <c>Item2</c> from its base kept above it.
    /// </summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<int> Item1LessThanItem2(this AnyPairRelation<int> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanItem2), item1Below: true, strict: true);
    }

    /// <summary>The <c>byte</c> form of <see cref="Item1LessThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<byte> Item1LessThanItem2(this AnyPairRelation<byte> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanItem2), item1Below: true, strict: true);
    }

    /// <summary>The <c>sbyte</c> form of <see cref="Item1LessThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<sbyte> Item1LessThanItem2(this AnyPairRelation<sbyte> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanItem2), item1Below: true, strict: true);
    }

    /// <summary>The <c>short</c> form of <see cref="Item1LessThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<short> Item1LessThanItem2(this AnyPairRelation<short> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanItem2), item1Below: true, strict: true);
    }

    /// <summary>The <c>ushort</c> form of <see cref="Item1LessThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<ushort> Item1LessThanItem2(this AnyPairRelation<ushort> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanItem2), item1Below: true, strict: true);
    }

    /// <summary>The <c>uint</c> form of <see cref="Item1LessThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<uint> Item1LessThanItem2(this AnyPairRelation<uint> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanItem2), item1Below: true, strict: true);
    }

    /// <summary>The <c>long</c> form of <see cref="Item1LessThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<long> Item1LessThanItem2(this AnyPairRelation<long> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanItem2), item1Below: true, strict: true);
    }

    /// <summary>The <c>ulong</c> form of <see cref="Item1LessThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<ulong> Item1LessThanItem2(this AnyPairRelation<ulong> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanItem2), item1Below: true, strict: true);
    }

    /// <summary>The <c>float</c> form of <see cref="Item1LessThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<float> Item1LessThanItem2(this AnyPairRelation<float> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanItem2), item1Below: true, strict: true);
    }

    /// <summary>The <c>double</c> form of <see cref="Item1LessThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<double> Item1LessThanItem2(this AnyPairRelation<double> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanItem2), item1Below: true, strict: true);
    }

    /// <summary>The <c>decimal</c> form of <see cref="Item1LessThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<decimal> Item1LessThanItem2(this AnyPairRelation<decimal> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanItem2), item1Below: true, strict: true);
    }

    /// <summary>The <c>TimeSpan</c> form of <see cref="Item1LessThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<TimeSpan> Item1LessThanItem2(this AnyPairRelation<TimeSpan> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanItem2), item1Below: true, strict: true);
    }

#if NET8_0_OR_GREATER
    /// <summary>The <c>Int128</c> form of <see cref="Item1LessThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<Int128> Item1LessThanItem2(this AnyPairRelation<Int128> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanItem2), item1Below: true, strict: true);
    }

#endif

#if NET8_0_OR_GREATER
    /// <summary>The <c>UInt128</c> form of <see cref="Item1LessThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<UInt128> Item1LessThanItem2(this AnyPairRelation<UInt128> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanItem2), item1Below: true, strict: true);
    }

#endif

#if NET8_0_OR_GREATER
    /// <summary>The <c>Half</c> form of <see cref="Item1LessThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<Half> Item1LessThanItem2(this AnyPairRelation<Half> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanItem2), item1Below: true, strict: true);
    }

#endif

    /// <summary>
    ///     Declares <c>Item1</c> less than or equal to <c>Item2</c>: <c>Item1</c> is drawn from its base kept at or below the
    ///     largest value <c>Item2</c>'s base can produce, then <c>Item2</c> from its base kept at or above it.
    /// </summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<int> Item1LessThanOrEqualToItem2(this AnyPairRelation<int> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanOrEqualToItem2), item1Below: true, strict: false);
    }

    /// <summary>The <c>byte</c> form of <see cref="Item1LessThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<byte> Item1LessThanOrEqualToItem2(this AnyPairRelation<byte> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanOrEqualToItem2), item1Below: true, strict: false);
    }

    /// <summary>The <c>sbyte</c> form of <see cref="Item1LessThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<sbyte> Item1LessThanOrEqualToItem2(this AnyPairRelation<sbyte> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanOrEqualToItem2), item1Below: true, strict: false);
    }

    /// <summary>The <c>short</c> form of <see cref="Item1LessThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<short> Item1LessThanOrEqualToItem2(this AnyPairRelation<short> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanOrEqualToItem2), item1Below: true, strict: false);
    }

    /// <summary>The <c>ushort</c> form of <see cref="Item1LessThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<ushort> Item1LessThanOrEqualToItem2(this AnyPairRelation<ushort> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanOrEqualToItem2), item1Below: true, strict: false);
    }

    /// <summary>The <c>uint</c> form of <see cref="Item1LessThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<uint> Item1LessThanOrEqualToItem2(this AnyPairRelation<uint> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanOrEqualToItem2), item1Below: true, strict: false);
    }

    /// <summary>The <c>long</c> form of <see cref="Item1LessThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<long> Item1LessThanOrEqualToItem2(this AnyPairRelation<long> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanOrEqualToItem2), item1Below: true, strict: false);
    }

    /// <summary>The <c>ulong</c> form of <see cref="Item1LessThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<ulong> Item1LessThanOrEqualToItem2(this AnyPairRelation<ulong> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanOrEqualToItem2), item1Below: true, strict: false);
    }

    /// <summary>The <c>float</c> form of <see cref="Item1LessThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<float> Item1LessThanOrEqualToItem2(this AnyPairRelation<float> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanOrEqualToItem2), item1Below: true, strict: false);
    }

    /// <summary>The <c>double</c> form of <see cref="Item1LessThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<double> Item1LessThanOrEqualToItem2(this AnyPairRelation<double> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanOrEqualToItem2), item1Below: true, strict: false);
    }

    /// <summary>The <c>decimal</c> form of <see cref="Item1LessThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<decimal> Item1LessThanOrEqualToItem2(this AnyPairRelation<decimal> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanOrEqualToItem2), item1Below: true, strict: false);
    }

    /// <summary>The <c>TimeSpan</c> form of <see cref="Item1LessThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<TimeSpan> Item1LessThanOrEqualToItem2(this AnyPairRelation<TimeSpan> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanOrEqualToItem2), item1Below: true, strict: false);
    }

#if NET8_0_OR_GREATER
    /// <summary>The <c>Int128</c> form of <see cref="Item1LessThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<Int128> Item1LessThanOrEqualToItem2(this AnyPairRelation<Int128> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanOrEqualToItem2), item1Below: true, strict: false);
    }

#endif

#if NET8_0_OR_GREATER
    /// <summary>The <c>UInt128</c> form of <see cref="Item1LessThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<UInt128> Item1LessThanOrEqualToItem2(this AnyPairRelation<UInt128> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanOrEqualToItem2), item1Below: true, strict: false);
    }

#endif

#if NET8_0_OR_GREATER
    /// <summary>The <c>Half</c> form of <see cref="Item1LessThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<Half> Item1LessThanOrEqualToItem2(this AnyPairRelation<Half> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1LessThanOrEqualToItem2), item1Below: true, strict: false);
    }

#endif

    /// <summary>
    ///     Declares <c>Item1</c> strictly greater than <c>Item2</c>: <c>Item2</c> is drawn from its base kept below the
    ///     largest value <c>Item1</c>'s base can produce, then <c>Item1</c> from its base kept above it.
    /// </summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<int> Item1GreaterThanItem2(this AnyPairRelation<int> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanItem2), item1Below: false, strict: true);
    }

    /// <summary>The <c>byte</c> form of <see cref="Item1GreaterThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<byte> Item1GreaterThanItem2(this AnyPairRelation<byte> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanItem2), item1Below: false, strict: true);
    }

    /// <summary>The <c>sbyte</c> form of <see cref="Item1GreaterThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<sbyte> Item1GreaterThanItem2(this AnyPairRelation<sbyte> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanItem2), item1Below: false, strict: true);
    }

    /// <summary>The <c>short</c> form of <see cref="Item1GreaterThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<short> Item1GreaterThanItem2(this AnyPairRelation<short> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanItem2), item1Below: false, strict: true);
    }

    /// <summary>The <c>ushort</c> form of <see cref="Item1GreaterThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<ushort> Item1GreaterThanItem2(this AnyPairRelation<ushort> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanItem2), item1Below: false, strict: true);
    }

    /// <summary>The <c>uint</c> form of <see cref="Item1GreaterThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<uint> Item1GreaterThanItem2(this AnyPairRelation<uint> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanItem2), item1Below: false, strict: true);
    }

    /// <summary>The <c>long</c> form of <see cref="Item1GreaterThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<long> Item1GreaterThanItem2(this AnyPairRelation<long> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanItem2), item1Below: false, strict: true);
    }

    /// <summary>The <c>ulong</c> form of <see cref="Item1GreaterThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<ulong> Item1GreaterThanItem2(this AnyPairRelation<ulong> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanItem2), item1Below: false, strict: true);
    }

    /// <summary>The <c>float</c> form of <see cref="Item1GreaterThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<float> Item1GreaterThanItem2(this AnyPairRelation<float> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanItem2), item1Below: false, strict: true);
    }

    /// <summary>The <c>double</c> form of <see cref="Item1GreaterThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<double> Item1GreaterThanItem2(this AnyPairRelation<double> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanItem2), item1Below: false, strict: true);
    }

    /// <summary>The <c>decimal</c> form of <see cref="Item1GreaterThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<decimal> Item1GreaterThanItem2(this AnyPairRelation<decimal> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanItem2), item1Below: false, strict: true);
    }

    /// <summary>The <c>TimeSpan</c> form of <see cref="Item1GreaterThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<TimeSpan> Item1GreaterThanItem2(this AnyPairRelation<TimeSpan> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanItem2), item1Below: false, strict: true);
    }

#if NET8_0_OR_GREATER
    /// <summary>The <c>Int128</c> form of <see cref="Item1GreaterThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<Int128> Item1GreaterThanItem2(this AnyPairRelation<Int128> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanItem2), item1Below: false, strict: true);
    }

#endif

#if NET8_0_OR_GREATER
    /// <summary>The <c>UInt128</c> form of <see cref="Item1GreaterThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<UInt128> Item1GreaterThanItem2(this AnyPairRelation<UInt128> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanItem2), item1Below: false, strict: true);
    }

#endif

#if NET8_0_OR_GREATER
    /// <summary>The <c>Half</c> form of <see cref="Item1GreaterThanItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<Half> Item1GreaterThanItem2(this AnyPairRelation<Half> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanItem2), item1Below: false, strict: true);
    }

#endif

    /// <summary>
    ///     Declares <c>Item1</c> greater than or equal to <c>Item2</c>: <c>Item2</c> is drawn from its base kept at or below the
    ///     largest value <c>Item1</c>'s base can produce, then <c>Item1</c> from its base kept at or above it.
    /// </summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<int> Item1GreaterThanOrEqualToItem2(this AnyPairRelation<int> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanOrEqualToItem2), item1Below: false, strict: false);
    }

    /// <summary>The <c>byte</c> form of <see cref="Item1GreaterThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<byte> Item1GreaterThanOrEqualToItem2(this AnyPairRelation<byte> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanOrEqualToItem2), item1Below: false, strict: false);
    }

    /// <summary>The <c>sbyte</c> form of <see cref="Item1GreaterThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<sbyte> Item1GreaterThanOrEqualToItem2(this AnyPairRelation<sbyte> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanOrEqualToItem2), item1Below: false, strict: false);
    }

    /// <summary>The <c>short</c> form of <see cref="Item1GreaterThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<short> Item1GreaterThanOrEqualToItem2(this AnyPairRelation<short> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanOrEqualToItem2), item1Below: false, strict: false);
    }

    /// <summary>The <c>ushort</c> form of <see cref="Item1GreaterThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<ushort> Item1GreaterThanOrEqualToItem2(this AnyPairRelation<ushort> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanOrEqualToItem2), item1Below: false, strict: false);
    }

    /// <summary>The <c>uint</c> form of <see cref="Item1GreaterThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<uint> Item1GreaterThanOrEqualToItem2(this AnyPairRelation<uint> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanOrEqualToItem2), item1Below: false, strict: false);
    }

    /// <summary>The <c>long</c> form of <see cref="Item1GreaterThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<long> Item1GreaterThanOrEqualToItem2(this AnyPairRelation<long> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanOrEqualToItem2), item1Below: false, strict: false);
    }

    /// <summary>The <c>ulong</c> form of <see cref="Item1GreaterThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<ulong> Item1GreaterThanOrEqualToItem2(this AnyPairRelation<ulong> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanOrEqualToItem2), item1Below: false, strict: false);
    }

    /// <summary>The <c>float</c> form of <see cref="Item1GreaterThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<float> Item1GreaterThanOrEqualToItem2(this AnyPairRelation<float> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanOrEqualToItem2), item1Below: false, strict: false);
    }

    /// <summary>The <c>double</c> form of <see cref="Item1GreaterThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<double> Item1GreaterThanOrEqualToItem2(this AnyPairRelation<double> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanOrEqualToItem2), item1Below: false, strict: false);
    }

    /// <summary>The <c>decimal</c> form of <see cref="Item1GreaterThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<decimal> Item1GreaterThanOrEqualToItem2(this AnyPairRelation<decimal> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanOrEqualToItem2), item1Below: false, strict: false);
    }

    /// <summary>The <c>TimeSpan</c> form of <see cref="Item1GreaterThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<TimeSpan> Item1GreaterThanOrEqualToItem2(this AnyPairRelation<TimeSpan> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanOrEqualToItem2), item1Below: false, strict: false);
    }

#if NET8_0_OR_GREATER
    /// <summary>The <c>Int128</c> form of <see cref="Item1GreaterThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<Int128> Item1GreaterThanOrEqualToItem2(this AnyPairRelation<Int128> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanOrEqualToItem2), item1Below: false, strict: false);
    }

#endif

#if NET8_0_OR_GREATER
    /// <summary>The <c>UInt128</c> form of <see cref="Item1GreaterThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<UInt128> Item1GreaterThanOrEqualToItem2(this AnyPairRelation<UInt128> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanOrEqualToItem2), item1Below: false, strict: false);
    }

#endif

#if NET8_0_OR_GREATER
    /// <summary>The <c>Half</c> form of <see cref="Item1GreaterThanOrEqualToItem2(AnyPairRelation{int})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<Half> Item1GreaterThanOrEqualToItem2(this AnyPairRelation<Half> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1GreaterThanOrEqualToItem2), item1Below: false, strict: false);
    }

#endif

    /// <summary>
    ///     Declares <c>Item1</c> strictly before <c>Item2</c>: <c>Item1</c> is drawn from its base kept below the
    ///     largest value <c>Item2</c>'s base can produce, then <c>Item2</c> from its base kept above it.
    /// </summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, when a base holds a <c>Local</c> value (ADR-0114),
    ///     or when no value of the side below lies within what the other base can follow.
    /// </exception>
    public static AnyPair<DateTime> Item1BeforeItem2(this AnyPairRelation<DateTime> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1BeforeItem2), item1Below: true, strict: true);
    }

    /// <summary>The <c>DateTimeOffset</c> form of <see cref="Item1BeforeItem2(AnyPairRelation{DateTime})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<DateTimeOffset> Item1BeforeItem2(this AnyPairRelation<DateTimeOffset> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1BeforeItem2), item1Below: true, strict: true);
    }

#if NET8_0_OR_GREATER
    /// <summary>The <c>DateOnly</c> form of <see cref="Item1BeforeItem2(AnyPairRelation{DateTime})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<DateOnly> Item1BeforeItem2(this AnyPairRelation<DateOnly> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1BeforeItem2), item1Below: true, strict: true);
    }

#endif

    /// <summary>
    ///     Declares <c>Item1</c> at or before <c>Item2</c>: <c>Item1</c> is drawn from its base kept at or below the
    ///     largest value <c>Item2</c>'s base can produce, then <c>Item2</c> from its base kept at or above it.
    /// </summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, when a base holds a <c>Local</c> value (ADR-0114),
    ///     or when no value of the side below lies within what the other base can follow.
    /// </exception>
    public static AnyPair<DateTime> Item1BeforeOrEqualToItem2(this AnyPairRelation<DateTime> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1BeforeOrEqualToItem2), item1Below: true, strict: false);
    }

    /// <summary>The <c>DateTimeOffset</c> form of <see cref="Item1BeforeOrEqualToItem2(AnyPairRelation{DateTime})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<DateTimeOffset> Item1BeforeOrEqualToItem2(this AnyPairRelation<DateTimeOffset> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1BeforeOrEqualToItem2), item1Below: true, strict: false);
    }

#if NET8_0_OR_GREATER
    /// <summary>The <c>DateOnly</c> form of <see cref="Item1BeforeOrEqualToItem2(AnyPairRelation{DateTime})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<DateOnly> Item1BeforeOrEqualToItem2(this AnyPairRelation<DateOnly> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1BeforeOrEqualToItem2), item1Below: true, strict: false);
    }

#endif

    /// <summary>
    ///     Declares <c>Item1</c> strictly after <c>Item2</c>: <c>Item2</c> is drawn from its base kept below the
    ///     largest value <c>Item1</c>'s base can produce, then <c>Item1</c> from its base kept above it.
    /// </summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, when a base holds a <c>Local</c> value (ADR-0114),
    ///     or when no value of the side below lies within what the other base can follow.
    /// </exception>
    public static AnyPair<DateTime> Item1AfterItem2(this AnyPairRelation<DateTime> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1AfterItem2), item1Below: false, strict: true);
    }

    /// <summary>The <c>DateTimeOffset</c> form of <see cref="Item1AfterItem2(AnyPairRelation{DateTime})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<DateTimeOffset> Item1AfterItem2(this AnyPairRelation<DateTimeOffset> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1AfterItem2), item1Below: false, strict: true);
    }

#if NET8_0_OR_GREATER
    /// <summary>The <c>DateOnly</c> form of <see cref="Item1AfterItem2(AnyPairRelation{DateTime})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<DateOnly> Item1AfterItem2(this AnyPairRelation<DateOnly> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1AfterItem2), item1Below: false, strict: true);
    }

#endif

    /// <summary>
    ///     Declares <c>Item1</c> at or after <c>Item2</c>: <c>Item2</c> is drawn from its base kept at or below the
    ///     largest value <c>Item1</c>'s base can produce, then <c>Item1</c> from its base kept at or above it.
    /// </summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, when a base holds a <c>Local</c> value (ADR-0114),
    ///     or when no value of the side below lies within what the other base can follow.
    /// </exception>
    public static AnyPair<DateTime> Item1AfterOrEqualToItem2(this AnyPairRelation<DateTime> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1AfterOrEqualToItem2), item1Below: false, strict: false);
    }

    /// <summary>The <c>DateTimeOffset</c> form of <see cref="Item1AfterOrEqualToItem2(AnyPairRelation{DateTime})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<DateTimeOffset> Item1AfterOrEqualToItem2(this AnyPairRelation<DateTimeOffset> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1AfterOrEqualToItem2), item1Below: false, strict: false);
    }

#if NET8_0_OR_GREATER
    /// <summary>The <c>DateOnly</c> form of <see cref="Item1AfterOrEqualToItem2(AnyPairRelation{DateTime})" />, with the same contract.</summary>
    /// <param name="having">The pair's <see cref="AnyPair{T}.Having" />.</param>
    /// <returns>The pair, its order declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="having" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">
    ///     Thrown when the pair already declares another relation, or when no value of the side below lies within
    ///     what the other base can follow.
    /// </exception>
    public static AnyPair<DateOnly> Item1AfterOrEqualToItem2(this AnyPairRelation<DateOnly> having) {
        if (having is null) { throw new ArgumentNullException(nameof(having)); }

        return having.Pair.Ordered(nameof(Item1AfterOrEqualToItem2), item1Below: false, strict: false);
    }

#endif

}
