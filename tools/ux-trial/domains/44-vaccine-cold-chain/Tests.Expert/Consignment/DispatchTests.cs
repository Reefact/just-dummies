using ColdChain.Domain;
using ColdChain.Tests.Expert.Generators;
using ColdChain.Tests.Expert.Support;
using Freight.Domain;
using JustDummies;
using Serialisation.Domain;
using Xunit;

namespace ColdChain.Tests.Expert.Consignment;

public sealed class DispatchTests {

    private static AnyColdChainConsignment CalibratedFromMarch1stToMarch31st() {
        return Journey.Planned().WithEachPallet(pallet => pallet.WithLogger(logger => logger.WithCalibratedOn(new DateOnly(2026, 3, 1)).WithCalibrationDueOn(new DateOnly(2026, 3, 31))));
    }

    private static AnyColdChainConsignment WithPacksExpiringOn(DateOnly expiry) {
        return Journey.Planned().WithEachPallet(pallet => pallet.WithEachPack(pack => pack.WithExpiresOn(expiry)));
    }

    [Fact]
    public void Dispatching_hands_the_consignment_to_the_carrier_of_the_first_leg() {
        // Arrange
        ColdChainConsignment consignment = Journey.Planned().Generate();
        CustodianId          carrier     = Any.CustodianId().Generate();

        // Act
        consignment.Dispatch(carrier, Journey.Start);

        // Assert
        Assert.Equal(ConsignmentStatus.InTransit, consignment.Status);
        Assert.Equal(Journey.Start, consignment.DispatchedAt);

        LegCustody custody = Assert.Single(consignment.Custody);

        Assert.Equal(0, custody.LegIndex);
        Assert.Equal(carrier, custody.Custodian);
        Assert.Equal(Journey.Start, custody.AcceptedAt);
        Assert.True(custody.IsOpen);
        Assert.Same(consignment.Shipment.Legs[0], consignment.CurrentLeg);
    }

    [Fact]
    public void Dispatching_sends_the_freight_shipment_on_its_way_too() {
        // Arrange
        ColdChainConsignment consignment = Journey.Planned().Generate();

        // Act
        consignment.Dispatch(Any.CustodianId().Generate(), Journey.Start);

        // Assert
        Assert.Equal(ShipmentStatus.InTransit, consignment.Shipment.Status);
        Assert.Equal(Journey.Start, consignment.Shipment.DepartedAt);
    }

    [Fact]
    public void A_consignment_cannot_be_dispatched_twice() {
        // Arrange
        Journey journey = Journey.Dispatched();

        // Assert
        Refused.Because(ColdChainRefusal.NotPlanned, () => journey.Consignment.Dispatch(Any.CustodianId().Generate(), Journey.Start.AddMinutes(1)));
    }

    [Fact]
    public void A_shipment_that_left_without_the_consignment_cannot_be_dispatched_with_it() {
        // Arrange
        ColdChainConsignment consignment = Journey.Planned().Generate();

        consignment.Shipment.Depart(Journey.Start);

        // Assert
        Refused.Because(ColdChainRefusal.ShipmentNotBooked, () => consignment.Dispatch(Any.CustodianId().Generate(), Journey.Start));
    }

