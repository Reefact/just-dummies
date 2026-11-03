using AgriTech.Domain;

using ColdChain.Domain;

using Freight.Domain;

using JustDummies;

using Serialisation.Domain;

using Warehouse.Domain;

using FreightShipment = Freight.Domain.Shipment;

namespace ColdChain.Tests.Regular;

/// <summary>Builds consignments whose times, label and budget are the ones a test is about; everything else is drawn.</summary>
internal static class Journeys {

    /// <summary>Every logger of these journeys records every five minutes: it tolerates a silence of ten.</summary>
    public const int Interval = 5;

    public static readonly TemperatureRange Label = new TemperatureRange(2m, 8m);

    public static VaccineProduct Product(int cumulativeMinutes = 10_080, int maxSingleMinutes = 10_080, int shelfLifeDays = 0, TemperatureRange? label = null) {
        return new AnyVaccineProduct().WithLabelledRange(label ?? Label)
                                      .WithBudget(new StabilityBudget(cumulativeMinutes, maxSingleMinutes))
                                      .WithMinimumShelfLifeOnReceiptDays(shelfLifeDays)
                                      .Generate();
    }

    /// <summary>A day, and 06:00 UTC on it: the moment the consignment leaves.</summary>
    public static DateTimeOffset AnyDispatchTime() {
        DateOnly anyDay = Any.DateOnly().Between(new DateOnly(2030, 1, 1), new DateOnly(2034, 12, 31)).Generate();

        return AtUtc(anyDay, 6, 0);
    }

    public static DateTimeOffset AtUtc(DateOnly day, int hour, int minute) {
        return new DateTimeOffset(day.Year, day.Month, day.Day, hour, minute, 0, TimeSpan.Zero);
    }

    public static DateOnly DayOf(DateTimeOffset instant) {
        return DateOnly.FromDateTime(instant.UtcDateTime);
    }

    public static SensorId[] AnyDistinctLoggerIds(int count) {
        return Any.ListOf(new AnySensorId()).WithCount(count).Distinct().Generate().ToArray();
    }

    public static ColdChainPallet AnyPallet(VaccineProduct product,
                                            DateTimeOffset dispatchAt,
                                            Sscc?          sscc                   = null,
                                            SensorId?      loggerId               = null,
                                            int            expiresInDays          = 400,
                                            int            calibrationDueInDays   = 200,
                                            int            packCount              = 3,
                                            int            loggerIntervalMinutes  = Interval) {
        DateOnly dispatchDay = DayOf(dispatchAt);
        DataLogger logger = new AnyDataLogger().WithId(loggerId ?? new AnySensorId().Generate())
                                               .WithRecordingIntervalMinutes(loggerIntervalMinutes)
                                               .WithCalibratedOn(dispatchDay.AddDays(-30))
                                               .WithCalibrationDueOn(dispatchDay.AddDays(calibrationDueInDays))
                                               .Generate();
        List<SerialisedPack> packs = Any.ListOf(new AnySerialisedPack().WithExpiresOn(dispatchDay.AddDays(expiresInDays)))
                                        .WithCount(packCount)
                                        .DistinctBy(pack => pack.Serial.Value)
                                        .Generate();

        return new AnyColdChainPallet().WithSscc(sscc ?? new AnySscc().Generate())
                                       .WithGtin(product.Gtin)
                                       .WithLogger(logger)
                                       .WithPacks(packs)
                                       .Generate();
    }

    public static ColdChainConsignment Planned(VaccineProduct product,
                                               DateTimeOffset dispatchAt,
                                               int            palletCount          = 1,
                                               int            legCount             = 1,
                                               int            expiresInDays        = 400,
                                               int            calibrationDueInDays = 200,
                                               int            packCount            = 3) {
        SensorId[]   loggerIds = AnyDistinctLoggerIds(palletCount);
        List<Sscc>   ssccs     = Any.ListOf(new AnySscc()).WithCount(palletCount).Distinct().Generate();

        List<ColdChainPallet> pallets = new List<ColdChainPallet>();
        for (int index = 0; index < palletCount; index++) {
            pallets.Add(AnyPallet(product, dispatchAt, ssccs[index], loggerIds[index], expiresInDays, calibrationDueInDays, packCount));
        }

        return new AnyColdChainConsignment().WithProduct(product)
                                            .WithShipment(AnyReeferWithLegs(legCount))
                                            .WithPallets(pallets)
                                            .Generate();
    }

    public static FreightShipment AnyReeferWithLegs(int legCount) {
        return new AnyShipment().WithContainerType(ContainerType.Reefer40).WithLegCount(legCount).Generate();
    }

    public static ReceivingWarehouse WarehouseAt(UnLocode location, TemperatureRange? coldRoom = null) {
        return new AnyReceivingWarehouse().WithLocation(location).WithColdRoom(coldRoom ?? Label).Generate();
    }

    public static ReceivingWarehouse WarehouseFor(ColdChainConsignment consignment, TemperatureRange? coldRoom = null) {
        return WarehouseAt(consignment.Shipment.Legs[^1].Destination, coldRoom);
    }

    public static CustodianId AnyCustodian() {
        return new AnyCustodianId().Generate();
    }

    public static LoggerReading Reading(ColdChainPallet pallet, DateTimeOffset at, decimal celsius) {
        return new AnyLoggerReading().WithLogger(pallet.Logger.Id).WithTakenAt(at).WithCelsius(celsius).Generate();
    }

    public static void Record(ColdChainConsignment consignment, ColdChainPallet pallet, DateTimeOffset at, decimal celsius) {
        consignment.RecordReading(pallet.Sscc, Reading(pallet, at, celsius));
    }

    /// <summary>One reading per logger interval, from the dispatch: <c>temperatures[i]</c> is taken <c>i * Interval</c> minutes after it.</summary>
    public static void RecordSeries(ColdChainConsignment consignment, ColdChainPallet pallet, DateTimeOffset dispatchAt, params decimal[] temperatures) {
        for (int index = 0; index < temperatures.Length; index++) {
            Record(consignment, pallet, dispatchAt.AddMinutes(index * Interval), temperatures[index]);
        }
    }

    /// <summary>
    ///     A consignment dispatched at <paramref name="dispatchAt" />, whose every pallet records <paramref name="temperatures" />
    ///     (one reading per interval) and is delivered with the last reading, to a warehouse as cold as the label.
    /// </summary>
    public static ColdChainConsignment Delivered(VaccineProduct product,
                                                 DateTimeOffset dispatchAt,
                                                 decimal[]      temperatures,
                                                 int            palletCount          = 1,
                                                 int            expiresInDays        = 400,
                                                 int            calibrationDueInDays = 200) {
        ColdChainConsignment consignment = Planned(product, dispatchAt, palletCount, 1, expiresInDays, calibrationDueInDays);
        consignment.Dispatch(AnyCustodian(), dispatchAt);
        foreach (ColdChainPallet pallet in consignment.Pallets) {
            RecordSeries(consignment, pallet, dispatchAt, temperatures);
        }

        consignment.Deliver(WarehouseFor(consignment), dispatchAt.AddMinutes((temperatures.Length - 1) * Interval));

        return consignment;
    }

    public static decimal[] Steady(int readings, decimal celsius = 5m) {
        return Enumerable.Repeat(celsius, readings).ToArray();
    }

}
