using JustDummies;

using Xunit;

namespace Laboratory.Domain.Tests;

public sealed class SpecimenTests {

    private static AnyDateTimeOffset AnyCollectionInstant() {
        return Any.DateTimeOffset().Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                                            new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void A_specimen_received_within_its_window_is_built_and_keeps_what_it_was_given() {
        // Arrange
        SampleId       anyId          = Any.SampleId().Generate();
        SpecimenType   anyType        = Any.Enum<SpecimenType>().Generate();
        DateTimeOffset anyCollectedAt = AnyCollectionInstant().Generate();
        TimeSpan       anyTransit     = Any.TimeSpan().Between(TimeSpan.Zero, Specimen.StabilityWindow).Generate();

        // Act
        Specimen specimen = new Specimen(anyId, anyType, anyCollectedAt, anyCollectedAt + anyTransit);

        // Assert
        Assert.Same(anyId, specimen.Id);
        Assert.Equal(anyType, specimen.Type);
        Assert.Equal(anyCollectedAt, specimen.CollectedAt);
        Assert.Equal(anyCollectedAt + anyTransit, specimen.ReceivedAt);
    }

    [Fact]
    public void The_generator_only_draws_specimens_the_constructor_accepts() {
        // Act
        Specimen anySpecimen = Any.Specimen().Generate();

        // Assert
        Assert.InRange(anySpecimen.ReceivedAt - anySpecimen.CollectedAt, TimeSpan.Zero, Specimen.StabilityWindow);
    }

    [Fact]
    public void The_stability_window_is_seventy_two_hours() {
        Assert.Equal(TimeSpan.FromHours(72), Specimen.StabilityWindow);
    }

    [Fact]
    public void A_specimen_received_the_instant_it_was_collected_is_accepted() {
        // Arrange
        DateTimeOffset anyInstant = AnyCollectionInstant().Generate();

        // Act
        Specimen specimen = new Specimen(Any.SampleId().Generate(), Any.Enum<SpecimenType>().Generate(), anyInstant, anyInstant);

        // Assert
        Assert.Equal(specimen.CollectedAt, specimen.ReceivedAt);
    }

    [Fact]
    public void A_specimen_received_exactly_seventy_two_hours_after_collection_is_accepted() {
        // Arrange
        DateTimeOffset anyCollectedAt = AnyCollectionInstant().Generate();

        // Act
        Specimen specimen = new Specimen(Any.SampleId().Generate(), Any.Enum<SpecimenType>().Generate(), anyCollectedAt, anyCollectedAt + Specimen.StabilityWindow);

        // Assert
        Assert.Equal(Specimen.StabilityWindow, specimen.ReceivedAt - specimen.CollectedAt);
    }

    [Fact]
    public void A_specimen_received_one_tick_after_its_window_is_refused() {
        // Arrange
        DateTimeOffset anyCollectedAt = AnyCollectionInstant().Generate();
        DateTimeOffset tooLate        = anyCollectedAt + Specimen.StabilityWindow + TimeSpan.FromTicks(1);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Specimen(Any.SampleId().Generate(), Any.Enum<SpecimenType>().Generate(), anyCollectedAt, tooLate));

        // Assert
        Assert.Equal("receivedAt", refusal.ParamName);
    }

    [Fact]
    public void A_specimen_received_long_after_its_window_is_refused() {
        // Arrange
        DateTimeOffset anyCollectedAt = AnyCollectionInstant().Generate();
        TimeSpan       anyDelay       = Any.TimeSpan().Between(Specimen.StabilityWindow + TimeSpan.FromTicks(1), TimeSpan.FromDays(3_650)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Specimen(Any.SampleId().Generate(), Any.Enum<SpecimenType>().Generate(), anyCollectedAt, anyCollectedAt + anyDelay));
    }

    [Fact]
    public void A_specimen_received_one_tick_before_it_was_collected_is_refused() {
        // Arrange
        DateTimeOffset anyCollectedAt = AnyCollectionInstant().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Specimen(Any.SampleId().Generate(), Any.Enum<SpecimenType>().Generate(), anyCollectedAt, anyCollectedAt - TimeSpan.FromTicks(1)));

        // Assert
        Assert.Equal("receivedAt", refusal.ParamName);
    }

    [Fact]
    public void A_specimen_received_long_before_it_was_collected_is_refused() {
        // Arrange
        DateTimeOffset anyCollectedAt = AnyCollectionInstant().Generate();
        TimeSpan       anyAdvance     = Any.TimeSpan().Between(TimeSpan.FromTicks(1), TimeSpan.FromDays(3_650)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Specimen(Any.SampleId().Generate(), Any.Enum<SpecimenType>().Generate(), anyCollectedAt, anyCollectedAt - anyAdvance));
    }

    [Fact]
    public void The_window_is_measured_between_instants_whatever_the_offsets() {
        // Arrange: collected at 22:00 +05:00 (17:00 UTC), received at 16:00 -03:00 three days later (19:00 UTC) = 74 h, then 70 h.
        DateTimeOffset collectedAt = new DateTimeOffset(2025, 3, 1, 22, 0, 0, TimeSpan.FromHours(5));
        DateTimeOffset inWindow    = new DateTimeOffset(2025, 3, 4, 14, 0, 0, TimeSpan.FromHours(-3));    // 17:00 UTC on the 4th = 72 h
        DateTimeOffset tooLate     = new DateTimeOffset(2025, 3, 4, 14, 0, 1, TimeSpan.FromHours(-3));

        // Act
        Specimen accepted = new Specimen(Any.SampleId().Generate(), Any.Enum<SpecimenType>().Generate(), collectedAt, inWindow);

        // Assert
        Assert.Equal(TimeSpan.FromHours(72), accepted.ReceivedAt - accepted.CollectedAt);
        Assert.Throws<ArgumentException>(() => new Specimen(Any.SampleId().Generate(), Any.Enum<SpecimenType>().Generate(), collectedAt, tooLate));
    }

    [Fact]
    public void A_missing_id_is_refused_as_a_null_argument() {
        // Arrange
        DateTimeOffset anyInstant = AnyCollectionInstant().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Specimen(null!, Any.Enum<SpecimenType>().Generate(), anyInstant, anyInstant));

        // Assert
        Assert.Equal("id", refusal.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void A_type_that_is_not_a_declared_member_is_refused_as_out_of_range(int undefinedType) {
        // Arrange
        DateTimeOffset anyInstant = AnyCollectionInstant().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Specimen(Any.SampleId().Generate(), (SpecimenType)undefinedType, anyInstant, anyInstant));

        // Assert
        Assert.Equal("type", refusal.ParamName);
    }

    [Fact]
    public void The_type_is_judged_before_the_times() {
        // Arrange: undefined type AND received before collected.
        DateTimeOffset anyCollectedAt = AnyCollectionInstant().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Specimen(Any.SampleId().Generate(), (SpecimenType)99, anyCollectedAt, anyCollectedAt - TimeSpan.FromDays(1)));
    }

    [Fact]
    public void Every_declared_specimen_type_is_accepted() {
        // Arrange
        DateTimeOffset anyInstant = AnyCollectionInstant().Generate();

        foreach (SpecimenType type in Enum.GetValues<SpecimenType>()) {
            // Act
            Specimen specimen = new Specimen(Any.SampleId().Generate(), type, anyInstant, anyInstant);

            // Assert
            Assert.Equal(type, specimen.Type);
        }
    }

}
