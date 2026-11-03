using JustDummies;

using TokenLedger.Domain;

using Xunit;

namespace TokenLedger.Tests.Discovery;

public sealed class TransferRecordTests {

    [Fact]
    public void A_movement_between_two_accounts_is_neither_a_mint_nor_a_burn() {
        // Arrange
        Address     from   = Any.Address().NonZero().Generate();
        Address     to     = Any.Address().NonZero().Generate();
        TokenAmount amount = Any.TokenAmount().Generate();

        // Act
        TransferRecord record = new TransferRecord(from, to, amount, 12);

        // Assert
        Assert.Same(from, record.From);
        Assert.Same(to, record.To);
        Assert.Same(amount, record.Amount);
        Assert.Equal(12UL, record.BlockNumber);
        Assert.False(record.IsMint);
        Assert.False(record.IsBurn);
    }

    [Fact]
    public void A_movement_from_the_zero_address_is_a_mint() {
        Address     anyRecipient = Any.Address().NonZero().Generate();
        TokenAmount anyAmount    = Any.TokenAmount().Generate();

        TransferRecord record = new TransferRecord(Address.Zero, anyRecipient, anyAmount, 0);

        Assert.True(record.IsMint);
        Assert.False(record.IsBurn);
    }

    [Fact]
    public void A_movement_to_the_zero_address_is_a_burn() {
        Address     anyHolder = Any.Address().NonZero().Generate();
        TokenAmount anyAmount = Any.TokenAmount().Generate();

        TransferRecord record = new TransferRecord(anyHolder, Address.Zero, anyAmount, 0);

        Assert.True(record.IsBurn);
        Assert.False(record.IsMint);
    }

    [Fact]
    public void A_movement_from_zero_to_zero_is_refused() {
        TokenAmount anyAmount = Any.TokenAmount().Generate();
        ulong       anyBlock  = Any.UInt64().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TransferRecord(Address.Zero, Address.Zero, anyAmount, anyBlock));

        Assert.Equal(typeof(ArgumentException), refusal.GetType());
        Assert.Equal("to", refusal.ParamName);
    }

    [Fact]
    public void A_zero_amount_is_a_legal_movement() {
        TransferRecord record = Any.TransferRecord().WithAmount(TokenAmount.Zero).Generate();

        Assert.True(record.Amount.IsZero);
    }

    [Fact]
    public void A_movement_may_go_from_an_account_to_itself() {
        Address     anyHolder = Any.Address().NonZero().Generate();
        TokenAmount anyAmount = Any.TokenAmount().Generate();

        TransferRecord record = new TransferRecord(anyHolder, anyHolder, anyAmount, 3);

        Assert.False(record.IsMint);
        Assert.False(record.IsBurn);
    }

    [Fact]
    public void A_null_argument_is_refused() {
        Address     anyAddress = Any.Address().NonZero().Generate();
        TokenAmount anyAmount  = Any.TokenAmount().Generate();

        Assert.Throws<ArgumentNullException>(() => new TransferRecord(null!, anyAddress, anyAmount, 0));
        Assert.Throws<ArgumentNullException>(() => new TransferRecord(anyAddress, null!, anyAmount, 0));
        Assert.Throws<ArgumentNullException>(() => new TransferRecord(anyAddress, anyAddress, null!, 0));
    }

    [Fact]
    public void Two_records_with_the_same_fields_are_equal_and_hash_alike() {
        TransferRecord anyRecord = Any.TransferRecord().Generate();

        TransferRecord copy = new TransferRecord(new Address(anyRecord.From.Value), new Address(anyRecord.To.Value), new TokenAmount(anyRecord.Amount.BaseUnits), anyRecord.BlockNumber);

        Assert.Equal(anyRecord, copy);
        Assert.True(anyRecord.Equals((object)copy));
        Assert.Equal(anyRecord.GetHashCode(), copy.GetHashCode());
    }

    [Fact]
    public void A_record_differs_when_any_one_field_differs() {
        Address first  = Any.Address().NonZero().Generate();
        Address second = Any.Address().NonZero().DifferentFromAddress(first).Generate();

        TransferRecord baseline = new TransferRecord(first, second, new TokenAmount(5), 1);

        Assert.NotEqual(baseline, new TransferRecord(second, second, new TokenAmount(5), 1));
        Assert.NotEqual(baseline, new TransferRecord(first, first, new TokenAmount(5), 1));
        Assert.NotEqual(baseline, new TransferRecord(first, second, new TokenAmount(6), 1));
        Assert.NotEqual(baseline, new TransferRecord(first, second, new TokenAmount(5), 2));
        Assert.False(baseline.Equals(null));
    }

    [Fact]
    public void A_record_describes_itself() {
        TransferRecord record = new TransferRecord(Address.Zero, new Address("0x5aaeb6053f3e94c9b9a09f33669435e7ef1beaed"), new TokenAmount(9), 3);

        Assert.Equal("0x0000000000000000000000000000000000000000 -> 0x5aaeb6053f3e94c9b9a09f33669435e7ef1beaed: 9 (block 3)", record.ToString());
    }

}
