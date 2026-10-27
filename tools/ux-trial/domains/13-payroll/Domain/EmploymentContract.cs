namespace Payroll.Domain;

/// <summary>The terms an employee is paid under.</summary>
public sealed class EmploymentContract {

    public static readonly DateOnly EarliestHiringDate = new DateOnly(1970, 1, 1);

    public EmploymentContract(EmployeeId employee, ContractType type, DateOnly hiredOn, DateOnly? endsOn, HourlyRate rate, int weeklyHours) {
        Validate(type, hiredOn, endsOn, weeklyHours);

        Employee    = employee ?? throw new ArgumentNullException(nameof(employee));
        Type        = type;
        HiredOn     = hiredOn;
        EndsOn      = endsOn;
        Rate        = rate ?? throw new ArgumentNullException(nameof(rate));
        WeeklyHours = weeklyHours;
    }

    public EmployeeId   Employee    { get; }
    public ContractType Type        { get; }
    public DateOnly     HiredOn     { get; }
    public DateOnly?    EndsOn      { get; }
    public HourlyRate   Rate        { get; }
    public int          WeeklyHours { get; }

    /// <summary>The hours a month is paid at the base rate: weekly hours × 52 / 12, to the hundredth.</summary>
    public decimal MonthlyContractualHours => decimal.Round(WeeklyHours * 52m / 12m, 2);

    public int CompletedYearsOfService(DateOnly asOf) {
        int years = asOf.Year - HiredOn.Year;
        if (asOf < HiredOn.AddYears(years)) {
            years--;
        }

        return Math.Max(0, years);
    }

    private static void Validate(ContractType type, DateOnly hiredOn, DateOnly? endsOn, int weeklyHours) {
        if (!Enum.IsDefined(type)) {
            throw new ArgumentOutOfRangeException(nameof(type));
        }
        if (hiredOn < EarliestHiringDate) {
            throw new ArgumentOutOfRangeException(nameof(hiredOn), hiredOn, "A hiring date cannot precede 1970.");
        }

        int maximumWeeklyHours = type switch {
            ContractType.Apprenticeship => 35,
            ContractType.Internship     => 35,
            _                           => 48
        };
        if (weeklyHours < 1 || weeklyHours > maximumWeeklyHours) {
            throw new ArgumentOutOfRangeException(nameof(weeklyHours), weeklyHours, $"Weekly hours lie between 1 and {maximumWeeklyHours} for {type}.");
        }

        bool needsEnd = type != ContractType.Permanent;
        if (needsEnd && endsOn is null) {
            throw new ArgumentException($"A {type} contract has an end date.", nameof(endsOn));
        }
        if (!needsEnd && endsOn is not null) {
            throw new ArgumentException("A permanent contract has no end date.", nameof(endsOn));
        }
        if (endsOn is { } end && end <= hiredOn) {
            throw new ArgumentException("A contract ends after it starts.", nameof(endsOn));
        }
    }

}
