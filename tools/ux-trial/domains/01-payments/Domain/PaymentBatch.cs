namespace Payments.Domain;

/// <summary>A pain.001 batch: one debtor, one execution date, at most 100 transfers.</summary>
public sealed class PaymentBatch {

    public PaymentBatch(string messageId, DateTime createdAt, IReadOnlyList<CreditTransfer> transfers) {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentNullException.ThrowIfNull(transfers);
        if (transfers.Count == 0 || transfers.Count > 100) {
            throw new ArgumentOutOfRangeException(nameof(transfers), "A batch holds 1 to 100 transfers.");
        }
        if (transfers.Select(t => t.Debtor.Account).Distinct().Count() != 1) {
            throw new ArgumentException("A batch has a single debtor account.", nameof(transfers));
        }
        if (transfers.Select(t => t.EndToEndId).Distinct().Count() != transfers.Count) {
            throw new ArgumentException("End-to-end identifiers are unique within a batch.", nameof(transfers));
        }
        MessageId = messageId;
        CreatedAt = createdAt;
        Transfers = transfers;
    }

    public string MessageId { get; }
    public DateTime CreatedAt { get; }
    public IReadOnlyList<CreditTransfer> Transfers { get; }

    public int NumberOfTransactions => Transfers.Count;

    public decimal ControlSum => Transfers.Sum(t => t.Amount.Amount);

}
