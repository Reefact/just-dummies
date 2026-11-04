#region Usings declarations

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     <c>Any.Pair(item1, item2)</c>: two values drawn from two bases, independently or tied by the one order
///     <c>Having</c> declares, held by construction and refused, when it cannot hold, where it is declared and naming
///     both sides (issue #428, ADR-0131).
/// </summary>
public sealed class AnyPairTests {

    private const int SampleCount = 400;

    private static AnyPair<int> Dating() {
        return Any.Pair(Any.Int32().Between(-800, 1600), Any.Int32().Between(-800, 1600));
    }

    [Fact(DisplayName = "Without a relation, each item is drawn from its own base, independently of the other.")]
    public void WithoutARelationTheItemsAreIndependent() {
        AnyPair<int> dating      = Dating();
        bool         firstBelow  = false;
        bool         secondBelow = false;
        for (int i = 0; i < SampleCount; i++) {
            (int item1, int item2) = dating.Generate();

            Check.That(item1).IsGreaterOrEqualThan(-800).And.IsLessOrEqualThan(1600);
            Check.That(item2).IsGreaterOrEqualThan(-800).And.IsLessOrEqualThan(1600);
            firstBelow  |= item1 < item2;
            secondBelow |= item2 < item1;
        }

        Check.That(firstBelow && secondBelow).IsTrue();
    }

    [Fact(DisplayName = "Without a relation, two bases no order could join are still a pair: nothing ties them.")]
    public void WithoutARelationDisjointBasesAreAPair() {
        (int item1, int item2) = Any.Pair(Any.Int32().Between(10, 20), Any.Int32().Between(0, 5)).Generate();

        Check.That(item1).IsGreaterOrEqualThan(10);
        Check.That(item2).IsLessOrEqualThan(5);
    }

    [Fact(DisplayName = "Item1LessThanOrEqualToItem2: Item1 never exceeds Item2, on every seed, inside both bases.")]
    public void LessThanOrEqualToHoldsOnEverySeed() {
        AnyPair<int> dating = Dating().Having.Item1LessThanOrEqualToItem2();
        for (int i = 0; i < SampleCount; i++) {
            (int from, int to) = dating.Generate();

            Check.That(from).IsLessOrEqualThan(to);
            Check.That(from).IsGreaterOrEqualThan(-800);
            Check.That(to).IsLessOrEqualThan(1600);
        }
    }

    [Fact(DisplayName = "The four orders draw exactly the pairs they admit, strict or not, in either direction.")]
    public void TheFourOrdersDrawExactlyThePairsTheyAdmit() {
        AnyPair<int> narrow = Any.Pair(Any.Int32().Between(1, 3), Any.Int32().Between(1, 3));

        Check.That(Seen(narrow.Having.Item1LessThanItem2())).IsOnlyMadeOf((1, 2), (1, 3), (2, 3)).And.HasSize(3);
        Check.That(Seen(narrow.Having.Item1GreaterThanItem2())).IsOnlyMadeOf((2, 1), (3, 1), (3, 2)).And.HasSize(3);
        Check.That(Seen(narrow.Having.Item1LessThanOrEqualToItem2())).IsOnlyMadeOf((1, 1), (1, 2), (1, 3), (2, 2), (2, 3), (3, 3)).And.HasSize(6);
        Check.That(Seen(narrow.Having.Item1GreaterThanOrEqualToItem2())).IsOnlyMadeOf((1, 1), (2, 1), (3, 1), (2, 2), (3, 2), (3, 3)).And.HasSize(6);

        static HashSet<(int, int)> Seen(AnyPair<int> pair) {
            HashSet<(int, int)> seen = [];
            for (int i = 0; i < SampleCount; i++) { seen.Add(pair.Generate()); }

            return seen;
        }
    }

