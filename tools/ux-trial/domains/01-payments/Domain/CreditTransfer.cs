namespace Payments.Domain;

/// <summary>A SEPA credit transfer: euros only, from one account to a different one, on a business day.</summary>
public sealed class CreditTransfer {

    public const decimal InstantCeiling = 100_000m;

    public CreditTransfer(string endToEndId,
                          Party debtor,
                          Party creditor,
                          Money amount,
                          DateOnly requestedExecutionDate,
                          TransferPriority priority,
                          RemittanceInformation? remittanceInformation) {
        ArgumentException.ThrowIfNullOrEmpty(endToEndId);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(endToEndId.Length, 35);
        ArgumentNullException.ThrowIfNull(debtor);
        ArgumentNullException.ThrowIfNull(creditor);
        ArgumentNullException.ThrowIfNull(amount);
        if (amount.Currency != Currency.EUR) {
            throw new ArgumentException("A SEPA credit transfer is denominated in euros.", nameof(amount));
        }
        if (debtor.Account.Equals(creditor.Account)) {
            throw new ArgumentException("The debtor and the creditor accounts must differ.", nameof(creditor));
        }
        if (!BusinessCalendar.IsBusinessDay(requestedExecutionDate)) {
            throw new ArgumentException($"{requestedExecutionDate} is not a TARGET2 business day.", nameof(requestedExecutionDate));
        }
        if (priority == TransferPriority.Instant && amount.Amount > InstantCeiling) {
            throw new ArgumentException($"An instant transfer is capped at {InstantCeiling} EUR.", nameof(amount));
        }
        EndToEndId             = endToEndId;
        Debtor                 = debtor;
        Creditor               = creditor;
        Amount                 = amount;
        RequestedExecutionDate = requestedExecutionDate;
        Priority               = priority;
        RemittanceInformation  = remittanceInformation;
        Status                 = TransferStatus.Draft;
    }

    public string EndToEndId { get; }
    public Party Debtor { get; }
    public Party Creditor { get; }
    public Money Amount { get; }
    public DateOnly RequestedExecutionDate { get; }
    public TransferPriority Priority { get; }
    public RemittanceInformation? RemittanceInformation { get; }
    public TransferStatus Status { get; private set; }

    /// <summary>A transfer is submitted on a given day; its execution date may not be in the past then.</summary>
    public void Submit(DateOnly today) {
        if (Status != TransferStatus.Draft) {
            throw new InvalidOperationException($"Only a draft can be submitted; this transfer is {Status}.");
        }
        if (RequestedExecutionDate < today) {
            throw new InvalidOperationException($"The requested execution date {RequestedExecutionDate} is before {today}.");
        }
        Status = TransferStatus.Submitted;
    }

    public void MarkExecuted() {
        if (Status != TransferStatus.Submitted) {
            throw new InvalidOperationException($"Only a submitted transfer can be executed; this one is {Status}.");
        }
        Status = TransferStatus.Executed;
    }

    public void Reject() {
        Status = Status switch {
            TransferStatus.Submitted => TransferStatus.Rejected,
            _ => throw new InvalidOperationException($"Only a submitted transfer can be rejected; this one is {Status}.")
        };
    }

    /// <summary>Cross-border inside SEPA when the two accounts are held in different countries.</summary>
    public bool IsCrossBorder => Debtor.Account.CountryCode != Creditor.Account.CountryCode;

}
