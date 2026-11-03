using Clinical.Domain;
using HealthcareClaims.Domain;
using JustDummies;
using Payments.Domain;
using Xunit;

namespace HealthcareClaims.Tests.Expert.Tests;

public sealed class DispensedLineTests {

    private static Money Euros(decimal amount) => new(amount, Currency.EUR);

    [Fact]
    public void The_price_paid_is_the_unit_price_times_the_packs() {
        DispensedLine line = Any.DispensedLine().WithUnitPrice(Euros(12.50m)).WithPacks(3).Generate();

        Assert.Equal(37.50m, line.PricePaid);
    }

    [Fact]
    public void Without_a_statutory_reimbursement_the_whole_price_remains_at_the_insureds_charge() {
        DispensedLine line = Any.DispensedLine()
                                .WithUnitPrice(Euros(12.50m)).WithPacks(3)
                                .WithoutStatutoryReimbursement()
                                .Generate();

        Assert.Equal(37.50m, line.RemainingCharge);
    }

    [Fact]
    public void What_the_statutory_scheme_paid_is_taken_off_the_remaining_charge() {
        DispensedLine line = Any.DispensedLine()
                                .WithCategory(ReimbursementCategory.Major)
                                .WithUnitPrice(Euros(12.50m)).WithPacks(3)
                                .WithStatutoryReimbursement(Euros(22.50m))
                                .Generate();

        Assert.Equal(15.00m, line.RemainingCharge);
    }

    [Fact]
    public void A_statutory_reimbursement_may_equal_the_price_paid() {
        DispensedLine line = Any.DispensedLine()
                                .WithCategory(ReimbursementCategory.Irreplaceable)
                                .WithUnitPrice(Euros(10.00m)).WithPacks(2)
                                .WithStatutoryReimbursement(Euros(20.00m))
                                .Generate();

        Assert.Equal(0m, line.RemainingCharge);
    }

    [Fact]
    public void A_statutory_reimbursement_cannot_exceed_the_price_paid() {
        AnyDispensedLine tooGenerous = Any.DispensedLine()
                                          .WithCategory(ReimbursementCategory.Major)
                                          .WithUnitPrice(Euros(10.00m)).WithPacks(2)
                                          .WithStatutoryReimbursement(Euros(20.01m));

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => tooGenerous.Generate());
        Assert.Equal("statutoryReimbursement", refusal.ParamName);
    }

    [Fact]
    public void An_unlisted_medicine_cannot_have_been_reimbursed_by_the_statutory_scheme() {
        AnyDispensedLine unlistedButReimbursed = Any.DispensedLine()
                                                    .WithCategory(ReimbursementCategory.Unlisted)
                                                    .WithUnitPrice(Euros(10.00m)).WithPacks(1)
                                                    .WithStatutoryReimbursement(Euros(1.00m));

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => unlistedButReimbursed.Generate());
        Assert.Equal("statutoryReimbursement", refusal.ParamName);
    }

    [Fact]
    public void An_unlisted_medicine_is_dispensed_without_any_statutory_reimbursement() {
        DispensedLine line = Any.DispensedLine()
                                .WithCategory(ReimbursementCategory.Unlisted)
                                .WithUnitPrice(Euros(10.00m)).WithPacks(1)
                                .WithoutStatutoryReimbursement()
                                .Generate();

        Assert.Null(line.StatutoryReimbursement);
        Assert.Equal(10.00m, line.RemainingCharge);
    }

    [Fact]
    public void The_statutory_reimbursement_is_in_euros() {
        AnyDispensedLine swissRefund = Any.DispensedLine()
                                          .WithCategory(ReimbursementCategory.Major)
                                          .WithUnitPrice(Euros(100.00m)).WithPacks(1)
                                          .WithStatutoryReimbursement(new Money(10.00m, Currency.CHF));

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => swissRefund.Generate());
        Assert.Equal("statutoryReimbursement", refusal.ParamName);
    }

    [Fact]
    public void The_unit_price_is_in_euros() {
        AnyDispensedLine swissPrice = Any.DispensedLine().WithUnitPrice(new Money(10.00m, Currency.CHF));

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => swissPrice.Generate());
        Assert.Equal("unitPrice", refusal.ParamName);
    }

    [Fact]
    public void A_unit_price_of_five_thousand_euros_is_accepted() {
        DispensedLine line = Any.DispensedLine().WithUnitPrice(Euros(5_000m)).WithPacks(12).WithoutStatutoryReimbursement().Generate();

        Assert.Equal(60_000m, line.PricePaid);
    }

    [Fact]
    public void A_unit_price_above_five_thousand_euros_is_refused() {
        AnyDispensedLine tooDear = Any.DispensedLine().WithUnitPrice(Euros(5_000.01m));

        Assert.Throws<ArgumentOutOfRangeException>(() => tooDear.Generate());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    public void Between_one_and_twelve_packs_are_accepted(int packs) {
        DispensedLine line = Any.DispensedLine().WithPacks(packs).Generate();

        Assert.Equal(packs, line.Packs);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void Fewer_than_one_or_more_than_twelve_packs_are_refused(int packs) {
        AnyDispensedLine dispensedLine = Any.DispensedLine().WithPacks(packs);

        Assert.Throws<ArgumentOutOfRangeException>(() => dispensedLine.Generate());
    }

    [Fact]
    public void An_undefined_category_is_refused() {
        AnyDispensedLine unknownCategory = Any.DispensedLine().WithCategory((ReimbursementCategory)42).WithoutStatutoryReimbursement();

        Assert.Throws<ArgumentOutOfRangeException>(() => unknownCategory.Generate());
    }

    [Fact]
    public void A_line_keeps_the_prescription_line_it_dispenses_by_reference() {
        PrescriptionLine anyPrescriptionLine = Any.PrescriptionLine().Generate();

        DispensedLine line = Any.DispensedLine().WithPrescriptionLine(anyPrescriptionLine).Generate();

        Assert.Same(anyPrescriptionLine, line.PrescriptionLine);
    }

    [Fact]
    public void A_line_needs_a_prescription_line_and_a_unit_price() {
        DispensedLine anyLine = Any.DispensedLine().Generate();

        Assert.Throws<ArgumentNullException>(() => new DispensedLine(null!, anyLine.Category, 1, anyLine.UnitPrice, null));
        Assert.Throws<ArgumentNullException>(() => new DispensedLine(anyLine.PrescriptionLine, anyLine.Category, 1, null!, null));
    }

}
