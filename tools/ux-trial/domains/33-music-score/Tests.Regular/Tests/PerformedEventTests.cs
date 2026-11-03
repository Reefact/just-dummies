using System;

using JustDummies;

using Xunit;

using static MusicScore.Domain.Tests.Notation;

namespace MusicScore.Domain.Tests;

public sealed class PerformedEventTests {

    [Fact]
    public void A_performed_event_exposes_what_it_was_built_from() {
        // Arrange
        string   anyStaffName = Any.String().Alpha().WithLengthBetween(1, 40).Generate();
        Duration onset        = D(3, 4);
        Duration length       = D(1, 8);

        // Act
        PerformedEvent performed = new PerformedEvent(anyStaffName, 2, P("A4"), 100, onset, length, 1.5, 0.25);

        // Assert
        Assert.Equal(anyStaffName, performed.StaffName);
        Assert.Equal(2, performed.VoiceNumber);
        Assert.Equal(P("A4"), performed.Pitch);
        Assert.Equal(100, performed.Velocity);
        Assert.Equal(onset, performed.Onset);
        Assert.Equal(length, performed.Length);
        Assert.Equal(1.5, performed.StartSeconds);
        Assert.Equal(0.25, performed.DurationSeconds);
    }

    [Fact]
    public void The_end_of_an_event_is_its_onset_plus_its_length() {
        // Arrange
        PerformedEvent performed = Arbitrary.PerformedEvent().WithOnset(D(3, 4)).WithLength(D(1, 3)).Generate();

        // Assert
        Assert.Equal(D(13, 12), performed.End);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(127)]
    public void The_extreme_velocities_are_accepted(int velocity) {
        // Assert
        Assert.Equal(velocity, Arbitrary.PerformedEvent().WithVelocity(velocity).Generate().Velocity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(128)]
    [InlineData(-1)]
    public void A_velocity_outside_one_to_one_hundred_twenty_seven_is_refused(int velocity) {
        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.PerformedEvent().WithVelocity(velocity).Generate());

        // Assert
        Assert.Equal("velocity", error.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void A_voice_number_outside_one_to_four_is_refused(int voiceNumber) {
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.PerformedEvent().WithVoiceNumber(voiceNumber).Generate());
    }

    [Fact]
    public void A_zero_length_is_refused() {
        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.PerformedEvent().WithLength(Duration.Zero).Generate());

        // Assert
        Assert.Equal("length", error.ParamName);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-0.001)]
    public void A_start_time_must_be_finite_and_not_negative(double startSeconds) {
        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.PerformedEvent().WithStartSeconds(startSeconds).Generate());

        // Assert
        Assert.Equal("startSeconds", error.ParamName);
    }

    [Fact]
    public void A_start_time_of_zero_is_accepted() {
        // Assert
        Assert.Equal(0.0, Arbitrary.PerformedEvent().WithStartSeconds(0.0).Generate().StartSeconds);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void A_duration_in_seconds_must_be_finite_and_positive(double durationSeconds) {
        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.PerformedEvent().WithDurationSeconds(durationSeconds).Generate());

        // Assert
        Assert.Equal("durationSeconds", error.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_staff_name_is_refused(string staffName) {
        // Assert
        Assert.Throws<ArgumentException>(() => Arbitrary.PerformedEvent().WithStaffName(staffName).Generate());
    }

    [Fact]
    public void A_null_pitch_onset_or_length_is_refused() {
        // Assert
        Assert.Throws<ArgumentNullException>(() => Arbitrary.PerformedEvent().WithPitch((Pitch)null!).Generate());
        Assert.Throws<ArgumentNullException>(() => Arbitrary.PerformedEvent().WithOnset((Duration)null!).Generate());
        Assert.Throws<ArgumentNullException>(() => Arbitrary.PerformedEvent().WithLength((Duration)null!).Generate());
    }

}
