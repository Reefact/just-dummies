namespace ShiftPlanning.Domain.Tests.Expert.Entities;

public sealed class EmployeeTests {

    [Fact]
    public void An_employee_keeps_what_it_was_given() {
        // Arrange
        EmployeeId anyId       = Any.EmployeeId().Generate();
        Site       anyHomeSite = Any.Site().Generate();

        // Act
        Employee employee = new Employee(anyId, "Camille Durand", anyHomeSite);

        // Assert
        Assert.Same(anyId, employee.Id);
        Assert.Same(anyHomeSite, employee.HomeSite);
        Assert.Equal("Camille Durand", employee.FullName);
    }

    [Fact]
    public void A_full_name_of_one_hundred_and_twenty_characters_is_the_longest_allowed() {
        // Arrange
        string longestName = new string('n', 120);

        // Act
        Employee employee = Any.Employee().WithFullName(longestName).Generate();

        // Assert
        Assert.Equal(120, employee.FullName.Length);
    }

    [Fact]
    public void A_full_name_of_more_than_one_hundred_and_twenty_characters_is_refused() {
        // Arrange
        string tooLongName = new string('n', 121);

        // Act
        Action construct = () => Any.Employee().WithFullName(tooLongName).Generate();

        // Assert
        Assert.Throws<ArgumentException>("fullName", construct);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_full_name_is_refused(string blank) {
        // Act
        Action construct = () => Any.Employee().WithFullName(blank).Generate();

        // Assert
        Assert.Throws<ArgumentException>("fullName", construct);
    }

    [Fact]
    public void A_missing_full_name_is_refused() {
        // Act
        Action construct = () => Any.Employee().WithFullName((string)null!).Generate();

        // Assert
        Assert.Throws<ArgumentNullException>("fullName", construct);
    }

    [Theory]
    [InlineData(" Camille")]
    [InlineData("Camille ")]
    [InlineData("\tCamille")]
    [InlineData("Camille\n")]
    [InlineData(" Camille")]   // a no-break space is whitespace for Trim
    public void A_full_name_with_leading_or_trailing_whitespace_is_refused(string untrimmed) {
        // Act
        Action construct = () => Any.Employee().WithFullName(untrimmed).Generate();

        // Assert
        Assert.Throws<ArgumentException>("fullName", construct);
    }

    [Theory]
    [InlineData("Camille Durand")]
    [InlineData("Jean-Pierre de La Fontaine")]
    [InlineData("C")]
    public void Spaces_inside_a_full_name_are_fine(string name) {
        // Act
        Employee employee = Any.Employee().WithFullName(name).Generate();

        // Assert
        Assert.Equal(name, employee.FullName);
    }

    [Fact]
    public void A_missing_id_or_home_site_is_refused() {
        // Arrange
        EmployeeId anyId       = Any.EmployeeId().Generate();
        Site       anyHomeSite = Any.Site().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>("id", () => new Employee(null!, "Camille Durand", anyHomeSite));
        Assert.Throws<ArgumentNullException>("homeSite", () => new Employee(anyId, "Camille Durand", null!));
    }

}
