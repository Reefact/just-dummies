using JustDummies;

using Xunit;

namespace ShiftPlanning.Domain.Tests.Regular.ValueObjects;

public sealed class EmployeeIdTests {

    [Theory]
    [InlineData("E004217")]
    [InlineData("E000001")]
    [InlineData("E999999")]
    public void An_E_followed_by_six_digits_is_accepted(string number) {
        // Act
        EmployeeId id = new EmployeeId(number);

        // Assert
        Assert.Equal(number, id.Value);
        Assert.Equal(number, id.ToString());
    }

    [Theory]
    [InlineData("e004217")]
    [InlineData("F004217")]
    [InlineData("E00421")]
    [InlineData("E0042177")]
    [InlineData("E00421A")]
    [InlineData("E00 217")]
    [InlineData("0004217")]
    [InlineData("E-04217")]
    [InlineData("E٠٠٤٢١٧")]
    [InlineData("")]
    public void A_number_of_another_shape_is_refused_as_a_format_error(string number) {
        Assert.Throws<FormatException>(() => new EmployeeId(number));
    }

    [Fact]
    public void E000000_is_not_a_personnel_number() {
        ArgumentException refusal = Assert.ThrowsAny<ArgumentException>(() => new EmployeeId("E000000"));

        Assert.IsNotType<FormatException>(refusal);
    }

    [Fact]
    public void A_null_number_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new EmployeeId(null!));
    }

    [Fact]
    public void Two_ids_of_the_same_number_are_equal() {
        // Arrange
        EmployeeId anyId = new AnyEmployeeId().Generate();

        // Act
        EmployeeId same = new EmployeeId(anyId.Value);

        // Assert
        Assert.Equal(anyId, same);
        Assert.Equal(anyId.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void Two_ids_of_different_numbers_are_not_equal() {
        // Arrange
        List<EmployeeId> anyDistinctIds = Any.ListOf(new AnyEmployeeId()).WithCount(2).Distinct().Generate();

        // Assert
        Assert.NotEqual(anyDistinctIds[0], anyDistinctIds[1]);
    }

}
