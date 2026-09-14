#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     The behaviour ADR-0102 promises: a collection or a composition chooses no source of its own and reuses the
///     one resolved from its operands — its element generator's, its key generator's or else its value generator's,
///     the source of the first operand that carries one — so one built over a context's generators is
///     deterministic with that context's seed, and a mixed one follows the documented operand (issue #184).
/// </summary>
public sealed class ContextInheritanceTests {

    private const int Seed = 1743029518;

    [Fact(DisplayName = "A collection over a context's generator replays from the context's seed, whatever the ambient scope.")]
    public void ACollectionOverAContextGeneratorReplaysFromTheContextSeed() {
        List<int> first;
        List<int> second;

        using (Any.UseSeed(1)) { first = Any.ListOf(Any.WithSeed(Seed).Int32().Between(1, 100)).NonEmpty().Generate(); }
        using (Any.UseSeed(2)) { second = Any.ListOf(Any.WithSeed(Seed).Int32().Between(1, 100)).NonEmpty().Generate(); }

        Check.That(first).ContainsExactly(second);
    }

    [Fact(DisplayName = "A set, a dictionary and a composition over a context's generators replay from the context's seed.")]
    public void EveryCombinatorOverAContextGeneratorReplaysFromTheContextSeed() {
        static (HashSet<int> Set, Dictionary<int, string> Dictionary, (string, int) Pair) Draw() {
            AnyContext context = Any.WithSeed(Seed);

            return (Any.SetOf(context.Int32().Between(1, 1000)).WithCount(5).Generate(),
                    Any.DictionaryOf(context.Int32().Between(1, 1000), context.String().WithLength(4)).WithCount(3).Generate(),
                    Any.PairOf(context.String().WithLength(8), context.Int32()).Generate());
        }

        (HashSet<int> set, Dictionary<int, string> dictionary, (string, int) pair) = Draw();
        (HashSet<int> setAgain, Dictionary<int, string> dictionaryAgain, (string, int) pairAgain) = Draw();

        Check.That(set).ContainsExactly(setAgain);
        Check.That(dictionary).ContainsExactly(dictionaryAgain);
        Check.That(pair).IsEqualTo(pairAgain);
    }

    [Fact(DisplayName = "A combinator reuses the source resolved from its operands: the element generator's, the key generator's or else the value generator's, the source of the first operand that carries one.")]
    public void ACombinatorReusesTheSourceResolvedFromItsOperands() {
        AnyContext    context       = Any.WithSeed(Seed);
        RandomSource? contextSource = SourceOf(context.Int32());
        RandomSource? ambientSource = SourceOf(Any.Int32());

        Check.That(contextSource).IsNotNull();
        Check.That(ambientSource).IsNotNull();
        Check.That(ReferenceEquals(ambientSource, contextSource)).IsFalse();

        // A collection: its element generator's source; none when the element generator is foreign.
        Check.That(SourceOf(Any.ListOf(context.Int32()))).IsSameReferenceAs(contextSource);
        Check.That(SourceOf(Any.ListOf(context.Int32()).ContainingAny(Any.Int32()))).IsSameReferenceAs(contextSource);
        Check.That(SourceOf(Any.ListOf(new Counter()))).IsNull();

        // A dictionary: its key generator's source, whatever the values carry; the value generator's only when the
        // key generator is foreign.
        Check.That(SourceOf(Any.DictionaryOf(Any.Int32(), context.String()))).IsSameReferenceAs(ambientSource);
        Check.That(SourceOf(Any.DictionaryOf(new Counter(), context.String()))).IsSameReferenceAs(contextSource);

        // A composition: the first operand that carries a source.
        Check.That(SourceOf(Any.Combine(Any.Int32(), context.Int32(), (left, right) => left + right))).IsSameReferenceAs(ambientSource);
        Check.That(SourceOf(Any.Combine(new Counter(), context.Int32(), (left, right) => left + right))).IsSameReferenceAs(contextSource);
    }

    [Fact(DisplayName = "A dictionary draws its entry count from the source it resolved: the ambient one over ambient keys, the context's over foreign keys and context-bound values.")]
    public void ADictionaryDrawsItsCountFromTheSourceItResolved() {
        static int CountOverAmbientKeys(int ambientSeed) {
            using (Any.UseSeed(ambientSeed)) {
                return Any.DictionaryOf(Any.Int32(), Any.WithSeed(Seed).String().WithLength(4)).WithCountBetween(0, 1000).Generate().Count;
            }
        }

        static int CountOverForeignKeys(int ambientSeed) {
            using (Any.UseSeed(ambientSeed)) {
                return Any.DictionaryOf(new Counter(), Any.WithSeed(Seed).String().WithLength(4)).WithCountBetween(0, 1000).Generate().Count;
            }
        }

        // Over ambient keys the count follows the ambient seed: it replays under the same one and moves with another,
        // however deterministic the values are.
        Check.That(CountOverAmbientKeys(1)).IsEqualTo(CountOverAmbientKeys(1));
        Check.That(CountOverAmbientKeys(1)).IsNotEqualTo(CountOverAmbientKeys(2));

        // Over foreign keys the dictionary falls back to its value generator's source, so the count replays from the
        // context's seed whatever the ambient scope.
        Check.That(CountOverForeignKeys(1)).IsEqualTo(CountOverForeignKeys(2));
    }

    private static RandomSource? SourceOf<T>(IAny<T> generator) {
        return (generator as IHasRandomSource)?.Source;
    }

    /// <summary>A foreign generator: distinct values, no random source of its own.</summary>
    private sealed class Counter : IAny<int> {

        private int _next;

        public int Generate() {
            return _next++;
        }

    }

}
