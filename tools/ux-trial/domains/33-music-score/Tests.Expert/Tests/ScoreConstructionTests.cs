using static MusicScore.Domain.Tests.Expert.Notation;

namespace MusicScore.Domain.Tests.Expert;

public sealed class ScoreConstructionTests {

    [Fact]
    public void A_score_exposes_its_title_its_tempo_and_its_staves() {
        // Arrange
        Staff violin = StaffOf("Violin I", Bars(3, FourFour).ToArray());
        Staff cello  = StaffOf("Cello", Bars(3, FourFour).ToArray());

        // Act
        Score score = Arbitrary.Score().WithTitle("Canon in D").WithTempo(QuarterEquals60).WithStaves([violin, cello]).Generate();

        // Assert
        Assert.Equal("Canon in D", score.Title);
        Assert.Same(QuarterEquals60, score.Tempo);
        Assert.Equal([violin, cello], score.Staves);
        Assert.Equal(3, score.MeasureCount);
    }

    [Fact]
    public void A_score_does_not_follow_the_list_of_staves_it_was_built_from() {
        // Arrange
        List<Staff> staves = [StaffOf("Violin I", Bars(2, FourFour).ToArray())];
        Score       score  = Arbitrary.Score().WithStaves(staves).Generate();

        // Act
        staves.Clear();

        // Assert
        Assert.Single(score.Staves);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void A_blank_title_is_refused(string title) {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => Arbitrary.Score().WithTitle(title).Generate());
    }

    [Fact]
    public void A_missing_title_is_refused() {
        Assert.ThrowsAny<ArgumentException>(() => Arbitrary.Score().WithTitle((string)null!).Generate());
    }

