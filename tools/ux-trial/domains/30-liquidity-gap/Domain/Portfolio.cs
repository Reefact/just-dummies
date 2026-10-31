namespace LiquidityGap.Domain;

/// <summary>
/// A banking book reported in one currency at one as-of date: contractual flows, schedules and
/// non-maturity deposits. It is open while positions are loaded, then frozen at the reporting
/// cut-off; it can be projected into a liquidity profile per scenario at any time.
/// </summary>
public sealed class Portfolio
{
    private readonly List<ContractualFlow> flows = [];
    private readonly List<NonMaturityDeposit> deposits = [];
    private readonly HashSet<string> positionIds = new(StringComparer.Ordinal);

    public Portfolio(string portfolioId, Currency reportingCurrency, DateOnly asOfDate)
    {
        if (string.IsNullOrWhiteSpace(portfolioId))
        {
            throw new ArgumentException("A portfolio needs an identifier.", nameof(portfolioId));
        }

        PortfolioId = portfolioId;
        ReportingCurrency = reportingCurrency ?? throw new ArgumentNullException(nameof(reportingCurrency));
        AsOfDate = asOfDate;
    }

    public string PortfolioId { get; }

    public Currency ReportingCurrency { get; }

    public DateOnly AsOfDate { get; }

    public bool IsFrozen { get; private set; }

    public IReadOnlyList<ContractualFlow> ContractualFlows => flows;

    public IReadOnlyList<NonMaturityDeposit> Deposits => deposits;

    /// <summary>Adds a contractual flow, refusing a frozen portfolio, a foreign currency, a past value date or a duplicate id.</summary>
    public void AddFlow(ContractualFlow flow)
    {
        ArgumentNullException.ThrowIfNull(flow);
        EnsureOpen();
        EnsureCurrency(flow.Currency, flow.FlowId);
        if (flow.ValueDate < AsOfDate)
        {
            throw new ArgumentException($"Flow '{flow.FlowId}' settles on {flow.ValueDate}, before the as-of date {AsOfDate}.", nameof(flow));
        }

        EnsureNewId(flow.FlowId);
        flows.Add(flow);
    }

    /// <summary>Adds every flow of a schedule that settles on or after the as-of date; earlier payments are history.</summary>
    public void AddSchedule(CashFlowSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        EnsureOpen();
        EnsureCurrency(schedule.Currency, schedule.ScheduleId);
        List<ContractualFlow> future = schedule.Flows().Where(f => f.ValueDate >= AsOfDate).ToList();
        string? clash = future.Select(static f => f.FlowId).FirstOrDefault(positionIds.Contains);
        if (clash is not null)
        {
            throw new ArgumentException($"Position '{clash}' is already in portfolio '{PortfolioId}'.", nameof(schedule));
        }

        foreach (ContractualFlow flow in future)
        {
            positionIds.Add(flow.FlowId);
        }

        flows.AddRange(future);
    }

    /// <summary>Adds a non-maturity deposit, refusing a frozen portfolio, a foreign currency or a duplicate id.</summary>
    public void AddDeposit(NonMaturityDeposit deposit)
    {
        ArgumentNullException.ThrowIfNull(deposit);
        EnsureOpen();
        EnsureCurrency(deposit.Currency, deposit.DepositId);
        EnsureNewId(deposit.DepositId);
        deposits.Add(deposit);
    }

    /// <summary>Closes the portfolio to new positions at the reporting cut-off. Freezing twice is refused.</summary>
    public void Freeze()
    {
        EnsureOpen();
        IsFrozen = true;
    }

    /// <summary>
    /// Projects the portfolio into the bucket ladder under a scenario: contractual inflows are cut by
    /// the inflow haircut, contractual outflows scaled by the outflow multiplier, deposits spread into
    /// behavioural outflows. Every flow lands in the bucket that contains its distance in days.
    /// </summary>
    public LiquidityProfile Project(Scenario scenario, IReadOnlyList<TimeBucket> buckets)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        TimeBucket.EnsureLadder(buckets);

        decimal[] inflows = new decimal[buckets.Count];
        decimal[] outflows = new decimal[buckets.Count];

        foreach (ContractualFlow flow in flows)
        {
            int index = BucketIndex(buckets, flow.ValueDate);
            if (flow.Direction == FlowDirection.Inflow)
            {
                inflows[index] += decimal.Round(flow.Amount * (1m - scenario.InflowHaircut), 2);
            }
            else
            {
                outflows[index] += decimal.Round(flow.Amount * scenario.OutflowMultiplier, 2);
            }
        }

        foreach (NonMaturityDeposit deposit in deposits)
        {
            foreach (ContractualFlow flow in deposit.ToBehaviouralFlows(AsOfDate, scenario))
            {
                outflows[BucketIndex(buckets, flow.ValueDate)] += flow.Amount;
            }
        }

        List<BucketGap> gaps = new(buckets.Count);
        decimal cumulative = 0m;
        for (int i = 0; i < buckets.Count; i++)
        {
            cumulative += inflows[i] - outflows[i];
            gaps.Add(new BucketGap(buckets[i], inflows[i], outflows[i], cumulative));
        }

        return new LiquidityProfile(scenario.Name, ReportingCurrency, gaps);
    }

    /// <summary>Projects the same portfolio under several scenarios, one profile each; scenario names must be distinct.</summary>
    public IReadOnlyList<LiquidityProfile> ProjectAll(IReadOnlyList<Scenario> scenarios, IReadOnlyList<TimeBucket> buckets)
    {
        ArgumentNullException.ThrowIfNull(scenarios);
        if (scenarios.Count == 0)
        {
            throw new ArgumentException("At least one scenario is needed.", nameof(scenarios));
        }

        if (scenarios.Select(static s => s.Name).Distinct(StringComparer.Ordinal).Count() != scenarios.Count)
        {
            throw new ArgumentException("Scenario names must be distinct.", nameof(scenarios));
        }

        return scenarios.Select(s => Project(s, buckets)).ToList();
    }

    private int BucketIndex(IReadOnlyList<TimeBucket> buckets, DateOnly valueDate)
    {
        int day = valueDate.DayNumber - AsOfDate.DayNumber;
        for (int i = 0; i < buckets.Count; i++)
        {
            if (buckets[i].Contains(day))
            {
                return i;
            }
        }

        throw new InvalidOperationException($"No bucket contains day {day}.");
    }

    private void EnsureOpen()
    {
        if (IsFrozen)
        {
            throw new InvalidOperationException($"Portfolio '{PortfolioId}' is frozen.");
        }
    }

    private void EnsureCurrency(Currency currency, string positionId)
    {
        if (!currency.Equals(ReportingCurrency))
        {
            throw new ArgumentException($"Position '{positionId}' is in {currency}, but portfolio '{PortfolioId}' reports in {ReportingCurrency}.");
        }
    }

    private void EnsureNewId(string positionId)
    {
        if (!positionIds.Add(positionId))
        {
            throw new ArgumentException($"Position '{positionId}' is already in portfolio '{PortfolioId}'.");
        }
    }
}
