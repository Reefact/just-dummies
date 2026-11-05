#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     <see cref="AnyExtensions.Except{T}(IAny{T}, IEnumerable{T})" /> on any <see cref="IAny{T}" />: the rejective
///     exclusion of a set of values, the one-value <c>DifferentFrom</c> extended to a set (ADR-0133, amending ADR-0121).
/// </summary>
public sealed class ExceptAnyTests {

    private const int SampleCount = 400;

    [Fact(DisplayName = "A generator seen as IAny never yields an excluded value.")]
    public void AGeneratorSeenAsIAnyNeverYieldsAnExcludedValue() {
        IAny<int> generator = Any.WithSeed(20261105).Int32().Between(1, 5);

        for (int i = 0; i < SampleCount; i++) { Check.That(generator.Except(2, 4).Generate()).IsOneOf(1, 3, 5); }
    }

    [Fact(DisplayName = "A collection of values is excluded whole, from a composed generator.")]
    public void ACollectionOfValuesIsExcludedWhole() {
        IAny<string> pitch = Any.Combine(Any.OneOf("A", "B"), Any.OneOf("4", "5"), (note, octave) => note + octave);
        List<string> alreadyUsed = ["A4", "B5"];

        HashSet<string> seen = [];
        for (int i = 0; i < SampleCount; i++) { seen.Add(pitch.Except(alreadyUsed).Generate()); }

        Check.That(seen).IsOnlyMadeOf("A5", "B4").And.HasSize(2);
    }

    [Fact(DisplayName = "An empty exclusion is the identity: the generator itself comes back (ADR-0122).")]
    public void AnEmptyExclusionIsTheIdentity() {
        IAny<int> generator = Any.Int32().Between(1, 5);

        Check.That(generator.Except(Array.Empty<int>())).IsSameReferenceAs(generator);
        Check.That(generator.Except(new List<int>())).IsSameReferenceAs(generator);
    }

    [Fact(DisplayName = "A null generator or a null set of values is refused as an argument.")]
    public void NullArgumentsAreRefused() {
        IAny<int> generator = Any.Int32();

        Check.ThatCode(() => ((IAny<int>)null!).Except(1)).Throws<ArgumentNullException>();
        Check.ThatCode(() => generator.Except((int[])null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => generator.Except((IEnumerable<int>)null!)).Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "A null element is excluded like any other value.")]
    public void ANullElementIsExcludedLikeAnyOther() {
        IAny<string?> maybe = Any.String().WithLength(3).OrNull();

        for (int i = 0; i < SampleCount; i++) { Check.That(maybe.Except((string?)null).Generate()).IsNotNull(); }
    }

    [Fact(DisplayName = "A generator that yields nothing else fails after one bounded redraw, naming the whole set and its seed.")]
    public void AnExhaustedRedrawNamesTheWholeSetAndItsSeed() {
        // Derived through As, the generator advertises no domain, so only the bounded redraw can find it out.
        IAny<int> twoValues = Any.WithSeed(9).Int32().Between(1, 2).As(value => value);

        Check.ThatCode(() => twoValues.Except(1, 2).Generate())
             .Throws<AnyGenerationException>()
             .WithProperty(caught => caught.Seed, 9)
             .And.WhichMember(caught => caught.Message)
             .Contains("outside {1, 2}", "Any.WithSeed(9)");
    }

    [Fact(DisplayName = "A generator known to yield only an excluded value is refused at once, with a definite message.")]
    public void AGeneratorKnownToYieldOnlyAnExcludedValueIsRefusedDefinitely() {
        Check.ThatCode(() => Any.Fixed("ORD-1").Except("ORD-1", "ORD-2").Generate())
             .Throws<AnyGenerationException>()
             .WhichMember(caught => caught.Message)
             .IsEqualTo("Could not draw a value outside {\"ORD-1\", \"ORD-2\"}: the generator yields only \"ORD-1\".");
    }

    [Fact(DisplayName = "The static type picks the guarantee: a typed builder refuses at declaration, IAny refuses at generation.")]
    [SuppressMessage(JustDummiesRule.JD023.Category, JustDummiesRule.JD023.Id, Justification = SuppressionJustification.JD023.ThrowIsTheSubject)]
    public void TheStaticTypePicksTheGuarantee() {
        Check.ThatCode(() => Any.Int32().OneOf(5).Except(5)).Throws<ConflictingAnyConstraintException>();

        IAny<int> seenAsIAny = Any.Int32().OneOf(5);
        IAny<int> declared   = seenAsIAny.Except(5, 6);

        Check.ThatCode(() => declared.Generate()).Throws<AnyGenerationException>();
    }

    [Fact(DisplayName = "A distinct collection counts the values left once a set is excluded.")]
    public void ADistinctCollectionCountsTheValuesLeft() {
        IAny<int> four = Any.Int32().Between(1, 4);

        Check.That(Any.SetOf(four.Except(2, 3)).WithCount(2).Generate()).IsOnlyMadeOf(1, 4);
        Check.ThatCode(() => Any.SetOf(four.Except(2, 3)).WithCount(3).Generate()).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "A comparer finer than the default still sees the spellings left after an exclusion (ADR-0069).")]
    public void AComparerFinerThanTheDefaultSeesTheSpellingsLeft() {
        // Two instants, each across a one-minute offset range. Excluding the first by its instant leaves one instant,
        // one value by default and two under EqualsExact: two distinct spellings are satisfiable, and were refused
        // when the exclusion forwarded the default bound to a collection carrying its own comparer.
        DateTimeOffset       start       = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        IAny<DateTimeOffset> twoInstants = Any.DateTimeOffset().Between(start, start.AddTicks(1)).WithOffsetBetween(TimeSpan.Zero, TimeSpan.FromMinutes(1));

        List<DateTimeOffset> viaExcept        = Any.ListOf(twoInstants.Except(start)).WithCount(2).Distinct(new BySpellingComparer()).Generate();
        List<DateTimeOffset> viaDifferentFrom = Any.ListOf(twoInstants.DifferentFrom(start)).WithCount(2).Distinct(new BySpellingComparer()).Generate();

        Check.That(viaExcept.Select(value => value.UtcTicks).Distinct()).ContainsExactly(start.AddTicks(1).UtcTicks);
        Check.That(viaDifferentFrom.Select(value => value.UtcTicks).Distinct()).ContainsExactly(start.AddTicks(1).UtcTicks);
    }

    [Fact(DisplayName = "The redraw replays: the same seed yields the same values.")]
    public void TheRedrawReplays() {
        Check.That(Draws(13)).ContainsExactly(Draws(13));

        static List<int> Draws(int seed) {
            IAny<int> generator = Any.WithSeed(seed).Int32().Between(1, 4);

            return Enumerable.Range(0, 50).Select(_ => generator.Except(1, 3).Generate()).ToList();
        }
    }

    private sealed class BySpellingComparer : IEqualityComparer<DateTimeOffset> {

        public bool Equals(DateTimeOffset x, DateTimeOffset y) {
            return x.EqualsExact(y);
        }

        public int GetHashCode(DateTimeOffset obj) {
            return obj.Offset.GetHashCode() ^ obj.DateTime.GetHashCode();
        }

    }

}
