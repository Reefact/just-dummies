namespace BatchGenealogy.Domain;

/// <summary>
/// The genealogy of a manufacturing site: the lots, and the transformations that consumed and produced them.
/// It is a directed acyclic graph with merges (blends) and splits. A lot is produced by at most one
/// transformation and may feed several.
/// </summary>
public sealed class BatchGenealogy
{
    private readonly Dictionary<LotNumber, Lot> lots = new();
    private readonly List<Transformation> transformations = new();
    private readonly Dictionary<LotNumber, Transformation> producedBy = new();
    private readonly List<ProvenanceLink> links = new();

    public BatchGenealogy(string siteCode)
    {
        ArgumentNullException.ThrowIfNull(siteCode);
        if (siteCode.Length != 4 || !siteCode.All(char.IsAsciiLetterUpper))
        {
            throw new FormatException($"'{siteCode}' is not a site code: expected four upper-case letters.");
        }

        SiteCode = siteCode;
    }

    public string SiteCode { get; }

    public IReadOnlyCollection<Lot> Lots => lots.Values;

    public IReadOnlyList<Transformation> Transformations => transformations;

    public IReadOnlyList<ProvenanceLink> Links => links;

    public void Register(Lot lot)
    {
        ArgumentNullException.ThrowIfNull(lot);
        if (!lots.TryAdd(lot.Number, lot))
        {
            throw new InvalidOperationException($"Lot {lot.Number} is already registered.");
        }
    }

    public Lot Get(LotNumber number)
    {
        ArgumentNullException.ThrowIfNull(number);
        return lots.TryGetValue(number, out Lot? lot) ? lot : throw new KeyNotFoundException($"Lot {number} is not registered.");
    }

    /// <summary>
    /// Records a transformation: every lot must be registered, inputs must be consumable, outputs must be fresh
    /// lots whose quantity is the one produced, and the graph must stay acyclic. Nothing changes on refusal.
    /// </summary>
    public void Record(Transformation transformation)
    {
        ArgumentNullException.ThrowIfNull(transformation);
        if (transformations.Any(existing => existing.Id == transformation.Id))
        {
            throw new InvalidOperationException($"Transformation {transformation.Id} is already recorded.");
        }

        foreach (LotQuantity input in transformation.Inputs)
        {
            Get(input.Lot).EnsureConsumable(input.Quantity, transformation.PerformedOn);
        }

        foreach (LotQuantity output in transformation.Outputs)
        {
            Lot lot = Get(output.Lot);
            if (producedBy.TryGetValue(output.Lot, out Transformation? origin))
            {
                throw new InvalidOperationException($"Lot {output.Lot} was already produced by {origin.Id}.");
            }

            if (!lot.InitialQuantity.Equals(output.Quantity))
            {
                throw new InvalidOperationException($"Lot {output.Lot} was registered with {lot.InitialQuantity} but {transformation.Id} produces {output.Quantity}.");
            }

            if (lot.ManufacturedOn != transformation.PerformedOn)
            {
                throw new InvalidOperationException($"Lot {output.Lot} was manufactured on {lot.ManufacturedOn:yyyy-MM-dd}, not on {transformation.PerformedOn:yyyy-MM-dd}.");
            }

            if (transformations.Any(existing => existing.Inputs.Any(input => input.Lot.Equals(output.Lot))))
            {
                throw new InvalidOperationException($"Lot {output.Lot} was already consumed: it cannot be produced afterwards.");
            }
        }

        if (transformation.Kind == TransformationKind.Split)
        {
            MaterialCode material = Get(transformation.Inputs[0].Lot).Material;
            LotQuantity? other = transformation.Outputs.FirstOrDefault(output => !Get(output.Lot).Material.Equals(material));
            if (other is not null)
            {
                throw new InvalidOperationException($"A split keeps the material {material}; sub-lot {other.Lot} is registered as {Get(other.Lot).Material}.");
            }
        }

        foreach (LotQuantity input in transformation.Inputs)
        {
            Get(input.Lot).Consume(input.Quantity, transformation.PerformedOn);
        }

        transformations.Add(transformation);
        foreach (LotQuantity output in transformation.Outputs)
        {
            producedBy.Add(output.Lot, transformation);
            foreach (LotQuantity input in transformation.Inputs)
            {
                links.Add(new ProvenanceLink(input.Lot, output.Lot, transformation.Id, transformation.ShareOf(input.Lot)));
            }
        }
    }

