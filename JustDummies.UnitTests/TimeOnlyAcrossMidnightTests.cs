#if NET8_0_OR_GREATER
#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     <c>Any.TimeOnly().Between(start, end)</c> with the start after the end: the window across midnight, and every
///     other constraint read along it (issue #357, ADR-0125).
/// </summary>
public sealed class TimeOnlyAcrossMidnightTests {

    private const int SampleCount = 4_000;

    private static readonly TimeOnly At2200   = new(22, 0);
    private static readonly TimeOnly At0600   = new(6, 0);
    private static readonly TimeOnly At2300   = new(23, 0);
    private static readonly TimeOnly At0200   = new(2, 0);
    private static readonly TimeOnly At0100   = new(1, 0);
    private static readonly TimeOnly Noon     = new(12, 0);
    private static readonly TimeOnly Midnight = new(0, 0);

    private static bool InNight(TimeOnly value) {
        return value >= At2200 || value <= At0600;
    }

    [Fact(DisplayName = "A start after the end draws across midnight, from both segments in proportion to their length.")]
    public void AStartAfterTheEndDrawsAcrossMidnight() {
        AnyTimeOnly night = Any.WithSeed(357).TimeOnly().Between(At2200, At0600);

        int beforeMidnight = 0;
        for (int i = 0; i < SampleCount; i++) {
            TimeOnly value = night.Generate();
            Check.That(InNight(value)).IsTrue();
            if (value >= At2200) { beforeMidnight++; }
        }

        // At0200 hours of eight lie before midnight: a quarter of the draws, with a wide margin for the sample.
        Check.That(beforeMidnight).IsStrictlyGreaterThan(SampleCount / 4 - 300).And.IsStrictlyLessThan(SampleCount / 4 + 300);
    }

    [Fact(DisplayName = "Between(t, t) stays the single value t.")]
    public void BetweenTheSameTimeStaysOneValue() {
        Check.That(Any.TimeOnly().Between(At2200, At2200).Generate()).IsEqualTo(At2200);
    }

    [Fact(DisplayName = "After and Before narrow the window along its direction.")]
    public void AfterAndBeforeNarrowAlongTheWindow() {
        AnyContext any = Any.WithSeed(1);
        for (int i = 0; i < 400; i++) {
            TimeOnly late  = any.TimeOnly().Between(At2200, At0600).After(At2300).Generate();
            TimeOnly early = any.TimeOnly().Between(At2200, At0600).Before(At0200).Generate();
            TimeOnly small = any.TimeOnly().Between(At2200, At0600).After(At0100).Generate();

            Check.That(late > At2300 || late <= At0600).IsTrue();
            Check.That(early >= At2200 || early < At0200).IsTrue();
            Check.That(small > At0100 && small <= At0600).IsTrue();
        }
    }

    [Fact(DisplayName = "The order of the declarations does not matter: a bound before the window means what it means after it.")]
    public void TheOrderOfTheDeclarationsDoesNotMatter() {
        Check.That(Draws(any => any.TimeOnly().After(At2300).Between(At2200, At0600).Before(At0200))).ContainsExactly(Draws(any => any.TimeOnly().Between(At2200, At0600).After(At2300).Before(At0200)));
        Check.That(Draws(any => any.TimeOnly().WithGranularity(TimeSpan.FromMinutes(15)).Except(Midnight).Between(At2200, At0600)))
             .ContainsExactly(Draws(any => any.TimeOnly().Between(At2200, At0600).WithGranularity(TimeSpan.FromMinutes(15)).Except(Midnight)));

        static List<TimeOnly> Draws(Func<AnyContext, AnyTimeOnly> declare) {
            AnyTimeOnly generator = declare(Any.WithSeed(42));

            return Enumerable.Range(0, 50).Select(_ => generator.Generate()).ToList();
        }
    }

    [Fact(DisplayName = "A time outside the window admits no value, and is refused naming both sides, in either order.")]
    public void ATimeOutsideTheWindowIsRefused() {
        Check.ThatCode(() => Any.TimeOnly().Between(At2200, At0600).After(Noon))
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply After(12:00:00.0000000) because 12:00:00.0000000 lies outside the window Between(22:00:00.0000000, 06:00:00.0000000) declares across midnight.");
        Check.ThatCode(() => Any.TimeOnly().Before(Noon).Between(At2200, At0600))
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply Between(22:00:00.0000000, 06:00:00.0000000) because Before(12:00:00.0000000) declares 12:00:00.0000000, outside the window it declares across midnight.");
    }

