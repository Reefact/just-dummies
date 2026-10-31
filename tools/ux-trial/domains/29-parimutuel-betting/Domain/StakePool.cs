namespace ParimutuelBetting.Domain;

/// <summary>
/// The pool of all stakes of one bet type on one race. Stakes are accepted while it is open;
/// once closed at the off, it is settled against the official result.
/// </summary>
public sealed class StakePool
{
    private readonly List<Stake> stakes = new List<Stake>();

    public StakePool(Race race, BetType betType, decimal unitStake, PoolRules rules)
    {
        ArgumentNullException.ThrowIfNull(race);
        ArgumentNullException.ThrowIfNull(rules);
        if (!Enum.IsDefined(betType))
        {
            throw new ArgumentOutOfRangeException(nameof(betType), betType, "Unknown bet type.");
        }

        if (unitStake <= 0m || unitStake > 100m || decimal.Round(unitStake, 2) != unitStake)
        {
            throw new ArgumentOutOfRangeException(nameof(unitStake), unitStake, "The unit stake is a whole number of cents between 0.01 and 100.00.");
        }

        if (betType.Arity() > race.FieldSize)
        {
            throw new ArgumentException($"A {betType} pool needs at least {betType.Arity()} runners; {race.Code} has {race.FieldSize}.", nameof(betType));
        }

        if (betType == BetType.Place && rules.PlacesPaid >= race.FieldSize)
        {
            throw new ArgumentException($"A Place pool paying {rules.PlacesPaid} places needs more than {rules.PlacesPaid} runners; {race.Code} has {race.FieldSize}.", nameof(rules));
        }

        Race = race;
        BetType = betType;
        UnitStake = unitStake;
        Rules = rules;
        Status = PoolStatus.Open;
    }

    public Race Race { get; }

    public BetType BetType { get; }

    public decimal UnitStake { get; }

    public PoolRules Rules { get; }

    public PoolStatus Status { get; private set; }

    public IReadOnlyList<Stake> Stakes => stakes;

    /// <summary>The money collected so far.</summary>
    public decimal GrossAmount => stakes.Sum(stake => stake.Units) * UnitStake;

    public void Accept(Stake stake)
    {
        ArgumentNullException.ThrowIfNull(stake);
        if (Status != PoolStatus.Open)
        {
            throw new InvalidOperationException($"The {BetType} pool of {Race.Code} is {Status}; it no longer accepts stakes.");
        }

        if (stake.Combination.BetType != BetType)
        {
            throw new ArgumentException($"A {stake.Combination.BetType} stake cannot enter a {BetType} pool.", nameof(stake));
        }

        if (stake.Combination.Horses.Any(horse => !Race.IsRunner(horse)))
        {
            throw new ArgumentException($"{stake.Combination} names a horse that is not a runner of {Race.Code}.", nameof(stake));
        }

        stakes.Add(stake);
    }

    /// <summary>Closes the pool at the off.</summary>
    public void Close()
    {
        if (Status != PoolStatus.Open)
        {
            throw new InvalidOperationException($"The {BetType} pool of {Race.Code} is already {Status}.");
        }

        Status = PoolStatus.Closed;
    }

    /// <summary>
    /// Settles the closed pool: stakes naming a scratched horse are refunded, the takeout is
    /// deducted, and the net pool is shared among winning units, rounded down to the breakage step
    /// and raised to the minimum return. When nobody won, the net pool is carried over.
    /// </summary>
    public Settlement Settle(RaceResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (Status != PoolStatus.Closed)
        {
            throw new InvalidOperationException($"Only a closed pool can be settled; the {BetType} pool of {Race.Code} is {Status}.");
        }

        if (result.Race.Code != Race.Code)
        {
            throw new ArgumentException($"The result of {result.Race.Code} cannot settle a pool on {Race.Code}.", nameof(result));
        }

        decimal grossPool = GrossAmount;
        List<Stake> running = stakes.Where(stake => !stake.Combination.Involves(result.Scratched)).ToList();
        decimal refunded = grossPool - (running.Sum(stake => stake.Units) * UnitStake);
        decimal deductions = decimal.Round((grossPool - refunded) * Rules.TakeoutRate, 2, MidpointRounding.ToZero);
        decimal netPool = grossPool - refunded - deductions;

        IReadOnlyList<Combination> winners = WinningCombinations(result);
        List<(Combination Combination, int Units)> backed = winners
            .Select(winner => (winner, running.Where(stake => stake.Combination.Equals(winner)).Sum(stake => stake.Units)))
            .Where(pair => pair.Item2 > 0)
            .ToList();

        Status = PoolStatus.Settled;
        if (backed.Count == 0)
        {
            return new Settlement(grossPool, refunded, deductions, Array.Empty<Dividend>(), netPool);
        }

        decimal share = netPool / backed.Count;
        decimal minimum = decimal.Round(UnitStake * Rules.MinimumReturnRatio, 2, MidpointRounding.AwayFromZero);
        List<Dividend> dividends = backed
            .Select(pair => new Dividend(pair.Combination, Math.Max(minimum, Breakage(share / pair.Units)), pair.Units))
            .ToList();
        return new Settlement(grossPool, refunded, deductions, dividends, 0m);
    }

    private IReadOnlyList<Combination> WinningCombinations(RaceResult result)
    {
        if (BetType != BetType.Place)
        {
            return new[] { result.WinningCombination(BetType) };
        }

        return result.FinishingOrder
            .Take(Rules.PlacesPaid)
            .Select(horse => new Combination(BetType.Place, new[] { horse }))
            .ToArray();
    }

    private decimal Breakage(decimal amount) => Math.Floor(amount / Rules.BreakageStep) * Rules.BreakageStep;
}
