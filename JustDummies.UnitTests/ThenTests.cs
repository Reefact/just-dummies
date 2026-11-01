#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     <see cref="AnyExtensions.Then{TSource,TResult}" />: a draw that depends on another draw. Each case is one of its
///     promises — the inner generator is built from the drawn value, only the chosen generator draws, and the whole
///     chain replays from one seed under the ambient source and under an explicit <see cref="AnyContext" /> alike — or
///     one of the ways a caller's function can fail it (ADR-0119).
/// </summary>
public sealed class ThenTests {

    private const int SampleCount = 200;

    [Fact(DisplayName = "The inner generator is built from the drawn value, so the dependent draw honours it.")]
    public void TheInnerGeneratorIsBuiltFromTheDrawnValue() {
        AnyContext any = Any.WithSeed(20261101);

        for (int i = 0; i < SampleCount; i++) {
            (int Capacity, int Cells) drawn = any.Int32().Between(1, 50)
                                                 .Then(capacity => any.Int32().Between(0, capacity).As(cells => (capacity, cells)))
                                                 .Generate();

            Check.That(drawn.Cells).IsGreaterOrEqualThan(0).And.IsLessOrEqualThan(drawn.Capacity);
        }
    }

    [Fact(DisplayName = "Choosing among generators draws only the chosen one.")]
    public void ChoosingAmongGeneratorsDrawsOnlyTheChosenOne() {
        // The branch not taken is never asked for a value: a Fixed branch draws nothing, so a chain choosing between
        // two fixed values consumes the coin alone, and the draw that follows it replays as if nothing else ran.
        int afterChoice;
        int afterCoinAlone;
        using (Any.UseSeed(7)) {
            _ = Any.Boolean().Then(heads => heads ? Any.Fixed("heads") : Any.Fixed("tails")).Generate();
            afterChoice = Any.Int32().Generate();
        }

        using (Any.UseSeed(7)) {
            _ = Any.Boolean().Generate();
            afterCoinAlone = Any.Int32().Generate();
        }

        Check.That(afterChoice).IsEqualTo(afterCoinAlone);

        HashSet<string> seen = [];
        AnyContext      any  = Any.WithSeed(11);
        for (int i = 0; i < SampleCount; i++) { seen.Add(any.Boolean().Then(heads => heads ? Any.Fixed("heads") : Any.Fixed("tails")).Generate()); }

        Check.That(seen).IsOnlyMadeOf("heads", "tails").And.HasSize(2);
    }

    [Fact(DisplayName = "Under an explicit context, a generator built from Any inside Then draws from that context.")]
    public void UnderAContextTheInnerDrawUsesThatContext() {
        // The partial-replay trap Then exists to close: the inner generator is built with the static Any, yet it must
        // draw from the context the outer value came from — or the run would depend on the ambient seed too.
        List<int> first  = DrawFromAFreshContext(ambientSeed: 1);
        List<int> second = DrawFromAFreshContext(ambientSeed: 2);

        Check.That(second).ContainsExactly(first);
    }

    [Fact(DisplayName = "Under the ambient seed, the whole chain replays from that seed.")]
    public void UnderTheAmbientSeedTheChainReplays() {
        List<int> first;
        List<int> second;
        using (Any.UseSeed(31)) { first = Draws(); }
        using (Any.UseSeed(31)) { second = Draws(); }

        Check.That(second).ContainsExactly(first);

        static List<int> Draws() {
            return Enumerable.Range(0, SampleCount).Select(_ => Any.Int32().Between(1, 100).Then(bound => Any.Int32().Between(0, bound)).Generate()).ToList();
        }
    }

    [Fact(DisplayName = "A function that throws is reported with the value it was given and the seed that replays it.")]
    public void AThrowingFunctionIsReportedWithItsValue() {
        AnyContext any = Any.WithSeed(4242);

        Check.ThatCode(() => any.Int32().Between(5, 5).Then<int, int>(_ => throw new InvalidOperationException("no generator for this")).Generate())
             .Throws<AnyGenerationException>()
             .WithProperty(caught => caught.Seed, 4242)
             .And.WhichMember(caught => caught.Message)
             .Contains("Then(...)", "5", "Any.WithSeed(4242)");
    }

    [Fact(DisplayName = "A function that returns no generator is reported, not dereferenced.")]
    public void AFunctionReturningNoGeneratorIsReported() {
        Check.ThatCode(() => Any.WithSeed(1).Int32().Between(3, 3).Then(_ => (IAny<int>)null!).Generate())
             .Throws<AnyGenerationException>()
             .WhichMember(caught => caught.Message)
             .Contains("Then(...)", "returned no generator");
    }

    [Fact(DisplayName = "A null generator or function is refused when the chain is declared.")]
    public void NullArgumentsAreRefused() {
        Check.ThatCode(() => AnyExtensions.Then<int, int>(null!, _ => Any.Int32())).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.Int32().Then<int, int>(null!)).Throws<ArgumentNullException>();
    }

    private static List<int> DrawFromAFreshContext(int ambientSeed) {
        using (Any.UseSeed(ambientSeed)) {
            AnyContext any = Any.WithSeed(99);

            return Enumerable.Range(0, SampleCount).Select(_ => any.Int32().Between(1, 100).Then(bound => Any.Int32().Between(0, bound)).Generate()).ToList();
        }
    }

}
