using JustDummies;

using Xunit;

namespace Insurance.Domain.Tests.Discovery;

public sealed class DriverTests {

    [Fact]
    public void A_driver_keeps_its_birth_date_and_years_of_licence() {
        // Arrange
        string anyName = Any.String().NotBlank().Generate();

        // Act
        Driver driver = new Driver(anyName, new DateOnly(1990, 5, 17), 12);

        // Assert
        Assert.Equal(new DateOnly(1990, 5, 17), driver.BirthDate);
        Assert.Equal(12, driver.YearsLicensed);
    }

    [Fact]
    public void The_full_name_is_stored_trimmed() {
        // Arrange
        DateOnly anyBirthDate = Any.DateOnly().Generate();

        // Act
        Driver driver = new Driver("  Jeanne Dupont \t", anyBirthDate, 10);

        // Assert
        Assert.Equal("Jeanne Dupont", driver.FullName);
    }

    [Fact]
    public void A_null_name_is_refused_as_null() {
        // Arrange
        DateOnly anyBirthDate = Any.DateOnly().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>("fullName", () => new Driver(null!, anyBirthDate, 10));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void A_blank_name_is_refused(string blank) {
        // Arrange
        DateOnly anyBirthDate = Any.DateOnly().Generate();

        // Assert
        Assert.Throws<ArgumentException>("fullName", () => new Driver(blank, anyBirthDate, 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(80)]
    public void The_bounds_of_the_years_of_licence_are_accepted(int years) {
        // Act
        Driver driver = new AnyDriver().WithYearsLicensed(years).Generate();

        // Assert
        Assert.Equal(years, driver.YearsLicensed);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(81)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void Years_of_licence_outside_zero_to_eighty_are_refused(int years) {
        // Arrange
        string   anyName      = Any.String().NotBlank().Generate();
        DateOnly anyBirthDate = Any.DateOnly().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("yearsLicensed", () => new Driver(anyName, anyBirthDate, years));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(80, false)]
    public void A_driver_is_young_for_the_first_three_years_of_licence(int years, bool expected) {
        // Arrange
        Driver driver = new AnyDriver().WithYearsLicensed(years).Generate();

        // Assert
        Assert.Equal(expected, driver.IsYoungDriver);
    }

    [Fact]
    public void The_birth_date_is_not_related_to_the_years_of_licence() {
        // Arrange: licensed for 80 years yet born yesterday, and the domain does not mind
        string   anyName = Any.String().NotBlank().Generate();
        DateOnly today   = DateOnly.FromDateTime(DateTime.Today);

        // Act
        Driver driver = new Driver(anyName, today, 80);

        // Assert
        Assert.Equal(80, driver.YearsLicensed);
    }

}
