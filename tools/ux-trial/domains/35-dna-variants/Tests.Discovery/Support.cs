using JustDummies;

namespace DnaVariants.Domain;

/// <summary>Hand-written helpers: a reference sequence built from a literal text, and variants that agree with a reference.</summary>
internal static class Support {

    public static ReferenceSequence ReferenceOf(string bases, Accession? accession = null) {
        return Any.ReferenceSequence()
                  .WithAccession(accession ?? Any.Accession().Generate())
                  .WithSequence(new NucleotideSequence(bases))
                  .Generate();
    }

    /// <summary>A reference without unknown bases: an allele is a called base, so no variant can lie over an N.</summary>
    public static AnyReferenceSequence ReferenceOfCalledBases() {
        return Any.ReferenceSequence().WithSequence(s => s.WithBases(b => b.WithoutChars("N")));
    }

    public static Variant Change(Accession accession, int position, string reference, string alternate) {
        return new Variant(accession, position, new Allele(reference), new Allele(alternate));
    }

    public static Variant Change(ReferenceSequence reference, int position, string referenceBases, string alternate) {
        return Change(reference.Accession, position, referenceBases, alternate);
    }

}

/// <summary>
///     A variant that really lies on a given reference: the reference allele is read from it, so
///     <see cref="ReferenceSequence.Verify" /> accepts it. Only the place and the alternate bases are arbitrary.
/// </summary>
public sealed class AnyVariantOn : IAny<Variant> {

    private readonly ReferenceSequence _reference;

    public AnyVariantOn(ReferenceSequence reference) {
        _reference = reference;
    }

    public Variant Generate() {
        int    position  = Any.Int32().Between(1, _reference.Length).Generate();
        int    maxLength = Math.Min(5, _reference.Length - position + 1);
        int    length    = Any.Int32().Between(0, maxLength).Generate();
        string covered   = _reference.Sequence.Slice(position, length);
        Allele reference = new Allele(covered);
        Allele alternate = new AnyAllele().WithBases(b => b.WithMaxLength(5)).DifferentFrom(reference).Generate();

        return new Variant(_reference.Accession, position, reference, alternate);
    }

}
