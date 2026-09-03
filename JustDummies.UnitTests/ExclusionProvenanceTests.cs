#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     Pins the conflict messages of the two bespoke generators — <c>DummyEnum</c> and <c>DummyGuid</c> — when an
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

    [Fact(DisplayName = "DummyEnum: an exclusion that empties an allow-list is named, and the allow-list is not blamed.")]
    public void EnumExclusionEmptyingAnAllowListIsNamed() {
        Check.ThatCode(() => Dummy.Enum<OrderStatus>()
                                .OneOf(OrderStatus.Draft, OrderStatus.Validated)
                                .Except(OrderStatus.Draft, OrderStatus.Validated)
                                .Generate())
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message)
             // "it", not "Except(...)": the culprit IS the constraint being applied, and the sentence already names it
             // before the "because".
             .Contains("it forbids every value OneOf(")
             // The defect this pins: the old message read "no value OneOf(...) allows remains available", which named
             // the victim and left the reader to guess the cause.
             .And.DoesNotContain("remains available");
    }

    [Fact(DisplayName = "DummyEnum: an earlier exclusion is named when a later constraint completes the exhaustion.")]
    public void EnumEarlierExclusionIsNamed() {
        Check.ThatCode(() => Dummy.Enum<OrderStatus>()
                                .Except(OrderStatus.Draft, OrderStatus.Validated)
                                .OneOf(OrderStatus.Draft, OrderStatus.Validated)
                                .Generate())
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message)
             // Here the culprit is NOT the constraint being applied, so it is named in full rather than as "it".
             .Contains("Except(")
             .And.Contains("forbids every value OneOf(");
    }

    [Fact(DisplayName = "DummyEnum: two exclusions that both bit are both named, and the verb agrees.")]
    public void EnumSeveralExclusionsAreAllNamed() {
        Check.ThatCode(() => Dummy.Enum<OrderStatus>()
                                .Except(OrderStatus.Draft)
                                .DifferentFrom(OrderStatus.Validated)
                                .Except(OrderStatus.Cancelled)
                                .Generate())
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message)
             .Contains("Except(")
             .And.Contains("DifferentFrom(")
             .And.Contains("forbid every declared OrderStatus member");
    }

    [Fact(DisplayName = "DummyEnum: an exclusion outside the allow-list never bit, so it is not named.")]
    public void EnumExclusionThatNeverBitIsNotNamed() {
        Check.ThatCode(() => Dummy.Enum<OrderStatus>()
                                .Except(OrderStatus.Cancelled)          // outside the allow-list below: never removed anything
                                .OneOf(OrderStatus.Draft)
                                .Except(OrderStatus.Draft)              // this one is the whole cause
                                .Generate())
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message)
             .DoesNotContain("Cancelled")
             .And.Contains("it forbids every value OneOf(");
    }

    [Fact(DisplayName = "DummyEnum: exhausting a flags universe names the exclusions and calls them combinations.")]
    public void EnumCombinationExhaustionNamesTheExclusions() {
        Check.ThatCode(() => Dummy.Enum<Permissions>()
                                .AllowingCombinations()
                                .Except(Permissions.Read, Permissions.Write, Permissions.Read | Permissions.Write)
                                .Generate())
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("it forbids every Permissions combination");
    }

    [Fact(DisplayName = "DummyGuid: an exclusion that empties an allow-list is named, and the allow-list is not blamed.")]
    public void GuidExclusionEmptyingAnAllowListIsNamed() {
        Guid first  = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        Check.ThatCode(() => Dummy.Guid().OneOf(first, second).Except(first, second))
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message)
             .Contains("it forbids every value OneOf(")
             .And.DoesNotContain("remains available");
    }

    [Fact(DisplayName = "DummyGuid: excluding a pinned value names the exclusion, not 'the exclusions'.")]
    public void GuidExcludingThePinNamesTheExclusion() {
        Check.ThatCode(() => Dummy.Guid().Empty().Except(Guid.Empty))
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message)
             .Contains("Empty() already pins the value to")
             .And.Contains("and it forbids it")
             // The defect this pins: a generic plural that named no exclusion at all.
             .And.DoesNotContain("which the exclusions forbid");
    }

    [Fact(DisplayName = "DummyGuid: pinning onto an earlier exclusion names that exclusion in full.")]
    public void GuidPinningOntoAnEarlierExclusionNamesIt() {
        Check.ThatCode(() => Dummy.Guid().NonEmpty().Empty())
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("NonEmpty() forbids it");
    }

    [Fact(DisplayName = "An element generator admitting nothing names its own exclusion, not the collection's Distinct().")]
    [SuppressMessage(JustDummiesRule.JD017.Category, JustDummiesRule.JD017.Id, Justification = "The emptied enum IS the subject: this pins which constraint the sentence names when such a generator is the element of a collection. NegativeTestGuard cannot reach it, and is right not to — the chain is nested inside Dummy.SetOf/Dummy.ListOf rather than being the whole lambda body.")]
    public void EmptyElementGeneratorNamesItsOwnExclusion() {
        // Same fault, same sentence, whether or not the collection is distinct. The non-distinct path reaches the
        // element's own refusal by drawing; the distinct path asks the cardinality gate first, and used to answer
        // "Cannot apply Distinct() because 1 element required to be distinct exceed the 0 distinct value(s) the
        // element generator can produce" -- naming a constraint the caller never wrote, over a domain emptied by
        // one they did. An exhausted element generator is not a collection asking for too many values.
        ConflictingDummyConstraintException distinct = Assert.Throws<ConflictingDummyConstraintException>(
            () => Dummy.SetOf(Dummy.Enum<OrderStatus>().Except(OrderStatus.Draft, OrderStatus.Validated, OrderStatus.Cancelled)).WithCount(1).Generate());

        ConflictingDummyConstraintException plain = Assert.Throws<ConflictingDummyConstraintException>(
            () => Dummy.ListOf(Dummy.Enum<OrderStatus>().Except(OrderStatus.Draft, OrderStatus.Validated, OrderStatus.Cancelled)).WithCount(1).Generate());

        Check.That(distinct.Message).IsEqualTo("Cannot apply Except(Draft, Validated, Cancelled) because it forbids every declared OrderStatus member.");
        Check.That(distinct.Message).IsEqualTo(plain.Message);
    }

    [Fact(DisplayName = "A distinct collection asking for more than a NON-empty domain still names Distinct().")]
    public void DistinctBeyondANonEmptyDomainStillNamesDistinct() {
        // The other side of the same guard: where the element generator does admit values and the collection asks
        // for more distinct ones than exist, Distinct() IS the constraint that cannot be honoured, and the
        // cardinality sentence is the right one. Letting an exhausted generator speak must not relax that.
        Check.ThatCode(() => Dummy.SetOf(Dummy.Enum<OrderStatus>()).WithCount(5).Generate())
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("Distinct()", "5");
    }

}
