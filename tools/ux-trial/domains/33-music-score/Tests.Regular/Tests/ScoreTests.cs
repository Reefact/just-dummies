using System;

using JustDummies;

using Xunit;

using static MusicScore.Domain.Tests.Notation;

namespace MusicScore.Domain.Tests;

public sealed class ScoreTests {

    [Fact]
    public void A_score_with_one_staff_is_accepted_and_exposes_what_it_was_given() {
        // Arrange
        string anyTitle = Any.String().NotBlank().WithMaxLength(80).Generate();
        Tempo  anyTempo = Arbitrary.Tempo().Generate();
        Staff  staff    = Structure.UnnamedStaff("4/4", "4/4", "3/4");

        // Act
        Score score = new Score(anyTitle, anyTempo, new[] { staff });

        // Assert
        Assert.Equal(anyTitle, score.Title);
        Assert.Same(anyTempo, score.Tempo);
        Assert.Same(staff, Assert.Single(score.Staves));
        Assert.Equal(3, score.MeasureCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void A_blank_title_is_refused(string title) {
        // Assert
        Assert.Throws<ArgumentException>(() => Arbitrary.Score().WithTitle(title).Generate());
    }

    [Fact]
    public void A_null_title_a_null_tempo_and_null_staves_are_refused() {
        // Assert
        Assert.Throws<ArgumentNullException>(() => Arbitrary.Score().WithTitle((string)null!).Generate());
        Assert.Throws<ArgumentNullException>(() => Arbitrary.Score().WithTempo((Tempo)null!).Generate());
        Assert.Throws<ArgumentNullException>(() => Arbitrary.Score().WithStaves((System.Collections.Generic.IReadOnlyList<Staff>)null!).Generate());
    }

    [Fact]
    public void A_score_without_staves_is_refused() {
        // Assert
        Assert.Throws<ArgumentException>(() => Arbitrary.Score().WithStaves(Array.Empty<Staff>()).Generate());
    }

    [Fact]
    public void A_score_holding_a_null_staff_is_refused() {
        // Assert
        Assert.Throws<ArgumentException>(() => Arbitrary.Score().WithStaves(new Staff[] { Structure.UnnamedStaff("4/4"), null! }).Generate());
    }

    [Fact]
    public void Two_staves_cannot_share_a_name() {
        // Arrange
        Staff first  = Structure.StaffOf("Violin I", "4/4");
        Staff second = Structure.StaffOf("Violin I", "4/4");

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => Arbitrary.Score().WithStaves(new[] { first, second }).Generate());

        // Assert
        Assert.Contains("share a name", error.Message);
    }

    [Fact]
    public void Staff_names_are_compared_case_sensitively() {
        // Arrange
        Staff upper = Structure.StaffOf("Violin", "4/4");
        Staff lower = Structure.StaffOf("violin", "4/4");

        // Act
        Score score = Arbitrary.Score().WithStaves(new[] { upper, lower }).Generate();

        // Assert
        Assert.Equal(2, score.Staves.Count);
    }

    [Fact]
    public void Staves_with_different_measure_counts_are_refused_naming_both() {
        // Arrange
        Staff violin = Structure.StaffOf("Violin", "4/4", "4/4", "4/4");
        Staff cello  = Structure.StaffOf("Cello", "4/4", "4/4");

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => Arbitrary.Score().WithStaves(new[] { violin, cello }).Generate());

        // Assert
        Assert.Contains("Cello", error.Message);
        Assert.Contains("Violin", error.Message);
        Assert.Contains("2 measures", error.Message);
        Assert.Contains("3", error.Message);
    }

    [Fact]
    public void Staves_must_agree_on_the_meter_of_every_measure() {
        // Arrange
        Staff violin = Structure.StaffOf("Violin", "4/4", "3/4");
        Staff cello  = Structure.StaffOf("Cello", "4/4", "6/8");

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => Arbitrary.Score().WithStaves(new[] { violin, cello }).Generate());

        // Assert
        Assert.Contains("Measure 2", error.Message);
    }

    [Fact]
    public void Staves_agreeing_on_every_meter_are_accepted_even_when_the_meters_change() {
        // Arrange
        Staff violin = Structure.StaffOf("Violin", "4/4", "3/4", "6/8");
        Staff cello  = Structure.StaffOf("Cello", "4/4", "3/4", "6/8");

        // Act
        Score score = Arbitrary.Score().WithStaves(new[] { violin, cello }).Generate();

        // Assert
        Assert.Equal(2, score.Staves.Count);
    }

    [Theory]
    [InlineData("[4/4", "4/4")]
    [InlineData("4/4]2", "4/4")]
    [InlineData("4/4]2", "4/4]3")]
    public void Staves_must_agree_on_their_repeat_barlines(string violinFirstBar, string celloFirstBar) {
        // Arrange: a closing bar follows so that the opening repeat is closed on the staff that has it
        Staff violin = Structure.StaffOf("Violin", violinFirstBar, "4/4]2");
        Staff cello  = Structure.StaffOf("Cello", celloFirstBar, "4/4]2");

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => Arbitrary.Score().WithStaves(new[] { violin, cello }).Generate());

        // Assert
        Assert.Contains("different repeat barlines", error.Message);
    }

    [Fact]
    public void A_repeat_opened_inside_a_repeat_is_refused() {
        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => Structure.ScoreOf("[4/4", "[4/4", "4/4]2"));

        // Assert
        Assert.Contains("nested", error.Message);
    }

    [Fact]
    public void A_start_repeat_never_closed_is_refused() {
        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => Structure.ScoreOf("4/4", "[4/4", "4/4"));

        // Assert
        Assert.Contains("never closed", error.Message);
    }

    [Fact]
    public void A_start_repeat_closed_in_the_same_measure_is_accepted() {
        // Act
        Score score = Structure.ScoreOf("[4/4]2");

        // Assert
        Assert.Equal(1, score.MeasureCount);
    }

    [Fact]
    public void An_end_repeat_without_a_start_repeat_is_accepted() {
        // Act
        Score score = Structure.ScoreOf("4/4", "4/4]2", "4/4");

        // Assert
        Assert.Equal(3, score.MeasureCount);
    }

    [Fact]
    public void Two_successive_repeats_are_not_nested() {
        // Act
        Score score = Structure.ScoreOf("[4/4]2", "[4/4", "4/4]3");

        // Assert
        Assert.Equal(3, score.MeasureCount);
    }

    [Fact]
    public void A_score_does_not_follow_later_changes_to_the_staff_list_it_was_given() {
        // Arrange
        System.Collections.Generic.List<Staff> staves = new() { Structure.StaffOf("Violin", "4/4") };

        // Act
        Score score = Arbitrary.Score().WithStaves(staves).Generate();
        staves.Add(Structure.StaffOf("Cello", "4/4"));

        // Assert
        Assert.Single(score.Staves);
    }

}
