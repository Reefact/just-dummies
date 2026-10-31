namespace LiquidityGap.Domain;

/// <summary>A behavioural and market scenario applied when projecting a portfolio.</summary>
public sealed class Scenario
{
    public Scenario(string name, decimal inflowHaircut, decimal outflowMultiplier, decimal depositStableShareHaircut)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 64)
        {
            throw new ArgumentException("A scenario name is non-blank and at most 64 characters.", nameof(name));
        }

        if (inflowHaircut is < 0m or >= 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(inflowHaircut), inflowHaircut, "An inflow haircut is in [0, 1).");
        }

        if (outflowMultiplier is < 1m or > 3m)
        {
            throw new ArgumentOutOfRangeException(nameof(outflowMultiplier), outflowMultiplier, "An outflow multiplier is in [1, 3].");
        }

        if (depositStableShareHaircut is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(depositStableShareHaircut), depositStableShareHaircut, "A stable-share haircut is in [0, 1].");
        }

        Name = name;
        InflowHaircut = inflowHaircut;
        OutflowMultiplier = outflowMultiplier;
        DepositStableShareHaircut = depositStableShareHaircut;
    }

    /// <summary>Business as usual: contractual flows as agreed, deposits behave as assumed.</summary>
    public static Scenario Baseline { get; } = new("Baseline", 0m, 1m, 0m);

    public string Name { get; }

    /// <summary>The fraction of contractual inflows assumed not to be received.</summary>
    public decimal InflowHaircut { get; }

    /// <summary>The factor applied to contractual outflows (drawn commitments, early withdrawals).</summary>
    public decimal OutflowMultiplier { get; }

    /// <summary>The fraction of the deposits' stable share that becomes volatile under stress.</summary>
    public decimal DepositStableShareHaircut { get; }

    public bool IsStressed => InflowHaircut > 0m || OutflowMultiplier > 1m || DepositStableShareHaircut > 0m;
}
