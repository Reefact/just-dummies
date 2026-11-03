using JustDummies;

using Xunit;

namespace Insurance.Domain.Tests;

public sealed class DriverTests {

    [Fact]
    public void A_driver_keeps_what_it_was_given() {
        // Act
        Driver driver = new Driver("Jane Doe", new DateOnly(1990, 5, 17), 12);

        // Assert
        Assert.Equal("Jane Doe", driver.FullName);
        Assert.Equal(new DateOnly(1990, 5, 17), driver.BirthDate);
        Assert.Equal(12, driver.YearsLicensed);
    }

    [Theory]
    [InlineData("  Jane Doe",   "Jane Doe")]
    [InlineData("Jane Doe  ",   "Jane Doe")]
    [InlineData("\tJane Doe\n", "Jane Doe")]
    [InlineData("Jane  Doe",    "Jane  Doe")]   // only the ends are trimmed
    public void The_full_name_is_stored_without_the_whitespace_around_it(string given, string stored) {
        // Act
        Driver driver = Any.Driver().WithFullName(given).Generate();

        // Assert
        Assert.Equal(stored, driver.FullName);
    }

    [Fact]
    public void A_null_full_name_is_refused_as_a_null_argument() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => Any.Driver().WithFullName((string)null!).Generate());

        // Assert
        Assert.Equal("fullName", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t\r\n ")]
    public void A_blank_full_name_is_refused_as_a_plain_argument_error(string blank) {
        // Act + Assert (the exact type: null has its own refusal)
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Driver().WithFullName(blank).Generate());
        Assert.Equal("fullName", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]    // a driver who just passed the exam
    [InlineData(1)]
    [InlineData(80)]   // the ceiling is accepted
    public void Years_licensed_between_zero_and_eighty_are_accepted_ends_included(int years) {
        // Act
        Driver driver = Any.Driver().WithYearsLicensed(years).Generate();

        // Assert
        Assert.Equal(years, driver.YearsLicensed);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(81)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void Years_licensed_outside_zero_and_eighty_are_out_of_range(int years) {
        // Act + Assert
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Driver().WithYearsLicensed(years).Generate());
        Assert.Equal("yearsLicensed", refusal.ParamName);
    }

    [Theory]
    [InlineData(0,  true)]
    [InlineData(2,  true)]
    [InlineData(3,  false)]   // three years is no longer young
    [InlineData(4,  false)]
    [InlineData(80, false)]
    public void A_driver_is_young_below_three_years_of_licence(int years, bool expected) {
        // Arrange
        Driver driver = Any.Driver().WithYearsLicensed(years).Generate();

        // Act
        bool young = driver.IsYoungDriver;

        // Assert
        Assert.Equal(expected, young);
    }

    [Fact]
    public void The_birth_date_is_not_related_to_the_years_licensed() {
        // Arrange: eighty years of licence, born last year, accepted all the same
        DateOnly birthDate = new DateOnly(2025, 6, 1);

        // Act
        Driver driver = Any.Driver().WithBirthDate(birthDate).WithYearsLicensed(80).Generate();

        // Assert
        Assert.Equal(birthDate, driver.BirthDate);
        Assert.Equal(80, driver.YearsLicensed);
    }

}
