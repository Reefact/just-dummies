using InternationalCatalogue.Domain;

using JustDummies;

using Xunit;

namespace InternationalCatalogue.Tests;

public sealed class DuplicateRecordExceptionTests {

    [Fact]
    public void The_exception_carries_the_reason_and_the_existing_record() {
        // Arrange
        RecordId existing = Any.RecordId().Generate();

        // Act
        DuplicateRecordException exception = new DuplicateRecordException(DuplicateReason.SameIsbn, existing, "message");

        // Assert
        Assert.Equal(DuplicateReason.SameIsbn, exception.Reason);
        Assert.Same(existing, exception.Existing);
        Assert.Equal("message", exception.Message);
    }

    [Fact]
    public void None_is_not_a_reason_for_a_refusal() {
        // Arrange
        RecordId anyExisting = Any.RecordId().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new DuplicateRecordException(DuplicateReason.None, anyExisting, "message"));
    }

    [Fact]
    public void An_undefined_reason_is_refused() {
        // Arrange
        RecordId anyExisting = Any.RecordId().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new DuplicateRecordException((DuplicateReason)99, anyExisting, "message"));
    }

    [Fact]
    public void The_existing_record_is_mandatory() {
        // Arrange
        DuplicateReason anyReason = Any.Enum<DuplicateReason>().Except(DuplicateReason.None).Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DuplicateRecordException(anyReason, null!, "message"));
    }

}
