#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     Named-case coverage for <see cref="AnyChar" /> that a property cannot express: the shape of the
///     unconstrained universe itself, and conflict message content. The family, casing, subtraction and exclusion
///     algebra is already quantified generically by <c>CharacterFamilyProperties</c>.
/// </summary>
[TestSubject(typeof(AnyChar))]
public sealed class AnyCharTests {

    private const int SampleCount = 300;

    #region Statics members declarations

    private static IEnumerable<char> Samples(IAny<char> generator) {
        for (int i = 0; i < SampleCount; i++) {
            yield return generator.Generate();
        }
    }

    #endregion

    [Fact(DisplayName = "An unconstrained draw can be a control character: the drawable universe is the whole of ASCII, 0x00 to 0x7F.")]
    public void UnconstrainedDrawIsTheWholeOfAscii() {
        List<char> values = Samples(Any.Char()).ToList();

        Check.That(values.All(value => value <= (char)0x7F)).IsTrue();
        // Not merely bounded above: a control character must actually be reachable, or ADR-0075's widened universe
        // would be a promise the draw never keeps.
        Check.That(values.Any(value => value < ' ' || value == (char)0x7F)).IsTrue();
    }

    [Fact(DisplayName = "A second character family conflict names both sides.")]
    [SuppressMessage(JustDummiesRule.JD034.Category, JustDummiesRule.JD034.Id, Justification = SuppressionJustification.JD034.ThrowIsTheSubject)]
    public void ASecondFamilyConflictNamesBothSides() {
        Check.ThatCode(() => Any.Char().Alpha().Numeric())
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("Alpha()", "Numeric()");
    }

    [Fact(DisplayName = "A second casing conflict names both sides.")]
    [SuppressMessage(JustDummiesRule.JD034.Category, JustDummiesRule.JD034.Id, Justification = SuppressionJustification.JD034.ThrowIsTheSubject)]
    public void ASecondCasingConflictNamesBothSides() {
        Check.ThatCode(() => Any.Char().InLowerCase().InUpperCase())
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("InLowerCase()", "InUpperCase()");
    }

    [Fact(DisplayName = "An allow-list emptied by what was declared before it names the constraint that emptied it.")]
    public void AnAllowListEmptiedByAnEarlierConstraintNamesIt() {
        // The allow-list applied last is the side the caller just wrote; the message used to quote it on both sides
        // of "because" and never name the family that refused every character it offers.
        Check.ThatCode(() => Any.Char().Numeric().OneOf('a', 'b'))
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply OneOf('a', 'b') because Numeric() allows none of its values.");
        Check.ThatCode(() => Any.Char().Numeric().Except('1').OneOf('1', 'a'))
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply OneOf('1', 'a') because no character it offers satisfies the constraints already declared.");
    }

    [Fact(DisplayName = "A family declared after the subtractions that empty it names them.")]
    public void AFamilyDeclaredAfterTheSubtractionsThatEmptyItNamesThem() {
        Check.ThatCode(() => Any.Char().WithoutAlpha().Alpha())
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply Alpha() because WithoutAlpha() removes every character Alpha() offers.");
        // Neither alone empties AlphaNumeric(); a constraint that removes nothing the family offers is not named.
        Check.ThatCode(() => Any.Char().WithoutAlpha().Except('!').WithoutNumeric().AlphaNumeric())
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply AlphaNumeric() because WithoutAlpha() and WithoutNumeric() remove every character AlphaNumeric() offers.");
    }

    [Fact(DisplayName = "A constraint that removes only what another already removed is not named among those that emptied a family.")]
    public void AConstraintRemovingOnlyWhatAnotherRemovedIsNotNamed() {
        // Except('a') and InLowerCase() each refuse a letter Alpha() offers, but WithoutAlpha() already removes it:
        // naming them would point at calls whose removal leaves the family just as empty.
        Check.ThatCode(() => Any.Char().WithoutAlpha().Except('a').WithoutNumeric().AlphaNumeric())
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply AlphaNumeric() because WithoutAlpha() and WithoutNumeric() remove every character AlphaNumeric() offers.");
        Check.ThatCode(() => Any.Char().InLowerCase().WithoutAlpha().Alpha())
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply Alpha() because WithoutAlpha() removes every character Alpha() offers.");
        // Neither covers the other here, so both are named.
        Check.ThatCode(() => Any.Char().InLowerCase().Except('a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
                                                              'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z').Alpha())
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).StartsWith("Cannot apply Alpha() because InLowerCase() and Except('a', ");
    }

    [Fact(DisplayName = "A subtraction or an exclusion that empties a declared family names the family.")]
    public void ASubtractionThatEmptiesADeclaredFamilyNamesTheFamily() {
        Check.ThatCode(() => Any.Char().Alpha().WithoutAlpha())
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply WithoutAlpha() because Alpha() admits nothing once it is applied.");
        Check.ThatCode(() => Any.Char().Numeric().Except('0', '1', '2', '3', '4', '5', '6', '7', '8', '9'))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("because Numeric() admits nothing once it is applied");
    }

}
