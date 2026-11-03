using AgriTech.Domain;

using ColdChain.Domain;

using Freight.Domain;

using JustDummies;

using Serialisation.Domain;

using Warehouse.Domain;

using Xunit;

using FreightShipment = Freight.Domain.Shipment;

namespace ColdChain.Tests.Regular.Tests;

public sealed class ColdChainConsignmentConstructionTests {

    [Fact]
    public void A_consignment_starts_planned_on_its_product_shipment_and_pallets() {
        // Arrange
        VaccineProduct       product  = Journeys.Product();
        DateTimeOffset       anyTime  = Journeys.AnyDispatchTime();
        FreightShipment      shipment = Journeys.AnyReeferWithLegs(2);
        ConsignmentId        id       = new AnyConsignmentId().Generate();
        List<ColdChainPallet> pallets = [Journeys.AnyPallet(product, anyTime, packCount: 3), Journeys.AnyPallet(product, anyTime, packCount: 4)];

        // Act
        ColdChainConsignment consignment = new ColdChainConsignment(id, product, shipment, pallets);

        // Assert
        Assert.Equal(ConsignmentStatus.Planned, consignment.Status);
        Assert.Equal(id, consignment.Id);
        Assert.Same(product, consignment.Product);
        Assert.Same(shipment, consignment.Shipment);
        Assert.Equal(pallets, consignment.Pallets);
        Assert.Equal(7, consignment.TotalPacks);
        Assert.Null(consignment.DispatchedAt);
        Assert.Null(consignment.DeliveredAt);
        Assert.Null(consignment.ReceivedBy);
        Assert.Null(consignment.CurrentLeg);
        Assert.Empty(consignment.Custody);
        Assert.Empty(consignment.Decisions);
    }

    [Fact]
    public void A_consignment_drawn_by_its_generator_is_planned() {
        // Act
        ColdChainConsignment anyConsignment = new AnyColdChainConsignment().Generate();

        // Assert
        Assert.Equal(ConsignmentStatus.Planned, anyConsignment.Status);
    }

    [Fact]
    public void A_consignment_is_planned_on_a_booked_shipment() {
        // Arrange
        FreightShipment departed = Journeys.AnyReeferWithLegs(1);
        departed.Depart(DateTimeOffset.UnixEpoch);
        FreightShipment cancelled = Journeys.AnyReeferWithLegs(1);
        cancelled.Cancel();

        // Act
        Action onDeparted  = () => new AnyColdChainConsignment().WithShipment(departed).Generate();
        Action onCancelled = () => new AnyColdChainConsignment().WithShipment(cancelled).Generate();

        // Assert
        Assert.Equal("shipment", Assert.Throws<ArgumentException>(onDeparted).ParamName);
        Assert.Equal("shipment", Assert.Throws<ArgumentException>(onCancelled).ParamName);
    }

    [Fact]
    public void Vaccines_travel_in_a_reefer_and_nothing_else() {
        // Arrange
        ContainerType anyOtherContainer = Any.Enum<ContainerType>().DifferentFrom(ContainerType.Reefer40).Generate();
        FreightShipment shipment = new AnyShipment().WithContainerType(anyOtherContainer).Generate();

        // Act
        Action act = () => new AnyColdChainConsignment().WithShipment(shipment).Generate();

        // Assert
        Assert.Equal("shipment", Assert.Throws<ArgumentException>(act).ParamName);
    }

    [Fact]
    public void A_consignment_holds_at_least_one_pallet() {
        // Act
        Action act = () => new AnyColdChainConsignment().WithPallets([]).Generate();

        // Assert
        Assert.Equal("pallets", Assert.Throws<ArgumentException>(act).ParamName);
    }

    [Fact]
    public void A_consignment_holds_up_to_twenty_pallets() {
        // Arrange
        VaccineProduct product = Journeys.Product();

        // Act
        ColdChainConsignment consignment = Journeys.Planned(product, Journeys.AnyDispatchTime(), palletCount: ColdChainConsignment.MaxPallets, packCount: 1);

        // Assert
        Assert.Equal(20, consignment.Pallets.Count);
        Assert.Equal(20, consignment.TotalPacks);
    }

