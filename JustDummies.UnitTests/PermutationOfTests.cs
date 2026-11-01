#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     <see cref="Any.PermutationOf{T}" />: a random order of exactly the given items, drawn by shuffling positions, so
///     duplicates are kept and nothing is compared.
/// </summary>
public sealed class PermutationOfTests {

    private enum Quarter { FrontLeft, FrontRight, RearLeft, RearRight }

    [Fact(DisplayName = "Every item is kept, exactly once per occurrence.")]
    public void EveryItemIsKept() {
        AnyContext any = Any.WithSeed(20261101);
        Quarter[]  all = [Quarter.FrontLeft, Quarter.FrontRight, Quarter.RearLeft, Quarter.RearRight];

        for (int i = 0; i < 100; i++) { Check.That(any.PermutationOf(all).Generate()).IsEquivalentTo(all); }
    }

    [Fact(DisplayName = "Duplicates are kept: positions are shuffled, values are never compared.")]
    public void DuplicatesAreKept() {
        AnyContext any = Any.WithSeed(5);

        for (int i = 0; i < 100; i++) { Check.That(any.PermutationOf(new[] { 1, 1, 2 }).Generate()).IsEquivalentTo(1, 1, 2); }
    }

    [Fact(DisplayName = "Every order is reachable.")]
    public void EveryOrderIsReachable() {
        AnyContext      any    = Any.WithSeed(9);
        HashSet<string> orders = [];
        for (int i = 0; i < 600; i++) { orders.Add(string.Concat(any.PermutationOf("abc").Generate())); }

        Check.That(orders).IsOnlyMadeOf("abc", "acb", "bac", "bca", "cab", "cba").And.HasSize(6);
    }

    [Fact(DisplayName = "The same seed draws the same order.")]
    public void TheSameSeedDrawsTheSameOrder() {
        Check.That(Any.WithSeed(13).PermutationOf(Enumerable.Range(0, 20)).Generate())
             .ContainsExactly(Any.WithSeed(13).PermutationOf(Enumerable.Range(0, 20)).Generate());
    }

    [Fact(DisplayName = "The items are read once, and each draw is a fresh list.")]
    public void TheItemsAreReadOnceAndEachDrawIsFresh() {
        int              reads = 0;
        IAny<List<int>>  order = Any.WithSeed(1).PermutationOf(Enumerable.Range(0, 3).Select(item => { reads++; return item; }));
        List<int>        first = order.Generate();
        first.Clear();

        Check.That(order.Generate()).HasSize(3);
        Check.That(reads).IsEqualTo(3);
    }

    [Fact(DisplayName = "A null sequence is refused as an argument.")]
    public void ANullSequenceIsRefused() {
        Check.ThatCode(() => Any.PermutationOf<int>(null!)).Throws<ArgumentNullException>();
    }

}
