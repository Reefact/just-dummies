using Insurance.Domain;

using JustDummies;

using Xunit;

namespace Insurance.Tests.Regular;

public sealed class DriverTests {

    [Fact]
    public void A_driver_keeps_its_birth_date_and_years_licensed() {
        // Arrange
        DateOnly anyBirthDate     = Any.DateOnly().Generate();
        int      anyYearsLicensed = Any.Int32().Between(0, 80).Generate();
        string   anyFullName      = Any.String().NotBlank().Generate();

        // Act
        Driver driver = new Driver(anyFullName, anyBirthDate, anyYearsLicensed);

        // Assert
        Assert.Equal(anyBirthDate, driver.BirthDate);
        Assert.Equal(anyYearsLicensed, driver.YearsLicensed);
    }

    [Fact]
    public void The_full_name_is_stored_trimmed() {
        // Arrange
        string anyName = Any.String().Alpha().WithLengthBetween(1, 30).Generate();

        // Act
        Driver driver = Any.Driver().WithFullName("  " + anyName + "\t ").Generate();

        // Assert
        Assert.Equal(anyName, driver.FullName);
    }

    [Fact]
    public void A_null_name_is_refused_as_null() {
        Assert.Throws<ArgumentNullException>(() => new Driver(null!, Any.DateOnly().Generate(), 5));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" \t\r\n ")]
    public void A_blank_name_is_refused(string blank) {
        Assert.Throws<ArgumentException>(() => new Driver(blank, Any.DateOnly().Generate(), 5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(80)]
    public void The_ends_of_the_licence_range_are_accepted(int yearsLicensed) {
        Assert.Equal(yearsLicensed, Any.Driver().WithYearsLicensed(yearsLicensed).Generate().YearsLicensed);
    }

    [Fact]
    public void Negative_years_licensed_are_out_of_range() {
        int anyNegative = Any.Int32().Negative().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Driver("Alex Martin", Any.DateOnly().Generate(), anyNegative));
    }

    [Fact]
    public void More_than_eighty_years_licensed_are_out_of_range() {
        int anyTooMany = Any.Int32().GreaterThan(80).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Driver("Alex Martin", Any.DateOnly().Generate(), anyTooMany));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(81)]
    public void The_values_just_outside_the_licence_range_are_out_of_range(int yearsLicensed) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Driver("Alex Martin", Any.DateOnly().Generate(), yearsLicensed));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(80, false)]
    public void A_driver_is_young_below_three_years_of_licence(int yearsLicensed, bool expected) {
        Driver driver = Any.Driver().WithYearsLicensed(yearsLicensed).Generate();

        Assert.Equal(expected, driver.IsYoungDriver);
    }

    [Fact]
    public void Any_driver_licensed_less_than_three_years_is_young() {
        Driver anyNewcomer = Any.Driver().WithYearsLicensed(y => y.Between(0, 2)).Generate();

        Assert.True(anyNewcomer.IsYoungDriver);
    }

    [Fact]
    public void Any_driver_licensed_three_years_or_more_is_not_young() {
        Driver anyExperienced = Any.Driver().WithYearsLicensed(y => y.Between(3, 80)).Generate();

        Assert.False(anyExperienced.IsYoungDriver);
    }

    [Fact]
    public void The_birth_date_is_not_related_to_the_years_licensed() {
        Driver oddDriver = new Driver("Alex Martin", DateOnly.MaxValue, 80);

        Assert.Equal(DateOnly.MaxValue, oddDriver.BirthDate);
        Assert.Equal(80, oddDriver.YearsLicensed);
    }

}
