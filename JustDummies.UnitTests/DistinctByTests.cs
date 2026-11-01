#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     <c>DistinctBy(key)</c> on the list, array and sequence builders: exactly <c>Distinct(comparer)</c> with a comparer
///     by that key, so the same bounded redraw, the same loud failure, and the same one-comparer rule.
/// </summary>
public sealed class DistinctByTests {

    private sealed record Layer(string Code, int Depth);

    private static IAny<Layer> Layers(AnyContext any) {
        return Any.Combine(any.String().OneOf("AA", "BB", "CC", "DD"), any.Int32().Between(0, 100), (code, depth) => new Layer(code, depth));
    }

    [Fact(DisplayName = "The elements are distinct by the key, on every builder that carries Distinct(comparer).")]
    public void TheElementsAreDistinctByTheKey() {
        AnyContext any = Any.WithSeed(20261101);
        for (int i = 0; i < 50; i++) {
            List<Layer>        list     = Any.ListOf(Layers(any)).WithCount(4).DistinctBy(layer => layer.Code).Generate();
            Layer[]            array    = Any.ArrayOf(Layers(any)).WithCount(4).DistinctBy(layer => layer.Code).Generate();
            IEnumerable<Layer> sequence = Any.SequenceOf(Layers(any)).WithCount(4).DistinctBy(layer => layer.Code).Generate();

            Check.That(list.Select(layer => layer.Code)).ContainsNoDuplicateItem();
            Check.That(array.Select(layer => layer.Code)).ContainsNoDuplicateItem();
            Check.That(sequence.Select(layer => layer.Code)).ContainsNoDuplicateItem();
        }
    }

    [Fact(DisplayName = "A key that runs out of values fails loudly with its seed, as Distinct(comparer) does.")]
    public void AKeyThatRunsOutFailsLoudly() {
        AnyContext any = Any.WithSeed(77);

        Check.ThatCode(() => Any.ListOf(Layers(any)).WithCount(5).DistinctBy(layer => layer.Code).Generate())
             .Throws<AnyGenerationException>()
             .WithProperty(caught => caught.Seed, 77);
    }

    [Fact(DisplayName = "A second, different equality is refused, naming the call that brought it.")]
    public void ASecondEqualityIsRefused() {
        Check.ThatCode(() => Any.ListOf(Layers(Any.WithSeed(1))).Distinct(EqualityComparer<Layer>.Default).DistinctBy(layer => layer.Code))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(caught => caught.Message)
             .Contains("DistinctBy");
    }

    [Fact(DisplayName = "A null key selector is refused as an argument.")]
    public void ANullKeySelectorIsRefused() {
        Check.ThatCode(() => Any.ListOf(Any.Int32()).DistinctBy<int>(null!)).Throws<ArgumentNullException>();
    }

}
