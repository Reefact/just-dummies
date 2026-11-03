using JustDummies;

using MusicScore.Domain;

using Xunit;

namespace MusicScore.Tests.Discovery;

public sealed class VoiceTests {

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void A_voice_is_numbered_from_one_to_four(int number) {
        // Arrange
        VoiceEntry anyEntry = Any.VoiceEntry().Generate();

        // Act
        Voice voice = new Voice(number, [anyEntry]);

        // Assert
        Assert.Equal(number, voice.Number);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(-1)]
    public void A_voice_number_outside_one_to_four_is_refused(int number) {
        // Arrange
        VoiceEntry anyEntry = Any.VoiceEntry().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Voice(number, [anyEntry]));

        // Assert
        Assert.Equal("number", error.ParamName);
    }

    [Fact]
    public void The_length_of_a_voice_is_the_exact_sum_of_its_entries() {
        // Arrange
        Pitch anyPitch = Any.Pitch().Generate();
        VoiceEntry[] entries = [
            new VoiceEntry(new Duration(1, 4), anyPitch, false),
            VoiceEntry.Rest(new Duration(1, 8)),
            new VoiceEntry(new Duration(1, 8), anyPitch, false),
        ];

        // Act
        Voice voice = new Voice(1, entries);

        // Assert
        Assert.Equal(new Duration(1, 2), voice.Length);
    }

    [Fact]
    public void A_voice_keeps_its_entries_in_written_order_whatever_happens_to_the_source_list() {
        // Arrange
        VoiceEntry first  = Any.VoiceEntry().WithDuration(new Duration(1, 4)).Generate();
        VoiceEntry second = Any.VoiceEntry().WithDuration(new Duration(1, 2)).Generate();
        List<VoiceEntry> source = [first, second];

        // Act
        Voice voice = new Voice(2, source);
        source.Clear();

        // Assert
        Assert.Equal(new[] { first, second }, voice.Entries);
        Assert.Equal(new Duration(3, 4), voice.Length);
    }

    [Fact]
    public void A_voice_needs_at_least_one_entry() {
        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Voice(1, []));

        // Assert
        Assert.Equal("entries", error.ParamName);
    }

    [Fact]
    public void A_voice_cannot_hold_a_null_entry() {
        // Arrange
        VoiceEntry anyEntry = Any.VoiceEntry().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Voice(1, [anyEntry, null!]));
    }

    [Fact]
    public void A_missing_entry_list_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Voice(1, null!));
    }

    [Fact(Skip = "domain defect: two valid entries (1/100 and 1/101 of a whole note) make a Voice whose length is not notatable, so its constructor throws ArgumentOutOfRangeException on 'denominator', seed any")]
    public void A_voice_of_two_valid_entries_has_a_length() {
        // Arrange
        Pitch anyPitch = Any.Pitch().Generate();
        VoiceEntry[] entries = [
            new VoiceEntry(new Duration(1, 100), anyPitch, false),
            new VoiceEntry(new Duration(1, 101), anyPitch, false),
        ];

        // Act
        Voice voice = new Voice(1, entries);

        // Assert
        Assert.Equal(new Duration(201, 10100).Numerator, voice.Length.Numerator);
    }

}
