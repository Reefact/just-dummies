using JustDummies;
using JustDummies.Xunit;

using ManuscriptReconstruction.Domain;

[assembly: Reproducible]

namespace ManuscriptReconstruction.Tests.Discovery;

/// <summary>Small constructors shared by the tests: the named parts of a tradition, spelled the way a scholar writes them.</summary>
internal static class Fixture {

    internal static WitnessSiglum Siglum(string value) {
        return new WitnessSiglum(value);
    }

    internal static PassageReference At(string passage) {
        return PassageReference.Parse(passage);
    }

    /// <summary>A witness with a chosen siglum; its repository and folios are dummies, its dating is a given of the scenario.</summary>
    internal static Witness WitnessDated(string siglum, int datedFrom, int datedTo) {
        return Any.Witness().WithSiglum(Siglum(siglum)).WithDatedFrom(datedFrom).WithDatedTo(datedTo).Generate();
    }

    /// <summary>A witness whose dating the scenario does not care about: all of them share one span, so none is certainly older.</summary>
    internal static Witness WitnessOfTheSameAge(string siglum) {
        return WitnessDated(siglum, 1000, 1100);
    }

    internal static Tradition TraditionOf(params string[] sigla) {
        Tradition tradition = Any.Tradition().Generate();
        foreach (string siglum in sigla) {
            tradition.AddWitness(WitnessOfTheSameAge(siglum));
        }

        return tradition;
    }

    /// <summary>What a witness reads, by the first hand unless said otherwise. The kind follows from the text.</summary>
    internal static Variant Reads(string siglum, string passage, string text, int hand = 1) {
        VariantKind kind = text.Length == 0 ? VariantKind.Omission
                         : hand > 1         ? VariantKind.Correction
                                            : VariantKind.Substitution;

        return Any.Variant().WithWitness(Siglum(siglum)).WithPassage(At(passage)).WithKind(kind).WithText(text).WithHand(hand).Generate();
    }

    internal static Tradition WithReadings(Tradition tradition, string passage, params (string Siglum, string Text)[] readings) {
        foreach ((string siglum, string text) in readings) {
            tradition.RecordVariant(Reads(siglum, passage, text));
        }

        return tradition;
    }

    internal static EditorialHypothesis Proposes(string passage, string reading, params string[] witnesses) {
        AnyEditorialHypothesis hypothesis = Any.EditorialHypothesis().WithPassage(At(passage)).WithProposedReading(reading);

        return (witnesses.Length == 0 ? hypothesis.AsConjecture() : hypothesis.CitingWitnesses(witnesses)).Generate();
    }

}
