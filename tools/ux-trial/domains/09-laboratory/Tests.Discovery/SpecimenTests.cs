using JustDummies;

using Laboratory.Domain;

using Xunit;

namespace Laboratory.Tests.Discovery;

public sealed class SpecimenQualityTests {

    [Fact]
    public void A_quality_keeps_what_it_was_given() {
        // Arrange
        TimeSpan anyTransit = Any.TimeSpan().Positive().Generate();

        // Act
        SpecimenQuality quality = new SpecimenQuality(4.5, 20, anyTransit);

        // Assert
        Assert.Equal(4.5, quality.VolumeMillilitres);
        Assert.Equal(20, quality.HemolysisIndex);
        Assert.Equal(anyTransit, quality.MaxTransit);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(0d)]
    [InlineData(-0.001d)]
    [InlineData(50.0001d)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_volume_that_is_not_positive_or_above_fifty_millilitres_is_refused(double volume) {
        // Arrange
        TimeSpan anyTransit = Any.TimeSpan().Positive().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpecimenQuality(volume, 10, anyTransit));
    }

    [Theory]
    [InlineData(double.Epsilon)]
    [InlineData(50d)]
    public void The_extreme_valid_volumes_are_accepted(double volume) {
        // Arrange
        TimeSpan anyTransit = Any.TimeSpan().Positive().Generate();

        // Assert
        Assert.Equal(volume, new SpecimenQuality(volume, 10, anyTransit).VolumeMillilitres);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(501)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void A_hemolysis_index_outside_zero_to_five_hundred_is_refused(int index) {
        // Arrange
        double   anyVolume  = Any.Double().Between(0.1d, 50d).Generate();
        TimeSpan anyTransit = Any.TimeSpan().Positive().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpecimenQuality(anyVolume, index, anyTransit));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    public void The_extreme_valid_hemolysis_indexes_are_accepted(int index) {
        // Arrange
        double   anyVolume  = Any.Double().Between(0.1d, 50d).Generate();
        TimeSpan anyTransit = Any.TimeSpan().Positive().Generate();

        // Assert
        Assert.Equal(index, new SpecimenQuality(anyVolume, index, anyTransit).HemolysisIndex);
    }

    [Fact]
    public void A_transit_that_is_not_positive_is_refused() {
        // Arrange
        double   anyVolume       = Any.Double().Between(0.1d, 50d).Generate();
        TimeSpan anyNonPositive  = Any.TimeSpan().LessThanOrEqualTo(TimeSpan.Zero).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpecimenQuality(anyVolume, 10, anyNonPositive));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpecimenQuality(anyVolume, 10, TimeSpan.Zero));
    }

    [Fact]
    public void The_smallest_positive_transit_is_accepted() {
        // Arrange
        double anyVolume = Any.Double().Between(0.1d, 50d).Generate();

        // Assert
        Assert.Equal(TimeSpan.FromTicks(1), new SpecimenQuality(anyVolume, 10, TimeSpan.FromTicks(1)).MaxTransit);
    }

    [Theory]
    [InlineData(0, 0.5d, true)]
    [InlineData(100, 0.5d, true)]      // both limits are inclusive
    [InlineData(100, 50d, true)]
    [InlineData(101, 0.5d, false)]     // hemolysed
    [InlineData(500, 10d, false)]
    [InlineData(0, 0.4999d, false)]    // too little blood for chemistry
    [InlineData(101, 0.1d, false)]
    public void Potassium_needs_a_low_hemolysis_and_enough_volume(int hemolysis, double volume, bool expected) {
        // Arrange
        TimeSpan anyTransit = Any.TimeSpan().Positive().Generate();

        // Act
        SpecimenQuality quality = new SpecimenQuality(volume, hemolysis, anyTransit);

        // Assert
        Assert.Equal(expected, quality.AcceptableForPotassium);
    }

    [Fact]
    public void The_minimum_volume_for_chemistry_is_half_a_millilitre() {
        Assert.Equal(0.5d, SpecimenQuality.MinimumVolumeForChemistry);
    }

    [Fact]
    public void A_drawn_quality_is_valid() {
        // Arrange
        SpecimenQuality anyQuality = Any.SpecimenQuality().Generate();

        // Assert
        Assert.InRange(anyQuality.VolumeMillilitres, double.Epsilon, 50d);
        Assert.InRange(anyQuality.HemolysisIndex, 0, 500);
        Assert.True(anyQuality.MaxTransit > TimeSpan.Zero);
    }

    [Fact]
    public void A_hemolysed_specimen_is_never_acceptable_for_potassium_whatever_its_volume() {
        // Arrange
        SpecimenQuality hemolysed = Any.SpecimenQuality().WithHemolysisIndex(h => h.Between(101, 500)).Generate();

        // Assert
        Assert.False(hemolysed.AcceptableForPotassium);
    }

}

public sealed class SpecimenTests {

    private static readonly DateTimeOffset CollectedAt = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_specimen_keeps_what_it_was_given() {
        // Arrange
        SampleId     anyId   = Any.SampleId().Generate();
        SpecimenType anyType = Any.Enum<SpecimenType>().Generate();
        DateTimeOffset received = CollectedAt.AddHours(3);

        // Act
        Specimen specimen = new Specimen(anyId, anyType, CollectedAt, received);

        // Assert
        Assert.Same(anyId, specimen.Id);
        Assert.Equal(anyType, specimen.Type);
        Assert.Equal(CollectedAt, specimen.CollectedAt);
        Assert.Equal(received, specimen.ReceivedAt);
    }

