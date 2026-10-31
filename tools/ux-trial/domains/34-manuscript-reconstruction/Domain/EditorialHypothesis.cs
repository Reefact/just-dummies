namespace ManuscriptReconstruction.Domain;

/// <summary>An editor's proposed reading of a passage, grounded in the witnesses that support it or flagged as a conjecture.</summary>
public sealed class EditorialHypothesis
{
    public const int MaxRationaleLength = 2000;

    public EditorialHypothesis(
        Guid id,
        PassageReference passage,
        string proposedReading,
        string editor,
        IReadOnlyCollection<WitnessSiglum> supportingWitnesses,
        bool isConjecture,
        string rationale)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A hypothesis has a non-empty identifier.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(passage);
        ArgumentNullException.ThrowIfNull(proposedReading);
        ArgumentException.ThrowIfNullOrWhiteSpace(editor);
        ArgumentNullException.ThrowIfNull(supportingWitnesses);
        ArgumentException.ThrowIfNullOrWhiteSpace(rationale);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(rationale.Length, MaxRationaleLength, nameof(rationale));
        if (supportingWitnesses.Any(w => w is null))
        {
            throw new ArgumentException("Supporting witnesses cannot contain null.", nameof(supportingWitnesses));
        }

        if (supportingWitnesses.Distinct().Count() != supportingWitnesses.Count)
        {
            throw new ArgumentException("A supporting witness is cited twice.", nameof(supportingWitnesses));
        }

        if (isConjecture && supportingWitnesses.Count != 0)
        {
            throw new ArgumentException("A conjecture is, by definition, attested by no witness.", nameof(supportingWitnesses));
        }

        if (!isConjecture && supportingWitnesses.Count == 0)
        {
            throw new ArgumentException("A reading that is not a conjecture cites at least one witness.", nameof(supportingWitnesses));
        }

        Id = id;
        Passage = passage;
        ProposedReading = Variant.Normalize(proposedReading);
        Editor = editor;
        SupportingWitnesses = supportingWitnesses.ToArray();
        IsConjecture = isConjecture;
        Rationale = rationale;
        Status = HypothesisStatus.Proposed;
    }

    public Guid Id { get; }

    public PassageReference Passage { get; }

    /// <summary>The proposed text, normalised like a variant; empty means the editor holds the passage to be an interpolation.</summary>
    public string ProposedReading { get; }

    public string Editor { get; }

    public IReadOnlyList<WitnessSiglum> SupportingWitnesses { get; }

    public bool IsConjecture { get; }

    public string Rationale { get; }

    public HypothesisStatus Status { get; private set; }

    public string? RejectionReason { get; private set; }

    internal void Accept()
    {
        if (Status != HypothesisStatus.Proposed)
        {
            throw new InvalidOperationException($"Only a proposed hypothesis can be accepted; this one is {Status}.");
        }

        Status = HypothesisStatus.Accepted;
    }

    internal void Withdraw()
    {
        if (Status != HypothesisStatus.Accepted)
        {
            throw new InvalidOperationException($"Only an accepted hypothesis can be withdrawn; this one is {Status}.");
        }

        Status = HypothesisStatus.Proposed;
    }

    public void Reject(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status == HypothesisStatus.Rejected)
        {
            throw new InvalidOperationException("The hypothesis is already rejected.");
        }

        if (Status == HypothesisStatus.Accepted)
        {
            throw new InvalidOperationException("An accepted hypothesis is withdrawn through its tradition before it can be rejected.");
        }

        Status = HypothesisStatus.Rejected;
        RejectionReason = reason;
    }
}
