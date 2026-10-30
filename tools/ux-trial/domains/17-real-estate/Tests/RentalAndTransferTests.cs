using JustDummies;
using RealEstate.Domain;
using Xunit;

namespace RealEstate.Tests;

public sealed class RentalAndTransferTests {

    [Fact]
    public void The_deposit_on_a_home_is_capped_at_one_month() {
        // Arrange
        RentalListing anyHomeListing = Arbitrary.RentalListing()
                                                .WithProperty(p => p.WithKind(k => k.Except(PropertyKind.Commercial)))
                                                .WithMonthlyRent(850.00m)
                                                .Generate();

        // Act
        decimal deposit = anyHomeListing.MaximumDeposit;

        // Assert
        Assert.Equal(850.00m, deposit);
    }

    [Fact]
    public void A_G_rated_home_cannot_be_listed_in_2026() {
        // Arrange
        Property anyGHome = Any.Property()
                               .WithKind(k => k.Except(PropertyKind.Commercial))
                               .WithDiagnosis(d => d.WithDeclaredRating(EnergyRating.G).WithIssuedOn(new DateOnly(2024, 6, 1)))
                               .Generate();
        decimal anyRent = Any.Decimal().Positive().WithScale(2).Generate();

        // Act
        Action list = () => _ = new RentalListing(anyGHome, anyRent, new DateOnly(2026, 2, 1), new DateOnly(2027, 2, 1));

        // Assert
        Assert.Throws<InvalidOperationException>(list);
    }

    [Fact]
    public void Transfer_duty_is_5_80665_percent_rounded_to_the_cent() {
        // Arrange
        Transfer anyTransfer = Arbitrary.Transfer().WithPrice(200_000m).Generate();

        // Act
        decimal duty = anyTransfer.TransferDuty;

        // Assert
        Assert.Equal(11_613.30m, duty);
    }

    [Fact]
    public void A_deed_registered_33_days_after_signing_is_late() {
        // Arrange
        Transfer anyTransfer = Arbitrary.Transfer()
                                        .WithSignedOn(new DateOnly(2024, 1, 10))
                                        .WithRegisteredOn(new DateOnly(2024, 2, 12))
                                        .Generate();

        // Act
        bool late = anyTransfer.IsLate;

        // Assert
        Assert.True(late);
    }
}
