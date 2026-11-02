using System.Text.RegularExpressions;

namespace PassiveComponents.Domain;

/// <summary>
/// The bill of materials of one board revision: the aggregate root. Parts are placed on reference designators,
/// substituted under the substitution rule (always judged against the part the design calls for, so chained
/// substitutions cannot drift), totalled per line, and checked against the preferred-value series. Once released,
/// the bill refuses every change.
/// </summary>
public sealed partial class BillOfMaterials
{
    public const int MaxBoardNameLength = 64;

    private readonly SortedDictionary<ReferenceDesignator, Placement> placements = new();

    public BillOfMaterials(string boardName, string revision)
    {
        if (string.IsNullOrWhiteSpace(boardName))
        {
            throw new ArgumentException("A board has a name.", nameof(boardName));
        }

        if (boardName.Length > MaxBoardNameLength || boardName != boardName.Trim())
        {
            throw new ArgumentException($"A board name is at most {MaxBoardNameLength} characters, with no leading or trailing space.", nameof(boardName));
        }

        ArgumentNullException.ThrowIfNull(revision);
        if (!RevisionFormat().IsMatch(revision))
        {
            throw new FormatException($"A revision is one or two capital letters (A, B, … AA); got '{revision}'.");
        }

        BoardName = boardName;
        Revision = revision;
    }

    public string BoardName { get; }

    public string Revision { get; }

    public bool IsReleased { get; private set; }

    /// <summary>Every placement, in designator order (C before L before R, then numerically).</summary>
    public IReadOnlyCollection<Placement> Placements => placements.Values;

    public int PlacementCount => placements.Count;

    public Placement? Find(ReferenceDesignator designator)
    {
        ArgumentNullException.ThrowIfNull(designator);
        return placements.GetValueOrDefault(designator);
    }

    /// <summary>
    /// Places a part. Refused once released, on a designator already used, or when the board already holds a
    /// different specification under the same part number; the designator's letter must match the part's kind.
    /// </summary>
    public Placement Place(ReferenceDesignator designator, Part part)
    {
        ArgumentNullException.ThrowIfNull(designator);
        ArgumentNullException.ThrowIfNull(part);
        RequireOpen();
        if (placements.ContainsKey(designator))
        {
            throw new InvalidOperationException($"{designator} is already placed.");
        }

        Placement placement = new(designator, part);
        RequireConsistentDefinition(part);
        placements.Add(designator, placement);
        return placement;
    }

    public void Remove(ReferenceDesignator designator)
    {
        ArgumentNullException.ThrowIfNull(designator);
        RequireOpen();
        if (!placements.Remove(designator))
        {
            throw new InvalidOperationException($"{designator} is not placed.");
        }
    }

    /// <summary>What substituting would answer, without changing anything: <see cref="SubstitutionRefusal.None"/> when allowed.</summary>
    public SubstitutionRefusal CheckSubstitution(ReferenceDesignator designator, Part substitute)
    {
        ArgumentNullException.ThrowIfNull(designator);
        ArgumentNullException.ThrowIfNull(substitute);
        Placement placement = Require(designator);
        return string.Equals(substitute.PartNumber, placement.FittedPart.PartNumber, StringComparison.Ordinal)
            ? SubstitutionRefusal.SamePart
            : placement.DesignedPart.CheckSubstitute(substitute);
    }

    /// <summary>
    /// Fits <paramref name="substitute"/> in place of the current part. The rule is judged against the designed part;
    /// a refusal throws <see cref="SubstitutionRefusedException"/> with its reason.
    /// </summary>
    public Placement Substitute(ReferenceDesignator designator, Part substitute)
    {
        ArgumentNullException.ThrowIfNull(designator);
        ArgumentNullException.ThrowIfNull(substitute);
        RequireOpen();
        Placement placement = Require(designator);
        SubstitutionRefusal refusal = CheckSubstitution(designator, substitute);
        if (refusal != SubstitutionRefusal.None)
        {
            throw new SubstitutionRefusedException(refusal, $"{substitute.PartNumber} cannot replace {placement.DesignedPart.PartNumber} on {designator}: {refusal}.");
        }

        RequireConsistentDefinition(substitute);
        placement.Fit(substitute);
        return placement;
    }

    /// <summary>Puts the designed part back; refused when the designator is not substituted.</summary>
    public Placement RevertSubstitution(ReferenceDesignator designator)
    {
        ArgumentNullException.ThrowIfNull(designator);
        RequireOpen();
        Placement placement = Require(designator);
        if (!placement.IsSubstituted)
        {
            throw new InvalidOperationException($"{designator} carries its designed part.");
        }

        placement.Fit(placement.DesignedPart);
        return placement;
    }

