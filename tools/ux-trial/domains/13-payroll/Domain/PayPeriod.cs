namespace Payroll.Domain;

/// <summary>A calendar month that a payslip covers.</summary>
public sealed class PayPeriod {

    public PayPeriod(int year, int month) {
        if (year < 2000 || year > 2100) {
            throw new ArgumentOutOfRangeException(nameof(year), year, "A pay period lies between 2000 and 2100.");
        }
        if (month is < 1 or > 12) {
            throw new ArgumentOutOfRangeException(nameof(month), month, "A month is between 1 and 12.");
        }

        Year  = year;
        Month = month;
    }

    public int Year  { get; }
    public int Month { get; }

    public DateOnly FirstDay => new DateOnly(Year, Month, 1);
    public DateOnly LastDay  => FirstDay.AddMonths(1).AddDays(-1);

}
