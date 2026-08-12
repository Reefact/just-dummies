#region Usings declarations

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     The pool inspection (ADR-0067): what a generator's declared constraints left of a caller-supplied value set,
///     and what they took. These are the named cases — which constraint a rejection blames, what a generator
///     carrying no value set answers, and which generators expose the interface at all. The universal half, that
///     survivors and rejections partition the supplied pool whatever the pool and the constraints, is in
///     <c>JustDummies.PropertyTests</c>.
/// </summary>
public sealed class PoolInspectionTests {

    /// <summary>A small enum for the pooled-enum cases; the universe is the declaration, never a caller's list.</summary>
    public enum Priority { Low, Medium, High }

    #region Statics members declarations

    private static IPoolInspection<string> Inspect(AnyString generator) {
        return generator;
    }

    /// <summary>Whether <paramref name="type" /> closes <paramref name="definition" /> at any type argument.</summary>
    private static bool Implements(Type type, Type definition) {
        return type.GetInterfaces().Any(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == definition);
    }

    #endregion

    [Fact(DisplayName = "Every generator that admits a caller-supplied value set carries the inspection.")]
    public void EveryPooledGeneratorCarriesTheInspection() {
        // Reflection rather than a hand-kept list: the drift this pins is a family that gains OneOf — or a whole new
        // family — without the inspection, which would leave the surface asymmetric for no stated reason.
        List<string> missing = typeof(Any).Assembly
                                          .GetTypes()
                                          .Where(type => type.IsPublic && Implements(type, typeof(IAny<>)))
                                          .Where(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                                             .Any(method => method.Name == "OneOf"))
                                          .Where(type => !Implements(type, typeof(IPoolInspection<>)))
                                          .Select(type => type.Name)
                                          .OrderBy(name => name, StringComparer.Ordinal)
                                          .ToList();

        Check.WithCustomMessage($"These generators expose OneOf but not IPoolInspection<T>: {string.Join(", ", missing)}")
             .That(missing)
             .IsEmpty();
    }

    [Fact(DisplayName = "A generator with no caller-supplied pool does not carry the inspection at all.")]
    public void AGeneratorWithoutAPoolDoesNotCarryTheInspection() {
        // The interface is optional by decision, so the cast is written as a test rather than assumed. A pattern
        // builds its value from a language, and a boolean has a two-value universe nobody supplied: neither has a
        // pool of the caller's to report on, so neither answers here — not even with an empty report.
        Check.That(Implements(typeof(AnyPattern), typeof(IPoolInspection<>))).IsFalse();
        Check.That(Implements(typeof(AnyBoolean), typeof(IPoolInspection<>))).IsFalse();
    }

    [Fact(DisplayName = "A scalar interval is not a pool, however countable it is.")]
    public void AScalarIntervalIsNotAPool() {
        // The trap this pins: a bounded integer range HAS a cardinality, so wiring IsPooled to "the domain is
        // countable" would compile and then try to enumerate a range nobody supplied. The inspection reports on a
        // pool the CALLER handed over, never on the generator's own domain.
        IPoolInspection<int> inspection = Any.Int32().Between(1, 1_000_000);

        Check.That(inspection.IsPooled).IsFalse();
        Check.That(inspection.GetSurvivors()).IsEmpty();
        Check.That(inspection.GetRejections()).IsEmpty();
    }

    [Fact(DisplayName = "On an integer pool the bound that removed a value is the one the rejection names.")]
    public void AnIntegerPoolNamesTheBoundThatRefusedTheValue() {
        IPoolInspection<int> inspection = Any.Int32().OneOf(1, 5, 42).Between(1, 10);

        Check.That(inspection.GetSurvivors()).ContainsExactly(1, 5);
        Check.That(inspection.GetRejections().Single().Value).IsEqualTo(42);
        Check.That(inspection.GetRejections().Single().RejectedBy.Single().ToString()).IsEqualTo("Between(1, 10)");
    }

    [Fact(DisplayName = "A two-bound call is named once, under the name the caller wrote.")]
    public void ATwoBoundCallIsNamedOnce() {
        // Between sets a minimum and a maximum under one name, and the caller can only loosen the call. Naming a
        // half would point at something they cannot edit on its own.
        IPoolInspection<int> inspection = Any.Int32().OneOf(0, 15, 50).Between(10, 20);

        Check.That(inspection.GetSurvivors()).ContainsExactly(15);
        Check.That(inspection.GetRejections().Select(rejection => rejection.Value)).ContainsExactly(0, 50);
        Check.That(inspection.GetRejections()[0].RejectedBy).HasSize(1);
        Check.That(inspection.GetRejections()[0].RejectedBy.Single().ToString()).IsEqualTo("Between(10, 20)");
    }

