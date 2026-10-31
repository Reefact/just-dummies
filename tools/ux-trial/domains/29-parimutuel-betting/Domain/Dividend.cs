namespace ParimutuelBetting.Domain;

/// <summary>The amount paid per unit stake on a winning combination.</summary>
public sealed class Dividend
{
    public Dividend(Combination combination, decimal amountPerUnit, int winningUnits)
    {
        ArgumentNullException.ThrowIfNull(combination);
        if (amountPerUnit <= 0m || decimal.Round(amountPerUnit, 2) != amountPerUnit)
        {
            throw new ArgumentOutOfRangeException(nameof(amountPerUnit), amountPerUnit, "A dividend is a positive whole number of cents.");
        }

        if (winningUnits < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(winningUnits), winningUnits, "A dividend is declared only for a combination that has winning units.");
        }

        Combination = combination;
        AmountPerUnit = amountPerUnit;
        WinningUnits = winningUnits;
    }

    public Combination Combination { get; }

    public decimal AmountPerUnit { get; }

    public int WinningUnits { get; }

    /// <summary>The total paid out on this combination.</summary>
    public decimal Payout => AmountPerUnit * WinningUnits;
}
