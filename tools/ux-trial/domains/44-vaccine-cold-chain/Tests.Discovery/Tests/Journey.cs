using AgriTech.Domain;

using ColdChain.Domain;

using Freight.Domain;

using JustDummies;

using Serialisation.Domain;

using Warehouse.Domain;

using FreightShipment = Freight.Domain.Shipment;

namespace ColdChain.Tests;

/// <summary>What a test of the consignment chooses about its scenario; everything else is drawn.</summary>
internal sealed record JourneyOptions {

    public int      Pallets           { get; init; } = 1;
    public int      Legs              { get; init; } = 1;
    public int      CumulativeMinutes { get; init; } = 120;
    public int      SingleMinutes     { get; init; } = 60;
    public int      ShelfLifeDays     { get; init; } = 0;
    public int      IntervalMinutes   { get; init; } = 10;
    public int      DurationMinutes   { get; init; } = 360;
    public DateOnly CalibratedOn      { get; init; } = new DateOnly(2026, 1, 15);
    public DateOnly CalibrationDueOn  { get; init; } = new DateOnly(2026, 12, 31);
    public DateOnly? PackExpiresOn    { get; init; } = null;
    public decimal  LabelMin          { get; init; } = 2m;
    public decimal  LabelMax          { get; init; } = 8m;

}

/// <summary>A consignment ready to dispatch, with the parts a test needs to drive it.</summary>
internal sealed class Journey {

    public static readonly DateTimeOffset DispatchedAt = new DateTimeOffset(2026, 3, 2, 6, 0, 0, TimeSpan.Zero);

    private Journey(JourneyOptions options, ColdChainConsignment consignment, ReceivingWarehouse warehouse, CustodianId carrier) {
        Options     = options;
        Consignment = consignment;
        Warehouse   = warehouse;
        Carrier     = carrier;
    }

    public JourneyOptions Options { get; }

    public ColdChainConsignment Consignment { get; }

    public ReceivingWarehouse Warehouse { get; }

    public CustodianId Carrier { get; }

    public DateTimeOffset DeliveredAt => DispatchedAt.AddMinutes(Options.DurationMinutes);

    public Sscc FirstPallet => Consignment.Pallets[0].Sscc;

    public static Journey Planned(JourneyOptions? options = null) {
        JourneyOptions chosen = options ?? new JourneyOptions();

        VaccineProduct product = new AnyVaccineProduct()
            .WithLabelledRange(new TemperatureRange(chosen.LabelMin, chosen.LabelMax))
            .WithBudget(new StabilityBudget(chosen.CumulativeMinutes, chosen.SingleMinutes))
            .WithMinimumShelfLifeOnReceiptDays(chosen.ShelfLifeDays)
            .Generate();

        FreightShipment shipment = BookedReefer(chosen.Legs);

        ColdChainConsignment consignment = new AnyColdChainConsignment()
            .WithProduct(product)
            .WithShipment(shipment)
            .WithPalletCount(chosen.Pallets)
            .WithPallets(pallet => pallet
                .WithLogger(logger => logger.WithRecordingIntervalMinutes(chosen.IntervalMinutes)
                                            .WithCalibratedOn(chosen.CalibratedOn)
                                            .WithCalibrationDueOn(chosen.CalibrationDueOn))
                .WithPacks(chosen.PackExpiresOn is { } expiresOn ? PacksExpiringOn(product.Gtin, expiresOn) : ColdChainPalletPacks(product.Gtin)))
            .Generate();

        ReceivingWarehouse warehouse = new AnyReceivingWarehouse()
            .WithLocation(shipment.Legs[^1].Destination)
            .WithColdRoom(new TemperatureRange(chosen.LabelMin, chosen.LabelMax))
            .Generate();

        return new Journey(chosen, consignment, warehouse, new AnyCustodianId().Generate());
    }

    public static FreightShipment BookedReefer(int legCount) {
        List<UnLocode> ports = Any.ListOf(new AnyUnLocode()).WithCount(legCount + 1).Distinct().Generate();
        List<Leg>      legs  = new List<Leg>();
        for (int index = 0; index < legCount; index++) {
            DateTimeOffset departure = DispatchedAt.AddDays(index);
            legs.Add(new AnyLeg().WithOrigin(ports[index]).WithDestination(ports[index + 1]).WithDeparture(departure).WithArrival(departure.AddHours(8)).Generate());
        }

        return new AnyShipment().WithContainerType(ContainerType.Reefer40).WithLegs(legs).Generate();
    }

    public static IReadOnlyList<SerialisedPack> PacksExpiringOn(Gtin gtin, DateOnly expiresOn) {
        return Any.ListOf(new AnySerialNumber()).WithCountBetween(1, 5).Distinct().Generate()
                  .Select(serial => new AnySerialisedPack().WithGtin(gtin).WithSerial(serial).WithExpiresOn(expiresOn).Generate())
                  .ToList();
    }

    private static IReadOnlyList<SerialisedPack> ColdChainPalletPacks(Gtin gtin) {
        return AnyColdChainPallet.PacksOf(gtin).Generate();
    }

    public Journey Dispatched() {
        Consignment.Dispatch(Carrier, DispatchedAt);

        return this;
    }

    public SensorId LoggerOf(Sscc pallet) {
        return Consignment.HistoryOf(pallet).Logger.Id;
    }

    public LoggerReading ReadingOf(Sscc pallet, int minuteAfterDispatch, decimal celsius) {
        return new AnyLoggerReading().WithLogger(LoggerOf(pallet)).WithTakenAt(DispatchedAt.AddMinutes(minuteAfterDispatch)).WithCelsius(celsius).Generate();
    }

    public void Record(Sscc pallet, int minuteAfterDispatch, decimal celsius) {
        Consignment.RecordReading(pallet, ReadingOf(pallet, minuteAfterDispatch, celsius));
    }

    /// <summary>
    ///     One reading per logger interval from dispatch to delivery, each at a temperature inside the label unless
    ///     <paramref name="overrides" /> sets that minute.
    /// </summary>
    public void RecordWholeTrip(Sscc pallet, IReadOnlyDictionary<int, decimal>? overrides = null) {
        for (int minute = 0; minute <= Options.DurationMinutes; minute += Options.IntervalMinutes) {
            decimal celsius = overrides is not null && overrides.TryGetValue(minute, out decimal chosen)
                                  ? chosen
                                  : Any.Decimal().Between(Options.LabelMin, Options.LabelMax).WithScale(1).Generate();
            Record(pallet, minute, celsius);
        }
    }

    public void RecordWholeTripOfEveryPallet() {
        foreach (ColdChainPallet pallet in Consignment.Pallets) {
            RecordWholeTrip(pallet.Sscc);
        }
    }

    /// <summary>Hands over every leg but the last to a fresh custodian at the given minutes, then delivers.</summary>
    public void DeliverAtTheEnd() {
        for (int leg = 1; leg < Consignment.Shipment.Legs.Count; leg++) {
            Consignment.HandOverToNextLeg(new AnyCustodianId().Generate(), DispatchedAt.AddMinutes(Options.DurationMinutes * leg / Consignment.Shipment.Legs.Count));
        }

        Consignment.Deliver(Warehouse, DeliveredAt);
    }

}