    /// <summary>Freezes the bill for manufacturing; refused when empty or already released.</summary>
    public void Release()
    {
        RequireOpen();
        if (placements.Count == 0)
        {
            throw new InvalidOperationException("An empty bill of materials cannot be released.");
        }

        IsReleased = true;
    }

    /// <summary>One line per fitted part number, ordered by kind, then nominal value, then part number.</summary>
    public IReadOnlyList<BomLine> Lines() =>
        placements.Values
            .GroupBy(p => p.FittedPart.PartNumber, StringComparer.Ordinal)
            .Select(g => new BomLine(g.First().FittedPart, g.Select(p => p.Designator)))
            .OrderBy(l => l.Part.Kind)
            .ThenBy(l => l.Part.NominalValue)
            .ThenBy(l => l.Part.PartNumber, StringComparer.Ordinal)
            .ToList()
            .AsReadOnly();

    public int CountOf(ComponentKind kind) => placements.Values.Count(p => p.FittedPart.Kind == kind);

    public decimal TotalCost() => placements.Values.Sum(p => p.FittedPart.UnitCost);

    public IReadOnlyList<ReferenceDesignator> SubstitutedDesignators() =>
        placements.Values.Where(p => p.IsSubstituted).Select(p => p.Designator).ToList().AsReadOnly();

    /// <summary>Designators whose fitted nominal value is not on the series' grid (relative 1e-9).</summary>
    public IReadOnlyList<ReferenceDesignator> OffSeries(ESeries series) =>
        placements.Values.Where(p => !p.FittedPart.HasPreferredValueOf(series)).Select(p => p.Designator).ToList().AsReadOnly();

    /// <summary>Designators whose fitted part is of the marking's kind and carries its value (relative 1e-9).</summary>
    public IReadOnlyList<ReferenceDesignator> FindByMarking(MarkingCode marking)
    {
        ArgumentNullException.ThrowIfNull(marking);
        return placements.Values
            .Where(p => p.FittedPart.Kind == marking.Kind && Magnitude.AreClose(p.FittedPart.NominalValue, marking.Value))
            .Select(p => p.Designator)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>RC time constant of a placed resistor and capacitor, in seconds, nominal and across both tolerance bands.</summary>
    public (double Minimum, double Nominal, double Maximum) TimeConstantSeconds(ReferenceDesignator resistor, ReferenceDesignator capacitor)
    {
        Part r = RequireKind(resistor, ComponentKind.Resistor);
        Part c = RequireKind(capacitor, ComponentKind.Capacitor);
        return (r.LowerBound * c.LowerBound, r.AsResistance().TimeConstantWith(c.AsCapacitance()), r.UpperBound * c.UpperBound);
    }

    /// <summary>Resonant frequency of a placed inductor and capacitor, in hertz, nominal and across both tolerance bands.</summary>
    public (double Minimum, double Nominal, double Maximum) ResonantFrequencyHertz(ReferenceDesignator inductor, ReferenceDesignator capacitor)
    {
        Part l = RequireKind(inductor, ComponentKind.Inductor);
        Part c = RequireKind(capacitor, ComponentKind.Capacitor);
        return (
            1.0 / (2 * Math.PI * Math.Sqrt(l.UpperBound * c.UpperBound)),
            l.AsInductance().ResonantFrequencyWith(c.AsCapacitance()),
            1.0 / (2 * Math.PI * Math.Sqrt(l.LowerBound * c.LowerBound)));
    }

    public override string ToString() => $"{BoardName} rev {Revision} ({placements.Count} placements{(IsReleased ? ", released" : string.Empty)})";

    private void RequireOpen()
    {
        if (IsReleased)
        {
            throw new InvalidOperationException($"{BoardName} rev {Revision} is released; issue a new revision to change it.");
        }
    }

    private Placement Require(ReferenceDesignator designator) =>
        placements.TryGetValue(designator, out Placement? placement)
            ? placement
            : throw new InvalidOperationException($"{designator} is not placed.");

    private Part RequireKind(ReferenceDesignator designator, ComponentKind kind)
    {
        ArgumentNullException.ThrowIfNull(designator);
        if (designator.Kind != kind)
        {
            throw new ArgumentException($"{designator} does not name a {Part.Describe(kind)}.", nameof(designator));
        }

        return Require(designator).FittedPart;
    }

    private void RequireConsistentDefinition(Part part)
    {
        Part? clash = placements.Values
            .SelectMany(p => new[] { p.DesignedPart, p.FittedPart })
            .FirstOrDefault(p => string.Equals(p.PartNumber, part.PartNumber, StringComparison.Ordinal) && !p.Equals(part));
        if (clash is not null)
        {
            throw new InvalidOperationException($"The board already lists {part.PartNumber} with a different specification.");
        }
    }

    [GeneratedRegex("^[A-Z]{1,2}$")]
    private static partial Regex RevisionFormat();
}
