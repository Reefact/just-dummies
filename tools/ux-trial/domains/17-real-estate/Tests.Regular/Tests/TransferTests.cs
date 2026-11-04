namespace RealEstate.Tests.Regular;

public sealed class TransferTests {

    [Fact]
    public void A_transfer_exposes_what_it_was_built_with() {
        // Arrange
        CadastralReference parcel = new CadastralReference("75056 000 AB 0123");

        // Act
        Transfer transfer = new Transfer(parcel, 350_000m, new DateOnly(2024, 3, 1), new DateOnly(2024, 3, 20));

        // Assert
        Assert.Same(parcel, transfer.Parcel);
        Assert.Equal(350_000m, transfer.Price);
        Assert.Equal(new DateOnly(2024, 3, 1), transfer.SignedOn);
        Assert.Equal(new DateOnly(2024, 3, 20), transfer.RegisteredOn);
    }

    [Fact]
    public void A_transfer_needs_a_parcel() {
        // Arrange
        decimal  anyPrice    = Any.Decimal().Positive().LessThanOrEqualTo(1_000_000_000m).Generate();
        DateOnly anySignedOn = Any.DateOnly().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Transfer(null!, anyPrice, anySignedOn, anySignedOn));

        // Assert
        Assert.Equal("parcel", exception.ParamName);
    }

    [Fact]
    public void A_price_of_zero_is_refused() {
        // Arrange
        CadastralReference anyParcel = Any.CadastralReference().Generate();
        DateOnly           anyDay    = Any.DateOnly().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Transfer(anyParcel, 0m, anyDay, anyDay));

        // Assert
        Assert.Equal("price", exception.ParamName);
    }

    [Fact]
    public void A_negative_price_is_refused() {
        // Arrange
        CadastralReference anyParcel   = Any.CadastralReference().Generate();
        decimal            anyNegative = Any.Decimal().Negative().Generate();
        DateOnly           anyDay      = Any.DateOnly().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Transfer(anyParcel, anyNegative, anyDay, anyDay));
    }

    [Fact]
    public void A_price_above_a_billion_is_refused() {
        // Arrange
        CadastralReference anyParcel   = Any.CadastralReference().Generate();
        decimal            anyTooHigh  = Any.Decimal().GreaterThan(1_000_000_000m).Generate();
        DateOnly           anyDay      = Any.DateOnly().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Transfer(anyParcel, anyTooHigh, anyDay, anyDay));

        // Assert
        Assert.Equal("price", exception.ParamName);
    }

    [Fact]
    public void A_cent_above_a_billion_is_refused() {
        // Arrange
        CadastralReference anyParcel = Any.CadastralReference().Generate();
        DateOnly           anyDay    = Any.DateOnly().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Transfer(anyParcel, 1_000_000_000.01m, anyDay, anyDay));
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("1000000000")]
    public void The_extreme_prices_are_accepted(string price) {
        // Arrange
        CadastralReference anyParcel = Any.CadastralReference().Generate();
        DateOnly           anyDay    = Any.DateOnly().Generate();

        // Act
        Transfer transfer = new Transfer(anyParcel, decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture), anyDay, anyDay);

        // Assert
        Assert.Equal(decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture), transfer.Price);
    }

    [Fact]
    public void A_registration_before_the_signature_is_refused() {
        // Arrange
        CadastralReference anyParcel = Any.CadastralReference().Generate();
        decimal            anyPrice  = Any.Decimal().Positive().LessThanOrEqualTo(1_000_000_000m).Generate();
        (DateOnly anyEarlier, DateOnly anyLater) = Any.Pair(Any.DateOnly(), Any.DateOnly()).Having.Item1BeforeItem2().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Transfer(anyParcel, anyPrice, anyLater, anyEarlier));

        // Assert
        Assert.Equal("registeredOn", exception.ParamName);
    }

    [Fact]
    public void A_registration_the_day_before_the_signature_is_refused() {
        // Arrange
        CadastralReference anyParcel = Any.CadastralReference().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Transfer(anyParcel, 100_000m, new DateOnly(2024, 3, 1), new DateOnly(2024, 2, 29)));
    }

    [Fact]
    public void A_registration_on_the_day_of_the_signature_is_accepted() {
        // Arrange
        CadastralReference anyParcel = Any.CadastralReference().Generate();
        DateOnly           anyDay    = Any.DateOnly().Generate();

        // Act
        Transfer transfer = new Transfer(anyParcel, 100_000m, anyDay, anyDay);

        // Assert
        Assert.Equal(0, transfer.RegistrationDelayDays);
        Assert.False(transfer.IsLate);
    }

    [Theory]
    [InlineData("250000", "14516.63")]      // 14516.625: halves round away from zero
    [InlineData("100000", "5806.65")]
    [InlineData("1000000000", "58066500.00")]
    [InlineData("0.01", "0.00")]
    [InlineData("1", "0.06")]               // 0.0580665
    [InlineData("86.00", "4.99")]           // 4.993719
    public void The_transfer_duty_is_the_rounded_product_of_the_price_and_the_duty_rate(string price, string duty) {
        // Arrange
        decimal    parsedPrice = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture);
        decimal    parsedDuty  = decimal.Parse(duty, System.Globalization.CultureInfo.InvariantCulture);
        Transfer   transfer    = Any.Transfer().WithPrice(parsedPrice).Generate();

        // Act
        decimal actual = transfer.TransferDuty;

        // Assert
        Assert.Equal(parsedDuty, actual);
    }

    [Fact]
    public void The_duty_rate_is_the_usual_combined_rate() {
        // Assert
        Assert.Equal(0.0580665m, Transfer.DutyRate);
    }

    [Theory]
    [InlineData("2024-01-01", "2024-01-01", 0, false)]
    [InlineData("2024-01-01", "2024-01-30", 29, false)]
    [InlineData("2024-01-01", "2024-01-31", 30, false)]
    [InlineData("2024-01-01", "2024-02-01", 31, true)]
    [InlineData("2023-12-15", "2024-02-15", 62, true)]
    [InlineData("2024-02-01", "2024-03-02", 30, false)]    // 2024 is a leap year
    public void A_deed_registered_more_than_thirty_days_after_the_signature_is_late(string signedOn, string registeredOn, int delay, bool isLate) {
        // Arrange
        Transfer transfer = Any.Transfer()
                               .WithSignedOn(DateOnly.Parse(signedOn))
                               .WithRegisteredOn(DateOnly.Parse(registeredOn))
                               .Generate();

        // Act & Assert
        Assert.Equal(delay, transfer.RegistrationDelayDays);
        Assert.Equal(isLate, transfer.IsLate);
    }

}
