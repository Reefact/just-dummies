#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     The value identity of <see cref="DeclaredConstraint" />: the reason a pooled value never draws travels as a
///     value a caller can compare and group by, rather than as text they would have to parse back (ADR-0042). What
///     <see cref="ValueObjectConventionTests" /> settles by reflection is that the members exist; that two equal
///     constraints really hash alike is settled here.
/// </summary>
/// <remarks>
///     These belong to the example suite (ADR-0019): each asserts one named pair, and the null cases have no input
///     space.
/// </remarks>
[TestSubject(typeof(DeclaredConstraint))]
public sealed class DeclaredConstraintTests {

    [Fact(DisplayName = "A declared constraint renders as the caller spelled it.")]
    public void RendersTheDeclarationAsWritten() {
        DeclaredConstraint constraint = new("WithMinLength", "2");

        Check.That(constraint.ToString()).IsEqualTo("WithMinLength(2)");
        Check.That(constraint.Name).IsEqualTo("WithMinLength");
        Check.That(constraint.Arguments).IsEqualTo("2");
    }

    // Grouping a catalogue's rejections by the constraint that took them is the use ADR-0067 names, and it rests on
    // equality being over the reading rather than over the instance.
    [Fact(DisplayName = "Two constraints built apart but reading the same are equal and hash alike.")]
    public void EqualsAnotherConstraintThatReadsTheSame() {
        DeclaredConstraint first  = new("WithLengthBetween", "2, 3");
        DeclaredConstraint second = new("WithLengthBetween", "2, 3");

        Check.That(first.Equals(second)).IsTrue();
        Check.That(first == second).IsTrue();
        Check.That(first != second).IsFalse();
        Check.That(first.GetHashCode()).IsEqualTo(second.GetHashCode());
    }

    [Fact(DisplayName = "The same name carrying different arguments is a different constraint.")]
    public void DiffersFromTheSameNameWithOtherArguments() {
        DeclaredConstraint first  = new("WithLength", "3");
        DeclaredConstraint second = new("WithLength", "4");

        Check.That(first.Equals(second)).IsFalse();
        Check.That(first != second).IsTrue();
    }

    [Fact(DisplayName = "Equality is ordinal, so casing tells two constraints apart.")]
    public void ComparesOrdinally() {
        Check.That(new DeclaredConstraint("numeric", "") == new DeclaredConstraint("Numeric", "")).IsFalse();
    }

    [Fact(DisplayName = "A constraint equals neither null nor a value of another type.")]
    public void EqualsNeitherNullNorAnotherType() {
        DeclaredConstraint  constraint = new("Numeric", "");
        DeclaredConstraint? nothing    = null;
        object              text       = "Numeric()";

        Check.That(constraint.Equals(nothing)).IsFalse();
        Check.That(constraint.Equals(text)).IsFalse();
        Check.That(constraint == nothing).IsFalse();
        Check.That(constraint != nothing).IsTrue();
    }

}