    [Fact]
    public void A_missing_tempo_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Arbitrary.Score().WithTempo((Tempo)null!).Generate());
    }

    [Fact]
    public void A_score_without_staves_is_refused() {
        Assert.Throws<ArgumentException>(() => Arbitrary.Score().WithStaves(Array.Empty<Staff>()).Generate());
    }

    [Fact]
    public void A_score_with_a_null_staff_is_refused() {
        // Arrange
        Staff anyStaff = Arbitrary.Staff().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Arbitrary.Score().WithStaves(new Staff?[] { anyStaff, null }!).Generate());
    }

    [Fact]
    public void A_score_without_a_staff_list_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Arbitrary.Score().WithStaves((IReadOnlyList<Staff>)null!).Generate());
    }

    [Fact]
    public void Two_staves_cannot_share_a_name() {
        // Arrange
        Staff first  = StaffOf("Violin", Bars(2, FourFour).ToArray());
        Staff second = StaffOf("Violin", Bars(2, FourFour).ToArray());

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Arbitrary.Score().WithStaves([first, second]).Generate());

        // Assert
        Assert.Equal("staves", refusal.ParamName);
        Assert.Contains("share a name", refusal.Message);
    }

    [Fact]
    public void Staff_names_differing_only_by_case_are_different_names() {
        // Arrange
        Staff upper = StaffOf("Violin", Bars(2, FourFour).ToArray());
        Staff lower = StaffOf("violin", Bars(2, FourFour).ToArray());

        // Act
        Score score = Arbitrary.Score().WithStaves([upper, lower]).Generate();

        // Assert
        Assert.Equal(2, score.Staves.Count);
    }

    [Fact]
    public void Staves_with_a_different_number_of_measures_are_refused() {
        // Arrange
        Staff twoMeasures   = StaffOf("Violin", Bars(2, FourFour).ToArray());
        Staff threeMeasures = StaffOf("Cello", Bars(3, FourFour).ToArray());

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Arbitrary.Score().WithStaves([twoMeasures, threeMeasures]).Generate());

        // Assert
        Assert.Contains("Cello", refusal.Message);
        Assert.Contains("3 measures", refusal.Message);
        Assert.Contains("2", refusal.Message);
    }

    [Fact]
    public void Staves_in_different_meters_in_the_same_measure_are_refused() {
        // Arrange
        Staff inFour  = StaffOf("Violin", Bar(1, FourFour).Generate(), Bar(2, FourFour).Generate());
        Staff inThree = StaffOf("Cello", Bar(1, FourFour).Generate(), Bar(2, ThreeFour).Generate());

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Arbitrary.Score().WithStaves([inFour, inThree]).Generate());

        // Assert
        Assert.Contains("Measure 2", refusal.Message);
        Assert.Contains("3/4", refusal.Message);
        Assert.Contains("4/4", refusal.Message);
    }

    [Theory]
    [InlineData(2, 3)]
    [InlineData(2, 0)]
    public void Staves_closing_a_repeat_a_different_number_of_times_are_refused(int violinTimes, int celloTimes) {
        // Arrange
        Staff violin = StaffOf("Violin", Bars(1, FourFour).Append(RepeatingBar(2, endTimes: violinTimes)).ToArray());
        Staff cello  = StaffOf("Cello", Bars(1, FourFour).Append(RepeatingBar(2, endTimes: celloTimes)).ToArray());

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Arbitrary.Score().WithStaves([violin, cello]).Generate());

        // Assert
        Assert.Contains("repeat barlines", refusal.Message);
    }

    [Fact]
    public void Staves_opening_a_repeat_in_different_measures_are_refused() {
        // Arrange
        Staff violin = StaffOf("Violin", RepeatingBar(1, start: true), RepeatingBar(2, endTimes: 2));
        Staff cello  = StaffOf("Cello", RepeatingBar(1), RepeatingBar(2, start: true, endTimes: 2));

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Arbitrary.Score().WithStaves([violin, cello]).Generate());
    }

    [Fact]
    public void Staves_may_use_different_voices_in_the_same_measure() {
        // Arrange
        Staff onlyVoiceOne = StaffOf("Violin", Bar(1, FourFour, VoiceOf(1, Note("C5", 1, 1))).Generate());
        Staff voicesTwoAndThree = StaffOf("Cello", Bar(1, FourFour, VoiceOf(2, Note("C3", 1, 1)), VoiceOf(3, Note("E3", 1, 1))).Generate());

        // Act
        Score score = Arbitrary.Score().WithStaves([onlyVoiceOne, voicesTwoAndThree]).Generate();

        // Assert
        Assert.Equal(2, score.Staves.Count);
    }

    [Fact]
    public void A_repeat_cannot_start_inside_an_open_repeat() {
        // Arrange
        Staff staff = StaffOf("Violin", RepeatingBar(1, start: true), RepeatingBar(2, start: true), RepeatingBar(3, endTimes: 2));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Arbitrary.Score().WithStaves([staff]).Generate());

        // Assert
        Assert.Contains("Measure 2", refusal.Message);
        Assert.Contains("nested", refusal.Message);
    }

    [Fact]
    public void A_start_repeat_must_be_closed() {
        // Arrange
        Staff staff = StaffOf("Violin", RepeatingBar(1), RepeatingBar(2, start: true), RepeatingBar(3));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Arbitrary.Score().WithStaves([staff]).Generate());

        // Assert
        Assert.Contains("never closed", refusal.Message);
    }

    [Fact]
    public void A_start_repeat_closed_by_a_later_start_repeat_is_still_unclosed() {
        // Arrange
        Staff staff = StaffOf("Violin", RepeatingBar(1, start: true), RepeatingBar(2, endTimes: 2), RepeatingBar(3, start: true));

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Arbitrary.Score().WithStaves([staff]).Generate());
    }

    [Fact]
    public void An_end_repeat_without_a_start_repeat_is_accepted() {
        // Arrange
        Staff staff = StaffOf("Violin", RepeatingBar(1), RepeatingBar(2, endTimes: 2));

        // Act
        Score score = Arbitrary.Score().WithStaves([staff]).Generate();

        // Assert
        Assert.Equal(2, score.MeasureCount);
    }

    [Fact]
    public void A_single_measure_can_be_repeated_on_its_own() {
        // Arrange
        Staff staff = StaffOf("Violin", RepeatingBar(1, start: true, endTimes: 2));

        // Act
        Score score = Arbitrary.Score().WithStaves([staff]).Generate();

        // Assert
        Assert.Single(score.Staves[0].Measures);
    }

    [Fact]
    public void Successive_repeats_are_not_nested() {
        // Arrange
        Staff staff = StaffOf("Violin", RepeatingBar(1, start: true), RepeatingBar(2, endTimes: 2), RepeatingBar(3, start: true), RepeatingBar(4, endTimes: 3));

        // Act
        Score score = Arbitrary.Score().WithStaves([staff]).Generate();

        // Assert
        Assert.Equal(4, score.MeasureCount);
    }

    private static Measure RepeatingBar(int number, bool start = false, int endTimes = 0) {
        return Arbitrary.Measure().WithNumber(number).WithTimeSignature(FourFour).WithRepeatStart(start).WithRepeatEndTimes(endTimes).Generate();
    }

}
