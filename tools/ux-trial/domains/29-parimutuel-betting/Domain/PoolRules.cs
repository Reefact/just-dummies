namespace ParimutuelBetting.Domain;

/// <summary>The redistribution rules of a pool: deductions, breakage, minimum return and paid places.</summary>
public sealed class PoolRules
{
    public PoolRules(decimal takeoutRate, decimal breakageStep, decimal minimumReturnRatio, int placesPaid)
    {
        if (takeoutRate < 0m || takeoutRate >= 0.5m)
        {
            throw new ArgumentOutOfRangeException(nameof(takeoutRate), takeoutRate, "The takeout rate lies in [0, 0.5).");
        }

        if (breakageStep < 0.01m || breakageStep > 1m || decimal.Round(breakageStep, 2) != breakageStep)
        {
            throw new ArgumentOutOfRangeException(nameof(breakageStep), breakageStep, "The breakage step is a whole number of cents between 0.01 and 1.00.");
        }

        if (minimumReturnRatio < 1m || minimumReturnRatio > 1.5m)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumReturnRatio), minimumReturnRatio, "The minimum return ratio lies between 1.00 and 1.50 of the unit stake.");
        }

        if (placesPaid < 1 || placesPaid > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(placesPaid), placesPaid, "A pool pays between 1 and 3 places.");
        }

        TakeoutRate = takeoutRate;
        BreakageStep = breakageStep;
        MinimumReturnRatio = minimumReturnRatio;
        PlacesPaid = placesPaid;
    }

    /// <summary>The share of the pool deducted for the operator and the racing levy.</summary>
    public decimal TakeoutRate { get; }

    /// <summary>The dividend per unit is rounded down to a multiple of this step.</summary>
    public decimal BreakageStep { get; }

    /// <summary>The smallest dividend per unit, as a multiple of the unit stake.</summary>
    public decimal MinimumReturnRatio { get; }

    /// <summary>The number of finishing places a Place pool pays.</summary>
    public int PlacesPaid { get; }
}
