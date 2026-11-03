using JustDummies;

using ManuscriptReconstruction.Domain;

namespace ManuscriptReconstruction.Tests.Regular;

/// <summary>Shared arranging helpers: they build what a test needs and does not care about.</summary>
internal static class Fixtures {

    public static IReadOnlyList<WitnessSiglum> DistinctSigla(int count) {
        return Any.ListOf(Any.WitnessSiglum()).WithCount(count).Distinct().Generate();
    }

    public static Witness WitnessOf(WitnessSiglum siglum) {
        return Any.Witness().WithSiglum(siglum).Generate();
    }

    public static Tradition TraditionWith(params WitnessSiglum[] sigla) {
        Tradition tradition = Any.Tradition().Generate();
        foreach (WitnessSiglum siglum in sigla) {
            tradition.AddWitness(WitnessOf(siglum));
        }

        return tradition;
    }

    public static PassageReference AnotherPassage(PassageReference than) {
        return new AnyPassageReference().DifferentFrom(than).Generate();
    }

    /// <summary>A reading of <paramref name="text" />: a substitution by the first hand, a correction by a later one.</summary>
    public static Variant Reading(WitnessSiglum witness, PassageReference passage, string text, int hand = Variant.FirstHand) {
        return Any.Variant()
                  .WithWitness(witness)
                  .WithPassage(passage)
                  .WithText(text)
                  .WithKind(hand == Variant.FirstHand ? VariantKind.Substitution : VariantKind.Correction)
                  .WithHand(hand)
                  .Generate();
    }

    public static Variant Omission(WitnessSiglum witness, PassageReference passage) {
        return Any.Variant().WithWitness(witness).WithPassage(passage).WithKind(VariantKind.Omission).WithHand(Variant.FirstHand).Generate();
    }

    /// <summary>A hypothesis citing <paramref name="witnesses" /> for <paramref name="text" /> (a conjecture when it cites none).</summary>
    public static EditorialHypothesis Hypothesis(PassageReference passage, string text, params WitnessSiglum[] witnesses) {
        return Any.EditorialHypothesis()
                  .WithPassage(passage)
                  .WithProposedReading(text)
                  .WithIsConjecture(witnesses.Length == 0)
                  .WithSupportingWitnesses(witnesses)
                  .Generate();
    }

}
