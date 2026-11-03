using AgriTech.Domain;

using ColdChain.Domain;

using Freight.Domain;

using JustDummies;

using Serialisation.Domain;

using Warehouse.Domain;

using Xunit;

namespace ColdChain.Tests.Regular.Tests;

public sealed class ColdChainPalletTests {

    private static List<SerialisedPack> AnyPacks(Gtin gtin, int count) {
        return Any.ListOf(new AnySerialisedPack().WithGtin(gtin)).WithCount(count).DistinctBy(pack => pack.Serial.Value).Generate();
    }

    [Fact]
    public void A_pallet_carries_its_packs_under_its_sscc_and_its_logger() {
        // Arrange
        Gtin           gtin   = new AnyGtin().Generate();
        Sscc           sscc   = new AnySscc().Generate();
        DataLogger     logger = new AnyDataLogger().Generate();
        List<SerialisedPack> packs = AnyPacks(gtin, 4);

        // Act
        ColdChainPallet pallet = new ColdChainPallet(sscc, logger, packs);

        // Assert
        Assert.Equal(sscc, pallet.Sscc);
        Assert.Same(logger, pallet.Logger);
        Assert.Equal(packs, pallet.Packs);
        Assert.Equal(4, pallet.PackCount);
        Assert.Equal(gtin, pallet.Gtin);
        Assert.Equal($"{sscc} (4 packs)", pallet.ToString());
    }

    [Fact]
    public void A_pallet_carries_one_pack() {
        // Arrange
        List<SerialisedPack> packs = AnyPacks(new AnyGtin().Generate(), 1);

        // Act
        ColdChainPallet pallet = new AnyColdChainPallet().WithPacks(packs).Generate();

        // Assert
        Assert.Equal(1, pallet.PackCount);
    }

    [Fact]
    public void A_pallet_carries_up_to_five_thousand_packs() {
        // Arrange
        List<SerialisedPack> packs = AnyPacks(new AnyGtin().Generate(), ColdChainPallet.MaxPacks);

        // Act
        ColdChainPallet pallet = new AnyColdChainPallet().WithPacks(packs).Generate();

        // Assert
        Assert.Equal(5_000, pallet.PackCount);
    }

    [Fact]
    public void A_pallet_of_more_than_five_thousand_packs_is_refused() {
        // Arrange
        List<SerialisedPack> packs = AnyPacks(new AnyGtin().Generate(), ColdChainPallet.MaxPacks + 1);

        // Act
        Action act = () => new AnyColdChainPallet().WithPacks(packs).Generate();

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(act);
        Assert.Equal("packs", refusal.ParamName);
    }

    [Fact]
    public void A_pallet_without_a_pack_is_refused() {
        // Act
        Action act = () => new ColdChainPallet(new AnySscc().Generate(), new AnyDataLogger().Generate(), []);

        // Assert
        Assert.Equal("packs", Assert.Throws<ArgumentException>(act).ParamName);
    }

