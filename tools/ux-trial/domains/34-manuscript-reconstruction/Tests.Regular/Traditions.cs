using ManuscriptReconstruction.Domain;

namespace ManuscriptReconstruction.Tests.Regular;

/// <summary>Test-side shortcuts for a tradition holding arbitrary witnesses under chosen sigla.</summary>
internal static class Traditions {

    public static Tradition WithWitnesses(params string[] sigla) {
        Tradition tradition = Arbitrary.Tradition().Generate();
        foreach (string siglum in sigla) {
            tradition.AddWitness(Arbitrary.Witness().WithSiglum(new WitnessSiglum(siglum)).Generate());
        }

        return tradition;
    }

    public static void Reads(this Tradition tradition, string siglum, PassageReference passage, string text, int hand = 1) {
        tradition.RecordVariant(Arbitrary.Variant()
                                         .WithWitness(new WitnessSiglum(siglum))
                                         .WithPassage(passage)
                                         .WithText(text)
                                         .WithHand(hand)
                                         .Generate());
    }

    public static EditorialHypothesis Proposes(this Tradition tradition, PassageReference passage, string reading, params string[] sigla) {
        EditorialHypothesis hypothesis = Arbitrary.EditorialHypothesis()
                                                  .WithPassage(passage)
                                                  .WithProposedReading(reading)
                                                  .WithSupportingWitnesses(sigla.Select(s => new WitnessSiglum(s)).ToArray())
                                                  .Generate();
        tradition.Propose(hypothesis);

        return hypothesis;
    }

}
