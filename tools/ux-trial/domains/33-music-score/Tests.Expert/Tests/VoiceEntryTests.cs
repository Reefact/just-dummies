using static MusicScore.Domain.Tests.Expert.Notation;

namespace MusicScore.Domain.Tests.Expert;

public sealed class VoiceEntryTests {

    [Fact]
    public void A_note_carries_its_duration_its_pitch_and_its_tie() {
        // Arrange
        Duration quarter = D(1, 4);
        Pitch    a4      = P("A4");

        // Act
        VoiceEntry tiedNote = new(quarter, a4, tiedToNext: true);

        // Assert
        Assert.Equal(quarter, tiedNote.Duration);
        Assert.Equal(a4, tiedNote.Pitch);
        Assert.True(tiedNote.TiedToNext);
        Assert.False(tiedNote.IsRest);
    }

    [Fact]
    public void A_note_is_not_tied_unless_it_says_so() {
        // Arrange
        VoiceEntry untiedNote = Arbitrary.VoiceEntry().WithTiedToNext(false).Generate();

        // Assert
        Assert.False(untiedNote.TiedToNext);
    }

    [Fact]
    public void An_entry_without_pitch_is_a_rest() {
        // Arrange
        Duration anyDuration = Arbitrary.Duration().NonZero().Generate();

        // Act
        VoiceEntry rest = new(anyDuration, pitch: null, tiedToNext: false);

        // Assert
        Assert.True(rest.IsRest);
        Assert.Null(rest.Pitch);
    }

    [Fact]
    public void The_rest_factory_makes_an_untied_entry_without_pitch() {
        // Act
        VoiceEntry rest = VoiceEntry.Rest(D(3, 8));

        // Assert
        Assert.True(rest.IsRest);
        Assert.False(rest.TiedToNext);
        Assert.Equal(D(3, 8), rest.Duration);
    }

    [Fact]
    public void A_note_of_no_length_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.VoiceEntry().WithDuration(Duration.Zero).Generate());

        // Assert
        Assert.Equal("duration", refusal.ParamName);
    }

    [Fact]
    public void A_rest_of_no_length_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => VoiceEntry.Rest(Duration.Zero));
    }

    [Fact]
    public void An_entry_without_duration_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Arbitrary.VoiceEntry().WithDuration((Duration)null!).Generate());
    }

    [Fact]
    public void A_rest_cannot_be_tied() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Arbitrary.VoiceEntry().WithoutPitch().WithTiedToNext(true).Generate());

        // Assert
        Assert.Equal("tiedToNext", refusal.ParamName);
    }

    [Fact]
    public void A_dotted_note_keeps_its_exact_length() {
        // Act
        VoiceEntry dottedQuarter = Note("C4", 3, 8);

        // Assert
        Assert.Equal(D(3, 8), dottedQuarter.Duration);
    }

}
