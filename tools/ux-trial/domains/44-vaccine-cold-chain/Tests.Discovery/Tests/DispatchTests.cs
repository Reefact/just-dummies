using ColdChain.Domain;

using Freight.Domain;

using JustDummies;

using Warehouse.Domain;

using Xunit;

namespace ColdChain.Tests;

public sealed class DispatchTests {

    private static ColdChainRefusal RefusalOf(Action action) {
        return Assert.Throws<ColdChainRefusedException>(action).Refusal;
    }

    [Fact]
    public void Dispatching_hands_the_first_leg_to_the_carrier_and_puts_the_consignment_in_transit() {
        // Arrange
        Journey     journey = Journey.Planned(new JourneyOptions { Legs = 2 });
        CustodianId carrier = new AnyCustodianId().Generate();

        // Act
        journey.Consignment.Dispatch(carrier, Journey.DispatchedAt);

        // Assert
        ColdChainConsignment consignment = journey.Consignment;
        Assert.Equal(ConsignmentStatus.InTransit, consignment.Status);
        Assert.Equal(Journey.DispatchedAt, consignment.DispatchedAt);
        LegCustody custody = Assert.Single(consignment.Custody);
        Assert.Equal(0, custody.LegIndex);
        Assert.Same(consignment.Shipment.Legs[0], custody.Leg);
        Assert.Same(carrier, custody.Custodian);
        Assert.Equal(Journey.DispatchedAt, custody.AcceptedAt);
        Assert.True(custody.IsOpen);
        Assert.Same(consignment.Shipment.Legs[0], consignment.CurrentLeg);
    }

    [Fact]
    public void Dispatching_departs_the_freight_shipment() {
        // Arrange
        Journey journey = Journey.Planned();

        // Act
        journey.Dispatched();

        // Assert
        Assert.Equal(ShipmentStatus.InTransit, journey.Consignment.Shipment.Status);
        Assert.Equal(Journey.DispatchedAt, journey.Consignment.Shipment.DepartedAt);
    }

    [Fact]
    public void A_consignment_cannot_be_dispatched_twice() {
        // Arrange
        Journey journey = Journey.Planned().Dispatched();

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.Dispatch(journey.Carrier, Journey.DispatchedAt.AddHours(1)));

