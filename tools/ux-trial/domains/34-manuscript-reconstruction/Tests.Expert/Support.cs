using JustDummies;

using ManuscriptReconstruction.Domain;

namespace ManuscriptReconstruction.Tests.Expert;

/// <summary>Shorthands that let a test say what it is about and leave the rest drawn.</summary>
internal static class Scene {

    public static PassageReference Passage(string reference) {
        return PassageReference.Parse(reference);
    }

    public static WitnessSiglum Siglum(string value) {
        return new WitnessSiglum(value);
    }

    /// <summary>Sigla that are pairwise different, for tests that need witnesses to exist and to be told apart.</summary>
    public static WitnessSiglum[] DistinctSigla(int count) {
        return Any.SetOf(new AnyWitnessSiglum()).WithCount(count).Generate().ToArray();
    }

    /// <summary>A siglum that none of <paramref name="known" /> carries, for tests about a witness the tradition has never heard of.</summary>
    public static WitnessSiglum SiglumOtherThan(params WitnessSiglum[] known) {
        return Any.WitnessSiglum().WithValue(v => v.Except(known.Select(s => s.Value))).Generate();
    }

    /// <summary>A passage that is not <paramref name="passage" />, for tests that need two places in the work.</summary>
    public static PassageReference AnyPassageOtherThan(PassageReference passage) {
        return new AnyPassageReference().DifferentFrom(passage).Generate();
    }

    public static Witness WitnessOf(WitnessSiglum siglum) {
        return Any.Witness().WithSiglum(siglum).Generate();
    }

    public static Witness WitnessOf(WitnessSiglum siglum, int datedFrom, int datedTo) {
        return Any.Witness().WithSiglum(siglum).WithDatedFrom(datedFrom).WithDatedTo(datedTo).Generate();
    }

    public static Tradition TraditionOf(params WitnessSiglum[] sigla) {
        Tradition tradition = Any.Tradition().Generate();
        foreach (WitnessSiglum siglum in sigla) {
            tradition.AddWitness(WitnessOf(siglum));
        }

        return tradition;
    }

    /// <summary>A tradition whose witnesses are all dated alike, so that no dating can refuse a copy relation.</summary>
    public static Tradition TraditionOfContemporaries(params WitnessSiglum[] sigla) {
        Tradition tradition = Any.Tradition().Generate();
        foreach (WitnessSiglum siglum in sigla) {
            tradition.AddWitness(WitnessOf(siglum, 1000, 1100));
        }

        return tradition;
    }

    public static Variant Reads(WitnessSiglum witness, PassageReference passage, string text, int hand = 1) {
        return Any.Variant().WithWitness(witness).WithPassage(passage).WithKind(VariantKind.Substitution).WithText(text).WithHand(hand).Generate();
    }

    /// <summary>A reading whose text is not the point: only the place and the hand are.</summary>
    public static Variant ReadsSomething(WitnessSiglum witness, PassageReference passage, int hand = 1) {
        return Any.Variant().WithWitness(witness).WithPassage(passage).WithKind(VariantKind.Substitution).WithHand(hand).Generate();
    }

    public static Variant Omits(WitnessSiglum witness, PassageReference passage, int hand = 1) {
        return Any.Variant().WithWitness(witness).WithPassage(passage).WithKind(VariantKind.Omission).WithHand(hand).Generate();
    }

    public static Variant Corrects(WitnessSiglum witness, PassageReference passage, string text, int hand) {
        return Any.Variant().WithWitness(witness).WithPassage(passage).WithKind(VariantKind.Correction).WithText(text).WithHand(hand).Generate();
    }

    public static Lacuna Gap(WitnessSiglum witness, string from, string to) {
        return Any.Lacuna().WithWitness(witness).WithFrom(Passage(from)).WithTo(Passage(to)).Generate();
    }

    /// <summary>A hypothesis citing witnesses, which must really read <paramref name="reading" /> for the tradition to take it.</summary>
    public static EditorialHypothesis Hypothesis(PassageReference passage, string reading, params WitnessSiglum[] supportedBy) {
        return Any.EditorialHypothesis()
                  .WithPassage(passage)
                  .WithProposedReading(reading)
                  .WithSupportingWitnesses(supportedBy)
                  .WithIsConjecture(supportedBy.Length == 0)
                  .Generate();
    }

}
