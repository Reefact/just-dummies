namespace JustDummies;

/// <summary>
///     The relation an <see cref="AnyPair{T}" /> declares between its two items, reached through
///     <see cref="AnyPair{T}.Having" />. Its members are spelled in the vocabulary of the pair's builder — from
///     <c>Item1LessThanItem2()</c> to <c>Item1GreaterThanOrEqualToItem2()</c> on the numbers and durations, from
///     <c>Item1BeforeItem2()</c> to <c>Item1AfterOrEqualToItem2()</c> on the dates — which is why they are declared in
///     <see cref="AnyPairRelations" />, one set per type (ADR-0131).
/// </summary>
/// <typeparam name="T">The type of both items.</typeparam>
public sealed class AnyPairRelation<T> {

    internal AnyPairRelation(AnyPair<T> pair) {
        Pair = pair ?? throw new ArgumentNullException(nameof(pair));
    }

    /// <summary>The pair the relation is declared on.</summary>
    internal AnyPair<T> Pair { get; }

}
