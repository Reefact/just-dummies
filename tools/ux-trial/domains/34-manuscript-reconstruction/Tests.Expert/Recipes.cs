using JustDummies;

using ManuscriptReconstruction.Domain;

namespace ManuscriptReconstruction.Tests.Expert;

/// <summary>
///     The arrange vocabulary of a philologist's test: a tradition, witnesses whose details do not matter,
///     and what a witness reads at a passage. Everything not named by the caller is drawn.
/// </summary>
internal static class Recipes {

    public static WitnessSiglum Siglum(string value) {
        return new WitnessSiglum(value);
    }

    public static PassageReference Passage(string reference) {
        return PassageReference.Parse(reference);
    }

    /// <summary>Distinct sigla the test does not care about, beyond their being distinct.</summary>
    public static WitnessSiglum[] AnyDistinctSigla(int count) {
        return Any.ArrayOf(Any.WitnessSiglum()).WithCount(count).Distinct().Generate();
    }

    /// <summary>An empty tradition of an arbitrary work, holding a witness for each siglum given.</summary>
    public static Tradition AnyTraditionWith(params WitnessSiglum[] sigla) {
        Tradition anyTradition = Any.Tradition().Generate();
        foreach (WitnessSiglum siglum in sigla) {
            anyTradition.AddWitness(Any.Witness().WithSiglum(siglum).Generate());
        }

        return anyTradition;
    }

    /// <summary>A reading that stands on its own: anything but a correction, which needs an earlier hand's reading.</summary>
    public static AnyVariant AnyStandaloneReading() {
        return Any.Variant().WithKind(kind => kind.DifferentFrom(VariantKind.Correction));
    }

    /// <summary>Records what the first hand of a witness wrote at a passage, of whichever kind fits the text.</summary>
    public static void FirstHandReads(this Tradition tradition, WitnessSiglum witness, PassageReference passage, string text) {
        tradition.RecordVariant(Any.Variant().WithWitness(witness).WithPassage(passage).WithText(text).WithHand(Variant.FirstHand).Generate());
    }

    /// <summary>Records a later hand's correction of a witness at a passage.</summary>
    public static void CorrectorWrites(this Tradition tradition, WitnessSiglum witness, PassageReference passage, string text) {
        tradition.RecordVariant(Any.Variant().WithWitness(witness).WithPassage(passage).WithText(text).WithKind(VariantKind.Correction).Generate());
    }

    /// <summary>Proposes a reading supported by the given witnesses, and returns its identifier.</summary>
    public static Guid Proposes(this Tradition tradition, PassageReference passage, string reading, params WitnessSiglum[] supportingWitnesses) {
        EditorialHypothesis anyHypothesis = Any.EditorialHypothesis()
                                               .WithPassage(passage)
                                               .WithProposedReading(reading)
                                               .WithSupportingWitnesses(supportingWitnesses)
                                               .Generate();
        tradition.Propose(anyHypothesis);

        return anyHypothesis.Id;
    }

}