    [Fact(DisplayName = "The side below is drawn kept within what the other base can follow, so disjoint bands never fail.")]
    public void DisjointBandsNeverFail() {
        AnyPair<int> bands = Any.Pair(Any.Int32().Between(-800, 800), Any.Int32().Between(800, 1600)).Having.Item1LessThanOrEqualToItem2();
        for (int i = 0; i < SampleCount; i++) {
            (int from, int to) = bands.Generate();

            Check.That(from).IsLessOrEqualThan(to);
        }

        // Where the other base ends below the top of the side below, that side is kept below that end — in either direction.
        AnyPair<int> crossing = Any.Pair(Any.Int32().Between(0, 100), Any.Int32().Between(0, 10)).Having.Item1LessThanOrEqualToItem2();
        AnyPair<int> mirrored = Any.Pair(Any.Int32().Between(0, 10), Any.Int32().Between(0, 100)).Having.Item1GreaterThanOrEqualToItem2();
        for (int i = 0; i < SampleCount; i++) {
            Check.That(crossing.Generate().Item1).IsLessOrEqualThan(10);
            Check.That(mirrored.Generate().Item2).IsLessOrEqualThan(10);
        }
    }

    [Fact(DisplayName = "The largest value of the base above is read through its lattice, exclusions and value set.")]
    [SuppressMessage(JustDummiesRule.JD029.Category, JustDummiesRule.JD029.Id, Justification = SuppressionJustification.JD029.RefusedValueIsTheSubject)]
    public void TheLargestValueIsReadThroughEveryConstraint() {
        AnyPair<int> lattice  = Any.Pair(Any.Int32().Between(0, 100), Any.Int32().Between(0, 25).MultipleOf(10)).Having.Item1LessThanOrEqualToItem2();
        AnyPair<int> excluded = Any.Pair(Any.Int32().Between(0, 100), Any.Int32().Between(0, 10).Except(10, 9)).Having.Item1LessThanOrEqualToItem2();
        AnyPair<int> pooled   = Any.Pair(Any.Int32().Between(0, 100), Any.Int32().OneOf(3, 7, 50).LessThan(20)).Having.Item1LessThanOrEqualToItem2();

        for (int i = 0; i < SampleCount; i++) {
            Check.That(lattice.Generate().Item1).IsLessOrEqualThan(20);
            Check.That(excluded.Generate().Item1).IsLessOrEqualThan(8);
            Check.That(pooled.Generate().Item1).IsLessOrEqualThan(7);
        }
    }

    [Fact(DisplayName = "An order no value of the side below can hold is refused where it is declared, naming both sides.")]
    public void AnOrderThatCannotHoldIsRefusedNamingBothSides() {
        Check.ThatCode(() => Any.Pair(Any.Int32().Between(10, 20), Any.Int32().Between(0, 5)).Having.Item1LessThanOrEqualToItem2())
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply Having.Item1LessThanOrEqualToItem2() because no Item1 value lies at or below 5, the largest value of Item2's base: Between(10, 20) already requires values greater than or equal to 10.");
        Check.ThatCode(() => Any.Pair(Any.Int32().Between(0, 5), Any.Int32().Between(10, 20)).Having.Item1GreaterThanItem2())
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply Having.Item1GreaterThanItem2() because no Item2 value lies below 5, the largest value of Item1's base: Between(10, 20) already requires values greater than or equal to 10.");
        Check.ThatCode(() => Any.Pair(Any.Int32().Between(5, 5), Any.Int32().Between(5, 5)).Having.Item1LessThanItem2())
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(caught => caught.Message).Contains("Having.Item1LessThanItem2()", "below 5");
    }

    [Fact(DisplayName = "A pair declares one relation: the same again changes nothing, another is refused naming the first.")]
    public void APairDeclaresOneRelation() {
        AnyPair<int> ordered = Dating().Having.Item1LessThanItem2();

        Check.That(ordered.Having.Item1LessThanItem2()).IsSameReferenceAs(ordered);
        Check.ThatCode(() => ordered.Having.Item1LessThanOrEqualToItem2())
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply Having.Item1LessThanOrEqualToItem2() because Having.Item1LessThanItem2() is already defined.");
    }

