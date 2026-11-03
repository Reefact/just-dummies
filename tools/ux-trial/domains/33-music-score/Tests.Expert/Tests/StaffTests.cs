using static MusicScore.Domain.Tests.Expert.Notation;

namespace MusicScore.Domain.Tests.Expert;

public sealed class StaffTests {

    [Theory]
    [InlineData("V")]
    [InlineData("Violin I")]
    [InlineData("Piano RH")]
    [InlineData("Flûte à bec")]
    public void A_name_is_kept_as_written(string name) {
        // Act
        Staff staff = Arbitrary.Staff().WithName(name).Generate();

        // Assert
        Assert.Equal(name, staff.Name);
    }

    [Fact]
    public void A_name_of_forty_characters_is_the_longest() {
        // Arrange
        string fortyCharacters = new('x', 40);

        // Act
        Staff staff = Arbitrary.Staff().WithName(fortyCharacters).Generate();

        // Assert
        Assert.Equal(fortyCharacters, staff.Name);
    }

    [Fact]
    public void A_name_of_forty_one_characters_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Arbitrary.Staff().WithName(new string('x', 41)).Generate());

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Theory]
    [InlineData(" Violin")]
    [InlineData("Violin ")]
    [InlineData("\tViolin")]
    [InlineData("Violin\n")]
    public void A_name_with_a_blank_at_either_end_is_refused(string name) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Arbitrary.Staff().WithName(name).Generate());

        // Assert
        Assert.Equal("name", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t")]
    public void A_blank_name_is_refused(string name) {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Arbitrary.Staff().WithName(name).Generate());
    }

    [Fact]
    public void A_missing_name_is_refused() {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => Arbitrary.Staff().WithName((string)null!).Generate());
    }

    [Fact]
    public void A_staff_keeps_its_consecutive_measures() {
        // Arrange
        List<Measure> bars = Bars(3, ThreeFour);

        // Act
        Staff staff = Arbitrary.Staff().WithMeasures(bars).Generate();

        // Assert
        Assert.Equal(bars, staff.Measures);
    }

    [Fact]
    public void A_staff_does_not_follow_the_list_it_was_built_from() {
        // Arrange
        List<Measure> bars  = Bars(2, FourFour);
        Staff         staff = Arbitrary.Staff().WithMeasures(bars).Generate();

        // Act
        bars.RemoveAt(1);

        // Assert
        Assert.Equal(2, staff.Measures.Count);
    }

    [Fact]
    public void Measures_that_do_not_start_at_one_are_refused() {
        // Arrange
        Measure second = Arbitrary.Measure().WithNumber(2).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Arbitrary.Staff().WithMeasures([second]).Generate());

        // Assert
        Assert.Equal("measures", refusal.ParamName);
        Assert.Contains("expects measure 1", refusal.Message);
    }

    [Fact]
    public void A_gap_in_the_numbering_is_refused() {
        // Arrange
        Measure first = Arbitrary.Measure().WithNumber(1).Generate();
        Measure third = Arbitrary.Measure().WithNumber(3).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Arbitrary.Staff().WithMeasures([first, third]).Generate());

        // Assert
        Assert.Contains("expects measure 2", refusal.Message);
        Assert.Contains("found measure 3", refusal.Message);
    }

    [Fact]
    public void A_repeated_number_is_refused() {
        // Arrange
        Measure first = Arbitrary.Measure().WithNumber(1).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Arbitrary.Staff().WithMeasures([first, first]).Generate());
    }

    [Fact]
    public void Measures_out_of_order_are_refused() {
        // Arrange
        Measure first  = Arbitrary.Measure().WithNumber(1).Generate();
        Measure second = Arbitrary.Measure().WithNumber(2).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Arbitrary.Staff().WithMeasures([second, first]).Generate());
    }

    [Fact]
    public void A_staff_without_measures_is_refused() {
        Assert.Throws<ArgumentException>(() => Arbitrary.Staff().WithMeasures(Array.Empty<Measure>()).Generate());
    }

    [Fact]
    public void A_staff_with_a_null_measure_is_refused() {
        // Arrange
        Measure first = Arbitrary.Measure().WithNumber(1).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Arbitrary.Staff().WithMeasures(new Measure?[] { first, null }!).Generate());
    }

    [Fact]
    public void A_staff_without_a_measure_list_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Arbitrary.Staff().WithMeasures((IReadOnlyList<Measure>)null!).Generate());
    }

}