    [Fact]
    public void A_consignment_of_more_than_twenty_pallets_is_refused() {
        // Arrange
        VaccineProduct product   = Journeys.Product();
        DateTimeOffset dispatch  = Journeys.AnyDispatchTime();
        SensorId[]     loggers   = Journeys.AnyDistinctLoggerIds(21);
        List<Sscc>     ssccs     = Any.ListOf(new AnySscc()).WithCount(21).Distinct().Generate();
        List<ColdChainPallet> pallets = Enumerable.Range(0, 21).Select(index => Journeys.AnyPallet(product, dispatch, ssccs[index], loggers[index], packCount: 1)).ToList();

        // Act
        Action act = () => new AnyColdChainConsignment().WithProduct(product).WithPallets(pallets).Generate();

        // Assert
        Assert.Equal("pallets", Assert.Throws<ArgumentException>(act).ParamName);
    }

    [Fact]
    public void A_consignment_cannot_list_a_missing_pallet() {
        // Arrange
        VaccineProduct product = Journeys.Product();
        List<ColdChainPallet> pallets = [Journeys.AnyPallet(product, Journeys.AnyDispatchTime()), null!];

        // Act
        Action act = () => new AnyColdChainConsignment().WithProduct(product).WithPallets(pallets).Generate();

        // Assert
        Assert.Equal("pallets", Assert.Throws<ArgumentException>(act).ParamName);
    }

    [Fact]
    public void An_sscc_cannot_appear_twice_in_a_consignment() {
        // Arrange
        VaccineProduct product  = Journeys.Product();
        DateTimeOffset dispatch = Journeys.AnyDispatchTime();
        Sscc           sscc     = new AnySscc().Generate();
        SensorId[]     loggers  = Journeys.AnyDistinctLoggerIds(2);
        List<ColdChainPallet> pallets = [Journeys.AnyPallet(product, dispatch, sscc, loggers[0]), Journeys.AnyPallet(product, dispatch, sscc, loggers[1])];

        // Act
        Action act = () => new AnyColdChainConsignment().WithProduct(product).WithPallets(pallets).Generate();

        // Assert
        Assert.Equal("pallets", Assert.Throws<ArgumentException>(act).ParamName);
    }

    [Fact]
    public void Each_pallet_travels_with_its_own_logger() {
        // Arrange
        VaccineProduct product  = Journeys.Product();
        DateTimeOffset dispatch = Journeys.AnyDispatchTime();
        SensorId       logger   = new AnySensorId().Generate();
        List<Sscc>     ssccs    = Any.ListOf(new AnySscc()).WithCount(2).Distinct().Generate();
        List<ColdChainPallet> pallets = [Journeys.AnyPallet(product, dispatch, ssccs[0], logger), Journeys.AnyPallet(product, dispatch, ssccs[1], logger)];

        // Act
        Action act = () => new AnyColdChainConsignment().WithProduct(product).WithPallets(pallets).Generate();

        // Assert
        Assert.Equal("pallets", Assert.Throws<ArgumentException>(act).ParamName);
    }

    [Fact]
    public void A_serialised_pack_cannot_be_on_two_pallets() {
        // Arrange
        VaccineProduct product  = Journeys.Product();
        DateTimeOffset dispatch = Journeys.AnyDispatchTime();
        SerialisedPack pack     = new AnySerialisedPack().WithGtin(product.Gtin).Generate();
        SensorId[]     loggers  = Journeys.AnyDistinctLoggerIds(2);
        List<Sscc>     ssccs    = Any.ListOf(new AnySscc()).WithCount(2).Distinct().Generate();
        ColdChainPallet first   = new AnyColdChainPallet().WithSscc(ssccs[0]).WithLogger(new AnyDataLogger().WithId(loggers[0]).Generate()).WithPacks([pack]).Generate();
        ColdChainPallet second  = new AnyColdChainPallet().WithSscc(ssccs[1]).WithLogger(new AnyDataLogger().WithId(loggers[1]).Generate()).WithPacks([pack]).Generate();

        // Act
        Action act = () => new AnyColdChainConsignment().WithProduct(product).WithPallets([first, second]).Generate();

        // Assert
        Assert.Equal("pallets", Assert.Throws<ArgumentException>(act).ParamName);
    }

