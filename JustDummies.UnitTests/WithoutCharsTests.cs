#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     <c>WithoutChars</c>: a subtraction of the same family as <c>WithoutAlpha</c> and <c>WithoutNumeric</c>, narrowing
///     the pool by construction — so a scaffolded alphabet can be narrowed at a call site without redeclaring it.
/// </summary>
public sealed class WithoutCharsTests {

    private const int SampleCount = 300;

    [Fact(DisplayName = "The removed characters are never drawn, from a declared pool or from the default one.")]
    public void TheRemovedCharactersAreNeverDrawn() {
        AnyContext any = Any.WithSeed(20261101);
        for (int i = 0; i < SampleCount; i++) {
            Check.That(any.String().WithChars("ACGTN").WithLengthBetween(1, 50).WithoutChars("N").Generate()).Not.Contains("N");
            Check.That(any.String().Alpha().WithLengthBetween(1, 50).WithoutChars("aeiou").Generate().IndexOfAny("aeiou".ToCharArray())).IsEqualTo(-1);
        }
    }

    [Fact(DisplayName = "Subtractions accumulate, in any order.")]
    public void SubtractionsAccumulate() {
        AnyContext any = Any.WithSeed(3);
        for (int i = 0; i < SampleCount; i++) {
            string drawn = any.String().WithChars("ABCD").WithLength(6).WithoutChars("A").WithoutChars("B").Generate();
            Check.That(drawn.Trim('C', 'D')).IsEmpty();
        }
    }

    [Fact(DisplayName = "A subtraction that empties the pool is refused, naming both constraints.")]
    [SuppressMessage(JustDummiesRule.JD015.Category, JustDummiesRule.JD015.Id, Justification = SuppressionJustification.JD015.ThrowIsTheSubject)]
    public void ASubtractionThatEmptiesThePoolIsRefused() {
        Check.ThatCode(() => Any.String().WithChars("AB").WithoutChars("BA"))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(caught => caught.Message)
             .Contains("WithoutChars(\"BA\")", "WithChars(\"AB\")");
    }

    [Fact(DisplayName = "An anchored literal is exempt, as for every subtraction.")]
    public void AnAnchoredLiteralIsExempt() {
        Check.That(Any.WithSeed(1).String().WithChars("AB").WithLength(4).StartingWith("N").WithoutChars("N").Generate()).StartsWith("N");
    }

    [Fact(DisplayName = "A null, empty or split-surrogate argument is refused as an argument.")]
    public void BadArgumentsAreRefused() {
        Check.ThatCode(() => Any.String().WithoutChars(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.String().WithoutChars("")).Throws<ArgumentException>();
        Check.ThatCode(() => Any.String().WithoutChars("\uD83D")).Throws<ArgumentException>();
    }

}
