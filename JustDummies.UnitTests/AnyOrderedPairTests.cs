#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     <c>Any.OrderedPair(first, second)</c>: two values tied by an order, drawn by construction, each pin bounding
///     the other draw, and every refusal naming both sides (issue #404, ADR-0126).
/// </summary>
public sealed class AnyOrderedPairTests {

    private const int SampleCount = 400;

    private static AnyOrderedPair<int> Dating(bool strict = false) {
        return Any.OrderedPair(Any.Int32().Between(-800, 1600), Any.Int32().Between(-800, 1600), strict);
    }

    [Fact(DisplayName = "Unpinned, the first never exceeds the second, on every seed.")]
    public void UnpinnedTheFirstNeverExceedsTheSecond() {
        AnyOrderedPair<int> dating = Dating();
        for (int i = 0; i < SampleCount; i++) {
            (int from, int to) = dating.Generate();

            Check.That(from).IsLessOrEqualThan(to);
            Check.That(from).IsGreaterOrEqualThan(-800);
            Check.That(to).IsLessOrEqualThan(1600);
        }
    }

    [Fact(DisplayName = "Strict, the first is strictly below the second.")]
    public void StrictTheFirstIsStrictlyBelowTheSecond() {
        AnyOrderedPair<int> narrow = Any.OrderedPair(Any.Int32().Between(1, 3), Any.Int32().Between(1, 3), strict: true);
        HashSet<(int, int)> seen   = [];
        for (int i = 0; i < SampleCount; i++) { seen.Add(narrow.Generate()); }

        Check.That(seen).IsOnlyMadeOf((1, 2), (1, 3), (2, 3)).And.HasSize(3);
    }

    [Fact(DisplayName = "The first is drawn from its base kept at or below the largest second, so disjoint bands never fail.")]
    public void DisjointBandsNeverFail() {
        AnyOrderedPair<int> bands = Any.OrderedPair(Any.Int32().Between(-800, 800), Any.Int32().Between(800, 1600));
        for (int i = 0; i < SampleCount; i++) {
            (int from, int to) = bands.Generate();

            Check.That(from).IsLessOrEqualThan(to);
        }

        // Where the second base ends below the first base's top, the first is kept below that end.
        AnyOrderedPair<int> crossing = Any.OrderedPair(Any.Int32().Between(0, 100), Any.Int32().Between(0, 10));
        for (int i = 0; i < SampleCount; i++) { Check.That(crossing.Generate().First).IsLessOrEqualThan(10); }
    }

    [Fact(DisplayName = "The largest second is read through the second base's lattice, exclusions and value set.")]
    [SuppressMessage(JustDummiesRule.JD029.Category, JustDummiesRule.JD029.Id, Justification = SuppressionJustification.JD029.RefusedValueIsTheSubject)]
    public void TheLargestSecondIsReadThroughEveryConstraint() {
        AnyOrderedPair<int> lattice = Any.OrderedPair(Any.Int32().Between(0, 100), Any.Int32().Between(0, 25).MultipleOf(10));
        AnyOrderedPair<int> excluded = Any.OrderedPair(Any.Int32().Between(0, 100), Any.Int32().Between(0, 10).Except(10, 9));
        AnyOrderedPair<int> pooled   = Any.OrderedPair(Any.Int32().Between(0, 100), Any.Int32().OneOf(3, 7, 50).LessThan(20));

        for (int i = 0; i < SampleCount; i++) {
            Check.That(lattice.Generate().First).IsLessOrEqualThan(20);
            Check.That(excluded.Generate().First).IsLessOrEqualThan(8);
            Check.That(pooled.Generate().First).IsLessOrEqualThan(7);
        }
    }

    [Fact(DisplayName = "Pinning the first draws the second at or above it; pinning the second draws the first at or below it.")]
    public void APinBoundsTheOtherDraw() {
        AnyOrderedPair<int> fromPinned = Dating().WithFirst(1500);
        AnyOrderedPair<int> toPinned   = Dating().WithSecond(-700);
        for (int i = 0; i < SampleCount; i++) {
            (int from, int to) = fromPinned.Generate();
            Check.That(from).IsEqualTo(1500);
            Check.That(to).IsGreaterOrEqualThan(1500).And.IsLessOrEqualThan(1600);

            (from, to) = toPinned.Generate();
            Check.That(to).IsEqualTo(-700);
            Check.That(from).IsLessOrEqualThan(-700).And.IsGreaterOrEqualThan(-800);
        }
    }

    [Fact(DisplayName = "A strict pin bounds the other draw strictly.")]
    public void AStrictPinBoundsStrictly() {
        AnyOrderedPair<int> fromPinned = Dating(strict: true).WithFirst(1599);
        AnyOrderedPair<int> toPinned   = Dating(strict: true).WithSecond(-799);
        for (int i = 0; i < SampleCount; i++) {
            Check.That(fromPinned.Generate().Second).IsEqualTo(1600);
            Check.That(toPinned.Generate().First).IsEqualTo(-800);
        }
    }

    [Fact(DisplayName = "Both pinned in order, the pair is those two values, drawing nothing.")]
    public void BothPinnedInOrderDrawNothing() {
        Check.That(Dating().WithFirst(100).WithSecond(200).Generate()).IsEqualTo((100, 200));
        Check.That(Dating().WithSecond(200).WithFirst(100).Generate()).IsEqualTo((100, 200));
        Check.That(Dating().WithFirst(100).WithSecond(100).Generate()).IsEqualTo((100, 100));
    }

