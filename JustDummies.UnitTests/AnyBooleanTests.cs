#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     Named-case coverage for <see cref="AnyBoolean" /> that a property cannot express: the pin resolving to a
///     concrete value at every draw, and conflict message content. The redeclaration and exclusion algebra is
///     already quantified generically by <c>BooleanProperties</c>.
/// </summary>
[TestSubject(typeof(AnyBoolean))]
public sealed class AnyBooleanTests {

    private const int SampleCount = 50;

    [Fact(DisplayName = "True always yields true.")]
    public void TrueAlwaysYieldsTrue() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.Boolean().True().Generate()).IsTrue();
        }
    }

    [Fact(DisplayName = "False always yields false.")]
    public void FalseAlwaysYieldsFalse() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.Boolean().False().Generate()).IsFalse();
        }
    }

    [Fact(DisplayName = "An unconstrained draw eventually reaches both values.")]
    public void UnconstrainedDrawReachesBothValues() {
        HashSet<bool> seen = [];
        for (int i = 0; i < SampleCount; i++) {
            seen.Add(Any.Boolean().Generate());
        }

        Check.That(seen).Contains(true, false);
    }

    [Fact(DisplayName = "A contradictory pin names both sides.")]
    public void AContradictoryPinNamesBothSides() {
        Check.ThatCode(() => Any.Boolean().True().False())
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("True()", "False()");
    }

}
