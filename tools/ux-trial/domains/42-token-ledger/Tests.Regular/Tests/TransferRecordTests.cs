namespace TokenLedger.Domain.Tests.Regular;

public sealed class TransferRecordTests {

    [Fact]
    public void A_record_exposes_what_it_was_built_with() {
        // Arrange
        Address     anySender    = Any.Address().Generate();
        Address     anyRecipient = Any.Address().Generate();
        TokenAmount anyAmount    = Any.TokenAmount().Generate();

        // Act
        TransferRecord record = new TransferRecord(anySender, anyRecipient, anyAmount, 42);

        // Assert
        Assert.Equal(anySender, record.From);
        Assert.Equal(anyRecipient, record.To);
        Assert.Equal(anyAmount, record.Amount);
        Assert.Equal(42UL, record.BlockNumber);
    }

    [Fact]
    public void A_movement_between_two_accounts_is_neither_a_mint_nor_a_burn() {
        // Act
        TransferRecord record = Any.TransferRecord().Generate();

        // Assert
        Assert.False(record.IsMint);
        Assert.False(record.IsBurn);
    }

    [Fact]
    public void A_movement_from_the_zero_address_is_a_mint() {
        // Act
        TransferRecord record = Any.TransferRecord().WithFrom(Address.Zero).Generate();

        // Assert
        Assert.True(record.IsMint);
        Assert.False(record.IsBurn);
    }

    [Fact]
    public void A_movement_to_the_zero_address_is_a_burn() {
        // Act
        TransferRecord record = Any.TransferRecord().WithTo(Address.Zero).Generate();

        // Assert
        Assert.True(record.IsBurn);
        Assert.False(record.IsMint);
    }

    [Fact]
    public void A_movement_from_the_zero_address_to_the_zero_address_is_refused() {
        Assert.Throws<ArgumentException>(() => Any.TransferRecord().WithFrom(Address.Zero).WithTo(Address.Zero).Generate());
    }

    [Fact]
    public void A_movement_of_nothing_is_legal() {
        // Act
        TransferRecord record = Any.TransferRecord().WithAmount(TokenAmount.Zero).Generate();

        // Assert
        Assert.True(record.Amount.IsZero);
    }

    [Fact]
    public void A_movement_from_an_account_to_itself_is_legal() {
        // Arrange
        Address anyAccount = Any.Address().Generate();

        // Act
        TransferRecord record = Any.TransferRecord().WithFrom(anyAccount).WithTo(anyAccount).Generate();

        // Assert
        Assert.Equal(record.From, record.To);
    }

    [Fact]
    public void A_record_refuses_null_arguments() {
        // Arrange
        Address     anyAddress = Any.Address().Generate();
        TokenAmount anyAmount  = Any.TokenAmount().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TransferRecord(null!, anyAddress, anyAmount, 0));
        Assert.Throws<ArgumentNullException>(() => new TransferRecord(anyAddress, null!, anyAmount, 0));
        Assert.Throws<ArgumentNullException>(() => new TransferRecord(anyAddress, anyAddress, null!, 0));
    }

    [Fact]
    public void Records_with_the_same_content_are_equal() {
        // Arrange
        TransferRecord anyRecord = Any.TransferRecord().Generate();

        // Act
        TransferRecord copy = new TransferRecord(anyRecord.From, anyRecord.To, anyRecord.Amount, anyRecord.BlockNumber);

        // Assert
        Assert.Equal(anyRecord, copy);
        Assert.Equal(anyRecord.GetHashCode(), copy.GetHashCode());
    }

    [Fact]
    public void Records_differing_in_one_part_are_not_equal() {
        // Arrange
        Address        anyFrom   = Any.Address().Generate();
        Address        anyTo     = Any.Address().Generate();
        Address        anyOther  = Any.Address().Generate();
        TransferRecord reference = new TransferRecord(anyFrom, anyTo, new TokenAmount(5), 10);

        // Assert
        Assert.NotEqual(reference, new TransferRecord(anyOther, anyTo, new TokenAmount(5), 10));
        Assert.NotEqual(reference, new TransferRecord(anyFrom, anyOther, new TokenAmount(5), 10));
        Assert.NotEqual(reference, new TransferRecord(anyFrom, anyTo, new TokenAmount(6), 10));
        Assert.NotEqual(reference, new TransferRecord(anyFrom, anyTo, new TokenAmount(5), 11));
    }

    [Fact]
    public void A_record_is_described_by_its_movement_and_block() {
        // Arrange
        Address anyFrom = Any.Address().Generate();
        Address anyTo   = Any.Address().Generate();

        // Act
        TransferRecord record = new TransferRecord(anyFrom, anyTo, new TokenAmount(250), 8);

        // Assert
        Assert.Equal($"{anyFrom} -> {anyTo}: 250 (block 8)", record.ToString());
    }

}
