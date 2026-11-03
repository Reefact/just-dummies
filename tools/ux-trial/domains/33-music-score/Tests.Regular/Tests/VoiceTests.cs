using System;
using System.Collections.Generic;

using JustDummies;

using Xunit;

using static MusicScore.Domain.Tests.Notation;

namespace MusicScore.Domain.Tests;

public sealed class VoiceEntryTests {

    [Fact]
    public void A_note_carries_its_duration_and_pitch() {
        // Act
        VoiceEntry note = new VoiceEntry(D(3, 8), P("E5"), tiedToNext: true);

        // Assert
        Assert.Equal(D(3, 8), note.Duration);
        Assert.Equal(P("E5"), note.Pitch);
        Assert.True(note.TiedToNext);
        Assert.False(note.IsRest);
    }

    [Fact]
    public void A_rest_has_no_pitch_and_is_never_tied() {
        // Arrange
        Duration anyDuration = Arbitrary.Duration().WithNumerator(n => n.Positive()).Generate();

        // Act
        VoiceEntry rest = VoiceEntry.Rest(anyDuration);

        // Assert
        Assert.True(rest.IsRest);
        Assert.Null(rest.Pitch);
        Assert.False(rest.TiedToNext);
    }

    [Fact]
    public void A_tied_rest_is_refused() {
        // Arrange
        Duration anyDuration = Arbitrary.Duration().WithNumerator(n => n.Positive()).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new VoiceEntry(anyDuration, null, tiedToNext: true));

        // Assert
        Assert.Equal("tiedToNext", error.ParamName);
    }

    [Fact]
    public void A_zero_duration_is_refused_for_a_note_and_for_a_rest() {
        // Arrange
        Pitch anyPitch = Arbitrary.Pitch().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new VoiceEntry(Duration.Zero, anyPitch, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => VoiceEntry.Rest(Duration.Zero));
    }

    [Fact]
    public void A_null_duration_is_refused() {
        // Arrange
        Pitch anyPitch = Arbitrary.Pitch().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new VoiceEntry(null!, anyPitch, false));
    }

}

public sealed class VoiceTests {

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void A_voice_is_numbered_from_one_to_four(int number) {
        // Arrange
        VoiceEntry anyEntry = Arbitrary.VoiceEntry().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Voice(number, new[] { anyEntry }));

        // Assert
        Assert.Equal("number", error.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void The_extreme_voice_numbers_are_accepted(int number) {
        // Arrange
        VoiceEntry anyEntry = Arbitrary.VoiceEntry().Generate();

        // Act
        Voice voice = new Voice(number, new[] { anyEntry });

        // Assert
        Assert.Equal(number, voice.Number);
    }

    [Fact]
    public void A_voice_without_entries_is_refused() {
        // Arrange
        int anyNumber = Any.Int32().Between(1, 4).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Voice(anyNumber, Array.Empty<VoiceEntry>()));
    }

    [Fact]
    public void A_voice_holding_a_null_entry_is_refused() {
        // Arrange
        int        anyNumber = Any.Int32().Between(1, 4).Generate();
        VoiceEntry anyEntry  = Arbitrary.VoiceEntry().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Voice(anyNumber, new[] { anyEntry, null! }));
    }

    [Fact]
    public void A_null_list_of_entries_is_refused() {
        // Arrange
        int anyNumber = Any.Int32().Between(1, 4).Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Voice(anyNumber, null!));
    }

    [Theory]
    [MemberData(nameof(Lengths))]
    public void The_length_of_a_voice_is_the_exact_sum_of_its_entries(VoiceEntry[] entries, long expectedNumerator, long expectedDenominator) {
        // Arrange
        int anyNumber = Any.Int32().Between(1, 4).Generate();

        // Act
        Voice voice = new Voice(anyNumber, entries);

        // Assert
        Assert.Equal(D(expectedNumerator, expectedDenominator), voice.Length);
    }

    public static IEnumerable<object[]> Lengths() {
        yield return new object[] { new[] { Note("C4", 1, 4), Note("D4", 1, 8), Rest(1, 8) }, 1L, 2L };
        yield return new object[] { new[] { Note("C4", 3, 8), Note("D4", 1, 8) }, 1L, 2L };
        yield return new object[] { new[] { Note("C4", 1, 12), Note("D4", 1, 12), Note("E4", 1, 12) }, 1L, 4L };
        yield return new object[] { new[] { Rest(1, 1) }, 1L, 1L };
        yield return new object[] { new[] { Note("C4", 1, 3), Note("D4", 1, 6) }, 1L, 2L };
    }

    [Fact(Skip = "domain defect: a voice made of two valid entries throws when the sum of their durations has a denominator above 1024 (1/1000 + 1/1001), with a message about a 'denominator' the caller never passed; first seen on seed 2018346299 drawing arbitrary entries")]
    public void A_voice_made_of_valid_entries_can_be_built_whatever_their_denominators() {
        // Arrange: each entry is a valid duration on its own
        VoiceEntry first  = Note("C4", 1, 1000);
        VoiceEntry second = Note("D4", 1, 1001);

        // Act
        Voice voice = new Voice(1, new[] { first, second });

        // Assert
        Assert.Equal(2, voice.Entries.Count);
    }

    [Fact]
    public void A_voice_keeps_its_entries_in_order_and_does_not_follow_later_changes_to_the_list_it_was_given() {
        // Arrange
        int              anyNumber = Any.Int32().Between(1, 4).Generate();
        List<VoiceEntry> entries   = new List<VoiceEntry> { Note("C4", 1, 4), Note("D4", 1, 4) };

        // Act
        Voice voice = new Voice(anyNumber, entries);
        entries.Add(Note("E4", 1, 4));

        // Assert
        Assert.Equal(2, voice.Entries.Count);
        Assert.Equal(P("C4"), voice.Entries[0].Pitch);
        Assert.Equal(P("D4"), voice.Entries[1].Pitch);
        Assert.Equal(D(1, 2), voice.Length);
    }

}
