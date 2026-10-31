namespace LiquidityGap.Domain;

/// <summary>
/// A deposit with no contractual maturity (current or savings accounts). It produces no contractual
/// flow; its balance is turned into behavioural outflows spread over several maturities.
/// </summary>
public sealed class NonMaturityDeposit
{
    public NonMaturityDeposit(string depositId, decimal balance, Currency currency, BehaviouralAssumption assumption)
    {
        if (string.IsNullOrWhiteSpace(depositId))
        {
            throw new ArgumentException("A deposit needs an identifier.", nameof(depositId));
        }

        if (balance <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(balance), balance, "A deposit balance is strictly positive.");
        }

        if (decimal.Round(balance, 2) != balance)
        {
            throw new ArgumentException("A deposit balance has at most two decimal places.", nameof(balance));
        }

        DepositId = depositId;
        Balance = balance;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        Assumption = assumption ?? throw new ArgumentNullException(nameof(assumption));
    }

    public string DepositId { get; }

    public decimal Balance { get; }

    public Currency Currency { get; }

    public BehaviouralAssumption Assumption { get; }

    /// <summary>
    /// Spreads the balance into outflows under a scenario. The scenario's haircut moves part of the
    /// stable share into the volatile share. The volatile part leaves at asOf + VolatileRunOffDays;
    /// the stable part leaves in CoreMaturityMonths monthly instalments, the last one absorbing
    /// rounding. The outflows always sum to the balance exactly.
    /// </summary>
    public IReadOnlyList<ContractualFlow> ToBehaviouralFlows(DateOnly asOfDate, Scenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        decimal effectiveStableShare = Assumption.StableShare * (1m - scenario.DepositStableShareHaircut);
        decimal stable = decimal.Round(Balance * effectiveStableShare, 2, MidpointRounding.ToZero);
        decimal volatilePart = Balance - stable;

        List<ContractualFlow> flows = [];
        if (volatilePart > 0m)
        {
            flows.Add(new ContractualFlow($"{DepositId}-volatile", asOfDate.AddDays(Assumption.VolatileRunOffDays), volatilePart, Currency, FlowDirection.Outflow));
        }

        if (stable > 0m)
        {
            int months = Assumption.CoreMaturityMonths;
            decimal instalment = decimal.Round(stable / months, 2, MidpointRounding.ToZero);
            decimal allocated = 0m;
            for (int m = 1; m <= months; m++)
            {
                decimal amount = m == months ? stable - allocated : instalment;
                allocated += amount;
                if (amount > 0m)
                {
                    flows.Add(new ContractualFlow($"{DepositId}-core-{m}", asOfDate.AddMonths(m), amount, Currency, FlowDirection.Outflow));
                }
            }
        }

        return flows;
    }
}
