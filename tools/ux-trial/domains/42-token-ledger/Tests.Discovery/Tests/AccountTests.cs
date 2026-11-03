using JustDummies;

using TokenLedger.Domain;

using Xunit;

namespace TokenLedger.Tests.Discovery;

public sealed class AccountTests {

    [Fact]
    public void A_new_account_is_empty_and_has_used_no_nonce() {
        // Arrange
        Address anyAddress = Any.Address().NonZero().Generate();

        // Act
        Account account = new Account(anyAddress);

        // Assert
        Assert.Same(anyAddress, account.Address);
        Assert.Equal(TokenAmount.Zero, account.Balance);
        Assert.Equal(UInt128.Zero, account.NextNonce);
        Assert.False(account.IsHolder);
    }

    [Fact]
    public void The_zero_address_owns_no_account() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Account(Address.Zero));

        Assert.Equal(typeof(ArgumentException), refusal.GetType());
        Assert.Equal("address", refusal.ParamName);
    }

    [Fact]
    public void A_null_address_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Account(null!));
    }

    [Fact]
    public void A_drawn_account_has_a_non_zero_address() {
        Account anyAccount = Any.Account().Generate();

        Assert.False(anyAccount.Address.IsZero);
    }

    [Fact]
    public void An_account_describes_itself() {
        Account account = new Account(new Address("0x5aaeb6053f3e94c9b9a09f33669435e7ef1beaed"));

        Assert.Equal("0x5aaeb6053f3e94c9b9a09f33669435e7ef1beaed: 0 (next nonce 0)", account.ToString());
    }

}
