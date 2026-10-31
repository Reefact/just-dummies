namespace CarbonLedger.Domain;

/// <summary>
/// One balanced, immutable line of the journal: every listed unit leaves the debit account and enters the credit
/// account, so the entry moves <see cref="Quantity"/> units out of one side and the same quantity into the other.
/// </summary>
public sealed class JournalEntry
{
    public JournalEntry(
        long sequence,
        DateTimeOffset recordedAt,
        EntryKind kind,
        Account debit,
        Account credit,
        IReadOnlyCollection<CreditSerial> serials,
        RetirementClaim? claim)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sequence, 1L);
        ArgumentNullException.ThrowIfNull(debit);
        ArgumentNullException.ThrowIfNull(credit);
        ArgumentNullException.ThrowIfNull(serials);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown entry kind.");
        }

        if (debit.AccountId == credit.AccountId)
        {
            throw new ArgumentException("An entry cannot debit and credit the same account.", nameof(credit));
        }

        if (serials.Count == 0)
        {
            throw new ArgumentException("An entry moves at least one unit.", nameof(serials));
        }

        if (serials.Any(s => s is null))
        {
            throw new ArgumentException("An entry cannot list a null serial.", nameof(serials));
        }

        if (serials.Distinct().Count() != serials.Count)
        {
            throw new ArgumentException("An entry cannot list the same unit twice.", nameof(serials));
        }

        (AccountKind expectedDebit, AccountKind expectedCredit) = kind switch
        {
            EntryKind.Issuance => (AccountKind.Issuance, AccountKind.Holding),
            EntryKind.Transfer => (AccountKind.Holding, AccountKind.Holding),
            _ => (AccountKind.Holding, AccountKind.Retirement),
        };
        if (debit.Kind != expectedDebit || credit.Kind != expectedCredit)
        {
            throw new ArgumentException(
                $"A {kind} entry moves units from a {expectedDebit} account to a {expectedCredit} account, not from {debit.Kind} to {credit.Kind}.",
                nameof(kind));
        }

        if (kind == EntryKind.Retirement)
        {
            if (claim is null)
            {
                throw new ArgumentNullException(nameof(claim), "A retirement must state its claim.");
            }

            int latestVintage = serials.Max(s => s.Vintage);
            if (claim.ReportingYear < latestVintage)
            {
                throw new ArgumentException(
                    $"Units of vintage {latestVintage} cannot offset emissions reported for {claim.ReportingYear}.",
                    nameof(claim));
            }
        }
        else if (claim is not null)
        {
            throw new ArgumentException("Only a retirement carries a claim.", nameof(claim));
        }

        Sequence = sequence;
        RecordedAt = recordedAt;
        Kind = kind;
        Debit = debit;
        Credit = credit;
        Serials = serials.ToArray();
        Claim = claim;
    }

    public long Sequence { get; }

    public DateTimeOffset RecordedAt { get; }

    public EntryKind Kind { get; }

    public Account Debit { get; }

    public Account Credit { get; }

    public IReadOnlyList<CreditSerial> Serials { get; }

    public RetirementClaim? Claim { get; }

    public int Quantity => Serials.Count;
}
