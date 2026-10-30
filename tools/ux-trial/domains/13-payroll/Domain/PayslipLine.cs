namespace Payroll.Domain;

/// <summary>A bonus or a deduction printed on a payslip.</summary>
public sealed class PayslipLine {

    public const int MaximumLabelLength = 40;

    public PayslipLine(string label, LineKind kind, decimal amount) {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        if (label.Length > MaximumLabelLength) {
            throw new ArgumentException($"A label holds at most {MaximumLabelLength} characters.", nameof(label));
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        if (amount != decimal.Round(amount, 2)) {
            throw new ArgumentException("An amount is expressed to the cent.", nameof(amount));
        }

        Label  = label;
        Kind   = kind;
        Amount = amount;
    }

    public string   Label  { get; }
    public LineKind Kind   { get; }
    public decimal  Amount { get; }

    /// <summary>The effect of the line on the net pay.</summary>
    public decimal SignedAmount => Kind switch {
        LineKind.Bonus     => Amount,
        LineKind.Deduction => -Amount,
        _                  => throw new InvalidOperationException($"Unknown line kind {Kind}.")
    };

}
