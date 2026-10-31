namespace ParimutuelBetting.Domain;

/// <summary>The settled accounts of a pool: where every collected unit went.</summary>
public sealed class Settlement
{
    public Settlement(decimal grossPool, decimal refunded, decimal deductions, IReadOnlyList<Dividend> dividends, decimal carryover)
    {
        ArgumentNullException.ThrowIfNull(dividends);
        if (grossPool < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(grossPool), grossPool, "The gross pool cannot be negative.");
        }

        if (refunded < 0m || refunded > grossPool)
        {
            throw new ArgumentOutOfRangeException(nameof(refunded), refunded, $"Refunds lie between 0 and the gross pool ({grossPool}).");
        }

        if (deductions < 0m || deductions > grossPool - refunded)
        {
            throw new ArgumentOutOfRangeException(nameof(deductions), deductions, $"Deductions lie between 0 and the pool left after refunds ({grossPool - refunded}).");
        }

        decimal netPool = grossPool - refunded - deductions;
        if (carryover < 0m || carryover > netPool)
        {
            throw new ArgumentOutOfRangeException(nameof(carryover), carryover, $"The carryover lies between 0 and the net pool ({netPool}).");
        }

        if (dividends.Any(dividend => dividend is null))
        {
            throw new ArgumentException("A settlement cannot hold a null dividend.", nameof(dividends));
        }

        if (dividends.Count > 0 && carryover != 0m)
        {
            throw new ArgumentException("A pool that declares dividends carries nothing over.", nameof(carryover));
        }

        GrossPool = grossPool;
        Refunded = refunded;
        Deductions = deductions;
        NetPool = netPool;
        Dividends = dividends.ToArray();
        Carryover = carryover;
    }

    public decimal GrossPool { get; }

    public decimal Refunded { get; }

    public decimal Deductions { get; }

    /// <summary>The amount to share among winners: gross pool less refunds and deductions.</summary>
    public decimal NetPool { get; }

    public IReadOnlyList<Dividend> Dividends { get; }

    /// <summary>The net pool rolled into the next pool of the same type when nobody won.</summary>
    public decimal Carryover { get; }

    public decimal TotalPayout => Dividends.Sum(dividend => dividend.Payout);

    /// <summary>Net pool kept by rounding down (positive) or topped up to honour the minimum return (negative).</summary>
    public decimal Breakage => NetPool - Carryover - TotalPayout;
}