    [Fact(DisplayName = "Having is read, not built: the same relation holder on every read.")]
    public void HavingIsTheSameOnEveryRead() {
        AnyPair<int> dating = Dating();

        Check.That(dating.Having).IsSameReferenceAs(dating.Having);
    }

    [Fact(DisplayName = "Every ordered family carries the pair, in its builder's own vocabulary: dates, durations, decimals and floats.")]
    public void EveryOrderedFamilyCarriesThePair() {
        DateTime       origin       = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTimeOffset originOffset = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (int i = 0; i < 50; i++) {
            (DateTime start, DateTime end)          = Any.Pair(Any.DateTime().Between(origin, origin.AddDays(30)), Any.DateTime().Between(origin, origin.AddDays(30))).Having.Item1BeforeOrEqualToItem2().Generate();
            (DateTimeOffset closes, DateTimeOffset opens) = Any.Pair(Any.DateTimeOffset().Between(originOffset, originOffset.AddDays(30)), Any.DateTimeOffset().Between(originOffset, originOffset.AddDays(30))).Having.Item1AfterItem2().Generate();
            (TimeSpan shortest, TimeSpan longest)   = Any.Pair(Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromHours(1)), Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromHours(1))).Having.Item1LessThanItem2().Generate();
            (decimal high, decimal low)             = Any.Pair(Any.Decimal().Between(0m, 100m).WithScale(2), Any.Decimal().Between(0m, 100m).WithScale(2)).Having.Item1GreaterThanOrEqualToItem2().Generate();
            (double min, double max)                = Any.Pair(Any.Double().Between(0, 1), Any.Double().Between(0, 1)).Having.Item1LessThanItem2().Generate();
#if NET8_0_OR_GREATER
            (DateOnly from, DateOnly to) = Any.Pair(Any.DateOnly().Between(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), Any.DateOnly().Between(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))).Having.Item1BeforeItem2().Generate();
            Check.That(from < to).IsTrue();
