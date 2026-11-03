using System;
using System.Linq;

using JustDummies;

using Xunit;

using static MusicScore.Domain.Tests.Notation;

namespace MusicScore.Domain.Tests;

public sealed class StaffTests {

    private static Measure AnyBar(int number) { return Arbitrary.Measure().WithNumber(number).Generate(); }

    [Theory]
    [InlineData("Violin I")]
    [InlineData("Piano RH")]
    [InlineData("A")]
    [InlineData("Contrebasse à cordes")]
    public void A_staff_keeps_its_name(string name) {
        // Act
        Staff staff = Arbitrary.Staff().WithName(name).Generate();

        // Assert
        Assert.Equal(name, staff.Name);
    }

    [Fact]
    public void A_name_of_forty_characters_is_accepted_and_forty_one_is_refused() {
        // Assert
        Assert.Equal(40, Arbitrary.Staff().WithName(new string('x', 40)).Generate().Name.Length);
        Assert.Throws<ArgumentException>(() => Arbitrary.Staff().WithName(new string('x', 41)).Generate());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void A_blank_name_is_refused(string name) {
        // Assert
        Assert.Throws<ArgumentException>(() => Arbitrary.Staff().WithName(name).Generate());
    }

    [Theory]
    [InlineData(" Violin")]
    [InlineData("Violin ")]
    [InlineData("\tViolin")]
    [InlineData("Violin\n")]
    public void A_name_with_surrounding_blanks_is_refused(string name) {
        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => Arbitrary.Staff().WithName(name).Generate());

        // Assert
        Assert.Equal("name", error.ParamName);
    }

    [Fact]
    public void A_null_name_is_refused() {
        // Assert
        Assert.Throws<ArgumentNullException>(() => Arbitrary.Staff().WithName((string)null!).Generate());
    }

    [Fact]
    public void Measures_numbered_from_one_in_order_are_accepted() {
        // Act
        Staff staff = StaffOf(AnyBar(1), AnyBar(2), AnyBar(3));

        // Assert
        Assert.Equal(new[] { 1, 2, 3 }, staff.Measures.Select(measure => measure.Number));
    }

    [Fact]
    public void A_staff_starting_at_measure_two_is_refused_naming_what_it_found() {
        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => StaffOf(AnyBar(2), AnyBar(3)));

        // Assert
        Assert.Contains("expects measure 1 at position 1 but found measure 2", error.Message);
    }

    [Fact]
    public void A_gap_in_the_numbering_is_refused() {
        // Assert
        Assert.Throws<ArgumentException>(() => StaffOf(AnyBar(1), AnyBar(3)));
    }

    [Fact]
    public void Measures_out_of_order_are_refused() {
        // Assert
        Assert.Throws<ArgumentException>(() => StaffOf(AnyBar(2), AnyBar(1)));
    }

    [Fact]
    public void The_same_measure_number_twice_is_refused() {
        // Assert
        Assert.Throws<ArgumentException>(() => StaffOf(AnyBar(1), AnyBar(1)));
    }

    [Fact]
    public void A_staff_without_measures_is_refused() {
        // Assert
        Assert.Throws<ArgumentException>(() => StaffOf(Array.Empty<Measure>()));
    }

    [Fact]
    public void A_staff_holding_a_null_measure_is_refused() {
        // Assert
        Assert.Throws<ArgumentException>(() => StaffOf(AnyBar(1), null!));
    }

    [Fact]
    public void A_null_list_of_measures_is_refused() {
        // Assert
        Assert.Throws<ArgumentNullException>(() => new Staff("Cello", null!));
    }

}
