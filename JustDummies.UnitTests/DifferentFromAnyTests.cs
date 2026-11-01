#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     <see cref="AnyExtensions.DifferentFrom{T}" /> on any <see cref="IAny{T}" />: the rejective exclusion, beside the
///     constructive one the typed builders keep. Each case is one of the two guarantees or the line between them
///     (ADR-0121).
/// </summary>
public sealed class DifferentFromAnyTests {

    private const int SampleCount = 400;

    [Fact(DisplayName = "A generator seen as IAny never yields the excluded value.")]
    public void AGeneratorSeenAsIAnyNeverYieldsTheExcludedValue() {
        AnyContext any       = Any.WithSeed(20261101);
        IAny<int>  generator = any.Int32().Between(1, 3);

        for (int i = 0; i < SampleCount; i++) { Check.That(generator.DifferentFrom(2).Generate()).IsNotEqualTo(2); }
    }

    [Fact(DisplayName = "A scaffolded or hand-written generator gets the exclusion too.")]
    public void AnyGeneratorGetsTheExclusion() {
        IAny<string> pitch = Any.Combine(Any.OneOf("A", "B"), Any.OneOf("4", "5"), (note, octave) => note + octave);

        HashSet<string> seen = [];
        for (int i = 0; i < SampleCount; i++) { seen.Add(pitch.DifferentFrom("A4").Generate()); }

        Check.That(seen).IsOnlyMadeOf("A5", "B4", "B5").And.HasSize(3);
    }

    [Fact(DisplayName = "A generator that yields nothing else fails loudly after the bounded redraw, with its seed.")]
    public void AGeneratorThatYieldsNothingElseFailsLoudly() {
        IAny<int> pinned = Any.WithSeed(9).Int32().Between(5, 5);

        Check.ThatCode(() => pinned.DifferentFrom(5).Generate())
             .Throws<AnyGenerationException>()
             .WithProperty(caught => caught.Seed, 9)
             .And.WhichMember(caught => caught.Message)
             .Contains("different from 5", "Any.WithSeed(9)");
    }

    [Fact(DisplayName = "The static type picks the guarantee: a typed builder refuses at declaration, IAny refuses at generation.")]
    [SuppressMessage(JustDummiesRule.JD023.Category, JustDummiesRule.JD023.Id, Justification = SuppressionJustification.JD023.ThrowIsTheSubject)]
    public void TheStaticTypePicksTheGuarantee() {
        Check.ThatCode(() => Any.Int32().OneOf(5).DifferentFrom(5)).Throws<ConflictingAnyConstraintException>();

        IAny<int> seenAsIAny = Any.Int32().OneOf(5);
        IAny<int> declared   = seenAsIAny.DifferentFrom(5);

        Check.ThatCode(() => declared.Generate()).Throws<AnyGenerationException>();
    }

    [Fact(DisplayName = "The exclusion compares with the type's own equality, null included.")]
    public void TheExclusionComparesWithTheTypesEquality() {
        IAny<string?> maybe = Any.String().WithLength(3).OrNull();

        for (int i = 0; i < SampleCount; i++) { Check.That(maybe.DifferentFrom(null).Generate()).IsNotNull(); }
    }

    [Fact(DisplayName = "The redraw replays: the same seed yields the same values.")]
    public void TheRedrawReplays() {
        Check.That(Draws(13)).ContainsExactly(Draws(13));

        static List<int> Draws(int seed) {
            IAny<int> generator = Any.WithSeed(seed).Int32().Between(1, 2);

            return Enumerable.Range(0, 50).Select(_ => generator.DifferentFrom(1).Generate()).ToList();
        }
    }

    [Fact(DisplayName = "A distinct collection counts one value fewer once a value it could draw is excluded.")]
    public void ADistinctCollectionCountsOneValueFewer() {
        IAny<int> three = Any.Int32().Between(1, 3);

        Check.That(Any.SetOf(three.DifferentFrom(2)).WithCount(2).Generate()).IsOnlyMadeOf(1, 3);
        Check.ThatCode(() => Any.SetOf(three.DifferentFrom(2)).WithCount(3).Generate()).Throws<ConflictingAnyConstraintException>();
    }

}
