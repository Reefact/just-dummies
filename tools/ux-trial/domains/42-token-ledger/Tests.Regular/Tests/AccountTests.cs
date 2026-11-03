namespace TokenLedger.Domain.Tests.Regular;

public sealed class AccountTests {

    [Fact]
    public void A_new_account_is_empty_and_has_not_sent_anything() {
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
    public void The_zero_address_owns_no_account() {
        Assert.Throws<ArgumentException>(() => new Account(Address.Zero));
    }

    [Fact]
    public void An_account_needs_an_address() {
        Assert.Throws<ArgumentNullException>(() => new Account(null!));
    }

    [Fact]
    public void An_account_is_described_by_address_balance_and_next_nonce() {
        // Arrange
        Address anyAddress = Any.Address().Generate();

        // Act
        Account account = new Account(anyAddress);

        // Assert
        Assert.Equal($"{anyAddress}: 0 (next nonce 0)", account.ToString());
    }

}
