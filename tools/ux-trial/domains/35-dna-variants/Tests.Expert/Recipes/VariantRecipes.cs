using DnaVariants.Domain;

using JustDummies;

namespace DnaVariants.Tests.Expert.Recipes;

/// <summary>
///     Recipes for variants that must agree with a given reference: the reference allele is read from it, never drawn.
/// </summary>
public static class VariantRecipes {

    /// <summary>The four called bases, either case: what an allele may carry.</summary>
    public const string CalledBases = "ACGTacgt";

    /// <summary>A single-base substitution at <paramref name="position" />, to any other called base.</summary>
    public static IAny<Variant> SubstitutionAt(ReferenceSequence reference, int position) {
        char referenceBase = reference.BaseAt(position);

        return Any.Char()
                  .OneOf("ACGT")
                  .DifferentFrom(referenceBase)
                  .As(alternateBase => new Variant(reference.Accession,
                                                   position,
                                                   new Allele(referenceBase.ToString()),
                                                   new Allele(alternateBase.ToString())));
    }

    /// <summary>A reference sequence drawn from called bases only, so that every position can be stated in an allele.</summary>
    public static AnyReferenceSequence CalledReferenceOfLength(int length) {
        return Any.ReferenceSequence()
                  .WithSequence(sequence => sequence.OfCalledBases(bases => bases.WithLength(length)));
    }

}
