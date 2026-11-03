namespace TokenLedger.Domain.Tests.Regular;

public sealed class LedgerRefusedExceptionTests {

    [Fact]
    public void A_refusal_carries_its_reason_and_message() {
        // Arrange
        RefusalReason anyReason  = Any.Enum<RefusalReason>().Generate();
        string        anyMessage = Any.String().Printable().NotBlank().WithMaxLength(200).Generate();

        // Act
        LedgerRefusedException refusal = new LedgerRefusedException(anyReason, anyMessage);

        // Assert
        Assert.Equal(anyReason, refusal.Reason);
        Assert.Equal(anyMessage, refusal.Message);
    }

    [Fact]
    public void A_refusal_is_an_invalid_operation() {
        // Arrange
        RefusalReason anyReason = Any.Enum<RefusalReason>().Generate();

        // Act
        LedgerRefusedException refusal = new LedgerRefusedException(anyReason, "refused");

        // Assert
        Assert.IsAssignableFrom<InvalidOperationException>(refusal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(14)]
    [InlineData(-1)]
    public void A_refusal_must_name_a_defined_reason(int reason) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LedgerRefusedException((RefusalReason)reason, "refused"));
    }

    [Fact]
    public void The_first_and_last_reasons_are_defined() {
        Assert.Equal(RefusalReason.NonceTooLow, new LedgerRefusedException(RefusalReason.NonceTooLow, "x").Reason);
        Assert.Equal(RefusalReason.InsufficientFundsForFee, new LedgerRefusedException(RefusalReason.InsufficientFundsForFee, "x").Reason);
    }

}
