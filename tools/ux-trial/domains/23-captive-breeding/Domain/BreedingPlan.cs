namespace CaptiveBreeding.Domain;

/// <summary>
/// The pairings recommended for one breeding season. Every pair is checked against the
/// pedigree and the programme's policy when proposed; an approved plan is frozen.
/// </summary>
public sealed class BreedingPlan
{
    private readonly List<BreedingPair> pairs = [];

    public BreedingPlan(Pedigree pedigree, KinshipPolicy policy, DateOnly seasonStart)
    {
        ArgumentNullException.ThrowIfNull(pedigree);
        ArgumentNullException.ThrowIfNull(policy);
        Pedigree = pedigree;
        Policy = policy;
        SeasonStart = seasonStart;
        Status = BreedingPlanStatus.Draft;
    }

    public Pedigree Pedigree { get; }

    public KinshipPolicy Policy { get; }

    public DateOnly SeasonStart { get; }

    public BreedingPlanStatus Status { get; private set; }

    public IReadOnlyList<BreedingPair> Pairs => pairs;

    /// <summary>Mean inbreeding coefficient of the offspring the plan would produce; 0 for an empty plan.</summary>
    public double ExpectedOffspringInbreeding => pairs.Count == 0 ? 0.0 : pairs.Average(p => p.Kinship.OffspringInbreeding);

    public BreedingPair Propose(StudbookId sireId, StudbookId damId)
    {
        ArgumentNullException.ThrowIfNull(sireId);
        ArgumentNullException.ThrowIfNull(damId);
        EnsureDraft();
        BreedingPair pair = Evaluate(sireId, damId);
        if (pairs.Contains(pair))
        {
            throw new PairingRefusedException(sireId, damId, "the pair is already in the plan.");
        }

        foreach (StudbookId id in new[] { sireId, damId })
        {
            if (pairs.Count(p => p.Involves(id)) >= Policy.MaximumPairingsPerIndividual)
            {
                throw new PairingRefusedException(sireId, damId, $"{id} already has {Policy.MaximumPairingsPerIndividual} pairing(s) this season.");
            }
        }

        pairs.Add(pair);
        return pair;
    }

    public void Withdraw(BreedingPair pair)
    {
        ArgumentNullException.ThrowIfNull(pair);
        EnsureDraft();
        if (!pairs.Remove(pair))
        {
            throw new InvalidOperationException($"{pair} is not in the plan.");
        }
    }

    public void Approve()
    {
        EnsureDraft();
        if (pairs.Count == 0)
        {
            throw new InvalidOperationException("An empty breeding plan cannot be approved.");
        }

        Status = BreedingPlanStatus.Approved;
    }

    /// <summary>
    /// Every eligible male for a dam, best first: lowest pair kinship, then lowest mean kinship
    /// (the most genetically under-represented males first).
    /// </summary>
    public IReadOnlyList<BreedingPair> RankSiresFor(StudbookId damId)
    {
        ArgumentNullException.ThrowIfNull(damId);
        List<(BreedingPair Pair, double MeanKinship)> candidates = [];
        foreach (Individual male in Pedigree.Living.Where(i => i.Sex == Sex.Male))
        {
            try
            {
                candidates.Add((Evaluate(male.Id, damId), Pedigree.MeanKinshipOf(male.Id)));
            }
            catch (PairingRefusedException)
            {
                // An ineligible male is simply not a candidate.
            }
        }

        return [.. candidates.OrderBy(c => c.Pair.Kinship.Value).ThenBy(c => c.MeanKinship).Select(c => c.Pair)];
    }

    private BreedingPair Evaluate(StudbookId sireId, StudbookId damId)
    {
        Individual sire = Pedigree.Get(sireId);
        Individual dam = Pedigree.Get(damId);
        if (sire.Sex != Sex.Male)
        {
            throw new PairingRefusedException(sireId, damId, $"{sireId} is not male.");
        }

        if (dam.Sex != Sex.Female)
        {
            throw new PairingRefusedException(sireId, damId, $"{damId} is not female.");
        }

        if (!sire.IsAlive || !dam.IsAlive)
        {
            throw new PairingRefusedException(sireId, damId, "both partners must be alive.");
        }

        if (!Policy.IsOfBreedingAge(sire, SeasonStart) || !Policy.IsOfBreedingAge(dam, SeasonStart))
        {
            throw new PairingRefusedException(sireId, damId, "both partners must be of breeding age at the start of the season.");
        }

        KinshipCoefficient kinship = Pedigree.KinshipBetween(sireId, damId);
        if (kinship.Exceeds(Policy.MaximumPairKinship))
        {
            throw new PairingRefusedException(sireId, damId, $"kinship {kinship} exceeds the limit {Policy.MaximumPairKinship}.");
        }

        return new BreedingPair(sireId, damId, kinship);
    }

    private void EnsureDraft()
    {
        if (Status != BreedingPlanStatus.Draft)
        {
            throw new InvalidOperationException("An approved breeding plan can no longer change.");
        }
    }
}
