#region Usings declarations

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

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

    #region Statics members declarations

    private static IPoolInspection<string> Inspect(AnyString generator) {
        return generator;
    }

    #endregion

    [Fact(DisplayName = "Only the generators whose pool the caller supplies carry the inspection.")]
    public void OnlyPoolBackedGeneratorsCarryTheInspection() {
        // The interface is optional by decision, so the cast is written as a test rather than assumed. A scalar
        // builder narrows within its own domain instead of picking from supplied values, and answers nothing here.
        // Asserted over the types rather than over instances: the compiler proves the negative cases outright, so
        // an `is` test against them is a warning rather than a check.
        Check.That(typeof(IPoolInspection<string>).IsAssignableFrom(typeof(AnyString))).IsTrue();
        Check.That(typeof(IPoolInspection<string>).IsAssignableFrom(typeof(AnyOneOf<string>))).IsTrue();
        Check.That(typeof(IPoolInspection<int>).IsAssignableFrom(typeof(AnyInt32))).IsFalse();
        Check.That(typeof(IPoolInspection<string>).IsAssignableFrom(typeof(AnyPattern))).IsFalse();
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