    [Fact(DisplayName = "A second window read backwards along the first leaves two pieces, and is refused naming the crossing window, in either order.")]
    public void ASecondWindowReadBackwardsIsRefused() {
        Check.ThatCode(() => Any.TimeOnly().Between(At2200, At0600).Between(new TimeOnly(5, 0), At2300))
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply Between(05:00:00.0000000, 23:00:00.0000000) because 23:00:00.0000000 comes before 05:00:00.0000000 along the window Between(22:00:00.0000000, 06:00:00.0000000) declares across midnight, which leaves two pieces rather than one window.");
        Check.ThatCode(() => Any.TimeOnly().Between(new TimeOnly(5, 0), At2300).Between(At2200, At0600))
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply Between(22:00:00.0000000, 06:00:00.0000000) because Between(05:00:00.0000000, 23:00:00.0000000) ends before it starts along the window it declares across midnight, which leaves two pieces rather than one window.");
        // Two windows that both cross midnight, the second reaching round past the first's start.
        Check.ThatCode(() => Any.TimeOnly().Between(At2200, At0600).Between(At0200, At0100))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(caught => caught.Message).Contains("two pieces", "Between(22:00:00.0000000, 06:00:00.0000000)");
    }

    [Fact(DisplayName = "Except and WithGranularity apply to both segments.")]
    public void ExceptAndGranularityApplyToBothSegments() {
        HashSet<TimeOnly> seen = [];
        AnyTimeOnly       grid = Any.TimeOnly().Between(At2300, At0100).WithGranularity(TimeSpan.FromMinutes(30)).Except(Midnight);
        for (int i = 0; i < 400; i++) { seen.Add(grid.Generate()); }

        Check.That(seen).IsOnlyMadeOf(At2300, new TimeOnly(23, 30), new TimeOnly(0, 30), At0100).And.HasSize(4);
    }

    [Fact(DisplayName = "A granularity that does not divide a day has no grid across midnight, and is refused there, in either order.")]
    public void AGranularityThatDoesNotDivideADayIsRefusedAcrossMidnight() {
        TimeSpan sevenMinutes = TimeSpan.FromMinutes(7);

        Check.ThatCode(() => Any.TimeOnly().Between(At2200, At0600).WithGranularity(sevenMinutes))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(caught => caught.Message).Contains("WithGranularity(00:07:00)", "Between(22:00:00.0000000, 06:00:00.0000000)", "does not divide a day");
        Check.ThatCode(() => Any.TimeOnly().WithGranularity(sevenMinutes).Between(At2200, At0600))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(caught => caught.Message).Contains("WithGranularity(00:07:00)", "Between(22:00:00.0000000, 06:00:00.0000000)", "does not divide a day");
        Check.That(Any.TimeOnly().Between(At0600, At2200).WithGranularity(sevenMinutes).Generate().Ticks % sevenMinutes.Ticks).IsEqualTo(0L);
    }

    [Fact(DisplayName = "A value set is filtered by the window, and a distinct collection counts the window's values.")]
    [SuppressMessage(JustDummiesRule.JD029.Category, JustDummiesRule.JD029.Id, Justification = SuppressionJustification.JD029.RefusedValueIsTheSubject)]
    public void AValueSetIsFilteredAndACollectionCounted() {
        for (int i = 0; i < 200; i++) { Check.That(Any.TimeOnly().OneOf(Noon, At2300, At0200).Between(At2200, At0600).Generate()).IsOneOf(At2300, At0200); }

        AnyTimeOnly halfHours = Any.TimeOnly().Between(At2300, At0100).WithGranularity(TimeSpan.FromMinutes(30));
        Check.That(Any.SetOf(halfHours).WithCount(5).Generate()).HasSize(5);
        Check.ThatCode(() => Any.SetOf(halfHours).WithCount(6).Generate()).Throws<ConflictingAnyConstraintException>();
    }

}
#endif
