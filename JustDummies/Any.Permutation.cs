namespace JustDummies;

public static partial class Any {

    /// <summary>
    ///     Draws a random order of exactly <paramref name="items" />, over the ambient random context: every item once
    ///     per occurrence, duplicates included, in a shuffled order — a milking order over four udder quarters, an
    ///     arrival order. Positions are shuffled; values are never compared.
    /// </summary>
    /// <remarks>
    ///     The sequence is read once, when the generator is built, and each draw returns a fresh list. A subset is not a
    ///     permutation: <c>Any.SetOf(Any.ElementOf(items)).WithCount(n)</c> draws one, and reads as the intent.
    /// </remarks>
    /// <param name="items">The items to order.</param>
    /// <typeparam name="T">The type of the items.</typeparam>
    /// <returns>A generator of orders of <paramref name="items" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items" /> is <c>null</c>.</exception>
    public static IAny<List<T>> PermutationOf<T>(IEnumerable<T> items) {
        if (items is null) { throw new ArgumentNullException(nameof(items)); }

        return new PermutationAny<T>(AmbientRandomSource.Instance, items.ToArray());
    }

}