    [Fact]
    public void The_stability_window_is_seventy_two_hours() {
        Assert.Equal(TimeSpan.FromHours(72), Specimen.StabilityWindow);
    }

    [Fact]
    public void A_missing_id_is_refused() {
        // Arrange
        SpecimenType anyType = Any.Enum<SpecimenType>().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Specimen(null!, anyType, CollectedAt, CollectedAt.AddHours(1)));
    }

    [Fact]
    public void A_type_that_is_not_a_declared_member_is_refused() {
        // Arrange
        SampleId     anyId          = Any.SampleId().Generate();
        SpecimenType anyUndeclared  = (SpecimenType)Any.Int32().Except(Enumerable.Range(0, 5)).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Specimen(anyId, anyUndeclared, CollectedAt, CollectedAt.AddHours(1)));
    }

    [Fact]
    public void Every_declared_type_is_accepted() {
        // Arrange
        SampleId anyId = Any.SampleId().Generate();

        foreach (SpecimenType type in Enum.GetValues<SpecimenType>()) {
            // Assert
            Assert.Equal(type, new Specimen(anyId, type, CollectedAt, CollectedAt.AddHours(1)).Type);
        }
    }

    [Fact]
    public void A_specimen_cannot_be_received_before_it_is_collected() {
        // Arrange
        SampleId     anyId   = Any.SampleId().Generate();
        SpecimenType anyType = Any.Enum<SpecimenType>().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Specimen(anyId, anyType, CollectedAt, CollectedAt.AddTicks(-1)));
    }

    [Fact]
    public void A_specimen_received_at_the_very_instant_of_its_collection_is_accepted() {
        // Arrange
        SampleId     anyId   = Any.SampleId().Generate();
        SpecimenType anyType = Any.Enum<SpecimenType>().Generate();

        // Assert
        Assert.Equal(CollectedAt, new Specimen(anyId, anyType, CollectedAt, CollectedAt).ReceivedAt);
    }

    [Fact]
    public void A_specimen_received_exactly_at_the_end_of_the_stability_window_is_accepted() {
        // Arrange
        SampleId     anyId   = Any.SampleId().Generate();
        SpecimenType anyType = Any.Enum<SpecimenType>().Generate();

        // Act
        Specimen specimen = new Specimen(anyId, anyType, CollectedAt, CollectedAt.AddHours(72));

        // Assert
        Assert.Equal(TimeSpan.FromHours(72), specimen.ReceivedAt - specimen.CollectedAt);
    }

    [Fact]
    public void A_specimen_received_one_tick_after_the_stability_window_is_refused() {
        // Arrange
        SampleId     anyId   = Any.SampleId().Generate();
        SpecimenType anyType = Any.Enum<SpecimenType>().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Specimen(anyId, anyType, CollectedAt, CollectedAt.AddHours(72).AddTicks(1)));
    }

    [Fact]
    public void Instants_are_compared_whatever_their_offsets() {
        // Arrange
        SampleId     anyId   = Any.SampleId().Generate();
        SpecimenType anyType = Any.Enum<SpecimenType>().Generate();

        // 10:00+02:00 is 08:00 UTC, so 09:00 UTC is one hour later even though its clock reads earlier
        DateTimeOffset collected = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.FromHours(2));
        DateTimeOffset received  = new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

        // Assert
        Assert.Equal(received, new Specimen(anyId, anyType, collected, received).ReceivedAt);
    }

    [Fact]
    public void The_window_is_measured_between_instants_not_between_clock_readings() {
        // Arrange
        SampleId     anyId   = Any.SampleId().Generate();
        SpecimenType anyType = Any.Enum<SpecimenType>().Generate();

        // The two clocks read exactly 72 hours apart, but the instants are 91 hours apart (18:00 UTC on Feb 28, 13:00 UTC on Mar 4).
        DateTimeOffset collected = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.FromHours(14));
        DateTimeOffset received  = new DateTimeOffset(2026, 3, 4, 8, 0, 0, TimeSpan.FromHours(-5));

        // Assert
        Assert.Throws<ArgumentException>(() => new Specimen(anyId, anyType, collected, received));
    }

    [Fact]
    public void The_type_is_checked_before_the_times() {
        // Arrange
        SampleId anyId = Any.SampleId().Generate();

        // Assert: undefined type AND received before collected: the type is reported
        Assert.Throws<ArgumentOutOfRangeException>(() => new Specimen(anyId, (SpecimenType)99, CollectedAt, CollectedAt.AddDays(-1)));
    }

    [Fact]
    public void A_drawn_specimen_is_valid() {
        // Arrange
        Specimen anySpecimen = Any.Specimen().Generate();

        // Assert
        Assert.True(anySpecimen.ReceivedAt >= anySpecimen.CollectedAt);
        Assert.True(anySpecimen.ReceivedAt - anySpecimen.CollectedAt <= Specimen.StabilityWindow);
    }

}
