using JustDummies;

using Xunit;

namespace ShiftPlanning.Domain.Tests.Regular.Entities;

public sealed class EmployeeTests {

    [Fact]
    public void An_employee_remembers_her_number_her_name_and_her_home_site() {
        // Arrange
        EmployeeId anyId       = new AnyEmployeeId().Generate();
        string     anyName     = Any.String().Alpha().WithLengthBetween(1, 120).Generate();
        Site       anyHomeSite = new AnySite().Generate();

        // Act
        Employee employee = new Employee(anyId, anyName, anyHomeSite);

        // Assert
        Assert.Equal(anyId, employee.Id);
        Assert.Equal(anyName, employee.FullName);
        Assert.Same(anyHomeSite, employee.HomeSite);
    }

    [Fact]
    public void A_name_of_exactly_120_characters_is_accepted() {
        // Arrange
        string anyLongName = Any.String().Alpha().WithLength(120).Generate();

        // Act & Assert
        Assert.Equal(anyLongName, new AnyEmployee().WithFullName(anyLongName).Generate().FullName);
    }

    [Fact]
    public void A_name_of_121_characters_is_refused() {
        // Arrange
        string anyTooLongName = Any.String().Alpha().WithLength(121).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AnyEmployee().WithFullName(anyTooLongName).Generate());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_name_is_refused(string name) {
        Assert.Throws<ArgumentException>(() => new AnyEmployee().WithFullName(name).Generate());
    }

    [Theory]
    [InlineData(" Ada Lovelace")]
    [InlineData("Ada Lovelace ")]
    [InlineData("\tAda Lovelace")]
    [InlineData("Ada Lovelace\n")]
    public void A_name_with_leading_or_trailing_whitespace_is_refused(string name) {
        Assert.Throws<ArgumentException>(() => new AnyEmployee().WithFullName(name).Generate());
    }

    [Fact]
    public void Spaces_inside_a_name_are_fine() {
        // Arrange
        string anyFirstName = Any.String().Alpha().WithLengthBetween(1, 20).Generate();
        string anyLastName  = Any.String().Alpha().WithLengthBetween(1, 20).Generate();
        string fullName     = anyFirstName + " " + anyLastName;

        // Act & Assert
        Assert.Equal(fullName, new AnyEmployee().WithFullName(fullName).Generate().FullName);
    }

    [Fact]
    public void Every_part_of_an_employee_is_required() {
        // Arrange
        EmployeeId anyId       = new AnyEmployeeId().Generate();
        Site       anyHomeSite = new AnySite().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Employee(null!, "Ada", anyHomeSite));
        Assert.ThrowsAny<ArgumentException>(() => new Employee(anyId, null!, anyHomeSite));
        Assert.Throws<ArgumentNullException>(() => new Employee(anyId, "Ada", null!));
    }

}
