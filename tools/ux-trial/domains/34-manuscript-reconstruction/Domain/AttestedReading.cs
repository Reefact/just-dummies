namespace ManuscriptReconstruction.Domain;

/// <summary>One column of an alignment: a reading of a passage and the witnesses that carry it (its provenance).</summary>
public sealed class AttestedReading
{
    public AttestedReading(PassageReference passage, string text, IReadOnlyCollection<WitnessSiglum> witnesses)
    {
        ArgumentNullException.ThrowIfNull(passage);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(witnesses);
        if (witnesses.Count == 0)
        {
            throw new ArgumentException("An attested reading has at least one witness.", nameof(witnesses));
        }

        if (witnesses.Any(w => w is null) || witnesses.Distinct().Count() != witnesses.Count)
        {
            throw new ArgumentException("Witnesses are non-null and distinct.", nameof(witnesses));
        }

        Passage = passage;
        Text = Variant.Normalize(text);
        Witnesses = witnesses.OrderBy(w => w.Value, StringComparer.Ordinal).ToArray();
    }

    public PassageReference Passage { get; }

    /// <summary>The normalised reading; empty when the witnesses omit the passage.</summary>
    public string Text { get; }

    public IReadOnlyList<WitnessSiglum> Witnesses { get; }

    public bool IsOmission => Text.Length == 0;
}