    [Fact]
    public void Every_pallet_carries_the_product_of_the_consignment() {
        // Arrange
        List<Gtin>       gtins             = Any.ListOf(new AnyGtin()).WithCount(2).Distinct().Generate();
        VaccineProduct   product           = new AnyVaccineProduct().WithGtin(gtins[0]).Generate();
        ColdChainPallet  ofAnotherProduct  = new AnyColdChainPallet().WithGtin(gtins[1]).Generate();

        // Act
        Action act = () => new AnyColdChainConsignment().WithProduct(product).WithPallets([ofAnotherProduct]).Generate();

        // Assert
        Assert.Equal("pallets", Assert.Throws<ArgumentException>(act).ParamName);
    }

    [Theory]
    [InlineData(10, 10, true)]
    [InlineData(10, 9, true)]
    [InlineData(10, 11, false)]
    [InlineData(30, 31, false)]
    [InlineData(1, 1, true)]
    [InlineData(1, 2, false)]
    [InlineData(0, 60, true)]
    [InlineData(0, 1, true)]
    public void A_logger_must_record_often_enough_to_resolve_the_single_excursion_allowance(int allowance, int interval, bool accepted) {
        // Arrange
        VaccineProduct product = Journeys.Product(cumulativeMinutes: Math.Max(allowance, 100), maxSingleMinutes: allowance);
        ColdChainPallet pallet = Journeys.AnyPallet(product, Journeys.AnyDispatchTime(), loggerIntervalMinutes: interval);

        // Act
        Action act = () => new AnyColdChainConsignment().WithProduct(product).WithPallets([pallet]).Generate();

        // Assert
        if (accepted) {
            act();
        } else {
            Assert.Equal("pallets", Assert.Throws<ArgumentException>(act).ParamName);
        }
    }

    [Fact]
    public void A_consignment_needs_an_id_a_product_a_shipment_and_pallets() {
        // Arrange
        VaccineProduct product = Journeys.Product();
        List<ColdChainPallet> pallets = [Journeys.AnyPallet(product, Journeys.AnyDispatchTime())];
        FreightShipment shipment = Journeys.AnyReeferWithLegs(1);
        ConsignmentId id = new AnyConsignmentId().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new ColdChainConsignment(null!, product, shipment, pallets));
        Assert.Throws<ArgumentNullException>(() => new ColdChainConsignment(id, null!, shipment, pallets));
        Assert.Throws<ArgumentNullException>(() => new ColdChainConsignment(id, product, null!, pallets));
        Assert.Throws<ArgumentNullException>(() => new ColdChainConsignment(id, product, shipment, null!));
    }

}

public sealed class DispatchTests {

