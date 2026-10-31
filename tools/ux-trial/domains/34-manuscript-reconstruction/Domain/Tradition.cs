namespace ManuscriptReconstruction.Domain;

/// <summary>
/// The aggregate: the manuscript tradition of one work, with its witnesses, their readings, lacunae,
/// copy relations and the editors' competing hypotheses.
/// </summary>
public sealed class Tradition
{
    private readonly Dictionary<WitnessSiglum, Witness> witnesses = new();
    private readonly List<Variant> variants = [];
    private readonly List<Lacuna> lacunae = [];
    private readonly List<CopyRelation> copyRelations = [];
    private readonly List<EditorialHypothesis> hypotheses = [];

    public Tradition(string workTitle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workTitle);
        WorkTitle = workTitle;
    }

    public string WorkTitle { get; }

    public IReadOnlyCollection<Witness> Witnesses => witnesses.Values;

    public IReadOnlyList<Variant> Variants => variants;

    public IReadOnlyList<Lacuna> Lacunae => lacunae;

    public IReadOnlyList<CopyRelation> CopyRelations => copyRelations;

    public IReadOnlyList<EditorialHypothesis> Hypotheses => hypotheses;

    public void AddWitness(Witness witness)
    {
        ArgumentNullException.ThrowIfNull(witness);
        if (!witnesses.TryAdd(witness.Siglum, witness))
        {
            throw new InvalidOperationException($"Witness {witness.Siglum} is already part of the tradition.");
        }
    }

    public void RecordLacuna(Lacuna lacuna)
    {
        ArgumentNullException.ThrowIfNull(lacuna);
        RequireWitness(lacuna.Witness);
        if (lacunae.Any(l => l.Overlaps(lacuna)))
        {
            throw new InvalidOperationException($"Witness {lacuna.Witness} already has a lacuna overlapping {lacuna.From}-{lacuna.To}.");
        }

        if (variants.Any(v => v.Witness.Equals(lacuna.Witness) && lacuna.Covers(v.Passage)))
        {
            throw new InvalidOperationException($"Witness {lacuna.Witness} has readings recorded inside {lacuna.From}-{lacuna.To}.");
        }

        lacunae.Add(lacuna);
    }

    /// <summary>Records what a witness reads at a passage. A witness has at most one reading per hand at a passage.</summary>
    public void RecordVariant(Variant variant)
    {
        ArgumentNullException.ThrowIfNull(variant);
        RequireWitness(variant.Witness);
        if (IsInLacuna(variant.Witness, variant.Passage))
        {
            throw new InvalidOperationException($"Witness {variant.Witness} is lacunose at {variant.Passage}; it can attest nothing there.");
        }

        if (variants.Any(v => v.Witness.Equals(variant.Witness) && v.Passage.Equals(variant.Passage) && v.Hand == variant.Hand))
        {
            throw new InvalidOperationException($"Hand {variant.Hand} of {variant.Witness} already has a reading at {variant.Passage}.");
        }

        if (variant.Kind == VariantKind.Correction
            && !variants.Any(v => v.Witness.Equals(variant.Witness) && v.Passage.Equals(variant.Passage) && v.Hand < variant.Hand))
        {
            throw new InvalidOperationException($"A correction in {variant.Witness} at {variant.Passage} needs an earlier hand's reading to correct.");
        }

        variants.Add(variant);
    }

    public bool IsInLacuna(WitnessSiglum witness, PassageReference passage)
    {
        ArgumentNullException.ThrowIfNull(witness);
        ArgumentNullException.ThrowIfNull(passage);
        return lacunae.Any(l => l.Witness.Equals(witness) && l.Covers(passage));
    }

    /// <summary>The reading a witness finally gives at a passage: the latest hand's, or null when unrecorded.</summary>
    public Variant? FinalReadingOf(WitnessSiglum witness, PassageReference passage)
    {
        ArgumentNullException.ThrowIfNull(witness);
        ArgumentNullException.ThrowIfNull(passage);
        return variants
            .Where(v => v.Witness.Equals(witness) && v.Passage.Equals(passage))
            .MaxBy(v => v.Hand);
    }

    /// <summary>Aligns the witnesses' final readings at a passage, largest group first, then by text.</summary>
    public IReadOnlyList<AttestedReading> Align(PassageReference passage)
    {
        ArgumentNullException.ThrowIfNull(passage);
        return variants
            .Where(v => v.Passage.Equals(passage))
            .GroupBy(v => v.Witness)
            .Select(g => g.MaxBy(v => v.Hand)!)
            .GroupBy(v => v.Text, StringComparer.Ordinal)
            .Select(g => new AttestedReading(passage, g.Key, g.Select(v => v.Witness).ToArray()))
            .OrderByDescending(r => r.Witnesses.Count)
            .ThenBy(r => r.Text, StringComparer.Ordinal)
            .ToArray();
    }

    public void Propose(EditorialHypothesis hypothesis)
    {
        ArgumentNullException.ThrowIfNull(hypothesis);
        if (hypotheses.Any(h => h.Id == hypothesis.Id))
        {
            throw new InvalidOperationException($"Hypothesis {hypothesis.Id} is already proposed.");
        }

        foreach (WitnessSiglum siglum in hypothesis.SupportingWitnesses)
        {
            RequireWitness(siglum);
            Variant? reading = FinalReadingOf(siglum, hypothesis.Passage);
            if (reading is null || reading.Text != hypothesis.ProposedReading)
            {
                throw new InvalidOperationException($"Witness {siglum} does not read '{hypothesis.ProposedReading}' at {hypothesis.Passage}.");
            }
        }

        hypotheses.Add(hypothesis);
    }

    /// <summary>
    /// Accepts a hypothesis as the edited text of its passage. Competing hypotheses stay proposed: they
    /// remain legitimate readings with their provenance. At most one hypothesis per passage is accepted.
    /// </summary>
    public void Accept(Guid hypothesisId)
    {
        EditorialHypothesis hypothesis = Find(hypothesisId);
        EditorialHypothesis? current = AcceptedHypothesisAt(hypothesis.Passage);
        if (current is not null && current.Id != hypothesisId)
        {
            throw new InvalidOperationException($"Hypothesis {current.Id} is already accepted at {hypothesis.Passage}; withdraw it first.");
        }

        hypothesis.Accept();
    }

    public void Withdraw(Guid hypothesisId) => Find(hypothesisId).Withdraw();

    public EditorialHypothesis? AcceptedHypothesisAt(PassageReference passage)
    {
        ArgumentNullException.ThrowIfNull(passage);
        return hypotheses.SingleOrDefault(h => h.Passage.Equals(passage) && h.Status == HypothesisStatus.Accepted);
    }

    /// <summary>The hypotheses still in play at a passage: every one that has not been rejected.</summary>
    public IReadOnlyList<EditorialHypothesis> CompetingReadingsAt(PassageReference passage)
    {
        ArgumentNullException.ThrowIfNull(passage);
        return hypotheses.Where(h => h.Passage.Equals(passage) && h.Status != HypothesisStatus.Rejected).ToArray();
    }

    /// <summary>
    /// Conjunctive errors: the passages with an accepted reading where both witnesses agree on the same
    /// different reading. Agreement in error, not in truth, is evidence of a common ancestor.
    /// </summary>
    public IReadOnlyList<PassageReference> SharedErrors(WitnessSiglum first, WitnessSiglum second)
    {
        RequireWitness(first);
        RequireWitness(second);
        if (first.Equals(second))
        {
            throw new ArgumentException("Shared errors compare two distinct witnesses.", nameof(second));
        }

        List<PassageReference> errors = [];
        foreach (EditorialHypothesis accepted in hypotheses.Where(h => h.Status == HypothesisStatus.Accepted))
        {
            Variant? a = FinalReadingOf(first, accepted.Passage);
            Variant? b = FinalReadingOf(second, accepted.Passage);
            if (a is not null && b is not null && a.Text == b.Text && a.Text != accepted.ProposedReading)
            {
                errors.Add(accepted.Passage);
            }
        }

        errors.Sort((x, y) => x.CompareTo(y));
        return errors;
    }

    /// <summary>
    /// Asserts that one witness descends from another. Refused when the copy is certainly older than its
    /// exemplar, when the relation would close a cycle, or when the copy already has an exemplar.
    /// </summary>
    public void DeclareCopy(CopyRelation relation)
    {
        ArgumentNullException.ThrowIfNull(relation);
        Witness exemplar = RequireWitness(relation.Exemplar);
        Witness copy = RequireWitness(relation.Copy);
        if (copy.IsCertainlyOlderThan(exemplar))
        {
            throw new InvalidOperationException($"{copy.Siglum} (by {copy.DatedTo}) predates {exemplar.Siglum} (from {exemplar.DatedFrom}) and cannot copy it.");
        }

        if (copyRelations.Any(r => r.Copy.Equals(relation.Copy)))
        {
            throw new InvalidOperationException($"{relation.Copy} already has a declared exemplar.");
        }

        if (AncestorsOf(relation.Exemplar).Contains(relation.Copy))
        {
            throw new InvalidOperationException($"Declaring {relation.Copy} a copy of {relation.Exemplar} would make the stemma circular.");
        }

        copyRelations.Add(relation);
    }

    /// <summary>The chain of exemplars above a witness, nearest first.</summary>
    public IReadOnlyList<WitnessSiglum> AncestorsOf(WitnessSiglum witness)
    {
        RequireWitness(witness);
        List<WitnessSiglum> chain = [];
        WitnessSiglum current = witness;
        while (copyRelations.FirstOrDefault(r => r.Copy.Equals(current)) is { } relation)
        {
            chain.Add(relation.Exemplar);
            current = relation.Exemplar;
        }

        return chain;
    }

    private Witness RequireWitness(WitnessSiglum siglum)
    {
        ArgumentNullException.ThrowIfNull(siglum);
        return witnesses.TryGetValue(siglum, out Witness? witness)
            ? witness
            : throw new KeyNotFoundException($"Witness {siglum} is not part of the tradition.");
    }

    private EditorialHypothesis Find(Guid hypothesisId) =>
        hypotheses.FirstOrDefault(h => h.Id == hypothesisId)
        ?? throw new KeyNotFoundException($"No hypothesis {hypothesisId} in the tradition.");
}