    [Fact(DisplayName = "A date pool reports in the caller's own type, not in the engine's ordinal space.")]
    public void ADatePoolReportsInTheCallersType() {
        DateTime kept    = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Unspecified);
        DateTime refused = new(2020, 3, 1, 0, 0, 0, DateTimeKind.Unspecified);

        IPoolInspection<DateTime> inspection = Any.DateTime().OneOf(kept, refused).After(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Unspecified));

        Check.That(inspection.GetSurvivors()).ContainsExactly(kept);
        Check.That(inspection.GetRejections().Single().Value).IsEqualTo(refused);
    }

    [Fact(DisplayName = "A decimal pool names the scale that refused a value.")]
    public void ADecimalPoolNamesTheScaleThatRefusedTheValue() {
        IPoolInspection<decimal> inspection = Any.Decimal().OneOf(1.5m, 2.25m).WithScale(1);

        Check.That(inspection.GetSurvivors()).ContainsExactly(1.5m);
        Check.That(inspection.GetRejections().Single().Value).IsEqualTo(2.25m);
    }

    [Fact(DisplayName = "A character pool names the character family that refused a value.")]
    public void ACharacterPoolNamesTheFamilyThatRefusedTheValue() {
        IPoolInspection<char> inspection = Any.Char().OneOf('a', '3').Numeric();

        Check.That(inspection.GetSurvivors()).ContainsExactly('3');
        Check.That(inspection.GetRejections().Single().Value).IsEqualTo('a');
        Check.That(inspection.GetRejections().Single().RejectedBy.Single().ToString()).IsEqualTo("Numeric()");
    }

    [Fact(DisplayName = "A Guid pool names the exclusion that removed a value.")]
    public void AGuidPoolNamesTheExclusionThatRemovedTheValue() {
        Guid kept    = Guid.NewGuid();
        Guid removed = Guid.NewGuid();

        IPoolInspection<Guid> inspection = Any.Guid().OneOf(kept, removed).Except(removed);

        Check.That(inspection.GetSurvivors()).ContainsExactly(kept);
        Check.That(inspection.GetRejections().Single().Value).IsEqualTo(removed);
        Check.That(inspection.GetRejections().Single().RejectedBy.Single().Name).IsEqualTo("Except");
    }

    [Fact(DisplayName = "An enum without OneOf is not pooled: its universe is the declaration's, not the caller's.")]
    public void AnEnumUniverseIsNotAPool() {
        Check.That(((IPoolInspection<Priority>)Any.Enum<Priority>()).IsPooled).IsFalse();
        Check.That(((IPoolInspection<Priority>)Any.Enum<Priority>().OneOf(Priority.Low, Priority.High)).IsPooled).IsTrue();
    }

    [Fact(DisplayName = "A pin dominates the report, because it dominates the draw.")]
    public void APinDominatesTheReport() {
        // Generate short-circuits on the pin before the allow-list is reached, so every other pooled value is
        // undrawable. Reporting them as survivors would name values no draw can yield.
        Guid other = Guid.NewGuid();

        IPoolInspection<Guid> inspection = Any.Guid().Empty().OneOf(Guid.Empty, other);

        Check.That(inspection.GetSurvivors()).ContainsExactly(Guid.Empty);
        Check.That(inspection.GetRejections().Single().Value).IsEqualTo(other);
        Check.That(inspection.GetRejections().Single().RejectedBy.Single().Name).IsEqualTo("Empty");
    }

    [Fact(DisplayName = "A top-level pool names every exclusion refusing a value, not only the one that removed it.")]
    public void ATopLevelPoolNamesEveryRefusingExclusion() {
        // The second exclusion finds the value already gone, but it refuses it just the same. A reader told to
        // loosen only the first would find the value still absent.
        IPoolInspection<string> inspection = (IPoolInspection<string>)Any.OneOf("a", "b").Except("a").DifferentFrom("a");

        Check.That(inspection.GetRejections().Single().Value).IsEqualTo("a");
        Check.That(inspection.GetRejections().Single().RejectedBy.Select(constraint => constraint.Name))
             .IsOnlyMadeOf("Except", "DifferentFrom");
    }

    [Fact(DisplayName = "A date pool reports the Kind the draw returns, not the engine's normalized one.")]
    public void ADatePoolReportsTheSuppliedKind() {
        // The ordinal carries only the ticks. Rebuilding from it would report Utc for a value the draw yields as
        // Local — a survivor that does not equal what comes out of Generate().
        DateTime local = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Local);

        IPoolInspection<DateTime> inspection = Any.DateTime().OneOf(local);

        Check.That(inspection.GetSurvivors().Single().Kind).IsEqualTo(DateTimeKind.Local);
        Check.That(inspection.GetSurvivors().Single()).IsEqualTo(local);
    }

    [Fact(DisplayName = "An offset-refused pooled value is reported, and the survivors keep their supplied offset.")]
    public void AnOffsetRefusedValueIsReported() {
        // The offset dimension filters the pool OUTSIDE the ordinal engine, so without care the values it removes
        // appear in neither list and the supplied pool stops adding up.
        DateTimeOffset kept    = new(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset refused = new(2026, 3, 1, 0, 0, 0, TimeSpan.FromHours(2));

        IPoolInspection<DateTimeOffset> inspection = Any.DateTimeOffset().OneOf(kept, refused).WithOffset(TimeSpan.Zero);

        Check.That(inspection.GetSurvivors()).ContainsExactly(kept);
        Check.That(inspection.GetSurvivors().Single().Offset).IsEqualTo(TimeSpan.Zero);
        Check.That(inspection.GetRejections().Single().Value).IsEqualTo(refused);
        Check.That(inspection.GetRejections().Single().RejectedBy.Single().Name).IsEqualTo("WithOffset");
    }

    [Fact(DisplayName = "Mutating the array handed to Except cannot change what the report says.")]
    public void TheReportDoesNotFollowTheCallersArray() {
        // The excluded values are copied at the boundary. Retaining the caller's array would let a later mutation
        // make the report accuse a value the generator actually draws.
        char[] excluded = ['a'];

        IPoolInspection<char> inspection = Any.Char().OneOf('a', 'b').Except(excluded);
        excluded[0] = 'b';

        Check.That(inspection.GetSurvivors()).ContainsExactly('b');
        Check.That(inspection.GetRejections().Single().Value).IsEqualTo('a');
        Check.That(inspection.GetRejections().Single().RejectedBy).HasSize(1);
    }

    [Fact(DisplayName = "A shaped string is not pooled, and reports neither survivors nor rejections.")]
    public void AShapedStringReportsNothing() {
        // Answering "no value set here" is the honest answer to the question, not a reason to refuse it: a caller
        // who inspects a generator built by shaping gets an empty report rather than an exception.
        IPoolInspection<string> inspection = Inspect(Any.String().WithLengthBetween(1, 64).Alpha());

        Check.That(inspection.IsPooled).IsFalse();
        Check.That(inspection.GetSurvivors()).IsEmpty();
        Check.That(inspection.GetRejections()).IsEmpty();
    }

    [Fact(DisplayName = "The survivors are the exact domain the draw picks from, in the order they were supplied.")]
    public void SurvivorsAreTheDomainTheDrawPicksFrom() {
        IPoolInspection<string> inspection = Inspect(Any.String().OneOf("Camille", "X", "Ada").WithMinLength(2));

        Check.That(inspection.IsPooled).IsTrue();
        Check.That(inspection.GetSurvivors()).ContainsExactly("Camille", "Ada");
    }

    [Fact(DisplayName = "A rejection names the constraint that refused the value.")]
    public void ARejectionNamesTheConstraintThatRefusedTheValue() {
        IReadOnlyList<PoolRejection<string>> rejections = Inspect(Any.String().OneOf("abc", "de").WithLength(3)).GetRejections();

        Check.That(rejections).HasSize(1);
        Check.That(rejections[0].Value).IsEqualTo("de");
        Check.That(rejections[0].RejectedBy.Select(constraint => constraint.ToString())).ContainsExactly("WithLength(3)");
    }

    [Fact(DisplayName = "A rejection names every constraint that refuses the value, not the first one met.")]
    public void ARejectionNamesEveryConstraintThatRefusesTheValue() {
        // "abcd" misses on both counts. Naming only one would send a reader at a constraint they could loosen
        // without changing the verdict — the value would still be rejected by the other.
        IReadOnlyList<PoolRejection<string>> rejections = Inspect(Any.String().OneOf("12", "abcd", "123").WithMaxLength(3).Numeric()).GetRejections();

        Check.That(rejections).HasSize(1);
        Check.That(rejections[0].Value).IsEqualTo("abcd");
        Check.That(rejections[0].RejectedBy.Select(constraint => constraint.ToString())).IsOnlyMadeOf("WithMaxLength(3)", "Numeric()");
    }

    [Fact(DisplayName = "A declared constraint carries its name and its rendered arguments apart, not one string to parse.")]
    public void ADeclaredConstraintKeepsItsNameAndArgumentsApart() {
        DeclaredConstraint constraint = Inspect(Any.String().OneOf("abc", "de").WithLength(3)).GetRejections()[0].RejectedBy[0];

        Check.That(constraint.Name).IsEqualTo("WithLength");
        Check.That(constraint.Arguments).IsEqualTo("3");
        Check.That(constraint.ToString()).IsEqualTo("WithLength(3)");
    }

    [Fact(DisplayName = "A pool in step with its constraints reports no rejection at all.")]
    public void APoolInStepWithItsConstraintsReportsNothing() {
        Check.That(Inspect(Any.String().OneOf("EUR", "USD", "GBP").WithLength(3)).GetRejections()).IsEmpty();
    }

    [Fact(DisplayName = "A duplicate collapses without being reported as a rejection.")]
    [SuppressMessage(JustDummiesRule.JD025.Category, JustDummiesRule.JD025.Id, Justification = SuppressionJustification.JD025.DuplicateIsTheSubject)]
    public void ADuplicateCollapsesWithoutBeingRejected() {
        // The second "Ada" is the same value, not a refused one: it is absent from the survivors because it is
        // already there, which is not a reason to blame a constraint for it.
        IPoolInspection<string> inspection = Inspect(Any.String().OneOf("Ada", "Ada", "Camille"));

        Check.That(inspection.GetSurvivors()).ContainsExactly("Ada", "Camille");
        Check.That(inspection.GetRejections()).IsEmpty();
    }

    [Fact(DisplayName = "On a top-level pool the exclusion that removed a value is the one the rejection names.")]
    public void AnExclusionOnATopLevelPoolNamesItself() {
        IPoolInspection<string> inspection = (IPoolInspection<string>)Any.OneOf("a", "b", "c").DifferentFrom("b");

        Check.That(inspection.IsPooled).IsTrue();
        Check.That(inspection.GetSurvivors()).ContainsExactly("a", "c");
        Check.That(inspection.GetRejections().Single().Value).IsEqualTo("b");
        Check.That(inspection.GetRejections().Single().RejectedBy.Single().Name).IsEqualTo("DifferentFrom");
    }

    [Fact(DisplayName = "A top-level pool renders its arguments elided, because the element type is the caller's.")]
    public void ATopLevelPoolRendersItsArgumentsElided() {
        // T is opaque, so its ToString belongs to the caller and could be anything; the library must not quote it.
        DeclaredConstraint constraint = ((IPoolInspection<string>)Any.OneOf("a", "b").Except("b")).GetRejections()[0].RejectedBy[0];

        Check.That(constraint.Arguments).IsEqualTo("...");
        Check.That(constraint.ToString()).IsEqualTo("Except(...)");
    }

    [Fact(DisplayName = "An exclusion naming a value the pool never held reports no rejection.")]
    public void AnExclusionOfAnAbsentValueReportsNothing() {
        Check.That(((IPoolInspection<string>)Any.OneOf("a", "b").Except("z")).GetRejections()).IsEmpty();
    }

    [Fact(DisplayName = "The reported lists cannot be cast back to something mutable.")]
    public void TheReportedListsAreNotAMutableHandle() {
        // A report a caller can edit is a report about nothing. The survivors in particular are the live domain the
        // draw samples, so handing the inner list out would let a caller change what the generator produces.
        IPoolInspection<string> inspection = Inspect(Any.String().OneOf("abc", "de").WithLength(3));

        Check.That(inspection.GetSurvivors() as List<string>).IsNull();
        Check.That(inspection.GetRejections() as List<PoolRejection<string>>).IsNull();
        Check.That(inspection.GetRejections()[0].RejectedBy).IsInstanceOf<ReadOnlyCollection<DeclaredConstraint>>();
    }

}
