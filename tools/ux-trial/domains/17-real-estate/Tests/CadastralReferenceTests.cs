using JustDummies;
using RealEstate.Domain;
using Xunit;

namespace RealEstate.Tests;

public sealed class CadastralReferenceTests {

    // Prefix, section and plan number of a reference: never the part these tests are about.
    private const string AnyRestOfReference = @" \d{3} [A-Z0-9]{2} \d{3}[1-9]";

    [Fact]
    public void A_parcel_in_Corse_du_Sud_belongs_to_department_2A() {
        // Arrange
        CadastralReference anyAjaccioParcel = Any.StringMatching("2A004" + AnyRestOfReference)
                                                 .As(value => new CadastralReference(value))
                                                 .Generate();

        // Act
        string department = anyAjaccioParcel.Department;

        // Assert
        Assert.Equal("2A", department);
    }

    [Fact]
    public void An_overseas_parcel_keeps_a_three_digit_department() {
        // Arrange
        CadastralReference anySaintDenisParcel = Any.StringMatching("97411" + AnyRestOfReference)
                                                    .As(value => new CadastralReference(value))
                                                    .Generate();

        // Act
        string department = anySaintDenisParcel.Department;

        // Assert
        Assert.Equal("974", department);
    }

    [Fact]
    public void Plan_number_zero_is_not_a_parcel() {
        // Arrange
        string anyReferenceOnPlanZero = Any.StringMatching(@"\d{5} \d{3} [A-Z0-9]{2} 0000").Generate();

        // Act
        Action create = () => _ = new CadastralReference(anyReferenceOnPlanZero);

        // Assert
        Assert.Throws<ArgumentException>(create);
    }
}
