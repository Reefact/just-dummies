using JustDummies;
using JustDummies.Xunit;
using Payments.Domain;
using Xunit;

namespace Payments.Tests;

[Reproducible]
public sealed class PolicyAndBatchTests {

    [Fact]
    public void A_standard_transfer_up_to_the_free_threshold_costs_nothing() {
        // Arrange
        FeePolicy      anyPolicyFreeUpTo1000 = Arbitrary.FeePolicy().WithFreeStandardThreshold(1_000m).Generate();
        CreditTransfer anyTransferOf1000     = Arbitrary.CreditTransfer()
                                                        .WithPriority(TransferPriority.Normal)
                                                        .WithAmount(new Money(1_000m, Currency.EUR))
                                                        .Generate();

        // Act
        decimal fee = anyPolicyFreeUpTo1000.FeeFor(anyTransferOf1000);

        // Assert
        Assert.Equal(0m, fee);
    }

    [Fact]
    public void A_standard_transfer_pays_the_rate_on_what_exceeds_the_threshold() {
        // Arrange
        FeePolicy anyPolicy = Arbitrary.FeePolicy()
                                       .WithFreeStandardThreshold(1_000m)
                                       .WithStandardRate(0.001m)
                                       .Generate();
        CreditTransfer anyTransferOf1500 = Arbitrary.CreditTransfer()
                                                    .WithPriority(TransferPriority.Normal)
                                                    .WithAmount(new Money(1_500m, Currency.EUR))
                                                    .Generate();

        // Act
        decimal fee = anyPolicy.FeeFor(anyTransferOf1500);

        // Assert
        Assert.Equal(0.50m, fee);
    }

    [Fact]
    public void An_instant_transfer_pays_the_flat_fee_plus_the_rate_rounded_to_the_cent() {
        // Arrange
        FeePolicy anyPolicy = Arbitrary.FeePolicy()
                                       .WithInstantFlatFee(0.20m)
                                       .WithInstantRate(0.0015m)
                                       .Generate();
        CreditTransfer anyInstantTransfer = Arbitrary.CreditTransfer()
                                                     .WithPriority(TransferPriority.Instant)
                                                     .WithAmount(new Money(333.33m, Currency.EUR))
                                                     .Generate();

        // Act
        decimal fee = anyPolicy.FeeFor(anyInstantTransfer);

        // Assert: 0.20 + 0.499995 = 0.699995, rounded away from zero to 0.70.
        Assert.Equal(0.70m, fee);
    }

    [Fact]
    public void The_control_sum_of_a_batch_is_the_sum_of_its_amounts() {
        // Arrange
        Party anyDebtor = Arbitrary.Party().Generate();
        CreditTransfer[] transfers = [
            Arbitrary.CreditTransfer().WithDebtor(anyDebtor).WithEndToEndId("E2E-1").WithAmount(new Money(10.00m, Currency.EUR)).Generate(),
            Arbitrary.CreditTransfer().WithDebtor(anyDebtor).WithEndToEndId("E2E-2").WithAmount(new Money(20.50m, Currency.EUR)).Generate()
        ];
        PaymentBatch anyBatch = Arbitrary.PaymentBatch().WithTransfers(transfers).Generate();

        // Act
        decimal controlSum = anyBatch.ControlSum;

        // Assert
        Assert.Equal(30.50m, controlSum);
    }

    [Fact]
    public void A_batch_mixing_two_debtors_is_refused() {
        // Arrange
        CreditTransfer[] anyTransfersFromTwoDebtors = [
            Arbitrary.CreditTransfer().WithEndToEndId("E2E-1").Generate(),
            Arbitrary.CreditTransfer().WithEndToEndId("E2E-2").Generate()
        ];

        // Act
        Func<PaymentBatch> create = () => Arbitrary.PaymentBatch().WithTransfers(anyTransfersFromTwoDebtors).Generate();

        // Assert
        Assert.Throws<ArgumentException>(create);
    }

    [Fact]
    public void The_next_business_day_after_christmas_skips_boxing_day_and_the_weekend() {
        // Arrange
        DateOnly christmas2026 = new(2026, 12, 25);

        // Act
        DateOnly next = BusinessCalendar.NextBusinessDayOnOrAfter(christmas2026);

        // Assert
        Assert.Equal(new DateOnly(2026, 12, 28), next);
    }

    [Fact]
    public void Adding_amounts_in_two_currencies_is_refused() {
        // Arrange
        Money anyEuros  = Arbitrary.Money().WithCurrency(Currency.EUR).Generate();
        Money anyFrancs = Arbitrary.Money().WithCurrency(Currency.CHF).Generate();

        // Act
        Func<Money> add = () => anyEuros.Add(anyFrancs);

        // Assert
        Assert.Throws<InvalidOperationException>(add);
    }

}
