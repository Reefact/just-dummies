using JustDummies;

using MusicScore.Domain;

using Xunit;

namespace MusicScore.Tests.Discovery;

public sealed class PerformedEventTests {

    private static PerformedEvent EventWith(string? staffName = null, int? voiceNumber = null, Pitch? pitch = null, int? velocity = null, Duration? length = null, double? startSeconds = null, double? durationSeconds = null) {
        // Everything the test does not name is an arbitrary valid value.
        return new PerformedEvent(staffName ?? Any.String().Alpha().WithLengthBetween(1, 40).Generate(),
                                  voiceNumber ?? Any.Int32().Between(1, 4).Generate(),
                                  pitch ?? Any.Pitch().Generate(),
                                  velocity ?? Any.Int32().Between(1, 127).Generate(),
                                  new Duration(1, 4),
                                  length ?? new Duration(1, 8),
                                  startSeconds ?? Any.Double().Between(0, 1000).Generate(),
                                  durationSeconds ?? Any.Double().Between(0.001, 100).Generate());
    }

    [Fact]
    public void An_event_keeps_what_it_was_given_and_ends_where_its_length_runs_out() {
        // Arrange
        Pitch anyPitch = Any.Pitch().Generate();

        // Act
        PerformedEvent performed = new PerformedEvent("Violin", 2, anyPitch, 80, new Duration(1, 4), new Duration(1, 8), 0.5, 0.25);

        // Assert
        Assert.Equal("Violin", performed.StaffName);
        Assert.Equal(2, performed.VoiceNumber);
        Assert.Same(anyPitch, performed.Pitch);
        Assert.Equal(80, performed.Velocity);
        Assert.Equal(0.5, performed.StartSeconds);
        Assert.Equal(0.25, performed.DurationSeconds);
        Assert.Equal(new Duration(3, 8), performed.End);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(127)]
    public void The_velocity_limits_are_accepted(int velocity) {
        Assert.Equal(velocity, EventWith(velocity: velocity).Velocity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(128)]
    [InlineData(-1)]
    public void A_velocity_outside_one_to_one_hundred_twenty_seven_is_refused(int velocity) {
        Assert.Throws<ArgumentOutOfRangeException>(() => EventWith(velocity: velocity));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void A_voice_number_outside_one_to_four_is_refused(int voiceNumber) {
        Assert.Throws<ArgumentOutOfRangeException>(() => EventWith(voiceNumber: voiceNumber));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_staff_name_is_refused(string staffName) {
        Assert.Throws<ArgumentException>(() => EventWith(staffName: staffName));
    }

    [Fact]
    public void A_zero_length_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => EventWith(length: Duration.Zero));
    }

    [Fact]
    public void A_start_of_zero_seconds_is_accepted_but_a_negative_one_is_not() {
        Assert.Equal(0.0, EventWith(startSeconds: 0.0).StartSeconds);
        Assert.Throws<ArgumentOutOfRangeException>(() => EventWith(startSeconds: -0.001));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_start_that_is_not_finite_is_refused(double startSeconds) {
        Assert.Throws<ArgumentOutOfRangeException>(() => EventWith(startSeconds: startSeconds));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_duration_in_seconds_must_be_finite_and_positive(double durationSeconds) {
        Assert.Throws<ArgumentOutOfRangeException>(() => EventWith(durationSeconds: durationSeconds));
    }

    [Fact]
    public void A_missing_pitch_onset_or_length_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new PerformedEvent("Violin", 1, null!, 80, new Duration(1, 4), new Duration(1, 8), 0, 1));
        Assert.Throws<ArgumentNullException>(() => new PerformedEvent("Violin", 1, Any.Pitch().Generate(), 80, null!, new Duration(1, 8), 0, 1));
        Assert.Throws<ArgumentNullException>(() => new PerformedEvent("Violin", 1, Any.Pitch().Generate(), 80, new Duration(1, 4), null!, 0, 1));
    }

}
