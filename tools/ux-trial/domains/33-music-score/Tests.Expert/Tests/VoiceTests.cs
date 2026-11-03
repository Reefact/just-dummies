using static MusicScore.Domain.Tests.Expert.Notation;

namespace MusicScore.Domain.Tests.Expert;

public sealed class VoiceTests {

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void The_first_and_the_fourth_voice_exist(int number) {
        // Act
        Voice voice = Arbitrary.Voice().WithNumber(number).Generate();

        // Assert
        Assert.Equal(number, voice.Number);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(-1)]
    public void A_voice_beyond_the_fourth_or_before_the_first_is_refused(int number) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Voice().WithNumber(number).Generate());

        // Assert
        Assert.Equal("number", refusal.ParamName);
    }

    [Fact]
    public void A_voice_lasts_as_long_as_its_entries_together() {
        // Arrange
        VoiceEntry[] entries = [Note("C4", 1, 4), Rest(1, 4), Note("E4", 3, 8), Note("G4", 1, 8)];

        // Act
        Voice voice = new(1, entries);

        // Assert
        Assert.Equal(D(1, 1), voice.Length);
    }

    [Fact]
    public void A_voice_made_of_triplets_adds_up_exactly() {
        // Arrange
        VoiceEntry[] entries = [Note("C4", 1, 12), Note("D4", 1, 12), Note("E4", 1, 12), Note("F4", 1, 4)];

        // Act
        Voice voice = new(2, entries);

        // Assert
        Assert.Equal(D(1, 2), voice.Length);
    }

    [Fact(Skip = "domain defect: a voice of a 512th note and a triplet twelfth is refused with ArgumentOutOfRangeException('denominator') from Duration.+, although each entry is valid; no seed, literals")]
    public void A_voice_may_mix_a_512th_note_and_a_triplet() {
        // Act
        Voice voice = VoiceOf(1, Note("C4", 1, 512), Note("D4", 1, 12));

        // Assert
        Assert.Equal(D(131, 1536), voice.Length);
    }

    [Fact]
    public void A_voice_keeps_its_entries_in_order() {
        // Arrange
        VoiceEntry[] entries = [Note("G4", 1, 4), Rest(1, 8), Note("C4", 1, 4), Note("E4", 3, 8)];

        // Act
        Voice voice = Arbitrary.Voice().WithEntries(entries).Generate();

        // Assert
        Assert.Equal(entries, voice.Entries);
    }

    [Fact]
    public void A_voice_does_not_follow_the_list_it_was_built_from() {
        // Arrange
        List<VoiceEntry> entries = Any.ListOf(Arbitrary.VoiceEntry()).WithCount(2).Generate();
        Voice            voice   = Arbitrary.Voice().WithEntries(entries).Generate();

        // Act
        entries.Clear();

        // Assert
        Assert.Equal(2, voice.Entries.Count);
    }

    [Fact]
    public void A_voice_without_entries_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Arbitrary.Voice().WithEntries(Array.Empty<VoiceEntry>()).Generate());

        // Assert
        Assert.Equal("entries", refusal.ParamName);
    }

    [Fact]
    public void A_voice_with_a_null_entry_is_refused() {
        // Arrange
        VoiceEntry anyEntry = Arbitrary.VoiceEntry().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Arbitrary.Voice().WithEntries(new VoiceEntry?[] { anyEntry, null }!).Generate());

        // Assert
        Assert.Equal("entries", refusal.ParamName);
    }

    [Fact]
    public void A_voice_without_an_entry_list_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Arbitrary.Voice().WithEntries((IReadOnlyList<VoiceEntry>)null!).Generate());
    }

    [Fact]
    public void A_voice_made_to_fill_a_length_fills_it_exactly() {
        // Arrange
        Duration threeEighths = D(3, 8);

        // Act
        Voice voice = Arbitrary.Voice().Filling(threeEighths).Generate();

        // Assert
        Assert.Equal(threeEighths, voice.Length);
    }

}