        // Assert
        Assert.Equal(ColdChainRefusal.NotPlanned, refusal);
        Assert.Single(journey.Consignment.Custody);
        Assert.Equal(Journey.DispatchedAt, journey.Consignment.DispatchedAt);
    }

    [Fact]
    public void A_carrier_is_required() {
        // Arrange
        Journey journey = Journey.Planned();

        // Act
        Exception? thrown = Record.Exception(() => journey.Consignment.Dispatch(null!, Journey.DispatchedAt));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
        Assert.Equal(ConsignmentStatus.Planned, journey.Consignment.Status);
    }

    [Fact]
    public void A_shipment_departed_behind_the_back_of_the_consignment_refuses_the_dispatch() {
        // Arrange
        Journey journey = Journey.Planned();
        journey.Consignment.Shipment.Depart(Journey.DispatchedAt);

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.Dispatch(journey.Carrier, Journey.DispatchedAt));

        // Assert
        Assert.Equal(ColdChainRefusal.ShipmentNotBooked, refusal);
        Assert.Equal(ConsignmentStatus.Planned, journey.Consignment.Status);
    }

    [Theory]
    [InlineData("2026-03-02", true)]
    [InlineData("2026-03-01", true)]
    [InlineData("2026-03-03", false)]
    public void A_logger_must_be_calibrated_on_the_day_of_dispatch_the_due_day_included(string dispatchDay, bool accepted) {
        // Arrange
        Journey        journey  = Journey.Planned(new JourneyOptions { CalibratedOn = new DateOnly(2026, 1, 15), CalibrationDueOn = new DateOnly(2026, 3, 2) });
        DateTimeOffset dispatch = DateTimeOffset.Parse(dispatchDay + "T12:00:00+00:00");

        // Act
        Exception? thrown = Record.Exception(() => journey.Consignment.Dispatch(journey.Carrier, dispatch));

        // Assert
        if (accepted) {
            Assert.Null(thrown);
        } else {
            ColdChainRefusedException refusal = Assert.IsType<ColdChainRefusedException>(thrown);
            Assert.Equal(ColdChainRefusal.LoggerCalibrationExpired, refusal.Refusal);
            Assert.Equal(ConsignmentStatus.Planned, journey.Consignment.Status);
        }
    }

    [Fact]
    public void A_logger_calibrated_the_day_of_dispatch_is_calibrated() {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { CalibratedOn = new DateOnly(2026, 3, 2), CalibrationDueOn = new DateOnly(2026, 6, 1) });

        // Act
        journey.Consignment.Dispatch(journey.Carrier, new DateTimeOffset(2026, 3, 2, 0, 0, 0, TimeSpan.Zero));

        // Assert
        Assert.Equal(ConsignmentStatus.InTransit, journey.Consignment.Status);
    }

    [Fact]
    public void A_logger_not_yet_calibrated_on_the_day_of_dispatch_is_refused() {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { CalibratedOn = new DateOnly(2026, 3, 3), CalibrationDueOn = new DateOnly(2026, 6, 1) });

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.Dispatch(journey.Carrier, Journey.DispatchedAt));

        // Assert
        Assert.Equal(ColdChainRefusal.LoggerCalibrationExpired, refusal);
    }

    [Fact]
    public void The_day_of_dispatch_is_the_utc_day_whatever_the_offset_it_is_written_with() {
        // Arrange: calibrated up to 2 March only. 00:30 on 3 March at +02:00 is still 2 March in UTC.
        Journey journey = Journey.Planned(new JourneyOptions { CalibratedOn = new DateOnly(2026, 1, 15), CalibrationDueOn = new DateOnly(2026, 3, 2) });

        // Act
        journey.Consignment.Dispatch(journey.Carrier, new DateTimeOffset(2026, 3, 3, 0, 30, 0, TimeSpan.FromHours(2)));

        // Assert
        Assert.Equal(ConsignmentStatus.InTransit, journey.Consignment.Status);
    }

    [Fact]
    public void Written_with_an_offset_behind_utc_the_day_of_dispatch_can_be_the_next_one() {
        // Arrange: 23:30 on 2 March at -02:00 is 01:30 on 3 March in UTC, after the calibration ran out.
        Journey journey = Journey.Planned(new JourneyOptions { CalibratedOn = new DateOnly(2026, 1, 15), CalibrationDueOn = new DateOnly(2026, 3, 2) });

        // Act
        ColdChainRefusal refusal = RefusalOf(() => journey.Consignment.Dispatch(journey.Carrier, new DateTimeOffset(2026, 3, 2, 23, 30, 0, TimeSpan.FromHours(-2))));

        // Assert
        Assert.Equal(ColdChainRefusal.LoggerCalibrationExpired, refusal);
    }

    [Fact]
    public void One_lapsed_logger_among_several_is_enough_to_refuse() {
        // Arrange
        VaccineProduct       product     = new AnyVaccineProduct().WithBudget(new StabilityBudget(120, 60)).Generate();
        ColdChainConsignment consignment = new AnyColdChainConsignment()
            .WithProduct(product)
            .WithPalletCount(3)
            .WithPallets(pallet => pallet.WithLogger(l => l.WithRecordingIntervalMinutes(10).WithCalibratedOn(new DateOnly(2026, 1, 15)).WithCalibrationDueOn(new DateOnly(2026, 12, 31))))
            .Generate();
        // The second pallet carries a logger whose certificate ran out the day before.
        ColdChainPallet lapsed = new AnyColdChainPallet().OfProduct(product.Gtin).WithLogger(l => l.WithRecordingIntervalMinutes(10).WithCalibratedOn(new DateOnly(2025, 6, 1)).WithCalibrationDueOn(new DateOnly(2026, 3, 1))).Generate();
        ColdChainConsignment withLapsed = new ColdChainConsignment(new AnyConsignmentId().Generate(), product, Journey.BookedReefer(1), [consignment.Pallets[0], lapsed, consignment.Pallets[1]]);

        // Act
        ColdChainRefusal refusal = RefusalOf(() => withLapsed.Dispatch(new AnyCustodianId().Generate(), Journey.DispatchedAt));

        // Assert
        Assert.Equal(ColdChainRefusal.LoggerCalibrationExpired, refusal);
        Assert.Equal(ConsignmentStatus.Planned, withLapsed.Status);
        Assert.Equal(ShipmentStatus.Booked, withLapsed.Shipment.Status);
    }

    [Theory]
    [InlineData("2026-03-02", true)]
    [InlineData("2026-03-03", true)]
    [InlineData("2026-03-01", false)]
    public void Packs_must_be_in_date_on_the_day_of_dispatch_the_expiry_day_included(string expiryDay, bool accepted) {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { PackExpiresOn = DateOnly.Parse(expiryDay) });

        // Act
        Exception? thrown = Record.Exception(() => journey.Consignment.Dispatch(journey.Carrier, Journey.DispatchedAt));

        // Assert
        if (accepted) {
            Assert.Null(thrown);
        } else {
            ColdChainRefusedException refusal = Assert.IsType<ColdChainRefusedException>(thrown);
            Assert.Equal(ColdChainRefusal.PackExpiredBeforeDispatch, refusal.Refusal);
            Assert.Equal(ConsignmentStatus.Planned, journey.Consignment.Status);
            Assert.Equal(ShipmentStatus.Booked, journey.Consignment.Shipment.Status);
        }
    }

    [Fact]
    public void A_refused_dispatch_leaves_the_consignment_ready_for_another_attempt() {
        // Arrange
        Journey journey = Journey.Planned(new JourneyOptions { CalibrationDueOn = new DateOnly(2026, 3, 2) });
        Assert.Throws<ColdChainRefusedException>(() => journey.Consignment.Dispatch(journey.Carrier, Journey.DispatchedAt.AddDays(5)));

        // Act
        journey.Consignment.Dispatch(journey.Carrier, Journey.DispatchedAt);

        // Assert
        Assert.Equal(ConsignmentStatus.InTransit, journey.Consignment.Status);
    }

}
