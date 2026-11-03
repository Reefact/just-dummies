using DnaVariants.Domain;

using JustDummies;

namespace DnaVariants.Tests.Expert.Generators;

/// <summary>Hand-written helpers beside the scaffolded generators: what no constructor guard can say.</summary>
public static class Scenario {

    /// <summary>A reference whose bases are the test's own (significant) literal; accession and name stay arbitrary.</summary>
    public static ReferenceSequence ReferenceOf(string bases) {
        return Any.ReferenceSequence().WithSequence(new NucleotideSequence(bases)).Generate();
    }

    /// <summary>A variant written on <paramref name="reference" />: only its accession is shared with it.</summary>
    public static Variant VariantOn(ReferenceSequence reference, int position, string referenceBases, string alternateBases) {
        return new Variant(reference.Accession, position, new Allele(referenceBases), new Allele(alternateBases));
    }

    /// <summary>A reference with only called bases (no N), so that any variant reading it can pass <c>Verify</c>.</summary>
    public static AnyReferenceSequence CalledReference() {
        return Any.ReferenceSequence().WithSequence(s => s.WithBases(b => b.WithoutChars("N")));
    }

    /// <summary>A reference of exactly <paramref name="length" /> called bases, for the tests whose subject is its size.</summary>
    public static ReferenceSequence LongCalledReference(int length) {
        string bases = Any.String().WithChars("ACGT").WithLength(length).Generate();

        return Any.ReferenceSequence().WithSequence(new NucleotideSequence(bases)).Generate();
    }

    /// <summary>A substitution of the base at an arbitrary position of <paramref name="reference" /> by another called base.</summary>
    public static IAny<Variant> SubstitutionOn(ReferenceSequence reference) {
        return Any.Int32().Between(1, reference.Length).Then(position => {
            char referenceBase = reference.BaseAt(position);

            return Any.OneOf("ACGT".Where(b => b != referenceBase).ToArray())
                      .As(alternate => VariantOn(reference, position, referenceBase.ToString(), alternate.ToString()));
        });
    }

}
