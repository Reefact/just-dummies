namespace DnaVariants.Domain;

/// <summary>A non-empty run of nucleotides over the alphabet A, C, G, T and N (unknown), stored upper-case.</summary>
public sealed class NucleotideSequence : IEquatable<NucleotideSequence>
{
    public const string Alphabet = "ACGTN";

    public NucleotideSequence(string bases)
    {
        ArgumentNullException.ThrowIfNull(bases);
        if (bases.Length == 0)
        {
            throw new ArgumentException("A nucleotide sequence cannot be empty.", nameof(bases));
        }

        string upper = bases.ToUpperInvariant();
        int invalid = upper.AsSpan().IndexOfAnyExcept(Alphabet);
        if (invalid >= 0)
        {
            throw new ArgumentException($"'{bases[invalid]}' at offset {invalid} is not a nucleotide ({Alphabet}).", nameof(bases));
        }

        Bases = upper;
    }

    public string Bases { get; }

    public int Length => Bases.Length;

    /// <summary>The bases from the 1-based <paramref name="start"/>, <paramref name="length"/> of them (may be empty).</summary>
    public string Slice(int start, int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(start, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        if (start - 1 + length > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length), $"[{start}, {start + length - 1}] runs past the sequence end ({Length}).");
        }

        return Bases.Substring(start - 1, length);
    }

    public NucleotideSequence ReverseComplement()
    {
        char[] result = new char[Length];
        for (int i = 0; i < Length; i++)
        {
            result[Length - 1 - i] = Complement(Bases[i]);
        }

        return new NucleotideSequence(new string(result));
    }

    public double GcContent => (double)Bases.Count(b => b is 'G' or 'C') / Length;

    internal static char Complement(char b) => b switch
    {
        'A' => 'T',
        'T' => 'A',
        'C' => 'G',
        'G' => 'C',
        _ => 'N',
    };

    public bool Equals(NucleotideSequence? other) => other is not null && Bases == other.Bases;

    public override bool Equals(object? obj) => Equals(obj as NucleotideSequence);

    public override int GetHashCode() => Bases.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Bases;
}
