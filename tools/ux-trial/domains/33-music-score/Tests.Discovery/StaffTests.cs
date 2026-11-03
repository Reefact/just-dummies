using JustDummies;

using MusicScore.Domain;

using Xunit;

using static MusicScore.Tests.Discovery.Scores;

namespace MusicScore.Tests.Discovery;

public sealed class StaffTests {

    [Fact]
    public void A_staff_holds_its_measures_numbered_from_one_in_order() {
        // Arrange
        string anyName = Any.String().Alpha().WithLengthBetween(1, 40).Generate();

        // Act
        Staff staff = new Staff(anyName, [RepeatBar(1), RepeatBar(2), RepeatBar(3)]);

        // Assert
        Assert.Equal(anyName, staff.Name);
        Assert.Equal(new[] { 1, 2, 3 }, staff.Measures.Select(measure => measure.Number));
    }

    [Fact]
    public void A_name_of_forty_characters_is_accepted() {
        // Arrange
        string fortyLetters = Any.String().Alpha().WithLength(Staff.MaxNameLength).Generate();

        // Act
        Staff staff = new Staff(fortyLetters, [RepeatBar(1)]);

        // Assert
        Assert.Equal(40, staff.Name.Length);
    }

    [Fact]
    public void A_name_of_forty_one_characters_is_refused() {
        // Arrange
        string tooLong = Any.String().Alpha().WithLength(Staff.MaxNameLength + 1).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Staff(tooLong, [RepeatBar(1)]));
    }

    [Theory]
    [InlineData(" Violin I")]
    [InlineData("Violin I ")]
    [InlineData("\tViolin I")]
    public void A_name_with_surrounding_blanks_is_refused(string name) {
        Assert.Throws<ArgumentException>(() => new Staff(name, [RepeatBar(1)]));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_name_is_refused(string name) {
        Assert.Throws<ArgumentException>(() => new Staff(name, [RepeatBar(1)]));
    }

    [Fact]
    public void A_missing_name_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Staff(null!, [RepeatBar(1)]));
    }

    [Fact]
    public void A_name_with_inner_blanks_is_fine() {
        Assert.Equal("Piano RH", new Staff("Piano RH", [RepeatBar(1)]).Name);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    public void A_staff_must_begin_at_measure_one(int firstNumber) {
        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Staff("Cello", [RepeatBar(firstNumber)]));

        // Assert
        Assert.Contains("expects measure 1", error.Message);
    }

    [Fact]
    public void A_gap_or_a_repeated_number_among_the_measures_is_refused() {
        // Assert
        Assert.Throws<ArgumentException>(() => new Staff("Cello", [RepeatBar(1), RepeatBar(3)]));
        Assert.Throws<ArgumentException>(() => new Staff("Cello", [RepeatBar(1), RepeatBar(1)]));
        Assert.Throws<ArgumentException>(() => new Staff("Cello", [RepeatBar(2), RepeatBar(1)]));
    }

    [Fact]
    public void A_staff_needs_a_measure_and_no_null_measure() {
        Assert.Throws<ArgumentException>(() => new Staff("Cello", []));
        Assert.Throws<ArgumentException>(() => new Staff("Cello", [RepeatBar(1), null!]));
        Assert.Throws<ArgumentNullException>(() => new Staff("Cello", null!));
    }

}
