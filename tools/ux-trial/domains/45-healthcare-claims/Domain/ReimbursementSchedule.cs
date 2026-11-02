namespace HealthcareClaims.Domain;

/// <summary>
///     The policy's guarantee table: a reimbursement rate per category. A category absent from the table is not
///     covered and reimburses nothing.
/// </summary>
public sealed class ReimbursementSchedule : IEquatable<ReimbursementSchedule> {

    private readonly Dictionary<ReimbursementCategory, ReimbursementRate> _rates;

    public ReimbursementSchedule(IReadOnlyDictionary<ReimbursementCategory, ReimbursementRate> rates) {
        ArgumentNullException.ThrowIfNull(rates);
        if (rates.Count == 0) {
            throw new ArgumentException("A schedule covers at least one category.", nameof(rates));
        }
        foreach (KeyValuePair<ReimbursementCategory, ReimbursementRate> entry in rates) {
            if (!Enum.IsDefined(entry.Key)) {
                throw new ArgumentOutOfRangeException(nameof(rates), entry.Key, "Unknown reimbursement category.");
            }
            if (entry.Value is null) {
                throw new ArgumentException($"The rate for {entry.Key} is missing.", nameof(rates));
            }
        }

        _rates = new Dictionary<ReimbursementCategory, ReimbursementRate>(rates);
    }

    public IReadOnlyDictionary<ReimbursementCategory, ReimbursementRate> Rates => _rates;

    public bool Covers(ReimbursementCategory category) => _rates.ContainsKey(category);

    public ReimbursementRate? RateFor(ReimbursementCategory category) => _rates.GetValueOrDefault(category);

    public bool Equals(ReimbursementSchedule? other) {
        if (other is null || other._rates.Count != _rates.Count) {
            return false;
        }

        return _rates.All(entry => other._rates.TryGetValue(entry.Key, out ReimbursementRate? rate) && rate.Equals(entry.Value));
    }

    public override bool Equals(object? obj) => Equals(obj as ReimbursementSchedule);

    public override int GetHashCode() {
        HashCode hash = new();
        foreach (KeyValuePair<ReimbursementCategory, ReimbursementRate> entry in _rates.OrderBy(entry => entry.Key)) {
            hash.Add(entry.Key);
            hash.Add(entry.Value);
        }

        return hash.ToHashCode();
    }

}