    [Fact]
    public void A_pallet_cannot_list_a_missing_pack() {
        // Arrange
        List<SerialisedPack> packs = AnyPacks(new AnyGtin().Generate(), 2);
        packs.Add(null!);

        // Act
        Action act = () => new ColdChainPallet(new AnySscc().Generate(), new AnyDataLogger().Generate(), packs);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void A_serial_number_cannot_appear_twice_on_a_pallet() {
        // Arrange
        Gtin gtin = new AnyGtin().Generate();
        SerialNumber serial = new AnySerialNumber().Generate();
        SerialisedPack first  = new AnySerialisedPack().WithGtin(gtin).WithSerial(serial).Generate();
        SerialisedPack second = new AnySerialisedPack().WithGtin(gtin).WithSerial(serial).Generate();

        // Act
        Action act = () => new ColdChainPallet(new AnySscc().Generate(), new AnyDataLogger().Generate(), [first, second]);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Only_active_packs_are_shipped() {
        // Arrange
        Gtin  gtin  = new AnyGtin().Generate();
        Batch batch = new Batch(gtin, new AnyBatchNumber().Generate(), new DateOnly(2030, 1, 1), new DateOnly(2032, 1, 1), 10, new ReimbursementPrice(12.50m));
        SerialisedPack recalled = batch.Commission(new AnySerialNumber().Generate());
        batch.Recall();
        SerialisedPack active = new AnySerialisedPack().WithGtin(gtin).Generate();

        // Act
        Action act = () => new ColdChainPallet(new AnySscc().Generate(), new AnyDataLogger().Generate(), [active, recalled]);

        // Assert
        Assert.Equal(PackStatus.Recalled, recalled.Status);
        Assert.Equal("packs", Assert.Throws<ArgumentException>(act).ParamName);
    }

    [Fact]
    public void A_pallet_carries_a_single_product() {
        // Arrange
        List<Gtin> gtins = Any.ListOf(new AnyGtin()).WithCount(2).Distinct().Generate();
        SerialisedPack ofTheFirst  = new AnySerialisedPack().WithGtin(gtins[0]).Generate();
        SerialisedPack ofTheSecond = new AnySerialisedPack().WithGtin(gtins[1]).Generate();

        // Act
        Action act = () => new ColdChainPallet(new AnySscc().Generate(), new AnyDataLogger().Generate(), [ofTheFirst, ofTheSecond]);

        // Assert
        Assert.Equal("packs", Assert.Throws<ArgumentException>(act).ParamName);
    }

    [Fact]
    public void Several_batches_of_the_product_may_share_a_pallet() {
        // Arrange
        Gtin gtin = new AnyGtin().Generate();
        List<BatchNumber> batches = Any.ListOf(new AnyBatchNumber()).WithCount(3).Distinct().Generate();
        List<SerialisedPack> packs = batches.Select(batch => new AnySerialisedPack().WithGtin(gtin).WithBatch(batch).Generate()).ToList();

        // Act
        ColdChainPallet pallet = new AnyColdChainPallet().WithPacks(packs).Generate();

        // Assert
        Assert.Equal(3, pallet.Batches.Count);
        Assert.All(batches, batch => Assert.Contains(batch, pallet.Batches));
    }

    [Fact]
    public void A_batch_present_on_several_packs_is_listed_once() {
        // Arrange
        Gtin        gtin  = new AnyGtin().Generate();
        BatchNumber batch = new AnyBatchNumber().Generate();
        List<SerialisedPack> packs = Any.ListOf(new AnySerialisedPack().WithGtin(gtin).WithBatch(batch)).WithCount(4).DistinctBy(pack => pack.Serial.Value).Generate();

        // Act
        ColdChainPallet pallet = new AnyColdChainPallet().WithPacks(packs).Generate();

        // Assert
        Assert.Equal([batch], pallet.Batches);
    }

    [Fact]
    public void The_earliest_expiry_is_the_one_of_the_soonest_pack() {
        // Arrange
        Gtin gtin = new AnyGtin().Generate();
        DateOnly[] expiries = [new DateOnly(2031, 5, 31), new DateOnly(2030, 11, 30), new DateOnly(2032, 1, 31)];
        List<SerialisedPack> packs = expiries.Select(day => new AnySerialisedPack().WithGtin(gtin).WithExpiresOn(day).Generate()).ToList();

        // Act
        ColdChainPallet pallet = new AnyColdChainPallet().WithPacks(packs).Generate();

        // Assert
        Assert.Equal(new DateOnly(2030, 11, 30), pallet.EarliestExpiry);
    }

    [Fact]
    public void A_pallet_carries_the_serials_of_its_packs_and_no_others() {
        // Arrange
        Gtin gtin = new AnyGtin().Generate();
        List<SerialisedPack> packs = AnyPacks(gtin, 3);
        ColdChainPallet pallet = new AnyColdChainPallet().WithPacks(packs).Generate();
        SerialNumber foreign = new AnySerialNumber().Generate();

        // Assert
        Assert.All(packs, pack => Assert.True(pallet.Carries(pack.Serial)));
        Assert.False(pallet.Carries(foreign));
    }

}

public sealed class TemperatureHistoryTests {

    private static readonly TemperatureRange Label = new TemperatureRange(2m, 8m);

    private static readonly DateTimeOffset Start = new DateTimeOffset(2030, 6, 1, 6, 0, 0, TimeSpan.Zero);

    private static DataLogger FiveMinuteLogger() {
        return new AnyDataLogger().WithRecordingIntervalMinutes(5).Generate();
    }

    private static LoggerReading ReadingOf(DataLogger logger, int minutesAfterStart, decimal celsius) {
        return new AnyLoggerReading().WithLogger(logger.Id).WithTakenAt(Start.AddMinutes(minutesAfterStart)).WithCelsius(celsius).Generate();
    }

    /// <summary>A history fed one reading every five minutes from <see cref="Start" />.</summary>
    private static TemperatureHistory HistoryOf(params decimal[] temperatures) {
        DataLogger         logger  = FiveMinuteLogger();
        TemperatureHistory history = new TemperatureHistory(logger, Label);
        for (int index = 0; index < temperatures.Length; index++) {
            history.Record(ReadingOf(logger, index * 5, temperatures[index]));
        }

        return history;
    }

    [Fact]
    public void A_history_without_a_reading_shows_nothing() {
        // Arrange
        TemperatureHistory history = new TemperatureHistory(FiveMinuteLogger(), Label);

        // Assert
        Assert.Empty(history.Readings);
        Assert.Null(history.Last);
        Assert.Empty(history.Excursions);
        Assert.Equal(0, history.MinutesOutsideRange);
        Assert.Equal(0, history.LongestExcursionMinutes);
        Assert.False(history.HasHardLimitBreach);
        Assert.False(history.Covers(Start, Start.AddMinutes(30)));
    }

    [Fact]
    public void A_history_keeps_its_readings_in_the_order_they_came() {
        // Arrange
        DataLogger     logger = FiveMinuteLogger();
        LoggerReading  first  = ReadingOf(logger, 0, 5m);
        LoggerReading  second = ReadingOf(logger, 5, 5.5m);
        TemperatureHistory history = new TemperatureHistory(logger, Label);

        // Act
        history.Record(first);
        history.Record(second);

        // Assert
        Assert.Equal([first, second], history.Readings);
        Assert.Same(second, history.Last);
        Assert.Same(logger, history.Logger);
        Assert.Same(Label, history.LabelledRange);
    }

    [Fact]
    public void A_reading_from_another_logger_is_refused() {
        // Arrange
        List<SensorId>     sensors = Any.ListOf(new AnySensorId()).WithCount(2).Distinct().Generate();
        DataLogger         logger  = new AnyDataLogger().WithId(sensors[0]).Generate();
        TemperatureHistory history = new TemperatureHistory(logger, Label);
        LoggerReading      foreign = new AnyLoggerReading().WithLogger(sensors[1]).WithTakenAt(Start).Generate();

        // Act
        Action act = () => history.Record(foreign);

        // Assert
        Assert.Equal("reading", Assert.Throws<ArgumentException>(act).ParamName);
    }

    [Fact]
    public void A_reading_must_come_strictly_after_the_last_one() {
        // Arrange
        DataLogger         logger  = FiveMinuteLogger();
        TemperatureHistory history = new TemperatureHistory(logger, Label);
        history.Record(ReadingOf(logger, 10, 5m));

        // Act
        Action sameInstant = () => history.Record(ReadingOf(logger, 10, 5m));
        Action earlier     = () => history.Record(ReadingOf(logger, 9, 5m));
        history.Record(ReadingOf(logger, 11, 5m));

        // Assert
        Assert.Throws<InvalidOperationException>(sameInstant);
        Assert.Throws<InvalidOperationException>(earlier);
        Assert.Equal(2, history.Readings.Count);
    }

    [Fact]
    public void Readings_on_the_ends_of_the_label_are_within_it() {
        // Arrange
        TemperatureHistory history = HistoryOf(2m, 8m, 5m, 2m, 8m);

        // Assert
        Assert.Empty(history.Excursions);
        Assert.Equal(0, history.MinutesOutsideRange);
    }

    [Fact]
    public void A_run_of_warm_readings_is_one_excursion_up_to_the_first_reading_back_inside() {
        // Arrange: 5.0, 5.0, 9.0, 9.5, 9.0, 5.0 -> outside from minute 10 to minute 25
        TemperatureHistory history = HistoryOf(5m, 5m, 9m, 9.5m, 9m, 5m);

        // Act
        Excursion excursion = Assert.Single(history.Excursions);

        // Assert
        Assert.Equal(ExcursionKind.Warm, excursion.Kind);
        Assert.Equal(Start.AddMinutes(10), excursion.StartedAt);
        Assert.Equal(Start.AddMinutes(25), excursion.EndedAt);
        Assert.Equal(9.5m, excursion.ExtremeCelsius);
        Assert.False(excursion.IsOpen);
        Assert.Equal(15, history.MinutesOutsideRange);
        Assert.Equal(15, history.LongestExcursionMinutes);
    }

    [Fact]
    public void A_run_of_cold_readings_keeps_the_coldest_as_its_extreme() {
        // Arrange
        TemperatureHistory history = HistoryOf(5m, 1.5m, 0.5m, 1m, 5m);

        // Act
        Excursion excursion = Assert.Single(history.Excursions);

        // Assert
        Assert.Equal(ExcursionKind.Cold, excursion.Kind);
        Assert.Equal(0.5m, excursion.ExtremeCelsius);
        Assert.Equal(15, excursion.DurationMinutes);
    }

    [Fact]
    public void Readings_just_outside_the_label_start_an_excursion() {
        // Arrange
        TemperatureHistory history = HistoryOf(5m, 1.9m, 5m, 8.1m, 5m);

        // Assert
        Assert.Equal([ExcursionKind.Cold, ExcursionKind.Warm], history.Excursions.Select(e => e.Kind).ToList());
        Assert.Equal(10, history.MinutesOutsideRange);
    }

    [Fact]
    public void A_record_that_ends_outside_the_label_ends_with_an_open_excursion() {
        // Arrange
        TemperatureHistory history = HistoryOf(5m, 9m, 9.5m, 10m);

        // Act
        Excursion excursion = Assert.Single(history.Excursions);

        // Assert
        Assert.True(excursion.IsOpen);
        Assert.Equal(Start.AddMinutes(5), excursion.StartedAt);
        Assert.Equal(Start.AddMinutes(15), excursion.EndedAt);
        Assert.Equal(10m, excursion.ExtremeCelsius);
        Assert.Equal(10, history.MinutesOutsideRange);
    }

    [Fact]
    public void A_last_reading_alone_outside_the_label_is_an_open_excursion_of_no_duration() {
        // Arrange
        TemperatureHistory history = HistoryOf(5m, 5m, 9m);

        // Act
        Excursion excursion = Assert.Single(history.Excursions);

        // Assert
        Assert.True(excursion.IsOpen);
        Assert.Equal(0, excursion.DurationMinutes);
        Assert.Equal(0, history.MinutesOutsideRange);
    }

    [Fact]
    public void A_reading_alone_outside_the_label_costs_the_time_until_the_next_reading() {
        // Arrange
        TemperatureHistory history = HistoryOf(5m, 9m, 5m);

        // Assert
        Assert.Equal(5, history.MinutesOutsideRange);
    }

    [Fact]
    public void A_jump_from_cold_to_warm_closes_one_excursion_and_opens_the_other() {
        // Arrange: cold from minute 5, warm from minute 10, back inside at minute 20
        TemperatureHistory history = HistoryOf(5m, 1m, 9m, 9m, 5m);

        // Act
        IReadOnlyList<Excursion> excursions = history.Excursions;

        // Assert
        Assert.Equal(2, excursions.Count);
        Assert.Equal(ExcursionKind.Cold, excursions[0].Kind);
        Assert.Equal(Start.AddMinutes(5), excursions[0].StartedAt);
        Assert.Equal(Start.AddMinutes(10), excursions[0].EndedAt);
        Assert.False(excursions[0].IsOpen);
        Assert.Equal(ExcursionKind.Warm, excursions[1].Kind);
        Assert.Equal(Start.AddMinutes(10), excursions[1].StartedAt);
        Assert.Equal(Start.AddMinutes(20), excursions[1].EndedAt);
        Assert.Equal(15, history.MinutesOutsideRange);
        Assert.Equal(10, history.LongestExcursionMinutes);
    }

    [Fact]
    public void Excursions_add_up_and_the_longest_is_reported() {
        // Arrange: out at 0..5 (5 min), out at 10..25 (15 min)
        TemperatureHistory history = HistoryOf(9m, 5m, 9m, 9m, 9m, 5m);

        // Assert
        Assert.Equal(2, history.Excursions.Count);
        Assert.Equal(20, history.MinutesOutsideRange);
        Assert.Equal(15, history.LongestExcursionMinutes);
    }

    [Fact]
    public void An_excursion_that_does_not_end_on_a_whole_minute_is_charged_the_started_minute() {
        // Arrange
        DataLogger         logger  = FiveMinuteLogger();
        TemperatureHistory history = new TemperatureHistory(logger, Label);
        history.Record(new AnyLoggerReading().WithLogger(logger.Id).WithTakenAt(Start).WithCelsius(5m).Generate());
        history.Record(new AnyLoggerReading().WithLogger(logger.Id).WithTakenAt(Start.AddSeconds(30)).WithCelsius(9m).Generate());
        history.Record(new AnyLoggerReading().WithLogger(logger.Id).WithTakenAt(Start.AddSeconds(30 + 61)).WithCelsius(5m).Generate());

        // Assert
        Assert.Equal(2, history.MinutesOutsideRange);
    }

    [Theory]
    [InlineData(-0.1, true)]
    [InlineData(0.0, false)]
    [InlineData(25.0, false)]
    [InlineData(25.1, true)]
    public void A_single_reading_beyond_a_hard_limit_marks_the_history(double celsius, bool breach) {
        // Arrange
        TemperatureHistory history = HistoryOf(5m, (decimal)celsius, 5m);

        // Assert
        Assert.Equal(breach, history.HasHardLimitBreach);
    }

    [Fact]
    public void A_record_covers_a_period_when_no_silence_exceeds_two_intervals() {
        // Arrange
        TemperatureHistory history = HistoryOf(5m, 5m, 5m, 5m);

        // Assert
        Assert.True(history.Covers(Start, Start.AddMinutes(15)));
        Assert.True(history.Covers(Start.AddMinutes(5), Start.AddMinutes(10)));
    }

    [Fact]
    public void A_record_covers_a_period_started_up_to_two_intervals_before_its_first_reading() {
        // Arrange
        TemperatureHistory history = HistoryOf(5m, 5m, 5m);

        // Assert
        Assert.True(history.Covers(Start.AddMinutes(-10), Start.AddMinutes(10)));
        Assert.False(history.Covers(Start.AddMinutes(-11), Start.AddMinutes(10)));
    }

    [Fact]
    public void A_record_covers_a_period_ending_up_to_two_intervals_after_its_last_reading() {
        // Arrange
        TemperatureHistory history = HistoryOf(5m, 5m, 5m);

        // Assert
        Assert.True(history.Covers(Start, Start.AddMinutes(20)));
        Assert.False(history.Covers(Start, Start.AddMinutes(21)));
    }

    [Fact]
    public void A_gap_between_two_readings_of_more_than_two_intervals_breaks_the_record() {
        // Arrange
        DataLogger         logger  = FiveMinuteLogger();
        TemperatureHistory exactly = new TemperatureHistory(logger, Label);
        TemperatureHistory beyond  = new TemperatureHistory(logger, Label);
        exactly.Record(ReadingOf(logger, 0, 5m));
        exactly.Record(ReadingOf(logger, 10, 5m));
        exactly.Record(ReadingOf(logger, 20, 5m));
        beyond.Record(ReadingOf(logger, 0, 5m));
        beyond.Record(ReadingOf(logger, 10, 5m));
        beyond.Record(ReadingOf(logger, 21, 5m));

        // Assert
        Assert.True(exactly.Covers(Start, Start.AddMinutes(20)));
        Assert.False(beyond.Covers(Start, Start.AddMinutes(21)));
    }

    [Fact]
    public void A_record_that_does_not_reach_the_period_does_not_cover_it() {
        // Arrange
        TemperatureHistory history = HistoryOf(5m, 5m);

        // Assert
        Assert.False(history.Covers(Start.AddHours(1), Start.AddHours(2)));
        Assert.False(history.Covers(Start.AddHours(-3), Start.AddHours(-2)));
    }

    [Fact]
    public void A_period_cannot_end_before_it_starts() {
        // Arrange
        TemperatureHistory history = HistoryOf(5m, 5m);

        // Act
        Action act = () => history.Covers(Start.AddMinutes(10), Start.AddMinutes(9));

        // Assert
        Assert.Equal("to", Assert.Throws<ArgumentException>(act).ParamName);
    }

    [Fact]
    public void A_history_needs_a_logger_and_a_label() {
        // Assert
        Assert.Throws<ArgumentNullException>(() => new TemperatureHistory(null!, Label));
        Assert.Throws<ArgumentNullException>(() => new TemperatureHistory(FiveMinuteLogger(), null!));
        Assert.Throws<ArgumentNullException>(() => HistoryOf().Record(null!));
    }

}

public sealed class LegCustodyTests {

    [Fact]
    public void A_custody_starts_open_at_its_acceptance() {
        // Arrange
        Leg            leg        = new AnyLeg().Generate();
        CustodianId    custodian  = new AnyCustodianId().Generate();
        DateTimeOffset acceptedAt = new DateTimeOffset(2030, 6, 1, 6, 0, 0, TimeSpan.Zero);

        // Act
        LegCustody custody = new LegCustody(2, leg, custodian, acceptedAt);

        // Assert
        Assert.Equal(2, custody.LegIndex);
        Assert.Same(leg, custody.Leg);
        Assert.Equal(custodian, custody.Custodian);
        Assert.Equal(acceptedAt, custody.AcceptedAt);
        Assert.True(custody.IsOpen);
        Assert.Null(custody.ReleasedAt);
        Assert.Equal($"leg 2 {leg.Origin}->{leg.Destination}: {custodian}", custody.ToString());
    }

    [Fact]
    public void A_leg_index_cannot_be_negative() {
        // Arrange
        int anyNegative = Any.Int32().Negative().Generate();

        // Act
        Action act = () => new LegCustody(anyNegative, new AnyLeg().Generate(), new AnyCustodianId().Generate(), DateTimeOffset.UnixEpoch);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void An_open_custody_covers_every_instant_from_its_acceptance_on() {
        // Arrange
        DateTimeOffset acceptedAt = new DateTimeOffset(2030, 6, 1, 6, 0, 0, TimeSpan.Zero);
        LegCustody     custody    = new LegCustody(0, new AnyLeg().Generate(), new AnyCustodianId().Generate(), acceptedAt);

        // Assert
        Assert.False(custody.Covers(acceptedAt.AddTicks(-1)));
        Assert.True(custody.Covers(acceptedAt));
        Assert.True(custody.Covers(acceptedAt.AddDays(400)));
    }

    [Fact]
    public void A_custody_needs_a_leg_and_a_custodian() {
        // Assert
        Assert.Throws<ArgumentNullException>(() => new LegCustody(0, null!, new AnyCustodianId().Generate(), DateTimeOffset.UnixEpoch));
        Assert.Throws<ArgumentNullException>(() => new LegCustody(0, new AnyLeg().Generate(), null!, DateTimeOffset.UnixEpoch));
    }

}