    [Fact]
    public void Dispatch_hands_the_consignment_to_the_carrier_of_the_first_leg() {
        // Arrange
        DateTimeOffset       dispatchAt  = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), dispatchAt, legCount: 2);
        CustodianId          carrier     = Journeys.AnyCustodian();

        // Act
        consignment.Dispatch(carrier, dispatchAt);

        // Assert
        Assert.Equal(ConsignmentStatus.InTransit, consignment.Status);
        Assert.Equal(dispatchAt, consignment.DispatchedAt);
        Assert.Equal(ShipmentStatus.InTransit, consignment.Shipment.Status);
        Assert.Equal(dispatchAt, consignment.Shipment.DepartedAt);
        LegCustody custody = Assert.Single(consignment.Custody);
        Assert.Equal(0, custody.LegIndex);
        Assert.Same(consignment.Shipment.Legs[0], custody.Leg);
        Assert.Equal(carrier, custody.Custodian);
        Assert.Equal(dispatchAt, custody.AcceptedAt);
        Assert.True(custody.IsOpen);
        Assert.Same(consignment.Shipment.Legs[0], consignment.CurrentLeg);
    }

    [Fact]
    public void A_consignment_is_dispatched_once() {
        // Arrange
        DateTimeOffset       dispatchAt  = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), dispatchAt);
        consignment.Dispatch(Journeys.AnyCustodian(), dispatchAt);

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.Dispatch(Journeys.AnyCustodian(), dispatchAt.AddMinutes(5)));

        // Assert
        Assert.Equal(ColdChainRefusal.NotPlanned, refusal.Refusal);
        Assert.Single(consignment.Custody);
    }

    [Fact]
    public void A_refusal_is_an_invalid_operation_carrying_its_reason() {
        // Arrange
        DateTimeOffset       dispatchAt  = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), dispatchAt);
        consignment.Dispatch(Journeys.AnyCustodian(), dispatchAt);

        // Act
        InvalidOperationException refusal = Assert.ThrowsAny<InvalidOperationException>(() => consignment.Dispatch(Journeys.AnyCustodian(), dispatchAt));

        // Assert
        Assert.IsType<ColdChainRefusedException>(refusal);
        Assert.Contains(consignment.Id.Value, refusal.Message);
    }

    [Fact]
    public void A_shipment_that_is_no_longer_booked_cannot_be_dispatched() {
        // Arrange
        DateTimeOffset       dispatchAt  = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), dispatchAt);
        consignment.Shipment.Cancel();

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.Dispatch(Journeys.AnyCustodian(), dispatchAt));

        // Assert
        Assert.Equal(ColdChainRefusal.ShipmentNotBooked, refusal.Refusal);
        Assert.Equal(ConsignmentStatus.Planned, consignment.Status);
    }

    [Fact]
    public void A_shipment_departed_by_someone_else_cannot_be_dispatched() {
        // Arrange
        DateTimeOffset       dispatchAt  = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), dispatchAt);
        consignment.Shipment.Depart(dispatchAt);

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.Dispatch(Journeys.AnyCustodian(), dispatchAt));

        // Assert
        Assert.Equal(ColdChainRefusal.ShipmentNotBooked, refusal.Refusal);
    }

    [Fact]
    public void A_logger_is_calibrated_on_the_day_of_dispatch_up_to_the_day_its_calibration_falls_due() {
        // Arrange: pallet calibrated 30 days before the dispatch day, due 200 days after it
        DateOnly             day         = DateOnly.FromDateTime(Journeys.AnyDispatchTime().UtcDateTime);
        ColdChainConsignment onFirstDay  = Journeys.Planned(Journeys.Product(), Journeys.AtUtc(day, 6, 0));
        DateTimeOffset       beforeDone  = Journeys.AtUtc(day.AddDays(-31), 23, 59);
        DateTimeOffset       onDone      = Journeys.AtUtc(day.AddDays(-30), 0, 0);
        DateTimeOffset       onDue       = Journeys.AtUtc(day.AddDays(200), 23, 59);
        DateTimeOffset       afterDue    = Journeys.AtUtc(day.AddDays(201), 0, 0);

        // Act
        ColdChainRefusedException tooEarly = Assert.Throws<ColdChainRefusedException>(() => onFirstDay.Dispatch(Journeys.AnyCustodian(), beforeDone));
        ColdChainRefusedException tooLate  = Assert.Throws<ColdChainRefusedException>(() => onFirstDay.Dispatch(Journeys.AnyCustodian(), afterDue));
        onFirstDay.Dispatch(Journeys.AnyCustodian(), onDone);

        ColdChainConsignment lastDay = Journeys.Planned(Journeys.Product(), Journeys.AtUtc(day, 6, 0));
        lastDay.Dispatch(Journeys.AnyCustodian(), onDue);

        // Assert
        Assert.Equal(ColdChainRefusal.LoggerCalibrationExpired, tooEarly.Refusal);
        Assert.Equal(ColdChainRefusal.LoggerCalibrationExpired, tooLate.Refusal);
        Assert.Equal(ConsignmentStatus.InTransit, onFirstDay.Status);
        Assert.Equal(ConsignmentStatus.InTransit, lastDay.Status);
    }

    [Fact]
    public void The_day_of_dispatch_is_the_utc_day_whatever_the_offset_of_the_instant() {
        // Arrange: the calibration falls due on the dispatch day
        DateOnly       day       = new DateOnly(2031, 3, 14);
        DateTimeOffset planned   = Journeys.AtUtc(day, 6, 0);
        // 01:00 on the 15th at +05:00 is 20:00 UTC on the 14th: still the due day
        DateTimeOffset eastOfUtc = new DateTimeOffset(2031, 3, 15, 1, 0, 0, TimeSpan.FromHours(5));
        // 23:00 on the 14th at -02:00 is 01:00 UTC on the 15th: past it
        DateTimeOffset westOfUtc = new DateTimeOffset(2031, 3, 14, 23, 0, 0, TimeSpan.FromHours(-2));
        ColdChainConsignment accepted = Journeys.Planned(Journeys.Product(), planned, calibrationDueInDays: 0);
        ColdChainConsignment refused  = Journeys.Planned(Journeys.Product(), planned, calibrationDueInDays: 0);

        // Act
        accepted.Dispatch(Journeys.AnyCustodian(), eastOfUtc);
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => refused.Dispatch(Journeys.AnyCustodian(), westOfUtc));

        // Assert
        Assert.Equal(ColdChainRefusal.LoggerCalibrationExpired, refusal.Refusal);
        Assert.Equal(ConsignmentStatus.InTransit, accepted.Status);
    }

    [Fact]
    public void One_logger_out_of_calibration_stops_the_whole_consignment() {
        // Arrange
        VaccineProduct product  = Journeys.Product();
        DateTimeOffset dispatch = Journeys.AnyDispatchTime();
        SensorId[]     loggers  = Journeys.AnyDistinctLoggerIds(2);
        List<Sscc>     ssccs    = Any.ListOf(new AnySscc()).WithCount(2).Distinct().Generate();
        ColdChainPallet fine    = Journeys.AnyPallet(product, dispatch, ssccs[0], loggers[0]);
        ColdChainPallet lapsed  = Journeys.AnyPallet(product, dispatch, ssccs[1], loggers[1], calibrationDueInDays: -1);
        ColdChainConsignment consignment = new AnyColdChainConsignment().WithProduct(product).WithPallets([fine, lapsed]).Generate();

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.Dispatch(Journeys.AnyCustodian(), dispatch));

        // Assert
        Assert.Equal(ColdChainRefusal.LoggerCalibrationExpired, refusal.Refusal);
        Assert.Contains(lapsed.Sscc.Value, refusal.Message);
        Assert.Equal(ConsignmentStatus.Planned, consignment.Status);
        Assert.Equal(ShipmentStatus.Booked, consignment.Shipment.Status);
    }

    [Fact]
    public void Packs_must_be_in_date_on_the_day_of_dispatch() {
        // Arrange: the earliest pack expires the day before the dispatch
        DateTimeOffset       dispatchAt  = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), dispatchAt, expiresInDays: -1);

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.Dispatch(Journeys.AnyCustodian(), dispatchAt));

        // Assert
        Assert.Equal(ColdChainRefusal.PackExpiredBeforeDispatch, refusal.Refusal);
        Assert.Equal(ConsignmentStatus.Planned, consignment.Status);
    }

    [Fact]
    public void Packs_expiring_on_the_day_of_dispatch_are_still_in_date() {
        // Arrange
        DateTimeOffset       dispatchAt  = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), dispatchAt, expiresInDays: 0);

        // Act
        consignment.Dispatch(Journeys.AnyCustodian(), dispatchAt.AddHours(10));

        // Assert
        Assert.Equal(ConsignmentStatus.InTransit, consignment.Status);
    }

    [Fact]
    public void One_expired_pack_among_good_ones_stops_the_dispatch() {
        // Arrange
        VaccineProduct product  = Journeys.Product();
        DateTimeOffset dispatch = Journeys.AnyDispatchTime();
        DateOnly       day      = Journeys.DayOf(dispatch);
        List<SerialisedPack> packs = [
            new AnySerialisedPack().WithGtin(product.Gtin).WithExpiresOn(day.AddDays(300)).Generate(),
            new AnySerialisedPack().WithGtin(product.Gtin).WithExpiresOn(day.AddDays(-1)).Generate(),
            new AnySerialisedPack().WithGtin(product.Gtin).WithExpiresOn(day.AddDays(500)).Generate()
        ];
        ColdChainPallet pallet = new AnyColdChainPallet().WithGtin(product.Gtin)
                                                         .WithLogger(new AnyDataLogger().WithCalibratedOn(day.AddDays(-5)).WithCalibrationDueOn(day.AddDays(300)).WithRecordingIntervalMinutes(Journeys.Interval).Generate())
                                                         .WithPacks(packs)
                                                         .Generate();
        ColdChainConsignment consignment = new AnyColdChainConsignment().WithProduct(product).WithPallets([pallet]).Generate();

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.Dispatch(Journeys.AnyCustodian(), dispatch));

        // Assert
        Assert.Equal(ColdChainRefusal.PackExpiredBeforeDispatch, refusal.Refusal);
    }

    [Fact]
    public void A_dispatch_needs_a_carrier() {
        // Arrange
        DateTimeOffset       dispatchAt  = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), dispatchAt);

        // Assert
        Assert.Throws<ArgumentNullException>(() => consignment.Dispatch(null!, dispatchAt));
    }

}

