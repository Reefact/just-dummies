namespace JustDummies;

/// <summary>
///     The order a list or an array arranges its drawn elements in — ascending, non-decreasing, by the elements' own
///     order or by a key. Applied after the draw, so it changes the arrangement and never the content: the count, the
///     contained values and distinctness hold exactly as they did (ADR-0124).
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
internal sealed class ElementOrder<T> {

    private readonly IComparer<T> _comparer;
    private readonly object       _identity;

    private ElementOrder(IComparer<T> comparer, object identity, ConstraintCall constraint) {
        _comparer  = comparer;
        _identity  = identity;
        Constraint = constraint;
    }

    /// <summary>The call that declared the order, for the message of a second, different one.</summary>
    internal ConstraintCall Constraint { get; }

    /// <summary>The elements' own order, through <see cref="Comparer{T}.Default" />.</summary>
    internal static ElementOrder<T> ByNaturalOrder(ConstraintCall constraint) {
        if (constraint is null) { throw new ArgumentNullException(nameof(constraint)); }

        // The default comparer is one instance per element type, so it is also the identity every Ordered() shares.
        return new ElementOrder<T>(Comparer<T>.Default, Comparer<T>.Default, constraint);
    }

    /// <summary>The order of a key, through the key type's <see cref="Comparer{T}.Default" />.</summary>
    internal static ElementOrder<T> ByKey<TKey>(Func<T, TKey> keySelector, ConstraintCall constraint) {
        if (keySelector is null) { throw new ArgumentNullException(nameof(keySelector)); }
        if (constraint is null) { throw new ArgumentNullException(nameof(constraint)); }

        Comparer<TKey> keys = Comparer<TKey>.Default;

        return new ElementOrder<T>(Comparer<T>.Create((left, right) => keys.Compare(keySelector(left), keySelector(right))), keySelector, constraint);
    }

    /// <summary>
    ///     The order a generator holds once <paramref name="declared" /> is added to <paramref name="current" />: the
    ///     same order again changes nothing, and a different one conflicts with the first. Two key orders are the
    ///     same only through the same key selector instance, since two functions cannot be compared otherwise.
    /// </summary>
    internal static ElementOrder<T> Declare(ElementOrder<T>? current, ElementOrder<T> declared) {
        if (declared is null) { throw new ArgumentNullException(nameof(declared)); }
        if (current is null) { return declared; }
        if (ReferenceEquals(current._identity, declared._identity)) { return current; }

        throw ConflictingAnyConstraintException.AlreadyDefined(declared.Constraint, current.Constraint);
    }

    /// <summary>Arranges <paramref name="items" /> in this order, in place.</summary>
    internal void Arrange(List<T> items) {
        if (items is null) { throw new ArgumentNullException(nameof(items)); }

        items.Sort(_comparer);
    }

}
