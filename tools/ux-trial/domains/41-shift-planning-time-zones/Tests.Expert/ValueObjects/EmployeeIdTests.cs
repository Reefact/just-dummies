namespace ShiftPlanning.Domain.Tests.Expert.ValueObjects;

public sealed class EmployeeIdTests {

    [Theory]
    [InlineData("E004217")]
    [InlineData("E000001")]   // the lowest number
    [InlineData("E999999")]   // the highest
    public void A_personnel_number_is_an_E_followed_by_six_digits(string number) {
        // Act
        EmployeeId employeeId = new EmployeeId(number);

        // Assert
        Assert.Equal(number, employeeId.Value);
        Assert.Equal(number, employeeId.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("E")]
    [InlineData("E00421")]       // five digits
    [InlineData("E0042170")]     // seven digits
    [InlineData("e004217")]      // lower case
    [InlineData("X004217")]
    [InlineData("004217E")]
    [InlineData("E00421A")]
    [InlineData("E00 217")]
    [InlineData("E-04217")]
    [InlineData("E００４２１７")]   // full-width digits are not ASCII digits
    [InlineData("E٠٠٤٢١٧")]       // Arabic-Indic digits neither
    public void A_malformed_personnel_number_is_a_format_error(string malformed) {
        // Act
        Action construct = () => new EmployeeId(malformed);

        // Assert
        Assert.Throws<FormatException>(construct);
    }

    [Fact]
    public void Personnel_number_zero_is_refused_as_an_argument_error() {
        // Act
        Action construct = () => new EmployeeId("E000000");

        // Assert
        Assert.Throws<ArgumentException>("value", construct);
    }

    [Fact]
    public void A_missing_personnel_number_is_refused() {
        // Act
        Action construct = () => new EmployeeId(null!);

        // Assert
        Assert.Throws<ArgumentNullException>("value", construct);
    }

    [Fact]
    public void Two_ids_built_from_the_same_number_are_equal_and_hash_alike() {
        // Arrange
        EmployeeId first  = new EmployeeId("E004217");
        EmployeeId second = new EmployeeId("E004217");

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Ids_with_different_numbers_are_not_equal() {
        // Arrange
        EmployeeId employeeId = new EmployeeId("E004217");

        // Assert
        Assert.NotEqual(employeeId, new EmployeeId("E004218"));
        Assert.False(employeeId.Equals(null));
    }

}