public sealed class RecordReadingTests {

    private static (ColdChainConsignment Consignment, DateTimeOffset DispatchedAt) InTransit(int palletCount = 1, int legCount = 1, VaccineProduct? product = null) {
        DateTimeOffset       dispatchAt  = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment = Journeys.Planned(product ?? Journeys.Product(), dispatchAt, palletCount, legCount);
        consignment.Dispatch(Journeys.AnyCustodian(), dispatchAt);

        return (consignment, dispatchAt);
    }

    [Fact]
    public void A_reading_is_recorded_in_the_history_of_its_pallet() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt) = InTransit(palletCount: 2);
        ColdChainPallet pallet = consignment.Pallets[1];
        LoggerReading   reading = Journeys.Reading(pallet, dispatchedAt.AddMinutes(30), 4.5m);

        // Act
        consignment.RecordReading(pallet.Sscc, reading);

        // Assert
        Assert.Same(reading, Assert.Single(consignment.HistoryOf(pallet.Sscc).Readings));
        Assert.Empty(consignment.HistoryOf(consignment.Pallets[0].Sscc).Readings);
    }

    [Fact]
    public void A_reading_cannot_be_recorded_before_the_dispatch_of_the_consignment() {
        // Arrange
        ColdChainConsignment consignment = Journeys.Planned(Journeys.Product(), Journeys.AnyDispatchTime());
        ColdChainPallet      pallet      = consignment.Pallets[0];

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.RecordReading(pallet.Sscc, Journeys.Reading(pallet, Journeys.AnyDispatchTime(), 5m)));

        // Assert
        Assert.Equal(ColdChainRefusal.NotDispatched, refusal.Refusal);
    }

    [Fact]
    public void A_reading_of_a_pallet_outside_the_consignment_is_refused() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt) = InTransit();
        ColdChainPallet foreign = Journeys.AnyPallet(consignment.Product, dispatchedAt);

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.RecordReading(foreign.Sscc, Journeys.Reading(foreign, dispatchedAt.AddMinutes(5), 5m)));

        // Assert
        Assert.Equal(ColdChainRefusal.UnknownPallet, refusal.Refusal);
    }

    [Fact]
    public void A_reading_must_come_from_the_logger_of_the_pallet() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt) = InTransit(palletCount: 2);
        ColdChainPallet pallet = consignment.Pallets[0];
        ColdChainPallet other  = consignment.Pallets[1];

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.RecordReading(pallet.Sscc, Journeys.Reading(other, dispatchedAt.AddMinutes(5), 5m)));

        // Assert
        Assert.Equal(ColdChainRefusal.LoggerMismatch, refusal.Refusal);
        Assert.Empty(consignment.HistoryOf(pallet.Sscc).Readings);
    }

    [Fact]
    public void A_reading_taken_before_the_dispatch_is_outside_the_transit() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt) = InTransit();
        ColdChainPallet pallet = consignment.Pallets[0];

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => consignment.RecordReading(pallet.Sscc, Journeys.Reading(pallet, dispatchedAt.AddTicks(-1), 5m)));
        Journeys.Record(consignment, pallet, dispatchedAt, 5m);

        // Assert
        Assert.Equal(ColdChainRefusal.ReadingOutsideTransit, refusal.Refusal);
        Assert.Single(consignment.HistoryOf(pallet.Sscc).Readings);
    }

    [Fact]
    public void A_reading_must_come_after_the_previous_one() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt) = InTransit();
        ColdChainPallet pallet = consignment.Pallets[0];
        Journeys.Record(consignment, pallet, dispatchedAt.AddMinutes(20), 5m);

        // Act
        ColdChainRefusedException same    = Assert.Throws<ColdChainRefusedException>(() => Journeys.Record(consignment, pallet, dispatchedAt.AddMinutes(20), 5m));
        ColdChainRefusedException earlier = Assert.Throws<ColdChainRefusedException>(() => Journeys.Record(consignment, pallet, dispatchedAt.AddMinutes(15), 5m));

        // Assert
        Assert.Equal(ColdChainRefusal.ReadingOutOfOrder, same.Refusal);
        Assert.Equal(ColdChainRefusal.ReadingOutOfOrder, earlier.Refusal);
        Assert.Single(consignment.HistoryOf(pallet.Sscc).Readings);
    }

    [Fact]
    public void Each_pallet_has_its_own_order_of_readings() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt) = InTransit(palletCount: 2);

        // Act
        Journeys.Record(consignment, consignment.Pallets[0], dispatchedAt.AddMinutes(20), 5m);
        Journeys.Record(consignment, consignment.Pallets[1], dispatchedAt.AddMinutes(10), 5m);

        // Assert
        Assert.Single(consignment.HistoryOf(consignment.Pallets[1].Sscc).Readings);
    }

    [Fact]
    public void A_reading_taken_after_the_delivery_is_outside_the_transit_but_one_taken_before_may_be_recorded_late() {
        // Arrange
        DateTimeOffset       dispatchedAt = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment  = Journeys.Planned(Journeys.Product(), dispatchedAt);
        ColdChainPallet      pallet       = consignment.Pallets[0];
        consignment.Dispatch(Journeys.AnyCustodian(), dispatchedAt);
        Journeys.Record(consignment, pallet, dispatchedAt, 5m);
        consignment.Deliver(Journeys.WarehouseFor(consignment), dispatchedAt.AddMinutes(30));

        // Act
        Journeys.Record(consignment, pallet, dispatchedAt.AddMinutes(30), 5m);
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => Journeys.Record(consignment, pallet, dispatchedAt.AddMinutes(31), 5m));

        // Assert
        Assert.Equal(ColdChainRefusal.ReadingOutsideTransit, refusal.Refusal);
        Assert.Equal(2, consignment.HistoryOf(pallet.Sscc).Readings.Count);
    }

    [Fact]
    public void A_reading_taken_in_the_gap_before_delivery_can_still_be_recorded_after_it() {
        // Arrange
        DateTimeOffset       dispatchedAt = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment  = Journeys.Planned(Journeys.Product(), dispatchedAt);
        ColdChainPallet      pallet       = consignment.Pallets[0];
        consignment.Dispatch(Journeys.AnyCustodian(), dispatchedAt);
        consignment.Deliver(Journeys.WarehouseFor(consignment), dispatchedAt.AddMinutes(60));

        // Act
        Journeys.Record(consignment, pallet, dispatchedAt.AddMinutes(40), 5m);

        // Assert
        Assert.Single(consignment.HistoryOf(pallet.Sscc).Readings);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(-12.0)]
    [InlineData(25.1)]
    [InlineData(40.0)]
    public void A_reading_beyond_a_hard_limit_writes_the_pallet_off_at_once(double celsius) {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt) = InTransit(palletCount: 2);
        ColdChainPallet pallet = consignment.Pallets[0];

        // Act
        Journeys.Record(consignment, pallet, dispatchedAt.AddMinutes(10), (decimal)celsius);

        // Assert
        ReleaseDecision decision = Assert.IsType<ReleaseDecision>(consignment.DecisionFor(pallet.Sscc));
        Assert.Equal(ReleaseOutcome.Rejected, decision.Outcome);
        Assert.Equal([DecisionReason.HardLimitBreached], decision.Reasons);
        Assert.Null(decision.QuarantineBay);
        Assert.True(consignment.IsWrittenOff(pallet.Sscc));
        Assert.Null(consignment.DecisionFor(consignment.Pallets[1].Sscc));
        Assert.False(consignment.IsWrittenOff(consignment.Pallets[1].Sscc));
        Assert.Equal(ConsignmentStatus.InTransit, consignment.Status);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(25.0)]
    [InlineData(0.1)]
    [InlineData(24.9)]
    public void A_reading_exactly_on_a_hard_limit_does_not_write_the_pallet_off(double celsius) {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt) = InTransit();
        ColdChainPallet pallet = consignment.Pallets[0];

        // Act
        Journeys.Record(consignment, pallet, dispatchedAt.AddMinutes(10), (decimal)celsius);

        // Assert
        Assert.Null(consignment.DecisionFor(pallet.Sscc));
        Assert.False(consignment.IsWrittenOff(pallet.Sscc));
    }

    [Fact]
    public void The_budget_already_consumed_is_carried_by_the_write_off() {
        // Arrange: warm from minute 5 to minute 15, then a reading below zero
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt) = InTransit();
        ColdChainPallet pallet = consignment.Pallets[0];
        Journeys.RecordSeries(consignment, pallet, dispatchedAt, 5m, 9m, 9m, -1m);

        // Act
        ReleaseDecision decision = Assert.IsType<ReleaseDecision>(consignment.DecisionFor(pallet.Sscc));

        // Assert
        Assert.Equal(10, decision.BudgetConsumedMinutes);
    }

    [Fact]
    public void A_pallet_written_off_takes_no_further_reading() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt) = InTransit();
        ColdChainPallet pallet = consignment.Pallets[0];
        Journeys.Record(consignment, pallet, dispatchedAt.AddMinutes(10), -5m);

        // Act
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(() => Journeys.Record(consignment, pallet, dispatchedAt.AddMinutes(15), 5m));

        // Assert
        Assert.Equal(ColdChainRefusal.PalletAlreadyDecided, refusal.Refusal);
    }

    [Fact]
    public void The_other_pallets_keep_recording_after_one_is_written_off() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt) = InTransit(palletCount: 2);
        Journeys.Record(consignment, consignment.Pallets[0], dispatchedAt.AddMinutes(10), -5m);

        // Act
        Journeys.Record(consignment, consignment.Pallets[1], dispatchedAt.AddMinutes(15), 5m);

        // Assert
        Assert.Single(consignment.HistoryOf(consignment.Pallets[1].Sscc).Readings);
    }

    [Fact]
    public void When_every_pallet_is_written_off_before_delivery_the_delivery_closes_the_consignment() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt) = InTransit(palletCount: 2);
        Journeys.Record(consignment, consignment.Pallets[0], dispatchedAt.AddMinutes(10), 30m);
        Journeys.Record(consignment, consignment.Pallets[1], dispatchedAt.AddMinutes(10), -2m);

        // Act
        consignment.Deliver(Journeys.WarehouseFor(consignment), dispatchedAt.AddMinutes(60));

        // Assert
        Assert.Equal(ConsignmentStatus.Closed, consignment.Status);
        Assert.Equal(2, consignment.Decisions.Count);
    }

    [Fact]
    public void A_late_recorded_reading_beyond_a_hard_limit_still_writes_the_pallet_off() {
        // Arrange
        DateTimeOffset       dispatchedAt = Journeys.AnyDispatchTime();
        ColdChainConsignment consignment  = Journeys.Planned(Journeys.Product(), dispatchedAt);
        ColdChainPallet      pallet       = consignment.Pallets[0];
        consignment.Dispatch(Journeys.AnyCustodian(), dispatchedAt);
        consignment.Deliver(Journeys.WarehouseFor(consignment), dispatchedAt.AddMinutes(60));

        // Act
        Journeys.Record(consignment, pallet, dispatchedAt.AddMinutes(20), 26m);

        // Assert
        Assert.True(consignment.IsWrittenOff(pallet.Sscc));
        Assert.Equal(ConsignmentStatus.Closed, consignment.Status);
    }

    [Fact]
    public void A_reading_needs_a_pallet_and_a_reading() {
        // Arrange
        (ColdChainConsignment consignment, DateTimeOffset dispatchedAt) = InTransit();
        ColdChainPallet pallet = consignment.Pallets[0];

        // Assert
        Assert.Throws<ArgumentNullException>(() => consignment.RecordReading(null!, Journeys.Reading(pallet, dispatchedAt, 5m)));
        Assert.Throws<ArgumentNullException>(() => consignment.RecordReading(pallet.Sscc, null!));
    }

}
