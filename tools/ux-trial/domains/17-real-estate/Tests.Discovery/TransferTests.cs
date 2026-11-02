using JustDummies;

using RealEstate.Domain;

using Xunit;

namespace RealEstate.Tests.Discovery;

public sealed class TransferTests {

    [Fact]
    public void A_transfer_keeps_what_it_is_given() {
        // Arrange
        CadastralReference parcel = new CadastralReference("75056 000 AB 0123");

        // Act
        Transfer transfer = new Transfer(parcel, 250_000m, new DateOnly(2025, 1, 10), new DateOnly(2025, 1, 20));

        // Assert
        Assert.Same(parcel, transfer.Parcel);
        Assert.Equal(250_000m, transfer.Price);
        Assert.Equal(new DateOnly(2025, 1, 10), transfer.SignedOn);
        Assert.Equal(new DateOnly(2025, 1, 20), transfer.RegisteredOn);
    }

    [Fact]
    public void A_transfer_without_a_parcel_is_refused() {
        // Arrange
        DateOnly signed = new DateOnly(2025, 1, 10);

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Transfer(null!, 250_000m, signed, signed));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("-250000.50")]
    [InlineData("1000000000.01")]
    [InlineData("2000000000")]
    public void A_price_that_is_not_in_zero_to_a_billion_is_refused(string price) {
        // Arrange
        CadastralReference anyParcel = Any.CadastralReference().Generate();
        DateOnly           signed    = new DateOnly(2025, 1, 10);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Transfer(anyParcel, decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture), signed, signed));
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("1000000000")]
    public void The_smallest_price_and_a_billion_are_accepted(string price) {
        // Arrange
        CadastralReference anyParcel = Any.CadastralReference().Generate();
        DateOnly           signed    = new DateOnly(2025, 1, 10);

        // Act
        Transfer transfer = new Transfer(anyParcel, decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture), signed, signed);

        // Assert
        Assert.Equal(decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture), transfer.Price);
    }

    [Fact]
    public void A_deed_registered_before_it_was_signed_is_refused() {
        // Arrange
        CadastralReference anyParcel = Any.CadastralReference().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Transfer(anyParcel, 250_000m, new DateOnly(2025, 1, 10), new DateOnly(2025, 1, 9)));
    }

    [Fact]
    public void A_deed_registered_the_day_it_was_signed_is_accepted_and_is_not_late() {
        // Arrange
        Transfer transfer = Any.Transfer().WithSignedOn(new DateOnly(2025, 1, 10)).WithRegisteredOn(new DateOnly(2025, 1, 10)).Generate();

        // Assert
        Assert.Equal(0, transfer.RegistrationDelayDays);
        Assert.False(transfer.IsLate);
    }

    [Theory]
    [InlineData("100000", "5806.65")]
    [InlineData("10000", "580.67")]       // 580.665 exactly: rounded away from zero, not to even (580.66)
    [InlineData("1", "0.06")]             // 0.0580665
    [InlineData("0.01", "0.00")]          // 0.000580665
    [InlineData("1000000000", "58066500.00")]
    [InlineData("250000.50", "14516.65")] // 14516.654033...
    public void The_transfer_duty_is_5_80665_percent_of_the_price_rounded_to_the_cent_away_from_zero(string price, string duty) {
        // Arrange
        Transfer transfer = Any.Transfer().WithPrice(decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture)).Generate();

        // Assert
        Assert.Equal(decimal.Parse(duty, System.Globalization.CultureInfo.InvariantCulture), transfer.TransferDuty);
    }

    [Theory]
    [InlineData("2025-01-10", "2025-01-10", 0, false)]
    [InlineData("2025-01-10", "2025-01-11", 1, false)]
    [InlineData("2025-01-10", "2025-02-09", 30, false)]
    [InlineData("2025-01-10", "2025-02-10", 31, true)]
    [InlineData("2024-02-15", "2024-03-16", 30, false)]   // across a leap day
    [InlineData("2024-02-15", "2024-03-17", 31, true)]
    [InlineData("2024-12-20", "2025-01-19", 30, false)]   // across a year end
    [InlineData("2024-12-20", "2025-01-20", 31, true)]
    public void A_deed_is_late_beyond_thirty_days(string signedOn, string registeredOn, int delay, bool isLate) {
        // Arrange
        Transfer transfer = Any.Transfer().WithSignedOn(DateOnly.Parse(signedOn)).WithRegisteredOn(DateOnly.Parse(registeredOn)).Generate();

        // Assert
        Assert.Equal(delay, transfer.RegistrationDelayDays);
        Assert.Equal(isLate, transfer.IsLate);
    }

}
