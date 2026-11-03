using JustDummies;

using Xunit;

namespace Serialisation.Domain.Tests.Expert;

public sealed class RecallNoticeTests {

    private static readonly DateOnly IssuedOn = new DateOnly(2026, 3, 15);

    [Fact]
    public void A_notice_keeps_what_it_was_made_of() {
        // Arrange
        BatchNumber batch = new BatchNumber("LOT-42");

        // Act
        RecallNotice notice = new RecallNotice(batch, IssuedOn, new DateOnly(2026, 3, 20), RecallClass.ClassII, "Particles in the vials");

        // Assert
        Assert.Same(batch, notice.Batch);
        Assert.Equal(IssuedOn, notice.IssuedOn);
        Assert.Equal(new DateOnly(2026, 3, 20), notice.EffectiveFrom);
        Assert.Equal(RecallClass.ClassII, notice.Class);
        Assert.Equal("Particles in the vials", notice.Reason);
    }

    [Fact]
    public void A_notice_effective_the_day_it_is_issued_is_accepted() {
        // Arrange
        BatchNumber   anyBatch  = Any.BatchNumber().Generate();
        RecallClass   anyClass  = Any.Enum<RecallClass>().Generate();
        string        anyReason = Any.String().NonEmpty().WithMaxLength(500).Generate();

        // Act
        RecallNotice notice = new RecallNotice(anyBatch, IssuedOn, IssuedOn, anyClass, anyReason);

        // Assert
        Assert.Equal(IssuedOn, notice.EffectiveFrom);
    }

    [Fact]
    public void A_notice_effective_the_day_after_it_is_issued_is_accepted() {
        // Arrange
        BatchNumber   anyBatch  = Any.BatchNumber().Generate();
        RecallClass   anyClass  = Any.Enum<RecallClass>().Generate();
        string        anyReason = Any.String().NonEmpty().WithMaxLength(500).Generate();

        // Act
        RecallNotice notice = new RecallNotice(anyBatch, IssuedOn, IssuedOn.AddDays(1), anyClass, anyReason);

        // Assert
        Assert.Equal(IssuedOn.AddDays(1), notice.EffectiveFrom);
    }

    [Fact]
    public void A_notice_effective_the_day_before_it_is_issued_is_refused() {
        // Arrange
        BatchNumber   anyBatch  = Any.BatchNumber().Generate();
        RecallClass   anyClass  = Any.Enum<RecallClass>().Generate();
        string        anyReason = Any.String().NonEmpty().WithMaxLength(500).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new RecallNotice(anyBatch, IssuedOn, IssuedOn.AddDays(-1), anyClass, anyReason));

        // Assert
        Assert.Equal("effectiveFrom", exception.ParamName);
    }

    [Fact]
    public void A_notice_without_a_batch_is_refused() {
        // Arrange
        RecallClass anyClass  = Any.Enum<RecallClass>().Generate();
        string      anyReason = Any.String().NonEmpty().WithMaxLength(500).Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new RecallNotice(null!, IssuedOn, IssuedOn, anyClass, anyReason));

        // Assert
        Assert.Equal("batch", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void A_class_that_is_not_a_declared_one_is_refused(int undeclared) {
        // Arrange
        BatchNumber anyBatch  = Any.BatchNumber().Generate();
        string      anyReason = Any.String().NonEmpty().WithMaxLength(500).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new RecallNotice(anyBatch, IssuedOn, IssuedOn, (RecallClass)undeclared, anyReason));

        // Assert
        Assert.Equal("recallClass", exception.ParamName);
    }

    [Fact]
    public void A_null_reason_is_refused_as_an_argument_null_exception() {
        // Arrange
        BatchNumber anyBatch = Any.BatchNumber().Generate();
        RecallClass anyClass = Any.Enum<RecallClass>().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new RecallNotice(anyBatch, IssuedOn, IssuedOn, anyClass, null!));

        // Assert
        Assert.Equal("reason", exception.ParamName);
    }

    [Fact]
    public void An_empty_reason_is_refused_as_an_argument_exception() {
        // Arrange
        BatchNumber anyBatch = Any.BatchNumber().Generate();
        RecallClass anyClass = Any.Enum<RecallClass>().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new RecallNotice(anyBatch, IssuedOn, IssuedOn, anyClass, string.Empty));

        // Assert
        Assert.Equal("reason", exception.ParamName);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void A_blank_but_not_empty_reason_is_accepted(string reason) {
        // Arrange
        BatchNumber anyBatch = Any.BatchNumber().Generate();
        RecallClass anyClass = Any.Enum<RecallClass>().Generate();

        // Act
        RecallNotice notice = new RecallNotice(anyBatch, IssuedOn, IssuedOn, anyClass, reason);

        // Assert
        Assert.Equal(reason, notice.Reason);
    }

    [Fact]
    public void A_reason_of_five_hundred_characters_is_accepted() {
        // Arrange
        BatchNumber anyBatch  = Any.BatchNumber().Generate();
        RecallClass anyClass  = Any.Enum<RecallClass>().Generate();
        string      anyReason = Any.String().WithLength(500).Generate();

        // Act
        RecallNotice notice = new RecallNotice(anyBatch, IssuedOn, IssuedOn, anyClass, anyReason);

        // Assert
        Assert.Equal(500, notice.Reason.Length);
    }

    [Fact]
    public void A_reason_of_five_hundred_and_one_characters_is_refused() {
        // Arrange
        BatchNumber anyBatch  = Any.BatchNumber().Generate();
        RecallClass anyClass  = Any.Enum<RecallClass>().Generate();
        string      anyReason = Any.String().WithLength(501).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new RecallNotice(anyBatch, IssuedOn, IssuedOn, anyClass, anyReason));

        // Assert
        Assert.Equal("reason", exception.ParamName);
    }

    [Fact]
    public void The_batch_is_checked_before_everything_else() {
        // Arrange: every argument is wrong
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RecallNotice(null!, IssuedOn, IssuedOn.AddDays(-1), (RecallClass)0, null!));
    }

    [Fact]
    public void The_dates_are_checked_before_the_class() {
        // Arrange
        BatchNumber anyBatch  = Any.BatchNumber().Generate();
        string      anyReason = Any.String().NonEmpty().WithMaxLength(500).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new RecallNotice(anyBatch, IssuedOn, IssuedOn.AddDays(-1), (RecallClass)0, anyReason));

        // Assert
        Assert.Equal("effectiveFrom", exception.ParamName);
    }

    [Fact]
    public void The_class_is_checked_before_the_reason() {
        // Arrange
        BatchNumber anyBatch = Any.BatchNumber().Generate();

        // Act & Assert: an out-of-range exception, not the null one the reason would raise
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecallNotice(anyBatch, IssuedOn, IssuedOn, (RecallClass)0, null!));
    }

    [Theory]
    [InlineData(RecallClass.ClassI, true)]
    [InlineData(RecallClass.ClassII, false)]
    [InlineData(RecallClass.ClassIII, false)]
    public void Only_a_class_one_recall_reaches_patients(RecallClass recallClass, bool reachesPatients) {
        // Arrange
        RecallNotice anyNotice = Any.RecallNotice().WithRecallClass(recallClass).Generate();

        // Act
        bool reached = anyNotice.ReachesPatients;

        // Assert
        Assert.Equal(reachesPatients, reached);
    }

}
