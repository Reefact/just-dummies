namespace CaptiveBreeding.Domain;

/// <summary>
/// The studbook of one conservation programme: every individual ever recorded, linked to its
/// parents. Parents are registered before their offspring, so the graph is acyclic by
/// construction and the registration order is a valid generation order.
/// </summary>
public sealed class Pedigree
{
    private readonly List<Individual> individuals = [];
    private readonly Dictionary<StudbookId, int> order = [];
    private readonly Dictionary<(int, int), double> kinshipCache = [];

    public Pedigree(string programmeName, string species)
    {
        ArgumentNullException.ThrowIfNull(programmeName);
        ArgumentNullException.ThrowIfNull(species);
        if (string.IsNullOrWhiteSpace(programmeName))
        {
            throw new ArgumentException("A programme has a name.", nameof(programmeName));
        }

        if (string.IsNullOrWhiteSpace(species))
        {
            throw new ArgumentException("A programme manages one named species.", nameof(species));
        }

        ProgrammeName = programmeName;
        Species = species;
    }

    public string ProgrammeName { get; }

    public string Species { get; }

    public IReadOnlyList<Individual> Individuals => individuals;

    public IEnumerable<Individual> Living => individuals.Where(i => i.IsAlive);

    public IEnumerable<Individual> Founders => individuals.Where(i => i.IsFounder);

    public bool Contains(StudbookId id) => order.ContainsKey(id);

    public Individual Get(StudbookId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return order.TryGetValue(id, out int index)
            ? individuals[index]
            : throw new KeyNotFoundException($"{id} is not registered in the {ProgrammeName} studbook.");
    }

    public void Register(Individual individual)
    {
        ArgumentNullException.ThrowIfNull(individual);
        if (Contains(individual.Id))
        {
            throw new InvalidOperationException($"{individual.Id} is already registered.");
        }

        if (!individual.IsFounder)
        {
            Individual sire = Get(individual.SireId!);
            Individual dam = Get(individual.DamId!);
            if (sire.Sex != Sex.Male)
            {
                throw new InvalidOperationException($"Sire {sire.Id} is not male.");
            }

            if (dam.Sex != Sex.Female)
            {
                throw new InvalidOperationException($"Dam {dam.Id} is not female.");
            }

            if (sire.BirthDate >= individual.BirthDate || dam.BirthDate >= individual.BirthDate)
            {
                throw new InvalidOperationException($"{individual.Id} cannot be born before or with its parents.");
            }

            if (dam.DeathDate is DateOnly damDeath && damDeath < individual.BirthDate)
            {
                throw new InvalidOperationException($"Dam {dam.Id} died before {individual.Id} was born.");
            }
        }

        order[individual.Id] = individuals.Count;
        individuals.Add(individual);
    }

    public int GenerationOf(StudbookId id)
    {
        Individual individual = Get(id);
        return individual.IsFounder
            ? 0
            : 1 + Math.Max(GenerationOf(individual.SireId!), GenerationOf(individual.DamId!));
    }

    public IReadOnlySet<StudbookId> AncestorsOf(StudbookId id)
    {
        HashSet<StudbookId> ancestors = [];
        Stack<StudbookId> pending = new();
        pending.Push(id);
        while (pending.Count > 0)
        {
            Individual current = Get(pending.Pop());
            if (current.IsFounder)
            {
                continue;
            }

            foreach (StudbookId parent in new[] { current.SireId!, current.DamId! })
            {
                if (ancestors.Add(parent))
                {
                    pending.Push(parent);
                }
            }
        }

        return ancestors;
    }

    public IReadOnlySet<StudbookId> CommonAncestorsOf(StudbookId first, StudbookId second)
    {
        HashSet<StudbookId> common = [.. AncestorsOf(first)];
        common.IntersectWith(AncestorsOf(second));
        return common;
    }

    /// <summary>Kinship by the tabular method: recurse on the younger of the two through its parents.</summary>
    public KinshipCoefficient KinshipBetween(StudbookId first, StudbookId second)
    {
        return new KinshipCoefficient(Math.Min(1.0, Kinship(order.GetValueOrDefault(first, -1), order.GetValueOrDefault(second, -1), first, second)));
    }

    public double InbreedingOf(StudbookId id)
    {
        Individual individual = Get(id);
        return individual.IsFounder ? 0.0 : KinshipBetween(individual.SireId!, individual.DamId!).Value;
    }

    /// <summary>Average kinship of an individual with every living individual, itself included.</summary>
    public double MeanKinshipOf(StudbookId id)
    {
        List<Individual> living = [.. Living];
        if (living.Count == 0)
        {
            throw new InvalidOperationException("Mean kinship is undefined without living individuals.");
        }

        return living.Average(other => KinshipBetween(id, other.Id).Value);
    }

    /// <summary>Expected share of each founder's genes in the living population; sums to 1.</summary>
    public IReadOnlyList<GeneticContribution> FounderContributions()
    {
        List<Individual> living = [.. Living];
        if (living.Count == 0)
        {
            throw new InvalidOperationException("No living individual carries any founder's genes.");
        }

        Dictionary<StudbookId, double> totals = Founders.ToDictionary(f => f.Id, _ => 0.0);
        foreach (Individual individual in living)
        {
            foreach (KeyValuePair<StudbookId, double> share in FounderSharesOf(individual.Id))
            {
                totals[share.Key] += share.Value / living.Count;
            }
        }

        return [.. totals.Select(t => new GeneticContribution(t.Key, Math.Min(1.0, t.Value)))];
    }

    /// <summary>The number of equally contributing founders that would give the same diversity: 1 / Σ p².</summary>
    public double FounderEquivalents()
    {
        double sumOfSquares = FounderContributions().Sum(c => c.Proportion * c.Proportion);
        return 1.0 / sumOfSquares;
    }

    private Dictionary<StudbookId, double> FounderSharesOf(StudbookId id)
    {
        Individual individual = Get(id);
        if (individual.IsFounder)
        {
            return new Dictionary<StudbookId, double> { [id] = 1.0 };
        }

        Dictionary<StudbookId, double> shares = [];
        foreach (StudbookId parent in new[] { individual.SireId!, individual.DamId! })
        {
            foreach (KeyValuePair<StudbookId, double> share in FounderSharesOf(parent))
            {
                shares[share.Key] = shares.GetValueOrDefault(share.Key) + (share.Value / 2.0);
            }
        }

        return shares;
    }

    private double Kinship(int a, int b, StudbookId first, StudbookId second)
    {
        if (a < 0)
        {
            throw new KeyNotFoundException($"{first} is not registered in the {ProgrammeName} studbook.");
        }

        if (b < 0)
        {
            throw new KeyNotFoundException($"{second} is not registered in the {ProgrammeName} studbook.");
        }

        (int younger, int older) = a >= b ? (a, b) : (b, a);
        if (kinshipCache.TryGetValue((younger, older), out double cached))
        {
            return cached;
        }

        Individual youngest = individuals[younger];
        double result;
        if (youngest.IsFounder)
        {
            result = younger == older ? 0.5 : 0.0;
        }
        else
        {
            int sire = order[youngest.SireId!];
            int dam = order[youngest.DamId!];
            result = younger == older
                ? 0.5 * (1.0 + Kinship(sire, dam, youngest.SireId!, youngest.DamId!))
                : 0.5 * (Kinship(sire, older, youngest.SireId!, individuals[older].Id)
                         + Kinship(dam, older, youngest.DamId!, individuals[older].Id));
        }

        kinshipCache[(younger, older)] = result;
        return result;
    }
}
