namespace JustDummies;

/// <summary>
///     <c>Ordered()</c> on the list and array generators, offered only where the element type has a natural order:
///     an extension can constrain the element type, a member of <see cref="AnyList{T}" /> cannot, so asking to order
///     elements that have no order is a compile error rather than a failure at <c>Generate()</c> (ADR-0124).
/// </summary>
public static class OrderingExtensions {

    /// <summary>
    ///     Arranges the elements in ascending, non-decreasing order — a load curve's timestamps, a ladder of
    ///     thresholds. The elements are drawn, then sorted, so every other constraint keeps applying: the order
    ///     changes the arrangement, never the content. Strictly increasing is <c>Ordered().Distinct()</c>. Declared
    ///     once per generator.
    /// </summary>
    /// <param name="list">The list generator to order.</param>
    /// <typeparam name="T">The element type, which has a natural order.</typeparam>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="list" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when a different order is already declared.</exception>
    public static AnyList<T> Ordered<T>(this AnyList<T> list) where T : IComparable<T> {
        if (list is null) { throw new ArgumentNullException(nameof(list)); }

        return list.WithOrder(ElementOrder<T>.ByNaturalOrder(ConstraintCall.Of(nameof(Ordered))));
    }

    /// <summary>
    ///     Arranges the elements in ascending, non-decreasing order — the array counterpart of
    ///     <see cref="Ordered{T}(AnyList{T})" />, with the same contract.
    /// </summary>
    /// <param name="array">The array generator to order.</param>
    /// <typeparam name="T">The element type, which has a natural order.</typeparam>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="array" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when a different order is already declared.</exception>
    public static AnyArray<T> Ordered<T>(this AnyArray<T> array) where T : IComparable<T> {
        if (array is null) { throw new ArgumentNullException(nameof(array)); }

        return array.WithOrder(ElementOrder<T>.ByNaturalOrder(ConstraintCall.Of(nameof(Ordered))));
    }

}
