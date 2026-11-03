using JustDummies;

using Xunit;

namespace Serialisation.Domain.Tests;

public sealed class RecallNoticeTests {

    private static readonly DateOnly Issued = new DateOnly(2026, 5, 4);

    [Fact]
    public void A_notice_keeps_what_it_was_built_from() {
        // Arrange
        BatchNumber anyBatch  = new AnyBatchNumber().Generate();
        string      anyReason = Any.String().NotBlank().WithMaxLength(500).Generate();
        DateOnly    effective = new DateOnly(2026, 5, 10);

        // Act
        RecallNotice notice = new RecallNotice(anyBatch, Issued, effective, RecallClass.ClassII, anyReason);

        // Assert
        Assert.Same(anyBatch, notice.Batch);
        Assert.Equal(Issued, notice.IssuedOn);
        Assert.Equal(effective, notice.EffectiveFrom);
        Assert.Equal(RecallClass.ClassII, notice.Class);
        Assert.Equal(anyReason, notice.Reason);
    }

    [Theory]
    [InlineData(RecallClass.ClassI, true)]
    [InlineData(RecallClass.ClassII, false)]
    [InlineData(RecallClass.ClassIII, false)]
    public void Only_a_class_I_recall_reaches_patients(RecallClass recallClass, bool reachesPatients) {
        // Arrange
        RecallNotice notice = new AnyRecallNotice().WithRecallClass(recallClass).Generate();

        // Act / Assert
        Assert.Equal(reachesPatients, notice.ReachesPatients);
    }

    [Fact]
    public void A_notice_may_take_effect_the_day_it_is_issued() {
        // Arrange
        BatchNumber anyBatch  = new AnyBatchNumber().Generate();
        string      anyReason = Any.String().NonEmpty().WithMaxLength(500).Generate();

        // Act
        RecallNotice notice = new RecallNotice(anyBatch, Issued, Issued, RecallClass.ClassIII, anyReason);

        // Assert
        Assert.Equal(notice.IssuedOn, notice.EffectiveFrom);
    }

    [Fact]
    public void A_notice_cannot_take_effect_before_it_is_issued() {
        // Arrange
        BatchNumber anyBatch  = new AnyBatchNumber().Generate();
        string      anyReason = Any.String().NonEmpty().WithMaxLength(500).Generate();

        // Act / Assert
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new RecallNotice(anyBatch, Issued, Issued.AddDays(-1), RecallClass.ClassI, anyReason));
        Assert.Equal("effectiveFrom", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void An_undefined_recall_class_is_refused(int value) {
        // Arrange
        BatchNumber anyBatch  = new AnyBatchNumber().Generate();
        string      anyReason = Any.String().NonEmpty().WithMaxLength(500).Generate();

        // Act / Assert
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new RecallNotice(anyBatch, Issued, Issued, (RecallClass)value, anyReason));
        Assert.Equal("recallClass", exception.ParamName);
    }

    [Fact]
    public void A_notice_without_a_batch_is_refused() {
        // Arrange
        string anyReason = Any.String().NonEmpty().WithMaxLength(500).Generate();

        // Act / Assert
        Assert.Equal("batch", Assert.Throws<ArgumentNullException>(() => new RecallNotice(null!, Issued, Issued, RecallClass.ClassI, anyReason)).ParamName);
    }

    [Fact]
    public void A_notice_without_a_reason_is_refused() {
        // Arrange
        BatchNumber anyBatch = new AnyBatchNumber().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new RecallNotice(anyBatch, Issued, Issued, RecallClass.ClassI, null!));
        Assert.Throws<ArgumentException>(() => new RecallNotice(anyBatch, Issued, Issued, RecallClass.ClassI, ""));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void A_blank_but_non_empty_reason_is_accepted(string reason) {
        // Arrange
        BatchNumber anyBatch = new AnyBatchNumber().Generate();

        // Act
        RecallNotice notice = new RecallNotice(anyBatch, Issued, Issued, RecallClass.ClassI, reason);

        // Assert
        Assert.Equal(reason, notice.Reason);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(500, true)]
    [InlineData(501, false)]
    [InlineData(5000, false)]
    public void A_reason_is_accepted_up_to_five_hundred_characters(int length, bool accepted) {
        // Arrange
        BatchNumber anyBatch  = new AnyBatchNumber().Generate();
        string      anyReason = Any.String().Printable().WithLength(length).Generate();

        // Act
        Func<RecallNotice> create = () => new RecallNotice(anyBatch, Issued, Issued, RecallClass.ClassI, anyReason);

        // Assert
        if (accepted) {
            Assert.Equal(length, create().Reason.Length);
        } else {
            Assert.Throws<ArgumentException>(create);
        }
    }

    [Fact]
    public void The_checks_run_in_order_batch_then_dates_then_class_then_reason() {
        // Arrange
        BatchNumber anyBatch = new AnyBatchNumber().Generate();
        DateOnly    before   = Issued.AddDays(-1);

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new RecallNotice(null!, Issued, before, (RecallClass)99, null!));
        Assert.Equal("effectiveFrom", Assert.Throws<ArgumentOutOfRangeException>(() => new RecallNotice(anyBatch, Issued, before, (RecallClass)99, null!)).ParamName);
        Assert.Equal("recallClass", Assert.Throws<ArgumentOutOfRangeException>(() => new RecallNotice(anyBatch, Issued, Issued, (RecallClass)99, null!)).ParamName);
    }

}
