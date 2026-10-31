namespace ManuscriptReconstruction.Domain;

/// <summary>A physical gap in a witness (lost leaf, damage) spanning a closed range of passages.</summary>
public sealed class Lacuna
{
    public Lacuna(WitnessSiglum witness, PassageReference from, PassageReference to, string cause)
    {
        ArgumentNullException.ThrowIfNull(witness);
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        ArgumentException.ThrowIfNullOrWhiteSpace(cause);
        if (from.CompareTo(to) > 0)
        {
            throw new ArgumentException($"A lacuna cannot start at {from} after it ends at {to}.", nameof(from));
        }

        Witness = witness;
        From = from;
        To = to;
        Cause = cause;
    }

    public WitnessSiglum Witness { get; }

    public PassageReference From { get; }

    public PassageReference To { get; }

    public string Cause { get; }

    public bool Covers(PassageReference passage)
    {
        ArgumentNullException.ThrowIfNull(passage);
        return From.CompareTo(passage) <= 0 && passage.CompareTo(To) <= 0;
    }

    public bool Overlaps(Lacuna other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Witness.Equals(other.Witness) && From.CompareTo(other.To) <= 0 && other.From.CompareTo(To) <= 0;
    }
}