#endif

            Check.That(start <= end).IsTrue();
            Check.That(closes > opens).IsTrue();
            Check.That(shortest < longest).IsTrue();
            Check.That(high >= low).IsTrue();
            Check.That(min < max).IsTrue();
        }
    }

    [Fact(DisplayName = "A DateTime base supplied a Local value is refused once an order makes a drawn value a bound, on either side.")]
    public void ALocalValueIsRefusedOnceTheOrderMakesItABound() {
        DateTime local = new(2026, 3, 10, 9, 0, 0, DateTimeKind.Local);
        DateTime utc   = new(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);

        Check.ThatCode(() => Any.Pair(Any.DateTime().OneOf(local, utc), Any.DateTime()).Having.Item1BeforeItem2())
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(caught => caught.Message).Contains("Having.Item1BeforeItem2()", "Item1's base holds", "DateTimeKind.Local");
        Check.ThatCode(() => Any.Pair(Any.DateTime(), Any.DateTime().OneOf(local)).Having.Item1AfterOrEqualToItem2())
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(caught => caught.Message).Contains("Having.Item1AfterOrEqualToItem2()", "Item2's base holds");
    }

    [Fact(DisplayName = "A Local value bounds nothing without an order, or once the base excludes it, and is then accepted.")]
    public void ALocalValueThatBoundsNothingIsAccepted() {
        DateTime local = new(2026, 3, 10, 9, 0, 0, DateTimeKind.Local);
        DateTime utc   = new(2026, 3, 11, 9, 0, 0, DateTimeKind.Utc);

        Check.That(Any.Pair(Any.DateTime().OneOf(local), Any.DateTime()).Generate().Item1.Kind).IsEqualTo(DateTimeKind.Local);
        Check.That(Any.Pair(Any.DateTime().OneOf(local, utc).Except(local), Any.DateTime()).Having.Item1BeforeItem2().Generate().Item1).IsEqualTo(utc);
    }

    [Fact(DisplayName = "The pair replays from its bases' seed, and composes with Then for a third value between the two.")]
    public void ThePairReplaysAndComposes() {
        Check.That(Draws(428)).ContainsExactly(Draws(428));

        IAny<(int Min, int Optimum, int Max)> triple = Dating().Having.Item1LessThanItem2().Then(pair => Any.Int32().Between(pair.Item1, pair.Item2).As(optimum => (pair.Item1, optimum, pair.Item2)));
        for (int i = 0; i < SampleCount; i++) {
            (int min, int optimum, int max) = triple.Generate();
            Check.That(min <= optimum && optimum <= max && min < max).IsTrue();
        }

        static List<(int, int)> Draws(int seed) {
            AnyContext   any    = Any.WithSeed(seed);
            AnyPair<int> dating = Any.Pair(any.Int32().Between(-800, 1600), any.Int32().Between(-800, 1600)).Having.Item1LessThanOrEqualToItem2();

            return Enumerable.Range(0, 30).Select(_ => dating.Generate()).ToList();
        }
    }

    [Fact(DisplayName = "Every ordered builder has its Pair and exactly the four relations of its own vocabulary; TimeOnly has none.")]
    public void EveryOrderedBuilderHasItsPairAndItsFourRelations() {
        string[] comparisons = ["Item1LessThanItem2", "Item1LessThanOrEqualToItem2", "Item1GreaterThanItem2", "Item1GreaterThanOrEqualToItem2"];
        string[] chronology  = ["Item1BeforeItem2", "Item1BeforeOrEqualToItem2", "Item1AfterItem2", "Item1AfterOrEqualToItem2"];
        Type[] builders = typeof(Any).Assembly.GetExportedTypes()
                                     .Where(type => type.Name.StartsWith("Any", StringComparison.Ordinal) && !type.IsGenericTypeDefinition)
                                     .Where(type => type.GetMethod("LessThanOrEqualTo") is not null || type.GetMethod("BeforeOrEqualTo") is not null)
                                     .Where(type => type.Name != "AnyTimeOnly")
                                     .ToArray();
        List<string> drift = [];

        foreach (Type builder in builders) {
            Type   value      = builder.GetInterfaces().Single(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IAny<>)).GetGenericArguments()[0];
            Type   pair       = typeof(AnyPair<>).MakeGenericType(value);
            Type   relation   = typeof(AnyPairRelation<>).MakeGenericType(value);
            bool   isDate     = builder.GetMethod("BeforeOrEqualTo") is not null;
            string[] expected = isDate ? chronology : comparisons;

            if (typeof(Any).GetMethod(nameof(Any.Pair), [builder, builder])?.ReturnType != pair) { drift.Add($"Any.Pair({builder.Name}, {builder.Name})"); }

            string[] declared = typeof(AnyPairRelations).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                                        .Where(method => method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == relation && method.ReturnType == pair)
                                                        .Select(method => method.Name)
                                                        .OrderBy(name => name, StringComparer.Ordinal)
                                                        .ToArray();
            if (!declared.SequenceEqual(expected.OrderBy(name => name, StringComparer.Ordinal))) { drift.Add($"{value.Name}: [{string.Join(", ", declared)}]"); }
        }

        Check.That(builders).Not.IsEmpty();
        Check.WithCustomMessage($"Drift: [{string.Join("; ", drift)}]").That(drift).IsEmpty();
        Check.That(typeof(Any).GetMethods().Where(method => method.Name == nameof(Any.Pair))).HasSize(builders.Length);
    }

    [Fact(DisplayName = "A null base or a null relation holder is refused as an argument.")]
    public void ANullArgumentIsRefused() {
        Check.ThatCode(() => Any.Pair(null!, Any.Int32())).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.Pair(Any.Int32(), null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => ((AnyPairRelation<int>)null!).Item1LessThanItem2()).Throws<ArgumentNullException>();
    }

}
