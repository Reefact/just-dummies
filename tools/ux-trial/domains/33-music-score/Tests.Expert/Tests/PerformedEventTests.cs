using static MusicScore.Domain.Tests.Expert.Notation;

namespace MusicScore.Domain.Tests.Expert;

public sealed class PerformedEventTests {

    [Fact]
    public void An_event_ends_where_its_onset_plus_its_length_says() {
        // Act
        PerformedEvent performed = Arbitrary.PerformedEvent().WithOnset(D(3, 4)).WithLength(D(1, 8)).Generate();

        // Assert
        Assert.Equal(D(7, 8), performed.End);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(127)]
    public void A_velocity_from_one_to_one_hundred_twenty_seven_is_accepted(int velocity) {
        // Act
        PerformedEvent performed = Arbitrary.PerformedEvent().WithVelocity(velocity).Generate();

        // Assert
        Assert.Equal(velocity, performed.Velocity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(128)]
    [InlineData(-1)]
    public void A_velocity_outside_one_to_one_hundred_twenty_seven_is_refused(int velocity) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.PerformedEvent().WithVelocity(velocity).Generate());

        // Assert
        Assert.Equal("velocity", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void A_voice_number_outside_one_to_four_is_refused(int voiceNumber) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.PerformedEvent().WithVoiceNumber(voiceNumber).Generate());

        // Assert
        Assert.Equal("voiceNumber", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void An_event_must_name_its_staff(string staffName) {
        Assert.ThrowsAny<ArgumentException>(() => Arbitrary.PerformedEvent().WithStaffName(staffName).Generate());
    }

    [Fact]
    public void An_event_of_no_length_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.PerformedEvent().WithLength(Duration.Zero).Generate());

        // Assert
        Assert.Equal("length", refusal.ParamName);
    }

    [Fact]
    public void An_event_may_start_at_the_very_beginning() {
        // Act
        PerformedEvent performed = Arbitrary.PerformedEvent().WithStartSeconds(0.0).WithOnset(Duration.Zero).Generate();

        // Assert
        Assert.Equal(0.0, performed.StartSeconds);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_start_that_is_negative_or_not_finite_is_refused(double startSeconds) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.PerformedEvent().WithStartSeconds(startSeconds).Generate());

        // Assert
        Assert.Equal("startSeconds", refusal.ParamName);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_sounding_time_that_is_not_positive_and_finite_is_refused(double durationSeconds) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.PerformedEvent().WithDurationSeconds(durationSeconds).Generate());

        // Assert
        Assert.Equal("durationSeconds", refusal.ParamName);
    }

    [Fact]
    public void An_event_without_pitch_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Arbitrary.PerformedEvent().WithPitch((Pitch)null!).Generate());
    }

}
