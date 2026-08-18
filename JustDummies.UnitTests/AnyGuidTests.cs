#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     Named-case coverage for <see cref="AnyGuid" /> that a property cannot express: the two identifier pins
///     documented on the type, and conflict message content. The <c>OneOf</c>/<c>Except</c>/<c>DifferentFrom</c>
///     algebra is already quantified generically by <c>GuidProperties</c>.
/// </summary>
[TestSubject(typeof(AnyGuid))]
public sealed class AnyGuidTests {

    private const int SampleCount = 50;

    [Fact(DisplayName = "NonEmpty never yields Guid.Empty.")]
    public void NonEmptyNeverYieldsGuidEmpty() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.Guid().NonEmpty().Generate()).IsNotEqualTo(Guid.Empty);
        }
    }

    [Fact(DisplayName = "Empty always yields Guid.Empty.")]
    public void EmptyAlwaysYieldsGuidEmpty() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.Guid().Empty().Generate()).IsEqualTo(Guid.Empty);
        }
    }

    [Fact(DisplayName = "An unconstrained draw is, for every practical purpose, never Guid.Empty.")]
    public void UnconstrainedDrawIsPracticallyNeverEmpty() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.Guid().Generate()).IsNotEqualTo(Guid.Empty);
        }
    }

    [Fact(DisplayName = "NonEmpty and Empty contradict each other, and the conflict names both sides.")]
    public void NonEmptyAndEmptyContradictEachOther() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Guid().NonEmpty().Empty());

        Check.That(conflict.Message).Contains("NonEmpty()");
        Check.That(conflict.Message).Contains("Empty()");
    }

}