    [Fact(DisplayName = "Both pinned in the wrong order are refused naming both, whichever came first.")]
    public void PinsInTheWrongOrderAreRefused() {
        Check.ThatCode(() => Dating().WithFirst(200).WithSecond(100))
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply WithSecond(100) because WithFirst(200) already requires values greater than or equal to 200.");
        Check.ThatCode(() => Dating().WithSecond(100).WithFirst(200))
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply WithFirst(200) because WithSecond(100) already requires values less than or equal to 100.");
        Check.ThatCode(() => Dating(strict: true).WithFirst(100).WithSecond(100))
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply WithSecond(100) because WithFirst(100) already requires values greater than 100.");
    }

    [Fact(DisplayName = "A pin no value of the other base can follow is refused at once, naming both sides.")]
    public void APinNoValueCanFollowIsRefused() {
        Check.ThatCode(() => Dating().WithFirst(1700))
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply WithFirst(1700) because no second value lies at or above 1700: Between(-800, 1600) already requires values less than or equal to 1600.");
        Check.ThatCode(() => Dating().WithSecond(-900))
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply WithSecond(-900) because no first value lies at or below -900: Between(-800, 1600) already requires values greater than or equal to -800.");
        Check.ThatCode(() => Dating(strict: true).WithFirst(1600))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(caught => caught.Message).Contains("WithFirst(1600)", "no second value lies above 1600");
    }

    [Fact(DisplayName = "Two bases no first value can precede are refused at the pair's declaration, naming both sides.")]
    public void BasesNoFirstCanPrecedeAreRefused() {
        Check.ThatCode(() => Any.OrderedPair(Any.Int32().Between(10, 20), Any.Int32().Between(0, 5)))
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply OrderedPair(first, second) because no first value lies at or below 5, the largest value of the second base: Between(10, 20) already requires values greater than or equal to 10.");
        Check.ThatCode(() => Any.OrderedPair(Any.Int32().Between(5, 5), Any.Int32().Between(5, 5), strict: true))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(caught => caught.Message).Contains("OrderedPair(first, second, strict: true)", "below 5");
    }

    [Fact(DisplayName = "A pin is declared once: the same value again changes nothing, another is refused naming the first.")]
    public void APinIsDeclaredOnce() {
        Check.That(Dating().WithFirst(100).WithFirst(100).Generate().First).IsEqualTo(100);
        Check.ThatCode(() => Dating().WithFirst(100).WithFirst(200))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(caught => caught.Message).Contains("WithFirst(200)", "WithFirst(100)");
        Check.ThatCode(() => Dating().WithSecond(100).WithSecond(200))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(caught => caught.Message).Contains("WithSecond(200)", "WithSecond(100)");
    }

    [Fact(DisplayName = "Every ordered family carries the pair: dates, durations, decimals and floats.")]
    public void EveryOrderedFamilyCarriesThePair() {
        DateTime origin = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < 50; i++) {
            (DateTime start, DateTime end)     = Any.OrderedPair(Any.DateTime().Between(origin, origin.AddDays(30)), Any.DateTime().Between(origin, origin.AddDays(30))).Generate();
            (TimeSpan shortest, TimeSpan longest) = Any.OrderedPair(Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromHours(1)), Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromHours(1)), strict: true).Generate();
            (decimal low, decimal high)        = Any.OrderedPair(Any.Decimal().Between(0m, 100m).WithScale(2), Any.Decimal().Between(0m, 100m).WithScale(2)).Generate();
            (double min, double max)           = Any.OrderedPair(Any.Double().Between(0, 1), Any.Double().Between(0, 1), strict: true).Generate();
#if NET8_0_OR_GREATER
            (DateOnly from, DateOnly to)       = Any.OrderedPair(Any.DateOnly().Between(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), Any.DateOnly().Between(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))).Generate();
            Check.That(from <= to).IsTrue();
#endif

            Check.That(start <= end).IsTrue();
            Check.That(shortest < longest).IsTrue();
            Check.That(low <= high).IsTrue();
            Check.That(min < max).IsTrue();
        }
    }

    [Fact(DisplayName = "The pair replays from its bases' seed, and composes with Then for a third value between the two.")]
    public void ThePairReplaysAndComposes() {
        Check.That(Draws(404)).ContainsExactly(Draws(404));

        IAny<(int Min, int Optimum, int Max)> triple = Dating(strict: true).Then(pair => Any.Int32().Between(pair.First, pair.Second).As(optimum => (pair.First, optimum, pair.Second)));
        for (int i = 0; i < SampleCount; i++) {
            (int min, int optimum, int max) = triple.Generate();
            Check.That(min <= optimum && optimum <= max && min < max).IsTrue();
        }

        static List<(int, int)> Draws(int seed) {
            AnyContext          any    = Any.WithSeed(seed);
            AnyOrderedPair<int> dating = Any.OrderedPair(any.Int32().Between(-800, 1600), any.Int32().Between(-800, 1600));

            return Enumerable.Range(0, 30).Select(_ => dating.Generate()).ToList();
        }
    }

    [Fact(DisplayName = "A null base is refused as an argument.")]
    public void ANullBaseIsRefused() {
        Check.ThatCode(() => Any.OrderedPair(null!, Any.Int32())).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.OrderedPair(Any.Int32(), null!)).Throws<ArgumentNullException>();
    }

}
