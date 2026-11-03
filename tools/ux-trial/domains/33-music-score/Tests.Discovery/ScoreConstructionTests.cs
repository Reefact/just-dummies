using JustDummies;

using MusicScore.Domain;

using Xunit;

using static MusicScore.Tests.Discovery.Scores;

namespace MusicScore.Tests.Discovery;

public sealed class ScoreConstructionTests {

    [Fact]
    public void A_score_keeps_its_title_tempo_and_staves() {
        // Arrange
        string anyTitle = Any.String().NotBlank().Generate();
        Tempo  anyTempo = Any.Tempo().Generate();
        Staff  violin   = StaffOf("Violin", RepeatBar(1), RepeatBar(2));
        Staff  cello    = StaffOf("Cello", RepeatBar(1), RepeatBar(2));

        // Act
        Score score = new Score(anyTitle, anyTempo, [violin, cello]);

        // Assert
        Assert.Equal(anyTitle, score.Title);
        Assert.Same(anyTempo, score.Tempo);
        Assert.Equal(new[] { violin, cello }, score.Staves);
        Assert.Equal(2, score.MeasureCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void A_blank_title_is_refused(string title) {
        // Arrange
        Tempo anyTempo = Any.Tempo().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Score(title, anyTempo, [StaffOf("Violin", RepeatBar(1))]));
    }

    [Fact]
    public void A_missing_title_tempo_or_staff_list_is_refused() {
        // Arrange
        string anyTitle = Any.String().NotBlank().Generate();
        Tempo  anyTempo = Any.Tempo().Generate();
        Staff  anyStaff = Any.Staff().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new Score(null!, anyTempo, [anyStaff]));
        Assert.Throws<ArgumentNullException>(() => new Score(anyTitle, null!, [anyStaff]));
        Assert.Throws<ArgumentNullException>(() => new Score(anyTitle, anyTempo, null!));
    }

    [Fact]
    public void A_score_needs_a_staff_and_no_null_staff() {
        // Arrange
        string anyTitle = Any.String().NotBlank().Generate();
        Tempo  anyTempo = Any.Tempo().Generate();
        Staff  anyStaff = Any.Staff().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Score(anyTitle, anyTempo, []));
        Assert.Throws<ArgumentException>(() => new Score(anyTitle, anyTempo, [anyStaff, null!]));
    }

    [Fact]
    public void Two_staves_cannot_share_a_name() {
        // Arrange
        string anyName = Any.String().Alpha().WithLengthBetween(1, 40).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => ScoreOf(Any.Tempo().Generate(), StaffOf(anyName, RepeatBar(1)), StaffOf(anyName, RepeatBar(1))));

        // Assert
        Assert.Equal("staves", error.ParamName);
    }

    [Fact]
    public void Staff_names_that_differ_only_by_case_are_different_names() {
        // Act
        Score score = ScoreOf(Any.Tempo().Generate(), StaffOf("Violin", RepeatBar(1)), StaffOf("violin", RepeatBar(1)));

        // Assert
        Assert.Equal(2, score.Staves.Count);
    }

    [Fact]
    public void Every_staff_must_have_as_many_measures_as_the_first() {
        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => ScoreOf(Any.Tempo().Generate(),
                                                                                  StaffOf("Violin", RepeatBar(1), RepeatBar(2)),
                                                                                  StaffOf("Cello", RepeatBar(1))));

        // Assert
        Assert.Contains("Cello", error.Message);
        Assert.Contains("1 measures", error.Message);
    }

    [Fact]
    public void Staves_must_share_the_meter_of_each_measure() {
        // Arrange
        Measure inFourFour   = RepeatBar(1);
        Measure inThreeFour  = Any.Measure().WithNumber(1).WithTimeSignature(new TimeSignature(3, 4)).WithRepeatStart(false).WithRepeatEndTimes(0).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => ScoreOf(Any.Tempo().Generate(), StaffOf("Violin", inFourFour), StaffOf("Cello", inThreeFour)));

        // Assert
        Assert.Contains("3/4", error.Message);
        Assert.Contains("4/4", error.Message);
    }

    [Fact]
    public void Six_eight_and_three_four_last_the_same_but_are_not_the_same_meter() {
        // Arrange
        Measure inSixEight  = Any.Measure().WithNumber(1).WithTimeSignature(new TimeSignature(6, 8)).WithRepeatStart(false).WithRepeatEndTimes(0).Generate();
        Measure inThreeFour = Any.Measure().WithNumber(1).WithTimeSignature(new TimeSignature(3, 4)).WithRepeatStart(false).WithRepeatEndTimes(0).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => ScoreOf(Any.Tempo().Generate(), StaffOf("Violin", inSixEight), StaffOf("Cello", inThreeFour)));
    }

    [Fact]
    public void Staves_must_share_their_repeat_barlines() {
        // Assert
        Assert.Throws<ArgumentException>(() => ScoreOf(Any.Tempo().Generate(),
                                                       StaffOf("Violin", RepeatBar(1, repeatStart: true), RepeatBar(2, repeatEndTimes: 2)),
                                                       StaffOf("Cello", RepeatBar(1), RepeatBar(2, repeatEndTimes: 2))));
        Assert.Throws<ArgumentException>(() => ScoreOf(Any.Tempo().Generate(),
                                                       StaffOf("Violin", RepeatBar(1, repeatStart: true), RepeatBar(2, repeatEndTimes: 2)),
                                                       StaffOf("Cello", RepeatBar(1, repeatStart: true), RepeatBar(2, repeatEndTimes: 3))));
    }

    [Fact]
    public void A_repeat_cannot_open_inside_a_repeat_that_is_still_open() {
        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => ScoreOf(Any.Tempo().Generate(),
                                                                                 StaffOf("Violin", RepeatBar(1, repeatStart: true), RepeatBar(2, repeatStart: true), RepeatBar(3, repeatEndTimes: 2))));

        // Assert
        Assert.Contains("Measure 2", error.Message);
    }

    [Fact]
    public void Every_start_repeat_must_be_closed() {
        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => ScoreOf(Any.Tempo().Generate(), StaffOf("Violin", RepeatBar(1), RepeatBar(2, repeatStart: true), RepeatBar(3))));

        // Assert
        Assert.Contains("never closed", error.Message);
    }

    [Fact]
    public void A_start_repeat_closed_in_its_own_measure_is_accepted() {
        // Act
        Score score = ScoreOf(Any.Tempo().Generate(), StaffOf("Violin", RepeatBar(1, repeatStart: true, repeatEndTimes: 3)));

        // Assert
        Assert.Equal(1, score.MeasureCount);
    }

    [Fact]
    public void An_end_repeat_needs_no_start_repeat() {
        // Act
        Score score = ScoreOf(Any.Tempo().Generate(), StaffOf("Violin", RepeatBar(1), RepeatBar(2, repeatEndTimes: 2)));

        // Assert
        Assert.Equal(2, score.MeasureCount);
    }

    [Fact]
    public void A_second_repeat_may_open_once_the_first_is_closed() {
        // Act
        Score score = ScoreOf(Any.Tempo().Generate(),
                              StaffOf("Violin", RepeatBar(1, repeatStart: true), RepeatBar(2, repeatEndTimes: 2), RepeatBar(3, repeatStart: true), RepeatBar(4, repeatEndTimes: 2)));

        // Assert
        Assert.Equal(4, score.MeasureCount);
    }

}
