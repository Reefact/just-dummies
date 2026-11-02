namespace TokenLedger.Domain;

/// <summary>
/// The gas each kind of transaction consumes. The three allowance operations cost
/// <see cref="Approve"/>. Crediting an account whose balance was zero writes a fresh storage
/// slot and adds <see cref="NewHolderSurcharge"/>.
/// </summary>
public sealed class GasSchedule : IEquatable<GasSchedule>
{
    /// <summary>The intrinsic cost of any transaction: no operation costs less.</summary>
    public const ulong IntrinsicGas = 21_000;

    /// <summary>No transaction may need or declare more gas than a block holds.</summary>
    public const ulong BlockGasLimit = 30_000_000;

    public const ulong MaxNewHolderSurcharge = 1_000_000;

    public GasSchedule(ulong transfer, ulong approve, ulong transferFrom, ulong mint, ulong burn, ulong newHolderSurcharge)
    {
        RequireBase(transfer, nameof(transfer));
        RequireBase(approve, nameof(approve));
        RequireBase(transferFrom, nameof(transferFrom));
        RequireBase(mint, nameof(mint));
        RequireBase(burn, nameof(burn));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(newHolderSurcharge, MaxNewHolderSurcharge);

        if (transferFrom < transfer)
        {
            throw new ArgumentException("A delegated transfer also spends an allowance: it costs at least a plain transfer.", nameof(transferFrom));
        }

        ulong crediting = Math.Max(Math.Max(transfer, transferFrom), mint);
        if (crediting + newHolderSurcharge > BlockGasLimit)
        {
            throw new ArgumentException(
                $"A transfer or mint to a new holder ({crediting} + {newHolderSurcharge}) must fit in a block ({BlockGasLimit}).",
                nameof(newHolderSurcharge));
        }

        Transfer = transfer;
        Approve = approve;
        TransferFrom = transferFrom;
        Mint = mint;
        Burn = burn;
        NewHolderSurcharge = newHolderSurcharge;
    }

    public static GasSchedule Standard { get; } = new(35_000, 30_000, 45_000, 40_000, 32_000, 20_000);

    public ulong Transfer { get; }

    public ulong Approve { get; }

    public ulong TransferFrom { get; }

    public ulong Mint { get; }

    public ulong Burn { get; }

    public ulong NewHolderSurcharge { get; }

    /// <summary>Gas used by a transaction of <paramref name="kind"/>; <paramref name="createsHolder"/> adds the surcharge.</summary>
    public ulong GasFor(TransactionKind kind, bool createsHolder)
    {
        ulong baseCost = kind switch
        {
            TransactionKind.Transfer => Transfer,
            TransactionKind.Approve or TransactionKind.IncreaseAllowance or TransactionKind.DecreaseAllowance => Approve,
            TransactionKind.TransferFrom => TransferFrom,
            TransactionKind.Mint => Mint,
            TransactionKind.Burn => Burn,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown transaction kind."),
        };

        bool credits = kind is TransactionKind.Transfer or TransactionKind.TransferFrom or TransactionKind.Mint;
        return createsHolder && credits ? baseCost + NewHolderSurcharge : baseCost;
    }

    private static void RequireBase(ulong gas, string name)
    {
        if (gas < IntrinsicGas || gas > BlockGasLimit)
        {
            throw new ArgumentOutOfRangeException(name, gas, $"An operation costs between {IntrinsicGas} and {BlockGasLimit} gas.");
        }
    }

    public bool Equals(GasSchedule? other) =>
        other is not null
        && Transfer == other.Transfer
        && Approve == other.Approve
        && TransferFrom == other.TransferFrom
        && Mint == other.Mint
        && Burn == other.Burn
        && NewHolderSurcharge == other.NewHolderSurcharge;

    public override bool Equals(object? obj) => Equals(obj as GasSchedule);

    public override int GetHashCode() => HashCode.Combine(Transfer, Approve, TransferFrom, Mint, Burn, NewHolderSurcharge);

    public override string ToString() =>
        $"transfer {Transfer}, approve {Approve}, transferFrom {TransferFrom}, mint {Mint}, burn {Burn}, new holder +{NewHolderSurcharge}";
}
