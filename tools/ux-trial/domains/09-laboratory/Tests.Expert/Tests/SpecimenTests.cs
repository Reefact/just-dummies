namespace Laboratory.Tests;

public sealed class SpecimenTests {

    [Fact]
    public void A_specimen_exposes_what_it_was_built_from() {
        // Arrange
        SampleId       sampleId    = new SampleId("S0000000042");
        DateTimeOffset collectedAt = new DateTimeOffset(2025, 3, 1, 8, 0, 0, TimeSpan.Zero);
        DateTimeOffset receivedAt  = new DateTimeOffset(2025, 3, 1, 9, 30, 0, TimeSpan.Zero);

        // Act
        Specimen specimen = new Specimen(sampleId, SpecimenType.Serum, collectedAt, receivedAt);

        // Assert
        Assert.Same(sampleId, specimen.Id);
        Assert.Equal(SpecimenType.Serum, specimen.Type);
        Assert.Equal(collectedAt, specimen.CollectedAt);
        Assert.Equal(receivedAt, specimen.ReceivedAt);
    }

    [Fact]
    public void A_specimen_without_an_id_is_refused() {
        // Arrange
        DateTimeOffset anyCollectedAt = Any.Instant().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Specimen(null!, Any.Enum<SpecimenType>().Generate(), anyCollectedAt, anyCollectedAt));

        // Assert
        Assert.Equal("id", refusal.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]                 // one past the last member
    [InlineData(int.MaxValue)]
    public void A_specimen_type_that_is_not_a_declared_member_is_refused(int undefined) {
        // Arrange
        SampleId       anySampleId    = Any.SampleId().Generate();
        DateTimeOffset anyCollectedAt = Any.Instant().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Specimen(anySampleId, (SpecimenType)undefined, anyCollectedAt, anyCollectedAt));

        // Assert
        Assert.Equal("type", refusal.ParamName);
    }

    [Fact]
    public void Every_declared_specimen_type_is_accepted() {
        // Arrange
        SampleId       anySampleId    = Any.SampleId().Generate();
        DateTimeOffset anyCollectedAt = Any.Instant().Generate();

        // Act / Assert
        foreach (SpecimenType type in Enum.GetValues<SpecimenType>()) {
            Assert.Equal(type, new Specimen(anySampleId, type, anyCollectedAt, anyCollectedAt).Type);
        }
    }

    [Fact]
    public void A_specimen_received_at_the_very_instant_it_was_collected_is_accepted() {
        // Arrange
        DateTimeOffset anyInstant = Any.Instant().Generate();

        // Act
        Specimen specimen = new Specimen(Any.SampleId().Generate(), Any.Enum<SpecimenType>().Generate(), anyInstant, anyInstant);

        // Assert
        Assert.Equal(specimen.CollectedAt, specimen.ReceivedAt);
    }

    [Fact]
    public void A_specimen_received_before_it_was_collected_is_refused() {
        // Arrange
        DateTimeOffset anyCollectedAt = Any.Instant().Generate();
        DateTimeOffset oneTickEarlier = anyCollectedAt - TimeSpan.FromTicks(1);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Specimen(Any.SampleId().Generate(), Any.Enum<SpecimenType>().Generate(), anyCollectedAt, oneTickEarlier));

        // Assert
        Assert.Equal("receivedAt", refusal.ParamName);
    }

    [Fact]
    public void A_specimen_received_exactly_at_the_end_of_its_stability_window_is_accepted() {
        // Arrange
        DateTimeOffset anyCollectedAt = Any.Instant().Generate();

        // Act
        Specimen specimen = new Specimen(Any.SampleId().Generate(), Any.Enum<SpecimenType>().Generate(), anyCollectedAt, anyCollectedAt + TimeSpan.FromHours(72));

        // Assert
        Assert.Equal(TimeSpan.FromHours(72), specimen.ReceivedAt - specimen.CollectedAt);
    }

    [Fact]
    public void A_specimen_received_one_tick_after_its_stability_window_is_refused() {
        // Arrange
        DateTimeOffset anyCollectedAt = Any.Instant().Generate();
        DateTimeOffset tooLate        = anyCollectedAt + TimeSpan.FromHours(72) + TimeSpan.FromTicks(1);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Specimen(Any.SampleId().Generate(), Any.Enum<SpecimenType>().Generate(), anyCollectedAt, tooLate));

        // Assert
        Assert.Equal("receivedAt", refusal.ParamName);
    }

    [Fact]
    public void The_stability_window_is_seventy_two_hours() {
        Assert.Equal(TimeSpan.FromHours(72), Specimen.StabilityWindow);
    }

    [Fact]
    public void Times_are_compared_as_instants_whatever_their_offsets() {
        // Arrange: 08:00 at +02:00 is 06:00 UTC; 07:00 UTC is an hour later
        DateTimeOffset collectedAt = new DateTimeOffset(2025, 3, 1, 8, 0, 0, TimeSpan.FromHours(2));
        DateTimeOffset receivedAt  = new DateTimeOffset(2025, 3, 1, 7, 0, 0, TimeSpan.Zero);

        // Act
        Specimen specimen = new Specimen(Any.SampleId().Generate(), Any.Enum<SpecimenType>().Generate(), collectedAt, receivedAt);

        // Assert: accepted although the local clock reading went backwards (07:00 < 08:00)
        Assert.Equal(TimeSpan.FromHours(1), specimen.ReceivedAt - specimen.CollectedAt);
    }

    [Fact]
    public void The_window_is_measured_between_instants_not_between_local_clock_readings() {
        // Arrange: exactly 72 hours apart on the wall clocks (08:00 and 08:00, three days on), but 73 hours apart in UTC
        DateTimeOffset collectedAt = new DateTimeOffset(2025, 3, 1, 8, 0, 0, TimeSpan.Zero);
        DateTimeOffset receivedAt  = new DateTimeOffset(2025, 3, 4, 8, 0, 0, TimeSpan.FromHours(-1));

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Specimen(Any.SampleId().Generate(), Any.Enum<SpecimenType>().Generate(), collectedAt, receivedAt));
    }

}
