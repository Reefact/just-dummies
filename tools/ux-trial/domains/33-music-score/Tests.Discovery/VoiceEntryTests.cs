using JustDummies;

using MusicScore.Domain;

using Xunit;

namespace MusicScore.Tests.Discovery;

public sealed class VoiceEntryTests {

    [Fact]
    public void A_note_keeps_its_duration_pitch_and_tie() {
        // Arrange
        Duration anyDuration = Any.Duration().WithNumerator(n => n.Positive()).Generate();
        Pitch    anyPitch    = Any.Pitch().Generate();

        // Act
        VoiceEntry entry = new VoiceEntry(anyDuration, anyPitch, tiedToNext: true);

        // Assert
        Assert.Same(anyDuration, entry.Duration);
        Assert.Same(anyPitch, entry.Pitch);
        Assert.True(entry.TiedToNext);
        Assert.False(entry.IsRest);
    }

    [Fact]
    public void A_missing_pitch_makes_a_rest() {
        // Arrange
        Duration anyDuration = Any.Duration().WithNumerator(n => n.Positive()).Generate();

        // Act
        VoiceEntry entry = new VoiceEntry(anyDuration, null, tiedToNext: false);

        // Assert
        Assert.True(entry.IsRest);
        Assert.Null(entry.Pitch);
    }

    [Fact]
    public void The_rest_factory_builds_an_untied_rest() {
        // Arrange
        Duration anyDuration = Any.Duration().WithNumerator(n => n.Positive()).Generate();

        // Act
        VoiceEntry rest = VoiceEntry.Rest(anyDuration);

        // Assert
        Assert.True(rest.IsRest);
        Assert.False(rest.TiedToNext);
        Assert.Equal(anyDuration, rest.Duration);
    }

    [Fact]
    public void A_rest_cannot_be_tied() {
        // Arrange
        Duration anyDuration = Any.Duration().WithNumerator(n => n.Positive()).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new VoiceEntry(anyDuration, null, tiedToNext: true));

        // Assert
        Assert.Equal("tiedToNext", error.ParamName);
    }

    [Fact]
    public void A_note_must_last_a_positive_duration() {
        // Arrange
        Pitch anyPitch = Any.Pitch().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new VoiceEntry(Duration.Zero, anyPitch, false));

        // Assert
        Assert.Equal("duration", error.ParamName);
    }

    [Fact]
    public void A_rest_must_last_a_positive_duration_too() {
        Assert.Throws<ArgumentOutOfRangeException>(() => VoiceEntry.Rest(Duration.Zero));
    }

    [Fact]
    public void A_missing_duration_is_refused() {
        // Arrange
        Pitch anyPitch = Any.Pitch().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new VoiceEntry(null!, anyPitch, false));
    }

}
