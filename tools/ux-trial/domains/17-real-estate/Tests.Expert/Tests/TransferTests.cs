using JustDummies;

using Xunit;

namespace RealEstate.Domain.Tests;

public sealed class TransferTests {

    // Two dates a transfer accepts: registered on or after the day it was signed.
    private static (DateOnly SignedOn, DateOnly RegisteredOn) AnyDatesInOrder() {
        return Any.Pair(Any.DateOnly(), Any.DateOnly()).Having.Item1BeforeOrEqualToItem2().Generate();
    }

    // A signing date far enough from the end of the calendar to add a delay to it.
    private static AnyDateOnly AnySigningDate() {
        return Any.DateOnly().BeforeOrEqualTo(DateOnly.MaxValue.AddDays(-1000));
    }

    [Fact]
    public void Keeps_the_parcel_the_price_and_the_dates_it_was_built_from() {
        // Arrange
        CadastralReference parcel = Any.CadastralReference().Generate();

        // Act
        Transfer transfer = new Transfer(parcel, 245_000m, new DateOnly(2025, 3, 3), new DateOnly(2025, 3, 20));

        // Assert
        Assert.Same(parcel, transfer.Parcel);
        Assert.Equal(245_000m, transfer.Price);
        Assert.Equal(new DateOnly(2025, 3, 3), transfer.SignedOn);
        Assert.Equal(new DateOnly(2025, 3, 20), transfer.RegisteredOn);
    }

    [Fact]
    public void Refuses_a_missing_parcel() {
        // Arrange
        (DateOnly anySignedOn, DateOnly anyRegisteredOn) = AnyDatesInOrder();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Transfer(null!, 245_000m, anySignedOn, anyRegisteredOn));

        // Assert
        Assert.Equal("parcel", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-245_000)]
    [InlineData(1_000_000_000.01)]
    [InlineData(2_000_000_000)]
    public void Refuses_a_price_that_is_not_above_zero_and_within_one_billion(decimal price) {
        // Arrange
        CadastralReference anyParcel = Any.CadastralReference().Generate();
        (DateOnly anySignedOn, DateOnly anyRegisteredOn) = AnyDatesInOrder();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Transfer(anyParcel, price, anySignedOn, anyRegisteredOn));

        // Assert
        Assert.Equal("price", exception.ParamName);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1_000_000_000)]
    public void Accepts_a_price_from_one_cent_up_to_one_billion(decimal price) {
        // Arrange
        CadastralReference anyParcel = Any.CadastralReference().Generate();
        (DateOnly anySignedOn, DateOnly anyRegisteredOn) = AnyDatesInOrder();

        // Act
        Transfer transfer = new Transfer(anyParcel, price, anySignedOn, anyRegisteredOn);

        // Assert
        Assert.Equal(price, transfer.Price);
    }

    [Fact]
    public void Refuses_a_registration_the_day_before_the_signature() {
        // Arrange
        CadastralReference anyParcel = Any.CadastralReference().Generate();
        DateOnly           signedOn  = AnySigningDate().After(DateOnly.MinValue).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Transfer(anyParcel, 245_000m, signedOn, signedOn.AddDays(-1)));

        // Assert
        Assert.Equal("registeredOn", exception.ParamName);
    }

    [Fact]
    public void Accepts_a_registration_on_the_day_of_the_signature() {
        // Arrange
        CadastralReference anyParcel = Any.CadastralReference().Generate();
        DateOnly           anyDay    = Any.DateOnly().Generate();

        // Act
        Transfer transfer = new Transfer(anyParcel, 245_000m, anyDay, anyDay);

        // Assert
        Assert.Equal(0, transfer.RegistrationDelayDays);
    }

    [Theory]
    [InlineData(100_000, 5806.65)]
    [InlineData(250_000, 14_516.63)] // 14516.625: halves go away from zero, not to the even cent
    [InlineData(245_000, 14_226.29)] // 14226.2925
    [InlineData(1, 0.06)]            // 0.0580665
    [InlineData(0.01, 0.00)]         // 0.000580665
    [InlineData(1_000_000_000, 58_066_500.00)]
    public void Charges_the_combined_duty_rate_rounded_to_the_cent(decimal price, decimal duty) {
        // Arrange
        Transfer transfer = Any.Transfer().WithPrice(price).Generate();

        // Act
        decimal actual = transfer.TransferDuty;

        // Assert
        Assert.Equal(duty, actual);
    }

    [Fact]
    public void Publishes_the_duty_rate_as_5_80665_percent() {
        // Act & Assert
        Assert.Equal(0.0580665m, Transfer.DutyRate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(31)]
    [InlineData(365)]
    public void Counts_the_days_between_the_signature_and_the_registration(int delay) {
        // Arrange
        DateOnly anySignedOn = AnySigningDate().Generate();
        Transfer transfer    = Any.Transfer().WithSignedOn(anySignedOn).WithRegisteredOn(anySignedOn.AddDays(delay)).Generate();

        // Act
        int actual = transfer.RegistrationDelayDays;

        // Assert
        Assert.Equal(delay, actual);
    }

    [Fact]
    public void Counts_the_leap_day_when_a_delay_straddles_it() {
        // Arrange
        Transfer transfer = Any.Transfer().WithSignedOn(new DateOnly(2024, 2, 28)).WithRegisteredOn(new DateOnly(2024, 3, 1)).Generate();

        // Act
        int delay = transfer.RegistrationDelayDays;

        // Assert
        Assert.Equal(2, delay);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(30, false)] // within a month
    [InlineData(31, true)]  // a day too late
    [InlineData(365, true)]
    public void Is_late_when_registered_more_than_thirty_days_after_the_signature(int delay, bool late) {
        // Arrange
        DateOnly anySignedOn = AnySigningDate().Generate();
        Transfer transfer    = Any.Transfer().WithSignedOn(anySignedOn).WithRegisteredOn(anySignedOn.AddDays(delay)).Generate();

        // Act
        bool isLate = transfer.IsLate;

        // Assert
        Assert.Equal(late, isLate);
    }

}
