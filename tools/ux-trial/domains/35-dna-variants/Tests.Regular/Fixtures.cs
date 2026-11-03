namespace DnaVariants.Tests.Regular;

internal static class Fixtures {

    /// <summary>The repeat-rich sequence of the model's examples; the accession and the name are dummies.</summary>
    public const string Repeats = "GGCACACATTT";

    public static ReferenceSequence ReferenceReading(string bases) {
        return new AnyReferenceSequence().WithSequence(new NucleotideSequence(bases)).Generate();
    }

    public static Variant OnReference(ReferenceSequence reference, int position, string referenceBases, string alternateBases) {
        return new Variant(reference.Accession, position, new Allele(referenceBases), new Allele(alternateBases));
    }

}