    [Fact]
    public void A_missing_carrier_is_refused() {
        // Arrange
        ColdChainConsignment consignment = Journey.Planned().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => consignment.Dispatch(null!, Journey.Start));
    }

    [Theory]
    [InlineData(2026, 2, 28, 23, 59, false)]
    [InlineData(2026, 3, 1, 0, 0, true)]
    [InlineData(2026, 3, 15, 12, 0, true)]
    [InlineData(2026, 3, 31, 23, 59, true)]
    [InlineData(2026, 4, 1, 0, 0, false)]
    public void A_consignment_leaves_only_on_a_day_the_logger_is_calibrated_both_ends_of_the_certificate_included(int year, int month, int day, int hour, int minute, bool expectedDispatched) {
        // Arrange
        ColdChainConsignment consignment = CalibratedFromMarch1stToMarch31st().Generate();
        DateTimeOffset       at          = new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero);

        // Act and assert
        if (expectedDispatched) {
            consignment.Dispatch(Any.CustodianId().Generate(), at);

            Assert.Equal(ConsignmentStatus.InTransit, consignment.Status);
        } else {
            Refused.Because(ColdChainRefusal.LoggerCalibrationExpired, () => consignment.Dispatch(Any.CustodianId().Generate(), at));
        }
    }

    [Fact]
    public void The_calibration_day_is_the_utc_day_of_the_dispatch_not_the_local_one() {
        // Arrange
        ColdChainConsignment consignment = CalibratedFromMarch1stToMarch31st().Generate();

        // 1 March 01:00 at +02:00 is still 28 February in UTC.
        DateTimeOffset localMarchFirst = new DateTimeOffset(2026, 3, 1, 1, 0, 0, TimeSpan.FromHours(2));

        // Assert
        Refused.Because(ColdChainRefusal.LoggerCalibrationExpired, () => consignment.Dispatch(Any.CustodianId().Generate(), localMarchFirst));
    }

    [Fact]
    public void One_logger_out_of_calibration_stops_the_whole_consignment() {
        // Arrange
        Gtin sharedGtin = Any.Gtin().Generate();

        ColdChainPallet calibrated = Any.ColdChainPallet().WithGtin(sharedGtin)
                                        .WithLogger(logger => logger.WithRecordingIntervalMinutes(5).WithCalibratedOn(new DateOnly(2026, 3, 1)).WithCalibrationDueOn(new DateOnly(2026, 3, 31)))
                                        .Generate();
        ColdChainPallet lapsed = Any.ColdChainPallet().WithGtin(sharedGtin)
                                    .WithLogger(logger => logger.WithRecordingIntervalMinutes(5).WithCalibratedOn(new DateOnly(2026, 1, 1)).WithCalibrationDueOn(new DateOnly(2026, 3, 8)))
                                    .Generate();

        ColdChainConsignment consignment = Journey.Planned().WithProduct(product => product.WithGtin(sharedGtin)).WithPallets([calibrated, lapsed]).Generate();

        // Assert
        Refused.Because(ColdChainRefusal.LoggerCalibrationExpired, () => consignment.Dispatch(Any.CustodianId().Generate(), Journey.Start));
    }

    [Theory]
    [InlineData(2026, 3, 9, true)]
    [InlineData(2026, 3, 10, true)]
    [InlineData(2026, 3, 8, false)]
    public void A_consignment_leaves_only_if_its_packs_are_in_date_the_day_they_expire_included(int year, int month, int day, bool expectedDispatched) {
        // Arrange
        ColdChainConsignment consignment = WithPacksExpiringOn(new DateOnly(year, month, day)).Generate();

        // Act and assert
        if (expectedDispatched) {
            consignment.Dispatch(Any.CustodianId().Generate(), Journey.Start);

            Assert.Equal(ConsignmentStatus.InTransit, consignment.Status);
        } else {
            Refused.Because(ColdChainRefusal.PackExpiredBeforeDispatch, () => consignment.Dispatch(Any.CustodianId().Generate(), Journey.Start));
        }
    }

    [Fact]
    public void The_expiry_is_judged_on_the_utc_day_of_the_dispatch_not_the_local_one() {
        // Arrange
        ColdChainConsignment consignment = WithPacksExpiringOn(new DateOnly(2026, 3, 8)).Generate();

        // 9 March 01:00 at +02:00 is still 8 March in UTC: the packs expire that very day.
        DateTimeOffset localMarchNinth = new DateTimeOffset(2026, 3, 9, 1, 0, 0, TimeSpan.FromHours(2));

        // Act
        consignment.Dispatch(Any.CustodianId().Generate(), localMarchNinth);

        // Assert
        Assert.Equal(ConsignmentStatus.InTransit, consignment.Status);
    }

    [Fact]
    public void A_refused_dispatch_changes_nothing() {
        // Arrange
        ColdChainConsignment consignment = WithPacksExpiringOn(new DateOnly(2026, 3, 8)).Generate();

        // Act
        Assert.ThrowsAny<ColdChainRefusedException>(() => consignment.Dispatch(Any.CustodianId().Generate(), Journey.Start));

        // Assert
        Assert.Equal(ConsignmentStatus.Planned, consignment.Status);
        Assert.Null(consignment.DispatchedAt);
        Assert.Empty(consignment.Custody);
        Assert.Equal(ShipmentStatus.Booked, consignment.Shipment.Status);
    }

}
