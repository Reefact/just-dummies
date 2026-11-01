#region Usings declarations

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     <c>Ordered()</c> and <c>OrderedBy(key)</c> on the list and array builders: the elements are drawn, then sorted
///     ascending, so the order changes the arrangement and never the content (issue #360, ADR-0124).
/// </summary>
public sealed class OrderedTests {

    private const int SampleCount = 200;

    private sealed record Reading(DateTime At, int Value);

    private static IAny<Reading> Readings(AnyContext any) {
        return Any.Combine(any.DateTime().Between(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)), any.Int32().Between(0, 3), (at, value) => new Reading(at, value));
    }

    [Fact(DisplayName = "Ordered arranges a list in non-decreasing order, repeats included.")]
    public void OrderedArrangesAListInNonDecreasingOrder() {
        for (int i = 0; i < SampleCount; i++) {
            List<int> value = Any.ListOf(Any.Int32().Between(0, 5)).WithCount(20).Ordered().Generate();

            Check.That(value).ContainsExactly(value.OrderBy(element => element));
        }
    }

    [Fact(DisplayName = "Ordered().Distinct() is strictly increasing.")]
    public void OrderedDistinctIsStrictlyIncreasing() {
        for (int i = 0; i < SampleCount; i++) {
            double[] value = Any.ArrayOf(Any.Double().Between(0, 100)).WithCount(4).Ordered().Distinct().Generate();

            Check.That(value.Zip(value.Skip(1), (left, right) => left < right).All(increasing => increasing)).IsTrue();
        }
    }

    [Fact(DisplayName = "The order changes the arrangement, not the content: the count and the contained values hold, wherever the order is declared.")]
    public void TheOrderChangesTheArrangementNotTheContent() {
        for (int i = 0; i < SampleCount; i++) {
            List<int> declaredFirst = Any.ListOf(Any.Int32().Between(0, 100)).Ordered().WithCount(6).Containing(50).Generate();
            List<int> declaredLast  = Any.ListOf(Any.Int32().Between(0, 100)).WithCount(6).Containing(50).Ordered().Generate();

            Check.That(declaredFirst).HasSize(6).And.Contains(50).And.ContainsExactly(declaredFirst.OrderBy(element => element));
            Check.That(declaredLast).HasSize(6).And.Contains(50).And.ContainsExactly(declaredLast.OrderBy(element => element));
        }
    }

    [Fact(DisplayName = "The order consumes no draw: the same seed yields the same elements, sorted.")]
    public void TheOrderConsumesNoDraw() {
        List<int> drawn   = Any.ListOf(Any.WithSeed(360).Int32()).WithCount(12).Generate();
        List<int> ordered = Any.ListOf(Any.WithSeed(360).Int32()).WithCount(12).Ordered().Generate();

        Check.That(ordered).ContainsExactly(drawn.OrderBy(element => element));
    }

    [Fact(DisplayName = "OrderedBy arranges the elements by the key, and OrderedBy(key).DistinctBy(key) is strictly increasing by it.")]
    public void OrderedByArrangesTheElementsByTheKey() {
        AnyContext any = Any.WithSeed(20261101);
        for (int i = 0; i < SampleCount; i++) {
            List<Reading> byTime   = Any.ListOf(Readings(any)).WithCount(8).OrderedBy(reading => reading.At).Generate();
            Reading[]     byValue  = Any.ArrayOf(Readings(any)).WithCount(4).OrderedBy(reading => reading.Value).DistinctBy(reading => reading.Value).Generate();

            Check.That(byTime.Zip(byTime.Skip(1), (left, right) => left.At <= right.At).All(ordered => ordered)).IsTrue();
            Check.That(byValue.Select(reading => reading.Value)).ContainsExactly(0, 1, 2, 3);
        }
    }

    [Fact(DisplayName = "The order is declared once: the same order again changes nothing, a different one is refused naming both.")]
    [SuppressMessage(JustDummiesRule.JD035.Category, JustDummiesRule.JD035.Id, Justification = SuppressionJustification.JD035.RepetitionIsTheSubject)]
    public void TheOrderIsDeclaredOnce() {
        Func<Reading, DateTime> byTime = reading => reading.At;
        AnyContext              any    = Any.WithSeed(5);

        List<int> twice = Any.ListOf(Any.Int32()).WithCount(5).Ordered().Ordered().Generate();
        Check.That(twice).ContainsExactly(twice.OrderBy(element => element));
        Check.That(Any.ListOf(Readings(any)).WithCount(3).OrderedBy(byTime).OrderedBy(byTime).Generate()).HasSize(3);

        Check.ThatCode(() => Any.ListOf(Any.Int32()).Ordered().OrderedBy(value => -value))
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply OrderedBy(keySelector) because Ordered() is already defined.");
        Check.ThatCode(() => Any.ArrayOf(Readings(any)).OrderedBy(byTime).OrderedBy(reading => reading.Value))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(caught => caught.Message).Contains("OrderedBy(keySelector)");
    }

    [Fact(DisplayName = "Ordered is offered only on an element type with a natural order: its type parameter requires IComparable<T>.")]
    public void OrderedRequiresAnElementTypeWithANaturalOrder() {
        IEnumerable<MethodInfo> ordered = typeof(OrderingExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(method => method.Name == "Ordered");

        Check.That(ordered.Count()).IsEqualTo(2);
        Check.That(ordered.All(method => method.GetGenericArguments()[0].GetGenericParameterConstraints().Any(constraint => constraint.IsGenericType && constraint.GetGenericTypeDefinition() == typeof(IComparable<>))))
             .IsTrue();
    }

    [Fact(DisplayName = "A null key selector or a null generator is refused as an argument.")]
    public void ANullArgumentIsRefused() {
        Check.ThatCode(() => Any.ListOf(Any.Int32()).OrderedBy<int>(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => ((AnyList<int>)null!).Ordered()).Throws<ArgumentNullException>();
    }

}
