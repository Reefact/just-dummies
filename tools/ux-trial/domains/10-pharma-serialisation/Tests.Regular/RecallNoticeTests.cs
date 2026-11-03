using JustDummies;

using Xunit;

namespace Serialisation.Domain.Tests;

public sealed class RecallNoticeTests {

    [Fact]
    public void A_notice_keeps_what_it_was_given() {
        // Arrange
        BatchNumber anyBatch  = new AnyBatchNumber().Generate();
        DateOnly    issuedOn  = new DateOnly(2025, 3, 10);
        DateOnly    effective = new DateOnly(2025, 3, 12);
        string      anyReason = Any.String().Printable().WithLengthBetween(1, 500).Generate();

        // Act
        RecallNotice notice = new RecallNotice(anyBatch, issuedOn, effective, RecallClass.ClassII, anyReason);

        // Assert
        Assert.Same(anyBatch, notice.Batch);
        Assert.Equal(issuedOn, notice.IssuedOn);
        Assert.Equal(effective, notice.EffectiveFrom);
        Assert.Equal(RecallClass.ClassII, notice.Class);
        Assert.Equal(anyReason, notice.Reason);
    }

    [Fact]
    public void A_drawn_notice_takes_effect_no_earlier_than_it_is_issued() {
        // Arrange
        RecallNotice anyNotice = new AnyRecallNotice().Generate();

        // Assert
        Assert.True(anyNotice.EffectiveFrom >= anyNotice.IssuedOn);
        Assert.True(Enum.IsDefined(anyNotice.Class));
    }

    [Fact]
    public void A_notice_may_take_effect_the_day_it_is_issued() {
        // Arrange
        DateOnly anyDay = Any.DateOnly().Generate();

        // Act
        RecallNotice notice = new AnyRecallNotice().WithIssuedOn(anyDay).WithEffectiveFrom(anyDay).Generate();

        // Assert
        Assert.Equal(notice.IssuedOn, notice.EffectiveFrom);
    }

    [Fact]
    public void A_notice_taking_effect_before_it_is_issued_is_refused() {
        // Arrange
        BatchNumber anyBatch  = new AnyBatchNumber().Generate();
        DateOnly    anyIssued = Fixtures.DatesOfTheModernEra().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new RecallNotice(anyBatch, anyIssued, anyIssued.AddDays(-1), RecallClass.ClassI, "contaminated"));

        // Assert
        Assert.Equal("effectiveFrom", refusal.ParamName);
    }

    [Fact]
    public void A_notice_without_a_batch_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new AnyRecallNotice().WithBatch((BatchNumber)null!).Generate());

        // Assert
        Assert.Equal("batch", refusal.ParamName);
    }

    [Theory]
    [InlineData(RecallClass.ClassI, true)]
    [InlineData(RecallClass.ClassII, false)]
    [InlineData(RecallClass.ClassIII, false)]
    public void Only_a_class_one_recall_reaches_patients(RecallClass recallClass, bool expected) {
        // Arrange
        RecallNotice notice = new AnyRecallNotice().WithRecallClass(recallClass).Generate();

        // Act
        bool reachesPatients = notice.ReachesPatients;

        // Assert
        Assert.Equal(expected, reachesPatients);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    [InlineData(99)]
    public void An_undefined_recall_class_is_refused(int value) {
        // Arrange
        BatchNumber anyBatch = new AnyBatchNumber().Generate();
        DateOnly    issued   = new DateOnly(2025, 3, 10);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new RecallNotice(anyBatch, issued, issued, (RecallClass)value, "contaminated"));

        // Assert
        Assert.Equal("recallClass", refusal.ParamName);
    }

    [Fact]
    public void A_reason_of_five_hundred_characters_is_accepted_and_one_more_is_refused() {
        // Arrange
        string anyLongest   = Any.String().WithLength(500).Generate();
        string anyTooLong   = Any.String().WithLength(501).Generate();
        BatchNumber anyBatch = new AnyBatchNumber().Generate();
        DateOnly    issued   = new DateOnly(2025, 3, 10);

        // Act
        RecallNotice notice = new RecallNotice(anyBatch, issued, issued, RecallClass.ClassIII, anyLongest);

        // Assert
        Assert.Equal(500, notice.Reason.Length);
        Assert.Throws<ArgumentException>(() => new RecallNotice(anyBatch, issued, issued, RecallClass.ClassIII, anyTooLong));
    }

    [Fact]
    public void A_missing_reason_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new AnyRecallNotice().WithReason((string)null!).Generate());

        // Assert
        Assert.Equal("reason", refusal.ParamName);
    }

    [Fact]
    public void An_empty_reason_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new AnyRecallNotice().WithReason(string.Empty).Generate());

        // Assert
        Assert.Equal("reason", refusal.ParamName);
    }

    [Fact]
    public void A_blank_but_non_empty_reason_is_accepted() {
        // Act
        RecallNotice notice = new AnyRecallNotice().WithReason("   ").Generate();

        // Assert
        Assert.Equal("   ", notice.Reason);
    }

    [Fact]
    public void The_batch_is_checked_first() {
        // Arrange
        DateOnly issued = new DateOnly(2025, 3, 10);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RecallNotice(null!, issued, issued.AddDays(-1), (RecallClass)42, string.Empty));
    }

    [Fact]
    public void The_dates_are_checked_before_the_class() {
        // Arrange
        BatchNumber anyBatch = new AnyBatchNumber().Generate();
        DateOnly    issued   = new DateOnly(2025, 3, 10);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new RecallNotice(anyBatch, issued, issued.AddDays(-1), (RecallClass)42, string.Empty));

        // Assert
        Assert.Equal("effectiveFrom", refusal.ParamName);
    }

    [Fact]
    public void The_class_is_checked_before_the_reason() {
        // Arrange
        BatchNumber anyBatch = new AnyBatchNumber().Generate();
        DateOnly    issued   = new DateOnly(2025, 3, 10);

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new RecallNotice(anyBatch, issued, issued, (RecallClass)42, string.Empty));

        // Assert
        Assert.Equal("recallClass", refusal.ParamName);
    }

}
