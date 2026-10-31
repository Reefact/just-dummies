namespace LiquidityGap.Domain;

/// <summary>
/// A regular schedule of equal contractual payments (a loan repaying, a bond paying coupons),
/// expanded into dated flows under a business-day convention.
/// </summary>
public sealed class CashFlowSchedule
{
    private static readonly int[] AllowedFrequencies = [1, 3, 6, 12];

    public CashFlowSchedule(
        string scheduleId,
        DateOnly firstPaymentDate,
        int frequencyMonths,
        int numberOfPayments,
        decimal amountPerPayment,
        Currency currency,
        FlowDirection direction,
        BusinessDayConvention convention)
    {
        if (string.IsNullOrWhiteSpace(scheduleId))
        {
            throw new ArgumentException("A schedule needs an identifier.", nameof(scheduleId));
        }

        if (!AllowedFrequencies.Contains(frequencyMonths))
        {
            throw new ArgumentOutOfRangeException(nameof(frequencyMonths), frequencyMonths, "A payment frequency is 1, 3, 6 or 12 months.");
        }

        if (numberOfPayments is < 1 or > 600)
        {
            throw new ArgumentOutOfRangeException(nameof(numberOfPayments), numberOfPayments, "A schedule has between 1 and 600 payments.");
        }

        if (amountPerPayment <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amountPerPayment), amountPerPayment, "A payment amount is strictly positive.");
        }

        if (!Enum.IsDefined(direction))
        {
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown flow direction.");
        }

        if (!Enum.IsDefined(convention))
        {
            throw new ArgumentOutOfRangeException(nameof(convention), convention, "Unknown business-day convention.");
        }

        ScheduleId = scheduleId;
        FirstPaymentDate = firstPaymentDate;
        FrequencyMonths = frequencyMonths;
        NumberOfPayments = numberOfPayments;
        AmountPerPayment = amountPerPayment;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        Direction = direction;
        Convention = convention;
    }

    public string ScheduleId { get; }

    public DateOnly FirstPaymentDate { get; }

    public int FrequencyMonths { get; }

    public int NumberOfPayments { get; }

    public decimal AmountPerPayment { get; }

    public Currency Currency { get; }

    public FlowDirection Direction { get; }

    public BusinessDayConvention Convention { get; }

    /// <summary>The unadjusted date of the last payment.</summary>
    public DateOnly LastPaymentDate => FirstPaymentDate.AddMonths((NumberOfPayments - 1) * FrequencyMonths);

    /// <summary>Expands the schedule into one flow per payment, identified "{ScheduleId}-{n}" from 1.</summary>
    public IReadOnlyList<ContractualFlow> Flows()
    {
        List<ContractualFlow> flows = new(NumberOfPayments);
        for (int n = 0; n < NumberOfPayments; n++)
        {
            DateOnly unadjusted = FirstPaymentDate.AddMonths(n * FrequencyMonths);
            flows.Add(new ContractualFlow($"{ScheduleId}-{n + 1}", Adjust(unadjusted, Convention), AmountPerPayment, Currency, Direction));
        }

        return flows;
    }

    /// <summary>Moves a weekend date to a business day under the given convention (no holiday calendar).</summary>
    public static DateOnly Adjust(DateOnly date, BusinessDayConvention convention)
    {
        if (!IsWeekend(date))
        {
            return date;
        }

        switch (convention)
        {
            case BusinessDayConvention.Unadjusted:
                return date;
            case BusinessDayConvention.Following:
                return Roll(date, 1);
            case BusinessDayConvention.Preceding:
                return Roll(date, -1);
            case BusinessDayConvention.ModifiedFollowing:
                DateOnly following = Roll(date, 1);
                return following.Month == date.Month ? following : Roll(date, -1);
            default:
                throw new ArgumentOutOfRangeException(nameof(convention), convention, "Unknown business-day convention.");
        }
    }

    private static DateOnly Roll(DateOnly date, int step)
    {
        DateOnly rolled = date;
        while (IsWeekend(rolled))
        {
            rolled = rolled.AddDays(step);
        }

        return rolled;
    }

    private static bool IsWeekend(DateOnly date) => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
}
