using JustDummies;

using Payments.Domain;

using Xunit;

namespace Payments.Tests;

public sealed class PaymentBatchTests {

    private static CreditTransfer TransferFor(Party debtor, string endToEndId) {
        return Any.CreditTransfer().WithDebtor(debtor).WithEndToEndId(endToEndId).Generate();
    }

    private static List<CreditTransfer> TransfersFor(Party debtor, int count) {
        List<CreditTransfer> transfers = new List<CreditTransfer>();
        for (int i = 0; i < count; i++) {
            transfers.Add(TransferFor(debtor, "E2E-" + i));
        }

        return transfers;
    }

    [Fact]
    public void A_batch_keeps_its_message_id_its_creation_time_and_its_transfers() {
        // Arrange
        Party          anyDebtor  = Any.Party().Generate();
        CreditTransfer anyFirst   = TransferFor(anyDebtor, "E2E-1");
        CreditTransfer anySecond  = TransferFor(anyDebtor, "E2E-2");
        DateTime       createdAt  = new DateTime(2026, 3, 4, 9, 30, 0, DateTimeKind.Utc);

        // Act
        PaymentBatch batch = new PaymentBatch("MSG-0001", createdAt, new List<CreditTransfer> { anyFirst, anySecond });

        // Assert
        Assert.Equal("MSG-0001", batch.MessageId);
        Assert.Equal(createdAt, batch.CreatedAt);
        Assert.Equal(new[] { anyFirst, anySecond }, batch.Transfers);
        Assert.Equal(2, batch.NumberOfTransactions);
    }

    [Fact]
    public void The_control_sum_adds_up_the_amounts_of_the_transfers() {
        // Arrange
        Party anyDebtor = Any.Party().Generate();
        List<CreditTransfer> transfers = new List<CreditTransfer> {
            Any.CreditTransfer().WithDebtor(anyDebtor).WithEndToEndId("E2E-1").WithAmount(new Money(10.10m, Currency.EUR)).Generate(),
            Any.CreditTransfer().WithDebtor(anyDebtor).WithEndToEndId("E2E-2").WithAmount(new Money(20.20m, Currency.EUR)).Generate(),
            Any.CreditTransfer().WithDebtor(anyDebtor).WithEndToEndId("E2E-3").WithAmount(new Money(0.01m, Currency.EUR)).Generate()
        };

        // Act
        PaymentBatch batch = Any.PaymentBatch().WithTransfers(transfers).Generate();

        // Assert
        Assert.Equal(30.31m, batch.ControlSum);
        Assert.Equal(3, batch.NumberOfTransactions);
    }

    [Fact]
    public void A_batch_of_a_single_transfer_is_accepted() {
        // Arrange
        Party anyDebtor = Any.Party().Generate();

        // Act
        PaymentBatch batch = Any.PaymentBatch().WithTransfers(TransfersFor(anyDebtor, 1)).Generate();

        // Assert
        Assert.Equal(1, batch.NumberOfTransactions);
    }

    [Fact]
    public void A_batch_of_100_transfers_is_accepted() {
        // Arrange
        Party anyDebtor = Any.Party().Generate();

        // Act
        PaymentBatch batch = Any.PaymentBatch().WithTransfers(TransfersFor(anyDebtor, 100)).Generate();

        // Assert
        Assert.Equal(100, batch.NumberOfTransactions);
    }

    [Fact]
    public void A_batch_of_101_transfers_is_refused() {
        // Arrange
        Party anyDebtor = Any.Party().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.PaymentBatch().WithTransfers(TransfersFor(anyDebtor, 101)).Generate());

        // Assert
        Assert.Equal("transfers", refusal.ParamName);
    }

    [Fact]
    public void A_batch_without_transfer_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.PaymentBatch().WithTransfers(new List<CreditTransfer>()).Generate());
    }

    [Fact]
    public void A_null_list_of_transfers_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.PaymentBatch().WithTransfers(null!).Generate());
    }

    [Fact]
    public void A_null_message_id_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.PaymentBatch().WithMessageId((string)null!).Generate());
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_message_id_is_refused(string blank) {
        Assert.Throws<ArgumentException>(() => Any.PaymentBatch().WithMessageId(blank).Generate());
    }

    [Fact]
    public void The_creation_time_is_not_checked() {
        // Arrange
        DateTime epoch = default;

        // Act
        PaymentBatch batch = Any.PaymentBatch().WithCreatedAt(epoch).Generate();

        // Assert
        Assert.Equal(epoch, batch.CreatedAt);
    }

    [Fact]
    public void Transfers_of_two_different_debtor_accounts_are_refused() {
        // Arrange
        CreditTransfer anyFirst  = TransferFor(Any.Party().Generate(), "E2E-1");
        CreditTransfer anySecond = TransferFor(Any.Party().Generate(), "E2E-2");

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.PaymentBatch().WithTransfers(new List<CreditTransfer> { anyFirst, anySecond }).Generate());

        // Assert
        Assert.Equal("transfers", refusal.ParamName);
    }

    [Fact]
    public void The_debtor_account_is_compared_by_value_so_two_parties_can_share_it() {
        // Arrange
        string anyValue   = Any.Iban().Generate().Value;
        Party  anyDebtor  = Any.Party().WithAccount(new Iban(anyValue)).Generate();
        Party  anyTwin    = Any.Party().WithAccount(new Iban(anyValue)).Generate();

        List<CreditTransfer> transfers = new List<CreditTransfer> { TransferFor(anyDebtor, "E2E-1"), TransferFor(anyTwin, "E2E-2") };

        // Act
        PaymentBatch batch = Any.PaymentBatch().WithTransfers(transfers).Generate();

        // Assert
        Assert.Equal(2, batch.NumberOfTransactions);
    }

    [Fact]
    public void Two_transfers_with_the_same_end_to_end_id_are_refused() {
        // Arrange
        Party anyDebtor = Any.Party().Generate();
        List<CreditTransfer> transfers = new List<CreditTransfer> { TransferFor(anyDebtor, "E2E-SAME"), TransferFor(anyDebtor, "E2E-SAME") };

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.PaymentBatch().WithTransfers(transfers).Generate());

        // Assert
        Assert.Equal("transfers", refusal.ParamName);
    }

    [Fact]
    public void End_to_end_ids_which_differ_only_by_case_are_distinct() {
        // Arrange
        Party anyDebtor = Any.Party().Generate();
        List<CreditTransfer> transfers = new List<CreditTransfer> { TransferFor(anyDebtor, "e2e-a"), TransferFor(anyDebtor, "E2E-A") };

        // Act
        PaymentBatch batch = Any.PaymentBatch().WithTransfers(transfers).Generate();

        // Assert
        Assert.Equal(2, batch.NumberOfTransactions);
    }

    [Fact]
    public void Transfers_are_not_required_to_share_a_date_or_a_priority() {
        // Arrange
        Party anyDebtor = Any.Party().Generate();
        List<CreditTransfer> transfers = new List<CreditTransfer> {
            Any.CreditTransfer().WithDebtor(anyDebtor).WithEndToEndId("E2E-1").WithRequestedExecutionDate(new DateOnly(2026, 3, 4)).WithPriority(TransferPriority.Normal).Generate(),
            Any.CreditTransfer().WithDebtor(anyDebtor).WithEndToEndId("E2E-2").WithRequestedExecutionDate(new DateOnly(2026, 3, 5)).WithPriority(TransferPriority.Instant).Generate()
        };

        // Act
        PaymentBatch batch = Any.PaymentBatch().WithTransfers(transfers).Generate();

        // Assert
        Assert.Equal(2, batch.NumberOfTransactions);
    }

}
