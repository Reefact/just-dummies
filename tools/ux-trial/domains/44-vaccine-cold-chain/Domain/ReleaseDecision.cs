using Warehouse.Domain;

namespace ColdChain.Domain;

/// <summary>
/// The quality unit's decision on one pallet at the receiving warehouse. A released pallet has no reason against it;
/// a rejected one has at least one rejecting reason; a quarantined one has reasons, none rejecting, and sits in the
/// quarantine bay. The budget consumed is carried forward with the stock.
/// </summary>
public sealed class ReleaseDecision : IEquatable<ReleaseDecision>
{
    private readonly DecisionReason[] _reasons;

    public ReleaseDecision(Sscc pallet, ReleaseOutcome outcome, IReadOnlyCollection<DecisionReason> reasons, int budgetConsumedMinutes, BinLocation? quarantineBay)
    {
        ArgumentNullException.ThrowIfNull(pallet);
        ArgumentNullException.ThrowIfNull(reasons);
        if (!Enum.IsDefined(outcome))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown outcome.");
        }

        if (reasons.Any(r => !Enum.IsDefined(r)))
        {
            throw new ArgumentOutOfRangeException(nameof(reasons), "Unknown decision reason.");
        }

        if (reasons.Distinct().Count() != reasons.Count)
        {
            throw new ArgumentException("A reason is stated once.", nameof(reasons));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(budgetConsumedMinutes);

        bool rejecting = reasons.Any(IsRejecting);
        switch (outcome)
        {
            case ReleaseOutcome.Released when reasons.Count != 0:
                throw new ArgumentException("A released pallet has no reason against it.", nameof(reasons));
            case ReleaseOutcome.Rejected when !rejecting:
                throw new ArgumentException("A rejection needs a rejecting reason (hard limit, exhausted budget or expired pack).", nameof(reasons));
            case ReleaseOutcome.Quarantined when reasons.Count == 0 || rejecting:
                throw new ArgumentException("A quarantine needs at least one reason, none of them rejecting.", nameof(reasons));
        }

        if ((outcome == ReleaseOutcome.Quarantined) != (quarantineBay is not null))
        {
            throw new ArgumentException("A quarantined pallet, and only a quarantined pallet, is assigned a quarantine bay.", nameof(quarantineBay));
        }

        Pallet = pallet;
        Outcome = outcome;
        _reasons = reasons.OrderBy(r => r).ToArray();
        BudgetConsumedMinutes = budgetConsumedMinutes;
        QuarantineBay = quarantineBay;
    }

    public Sscc Pallet { get; }

    public ReleaseOutcome Outcome { get; }

    public IReadOnlyList<DecisionReason> Reasons => _reasons;

    public int BudgetConsumedMinutes { get; }

    public BinLocation? QuarantineBay { get; }

    /// <summary>Hard-limit breach, exhausted budget and expired packs reject; every other reason holds the pallet for review.</summary>
    public static bool IsRejecting(DecisionReason reason) =>
        reason is DecisionReason.HardLimitBreached or DecisionReason.StabilityBudgetExhausted or DecisionReason.PackExpired;

    /// <summary>The outcome the reasons call for: rejected if one rejects, quarantined if any, released otherwise.</summary>
    public static ReleaseOutcome OutcomeFor(IReadOnlyCollection<DecisionReason> reasons)
    {
        ArgumentNullException.ThrowIfNull(reasons);
        if (reasons.Any(IsRejecting))
        {
            return ReleaseOutcome.Rejected;
        }

        return reasons.Count == 0 ? ReleaseOutcome.Released : ReleaseOutcome.Quarantined;
    }

    public bool Equals(ReleaseDecision? other) =>
        other is not null
        && other.Pallet.Equals(Pallet)
        && other.Outcome == Outcome
        && other._reasons.SequenceEqual(_reasons)
        && other.BudgetConsumedMinutes == BudgetConsumedMinutes
        && Equals(other.QuarantineBay, QuarantineBay);

    public override bool Equals(object? obj) => Equals(obj as ReleaseDecision);

    public override int GetHashCode() => HashCode.Combine(Pallet, Outcome, _reasons.Length, BudgetConsumedMinutes);

    public override string ToString() =>
        _reasons.Length == 0 ? $"{Pallet}: {Outcome}" : $"{Pallet}: {Outcome} ({string.Join(", ", _reasons)})";
}
