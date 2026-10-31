namespace ManuscriptReconstruction.Domain;

/// <summary>What one witness reads at one passage, by a given hand (1 is the original scribe, 2 and above are correctors).</summary>
public sealed class Variant
{
    public const int FirstHand = 1;
    public const int MaxHand = 9;
    public const int MaxTextLength = 500;

    public Variant(PassageReference passage, WitnessSiglum witness, string text, VariantKind kind, int hand)
    {
        ArgumentNullException.ThrowIfNull(passage);
        ArgumentNullException.ThrowIfNull(witness);
        ArgumentNullException.ThrowIfNull(text);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown variant kind.");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(hand, FirstHand);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(hand, MaxHand);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(text.Length, MaxTextLength, nameof(text));

        string normalized = Normalize(text);
        if (kind == VariantKind.Omission && normalized.Length != 0)
        {
            throw new ArgumentException("An omission carries no text.", nameof(text));
        }

        if (kind != VariantKind.Omission && normalized.Length == 0)
        {
            throw new ArgumentException($"A {kind} carries text; use an omission for an empty reading.", nameof(text));
        }

        if (kind == VariantKind.Correction && hand == FirstHand)
        {
            throw new ArgumentException("A correction is made by a later hand, not the first.", nameof(hand));
        }

        Passage = passage;
        Witness = witness;
        Text = normalized;
        Kind = kind;
        Hand = hand;
    }

    public PassageReference Passage { get; }

    public WitnessSiglum Witness { get; }

    /// <summary>The reading with surrounding whitespace trimmed and inner runs collapsed to one space.</summary>
    public string Text { get; }

    public VariantKind Kind { get; }

    public int Hand { get; }

    public bool IsByFirstHand => Hand == FirstHand;

    internal static string Normalize(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
