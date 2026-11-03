using JustDummies;

using Xunit;

namespace TokenLedger.Domain.Tests;

public sealed class TransferRecordTests {

    [Fact]
    public void A_record_keeps_what_it_was_given() {
        // Arrange
        Address anyFrom = Any.Address().Generate();
        Address anyTo   = Any.Address().Generate();

        // Act
        TransferRecord record = new TransferRecord(anyFrom, anyTo, Amount.Of(500), 42);

        // Assert
        Assert.Equal(anyFrom, record.From);
        Assert.Equal(anyTo, record.To);
        Assert.Equal(Amount.Of(500), record.Amount);
        Assert.Equal(42UL, record.BlockNumber);
    }

    [Fact]
    public void A_movement_between_two_ordinary_addresses_is_neither_a_mint_nor_a_burn() {
        // Arrange
        TransferRecord anyRecord = Any.TransferRecord().Generate();

        // Assert
        Assert.False(anyRecord.IsMint);
        Assert.False(anyRecord.IsBurn);
    }

    [Fact]
    public void A_movement_from_the_zero_address_is_a_mint() {
        // Arrange
        TransferRecord record = Any.TransferRecord().WithFrom(Address.Zero).Generate();

        // Assert
        Assert.True(record.IsMint);
        Assert.False(record.IsBurn);
    }

    [Fact]
    public void A_movement_to_the_zero_address_is_a_burn() {
        // Arrange
        TransferRecord record = Any.TransferRecord().WithTo(Address.Zero).Generate();

        // Assert
        Assert.True(record.IsBurn);
        Assert.False(record.IsMint);
    }

    [Fact]
    public void A_movement_from_the_zero_address_to_the_zero_address_is_refused() {
        // Arrange
        TokenAmount anyAmount = Any.TokenAmount().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new TransferRecord(Address.Zero, Address.Zero, anyAmount, 1));
    }

    [Fact]
    public void A_movement_of_nothing_is_legal() {
        // Arrange
        TransferRecord record = Any.TransferRecord().WithAmount(TokenAmount.Zero).Generate();

        // Assert
        Assert.True(record.Amount.IsZero);
    }

    [Fact]
    public void A_movement_may_go_from_an_address_to_itself() {
        // Arrange
        Address anyAddress = Any.Address().Generate();

        // Act
        TransferRecord record = new TransferRecord(anyAddress, anyAddress, Amount.Of(1), 1);

        // Assert
        Assert.Equal(record.From, record.To);
    }

    [Fact]
    public void A_missing_end_or_amount_is_refused_as_a_missing_argument() {
        // Arrange
        Address     anyAddress = Any.Address().Generate();
        TokenAmount anyAmount  = Any.TokenAmount().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new TransferRecord(null!, anyAddress, anyAmount, 1));
        Assert.Throws<ArgumentNullException>(() => new TransferRecord(anyAddress, null!, anyAmount, 1));
        Assert.Throws<ArgumentNullException>(() => new TransferRecord(anyAddress, anyAddress, null!, 1));
    }

    [Fact]
    public void Two_records_with_the_same_four_facts_are_equal_and_share_their_hash_code() {
        // Arrange
        Address anyFrom = Any.Address().Generate();
        Address anyTo   = Any.Address().Generate();

        // Act
        TransferRecord first  = new TransferRecord(anyFrom, anyTo, Amount.Of(500), 42);
        TransferRecord second = new TransferRecord(anyFrom, anyTo, Amount.Of(500), 42);

        // Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void A_record_differing_by_any_of_the_four_facts_is_not_equal() {
        // Arrange
        Address        anyFrom      = Any.Address().Generate();
        Address        anyTo        = Any.Address().DifferentFrom(anyFrom).Generate();
        Address        anyOther     = Any.Address().DifferentFrom(anyFrom).Generate();
        TransferRecord reference    = new TransferRecord(anyFrom, anyTo, Amount.Of(500), 42);

        // Assert
        Assert.False(reference.Equals(new TransferRecord(anyOther, anyTo, Amount.Of(500), 42)));
        Assert.False(reference.Equals(new TransferRecord(anyFrom, anyOther, Amount.Of(500), 42)));
        Assert.False(reference.Equals(new TransferRecord(anyFrom, anyTo, Amount.Of(501), 42)));
        Assert.False(reference.Equals(new TransferRecord(anyFrom, anyTo, Amount.Of(500), 43)));
        Assert.False(reference.Equals((TransferRecord?)null));
    }

    [Fact]
    public void A_record_prints_both_ends_the_amount_and_the_block() {
        // Arrange
        Address from = new Address("0x5aaeb6053f3e94c9b9a09f33669435e7ef1beaed");

        // Act
        TransferRecord record = new TransferRecord(from, Address.Zero, Amount.Of(500), 42);

        // Assert
        Assert.Equal("0x5aaeb6053f3e94c9b9a09f33669435e7ef1beaed -> 0x0000000000000000000000000000000000000000: 500 (block 42)", record.ToString());
    }

}
