using JustDummies;

using Xunit;

namespace TokenLedger.Domain.Tests;

public sealed class AccountAndRefusalTests {

    // ---- Account --------------------------------------------------------------------------------------------------

    [Fact]
    public void A_new_account_is_empty_and_expects_its_first_nonce() {
        // Arrange
        Address anyAddress = Any.Address().Generate();

        // Act
        Account account = new Account(anyAddress);

        // Assert
        Assert.Equal(anyAddress, account.Address);
        Assert.Equal(TokenAmount.Zero, account.Balance);
        Assert.Equal(UInt128.Zero, account.NextNonce);
        Assert.False(account.IsHolder);
    }

    [Fact]
    public void The_zero_address_holds_no_account() {
        Assert.Throws<ArgumentException>(() => new Account(Address.Zero));
    }

    [Fact]
    public void An_account_needs_an_address() {
        Assert.Throws<ArgumentNullException>(() => new Account(null!));
    }

    [Fact]
    public void An_account_prints_its_address_balance_and_next_nonce() {
        // Arrange
        Address address = new Address("0x5aaeb6053f3e94c9b9a09f33669435e7ef1beaed");

        // Act
        Account account = new Account(address);

        // Assert
        Assert.Equal("0x5aaeb6053f3e94c9b9a09f33669435e7ef1beaed: 0 (next nonce 0)", account.ToString());
    }

    // ---- LedgerRefusedException -----------------------------------------------------------------------------------

    [Theory]
    [InlineData(RefusalReason.NonceTooLow)]
    [InlineData(RefusalReason.InsufficientFundsForFee)]
    public void A_refusal_carries_its_reason_and_its_message(RefusalReason reason) {
        // Act
        LedgerRefusedException refusal = new LedgerRefusedException(reason, "because");

        // Assert
        Assert.Equal(reason, refusal.Reason);
        Assert.Equal("because", refusal.Message);
        Assert.IsAssignableFrom<InvalidOperationException>(refusal);
    }

    [Fact]
    public void A_refusal_must_name_a_defined_reason() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LedgerRefusedException((RefusalReason)99, "because"));
    }

    [Fact]
    public void Having_no_reason_at_all_is_not_a_reason() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LedgerRefusedException(default, "because"));
    }

    [Fact]
    public void Every_reason_has_a_nonzero_value() {
        // Assert
        foreach (RefusalReason reason in Enum.GetValues<RefusalReason>()) {
            Assert.NotEqual(0, (int)reason);
        }
    }

}