    /// <summary>
    /// Every lot the product was made from, with the fraction of the product each contributes
    /// and the quantity of it the product contains, nearest first.
    /// </summary>
    public IReadOnlyList<TraceEntry> TraceUpstream(LotNumber product)
    {
        Lot target = Get(product);
        Dictionary<LotNumber, int> depths = Walk(product, lot => links.Where(link => link.Target.Equals(lot)).Select(link => link.Source));
        return depths
            .Select(pair => new TraceEntry(pair.Key, pair.Value, ShareOf(pair.Key, product), target.InitialQuantity.Scale(ShareOf(pair.Key, product))))
            .OrderBy(entry => entry.Depth).ThenBy(entry => entry.Lot.Value, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Every lot made, directly or not, from the material, with the fraction of it that comes from the material
    /// and the quantity of the material it carries, nearest first.
    /// </summary>
    public IReadOnlyList<TraceEntry> TraceDownstream(LotNumber material)
    {
        Get(material);
        Dictionary<LotNumber, int> depths = Walk(material, lot => links.Where(link => link.Source.Equals(lot)).Select(link => link.Target));
        return depths
            .Select(pair => new TraceEntry(pair.Key, pair.Value, ShareOf(material, pair.Key), Get(pair.Key).InitialQuantity.Scale(ShareOf(material, pair.Key))))
            .OrderBy(entry => entry.Depth).ThenBy(entry => entry.Lot.Value, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Containment of a non-conforming material: rejects it and quarantines every downstream lot that carries
    /// at least <paramref name="minimumShare"/> of it. Returns the lots quarantined.
    /// </summary>
    public IReadOnlyList<LotNumber> Contain(LotNumber nonConforming, decimal minimumShare)
    {
        if (minimumShare <= 0m || minimumShare > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumShare), minimumShare, "A minimum share lies in (0, 1].");
        }

        IReadOnlyList<TraceEntry> affected = TraceDownstream(nonConforming);
        Get(nonConforming).Reject();
        List<LotNumber> quarantined = new();
        foreach (TraceEntry entry in affected.Where(entry => entry.Share >= minimumShare))
        {
            Lot lot = Get(entry.Lot);
            if (lot.Status != LotStatus.Rejected)
            {
                lot.Quarantine();
                quarantined.Add(lot.Number);
            }
        }

        return quarantined;
    }

    /// <summary>The fraction of <paramref name="lot"/> that comes from <paramref name="ancestor"/>.</summary>
    public decimal ShareOf(LotNumber ancestor, LotNumber lot)
    {
        ArgumentNullException.ThrowIfNull(ancestor);
        ArgumentNullException.ThrowIfNull(lot);
        return Share(ancestor, lot, new Dictionary<LotNumber, decimal>());
    }

    private decimal Share(LotNumber ancestor, LotNumber lot, Dictionary<LotNumber, decimal> memo)
    {
        if (lot.Equals(ancestor))
        {
            return 1m;
        }

        if (memo.TryGetValue(lot, out decimal known))
        {
            return known;
        }

        decimal share = producedBy.TryGetValue(lot, out Transformation? origin)
            ? origin.Inputs.Sum(input => origin.ShareOf(input.Lot) * Share(ancestor, input.Lot, memo))
            : 0m;
        share = Math.Min(1m, share);
        memo[lot] = share;
        return share;
    }

    private static Dictionary<LotNumber, int> Walk(LotNumber start, Func<LotNumber, IEnumerable<LotNumber>> next)
    {
        Dictionary<LotNumber, int> depths = new();
        Queue<LotNumber> frontier = new();
        frontier.Enqueue(start);
        Dictionary<LotNumber, int> seen = new() { [start] = 0 };
        while (frontier.Count > 0)
        {
            LotNumber current = frontier.Dequeue();
            foreach (LotNumber neighbour in next(current))
            {
                if (seen.TryAdd(neighbour, seen[current] + 1))
                {
                    depths[neighbour] = seen[neighbour];
                    frontier.Enqueue(neighbour);
                }
            }
        }

        return depths;
    }
}
