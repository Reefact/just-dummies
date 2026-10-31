using System.Text.RegularExpressions;

namespace CatastropheLosses.Domain;

/// <summary>
/// The aggregate the analysis runs on. Exposures are added while it is open; it is then frozen as
/// the snapshot the loss model may run on, and refuses any further change.
/// </summary>
public sealed partial class Portfolio
{
    private readonly List<Exposure> _exposures = [];
    private readonly HashSet<string> _propertyIds = new(StringComparer.Ordinal);

    public Portfolio(string name, string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(currency);

        if (name.Length > 100)
        {
            throw new ArgumentException("A portfolio name holds at most 100 characters.", nameof(name));
        }

        if (!CurrencyFormat().IsMatch(currency))
        {
            throw new FormatException($"Currency '{currency}' must be an ISO 4217 code: three upper-case letters.");
        }

        Name = name;
        Currency = currency;
    }

    public string Name { get; }

    public string Currency { get; }

    public bool IsFrozen { get; private set; }

    public IReadOnlyList<Exposure> Exposures => _exposures;

    public decimal TotalInsuredValue => _exposures.Sum(e => e.Property.InsuredValue);

    public decimal TotalLimit => _exposures.Sum(e => e.Terms.Limit);

    public void Add(Exposure exposure)
    {
        ArgumentNullException.ThrowIfNull(exposure);

        if (IsFrozen)
        {
            throw new InvalidOperationException($"Portfolio '{Name}' is frozen: no exposure can be added.");
        }

        if (!_propertyIds.Add(exposure.Property.Id))
        {
            throw new InvalidOperationException($"Property {exposure.Property.Id} is already exposed in portfolio '{Name}'.");
        }

        _exposures.Add(exposure);
    }

    public void Freeze()
    {
        if (IsFrozen)
        {
            throw new InvalidOperationException($"Portfolio '{Name}' is already frozen.");
        }

        if (_exposures.Count == 0)
        {
            throw new InvalidOperationException($"Portfolio '{Name}' has no exposure: there is nothing to analyse.");
        }

        IsFrozen = true;
    }

    /// <summary>Total insured loss of one occurrence: every exposure under the same footprint and the same severity.</summary>
    public decimal EventLoss(CatastropheEvent catastrophe, double severityFactor)
    {
        ArgumentNullException.ThrowIfNull(catastrophe);
        decimal total = 0m;
        foreach (Exposure exposure in _exposures)
        {
            total += exposure.InsuredLoss(catastrophe.Footprint, severityFactor);
        }

        return total;
    }

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex CurrencyFormat();
}
