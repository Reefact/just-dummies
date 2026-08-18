#region Usings declarations

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
    public void ASecondFamilyConflictNamesBothSides() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Char().Alpha().Numeric());

        Check.That(conflict.Message).Contains("Alpha()");
        Check.That(conflict.Message).Contains("Numeric()");
    }

    [Fact(DisplayName = "A second casing conflict names both sides.")]
    public void ASecondCasingConflictNamesBothSides() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Char().LowerCase().UpperCase());

        Check.That(conflict.Message).Contains("LowerCase()");
        Check.That(conflict.Message).Contains("UpperCase()");
    }

}
