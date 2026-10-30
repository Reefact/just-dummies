namespace Payroll.Domain;

/// <summary>A month's pay for one contract: hours worked, bonuses and deductions.</summary>
public sealed class Payslip {

    public const decimal OvertimePremium = 1.25m;

    private readonly List<PayslipLine> _lines;

    public Payslip(EmploymentContract contract, PayPeriod period, decimal hoursWorked, IReadOnlyList<PayslipLine> lines) {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentOutOfRangeException.ThrowIfNegative(hoursWorked);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(hoursWorked, 300m);
        if (period.LastDay < contract.HiredOn) {
            throw new ArgumentException("A payslip cannot cover a month before the hiring date.", nameof(period));
        }

        Contract    = contract;
        Period      = period;
        HoursWorked = hoursWorked;
        _lines      = new List<PayslipLine>(lines);
        Status      = PayslipStatus.Draft;
    }

    public EmploymentContract         Contract    { get; }
    public PayPeriod                  Period      { get; }
    public decimal                    HoursWorked { get; }
    public IReadOnlyList<PayslipLine> Lines       => _lines;
    public PayslipStatus              Status      { get; private set; }

    public decimal OvertimeHours => Math.Max(0m, HoursWorked - Contract.MonthlyContractualHours);

    public decimal BasePay => decimal.Round((HoursWorked - OvertimeHours) * Contract.Rate.Value, 2, MidpointRounding.AwayFromZero);

    public decimal OvertimePay => decimal.Round(OvertimeHours * Contract.Rate.Value * OvertimePremium, 2, MidpointRounding.AwayFromZero);

    public decimal TotalBonuses => _lines.Where(line => line.Kind == LineKind.Bonus).Sum(line => line.Amount);

    public decimal TotalDeductions => _lines.Where(line => line.Kind == LineKind.Deduction).Sum(line => line.Amount);

    public decimal GrossPay => BasePay + OvertimePay + TotalBonuses;

    /// <summary>Net pay never goes below zero: an excess of deductions is carried over, not paid back.</summary>
    public decimal NetPay => Math.Max(0m, GrossPay - TotalDeductions);

    public void AddLine(PayslipLine line) {
        ArgumentNullException.ThrowIfNull(line);
        if (Status == PayslipStatus.Issued) {
            throw new InvalidOperationException("An issued payslip cannot be changed.");
        }

        _lines.Add(line);
    }

    public void Issue() {
        if (Status == PayslipStatus.Issued) {
            throw new InvalidOperationException("The payslip is already issued.");
        }

        Status = PayslipStatus.Issued;
    }

}
