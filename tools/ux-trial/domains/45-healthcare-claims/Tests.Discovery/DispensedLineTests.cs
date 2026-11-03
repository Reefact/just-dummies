using Clinical.Domain;

using JustDummies;

using Payments.Domain;

using Xunit;

namespace HealthcareClaims.Domain;

public sealed class DispensedLineTests {

    private static Money Euros(decimal amount) => new(amount, Currency.EUR);

    [Fact]
    public void The_price_paid_is_the_unit_price_times_the_packs() {
        // Arrange
        PrescriptionLine anyPrescriptionLine = Any.PrescriptionLine().Generate();

        // Act
        DispensedLine line = new(anyPrescriptionLine, ReimbursementCategory.Major, 3, Euros(4.35m), null);

        // Assert
        Assert.Equal(13.05m, line.PricePaid);
    }

    [Fact]
    public void Without_a_statutory_reimbursement_the_whole_price_remains_at_the_insureds_charge() {
        // Arrange
        PrescriptionLine anyPrescriptionLine = Any.PrescriptionLine().Generate();

        // Act
        DispensedLine line = new(anyPrescriptionLine, ReimbursementCategory.Major, 2, Euros(10m), null);

        // Assert
        Assert.Null(line.StatutoryReimbursement);
        Assert.Equal(20m, line.RemainingCharge);
    }

    [Fact]
    public void The_remaining_charge_is_the_price_paid_less_the_statutory_reimbursement() {
        // Arrange
        PrescriptionLine anyPrescriptionLine = Any.PrescriptionLine().Generate();

        // Act
        DispensedLine line = new(anyPrescriptionLine, ReimbursementCategory.Major, 2, Euros(10m), Euros(12.5m));

        // Assert
        Assert.Equal(7.5m, line.RemainingCharge);
    }

    [Fact]
    public void The_statutory_scheme_may_have_reimbursed_the_whole_price() {
        // Arrange
        PrescriptionLine anyPrescriptionLine = Any.PrescriptionLine().Generate();

        // Act
        DispensedLine line = new(anyPrescriptionLine, ReimbursementCategory.Irreplaceable, 2, Euros(10m), Euros(20m));

        // Assert
        Assert.Equal(0m, line.RemainingCharge);
    }

    [Fact]
    public void A_statutory_reimbursement_above_the_price_paid_is_refused() {
        // Arrange
        PrescriptionLine anyPrescriptionLine = Any.PrescriptionLine().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new DispensedLine(anyPrescriptionLine, ReimbursementCategory.Major, 2, Euros(10m), Euros(20.01m)));

        // Assert
        Assert.Equal("statutoryReimbursement", refusal.ParamName);
    }

    [Fact]
    public void An_unlisted_medicine_cannot_carry_a_statutory_reimbursement() {
        // Arrange
        PrescriptionLine anyPrescriptionLine = Any.PrescriptionLine().Generate();
        Money            anyReimbursement    = Euros(0.01m);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new DispensedLine(anyPrescriptionLine, ReimbursementCategory.Unlisted, 1, Euros(10m), anyReimbursement));

        // Assert
        Assert.Equal("statutoryReimbursement", refusal.ParamName);
    }

    [Fact]
    public void An_unlisted_medicine_without_a_statutory_reimbursement_is_accepted() {
        // Arrange
        PrescriptionLine anyPrescriptionLine = Any.PrescriptionLine().Generate();

        // Act
        DispensedLine line = new(anyPrescriptionLine, ReimbursementCategory.Unlisted, 1, Euros(10m), null);

        // Assert
        Assert.Equal(10m, line.RemainingCharge);
    }

    [Fact]
    public void A_statutory_reimbursement_in_another_currency_is_refused() {
        // Arrange
        PrescriptionLine anyPrescriptionLine = Any.PrescriptionLine().Generate();
        Currency         anyForeignCurrency  = Any.Enum<Currency>().Except(Currency.EUR).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(
            () => new DispensedLine(anyPrescriptionLine, ReimbursementCategory.Major, 1, Euros(10m), new Money(1m, anyForeignCurrency)));
    }

    [Fact]
    public void A_unit_price_in_another_currency_is_refused() {
        // Arrange
        PrescriptionLine anyPrescriptionLine = Any.PrescriptionLine().Generate();
        Currency         anyForeignCurrency  = Any.Enum<Currency>().Except(Currency.EUR).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new DispensedLine(anyPrescriptionLine, ReimbursementCategory.Major, 1, new Money(10m, anyForeignCurrency), null));

        // Assert
        Assert.Equal("unitPrice", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    public void One_to_twelve_packs_are_accepted(int packs) {
        // Arrange
        PrescriptionLine anyPrescriptionLine = Any.PrescriptionLine().Generate();

        // Act / Assert
        Assert.Equal(packs, new DispensedLine(anyPrescriptionLine, ReimbursementCategory.Low, packs, Euros(1m), null).Packs);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void A_number_of_packs_outside_one_to_twelve_is_refused(int packs) {
        // Arrange
        PrescriptionLine anyPrescriptionLine = Any.PrescriptionLine().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new DispensedLine(anyPrescriptionLine, ReimbursementCategory.Low, packs, Euros(1m), null));

        // Assert
        Assert.Equal("packs", refusal.ParamName);
    }

    [Fact]
    public void A_unit_price_of_exactly_five_thousand_euros_is_accepted() {
        // Arrange
        PrescriptionLine anyPrescriptionLine = Any.PrescriptionLine().Generate();

        // Act
        DispensedLine line = new(anyPrescriptionLine, ReimbursementCategory.Irreplaceable, 12, Euros(5_000m), null);

        // Assert
        Assert.Equal(60_000m, line.PricePaid);
    }

    [Fact]
    public void A_unit_price_above_five_thousand_euros_is_refused() {
        // Arrange
        PrescriptionLine anyPrescriptionLine = Any.PrescriptionLine().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new DispensedLine(anyPrescriptionLine, ReimbursementCategory.Irreplaceable, 1, Euros(5_000.01m), null));

        // Assert
        Assert.Equal("unitPrice", refusal.ParamName);
    }

    [Fact]
    public void A_category_that_is_not_a_defined_member_is_refused() {
        // Arrange
        PrescriptionLine anyPrescriptionLine = Any.PrescriptionLine().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new DispensedLine(anyPrescriptionLine, (ReimbursementCategory)42, 1, Euros(1m), null));

        // Assert
        Assert.Equal("category", refusal.ParamName);
    }

    [Fact]
    public void A_missing_prescription_line_or_unit_price_is_refused() {
        // Arrange
        PrescriptionLine anyPrescriptionLine = Any.PrescriptionLine().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new DispensedLine(null!, ReimbursementCategory.Low, 1, Euros(1m), null));
        Assert.Throws<ArgumentNullException>(() => new DispensedLine(anyPrescriptionLine, ReimbursementCategory.Low, 1, null!, null));
    }

}
