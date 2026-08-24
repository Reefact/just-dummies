#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     Pins the conflict messages of the two bespoke generators — <c>AnyEnum</c> and <c>AnyGuid</c> — when an
///     exclusion is what empties the domain. Both used to name the allow-list or the pin, that is the constraint
///     being emptied, and never the exclusion doing the emptying: "no value OneOf(...) allows remains available"
///     told the reader nothing they had not just written. The interval engines were fixed first; these two carry
///     their own message code and were left behind.
///     <para>
///         These are example tests rather than properties: what is asserted is message CONTENT for named shapes,
///         which has no input space to quantify over.
///     </para>
/// </summary>
public sealed class ExclusionProvenanceTests {

    #region Nested types declarations

    private enum OrderStatus {

        Draft,
        Validated,
        Cancelled

    }

    [Flags]
    private enum Permissions {

        Read  = 1,
        Write = 2

    }

    #endregion

    [Fact(DisplayName = "AnyEnum: an exclusion that empties an allow-list is named, and the allow-list is not blamed.")]
    public void EnumExclusionEmptyingAnAllowListIsNamed() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Enum<OrderStatus>()
                     .OneOf(OrderStatus.Draft, OrderStatus.Validated)
                     .Except(OrderStatus.Draft, OrderStatus.Validated)
                     .Generate());

        // "it", not "Except(...)": the culprit IS the constraint being applied, and the sentence already names it
        // before the "because".
        Check.That(conflict.Message).Contains("it forbids every value OneOf(");
        // The defect this pins: the old message read "no value OneOf(...) allows remains available", which named
        // the victim and left the reader to guess the cause.
        Check.That(conflict.Message).DoesNotContain("remains available");
    }

    [Fact(DisplayName = "AnyEnum: an earlier exclusion is named when a later constraint completes the exhaustion.")]
    public void EnumEarlierExclusionIsNamed() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Enum<OrderStatus>()
                     .Except(OrderStatus.Draft, OrderStatus.Validated)
                     .OneOf(OrderStatus.Draft, OrderStatus.Validated)
                     .Generate());

        // Here the culprit is NOT the constraint being applied, so it is named in full rather than as "it".
        Check.That(conflict.Message).Contains("Except(");
        Check.That(conflict.Message).Contains("forbids every value OneOf(");
    }

    [Fact(DisplayName = "AnyEnum: two exclusions that both bit are both named, and the verb agrees.")]
    public void EnumSeveralExclusionsAreAllNamed() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Enum<OrderStatus>()
                     .Except(OrderStatus.Draft)
                     .DifferentFrom(OrderStatus.Validated)
                     .Except(OrderStatus.Cancelled)
                     .Generate());

        Check.That(conflict.Message).Contains("Except(");
        Check.That(conflict.Message).Contains("DifferentFrom(");
        Check.That(conflict.Message).Contains("forbid every declared OrderStatus member");
    }

    [Fact(DisplayName = "AnyEnum: an exclusion outside the allow-list never bit, so it is not named.")]
    public void EnumExclusionThatNeverBitIsNotNamed() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Enum<OrderStatus>()
                     .Except(OrderStatus.Cancelled)          // outside the allow-list below: never removed anything
                     .OneOf(OrderStatus.Draft)
                     .Except(OrderStatus.Draft)              // this one is the whole cause
                     .Generate());

        Check.That(conflict.Message).DoesNotContain("Cancelled");
        Check.That(conflict.Message).Contains("it forbids every value OneOf(");
    }

    [Fact(DisplayName = "AnyEnum: exhausting a flags universe names the exclusions and calls them combinations.")]
    public void EnumCombinationExhaustionNamesTheExclusions() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Enum<Permissions>()
                     .AllowingCombinations()
                     .Except(Permissions.Read, Permissions.Write, Permissions.Read | Permissions.Write)
                     .Generate());

        Check.That(conflict.Message).Contains("it forbids every Permissions combination");
    }

    [Fact(DisplayName = "AnyGuid: an exclusion that empties an allow-list is named, and the allow-list is not blamed.")]
    public void GuidExclusionEmptyingAnAllowListIsNamed() {
        Guid first  = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Guid().OneOf(first, second).Except(first, second));

        Check.That(conflict.Message).Contains("it forbids every value OneOf(");
        Check.That(conflict.Message).DoesNotContain("remains available");
    }

    [Fact(DisplayName = "AnyGuid: excluding a pinned value names the exclusion, not 'the exclusions'.")]
    public void GuidExcludingThePinNamesTheExclusion() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Guid().Empty().Except(Guid.Empty));

        Check.That(conflict.Message).Contains("Empty() already pins the value to");
        Check.That(conflict.Message).Contains("and it forbids it");
        // The defect this pins: a generic plural that named no exclusion at all.
        Check.That(conflict.Message).DoesNotContain("which the exclusions forbid");
    }

    [Fact(DisplayName = "AnyGuid: pinning onto an earlier exclusion names that exclusion in full.")]
    public void GuidPinningOntoAnEarlierExclusionNamesIt() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Guid().NonEmpty().Empty());

        Check.That(conflict.Message).Contains("NonEmpty() forbids it");
    }

}
