using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Expert;

/// <summary>
///     The hand-completed generators are code of this project like any other: these tests hold the rules they
///     encode (the ties between parameters the scaffolder could not draw) to the constructors they feed.
/// </summary>
public sealed class GeneratorsTests {

    private const int Draws = 200;

    [Fact]
    public void A_drawn_variant_respects_the_ties_between_its_kind_its_text_and_its_hand() {
        // Act
        List<Variant> variants = Any.ListOf(Any.Variant()).WithCount(Draws).Generate();

        // Assert
        Assert.All(variants, v => Assert.Equal(v.Kind == VariantKind.Omission, v.Text.Length == 0));
        Assert.All(variants.Where(v => v.Kind == VariantKind.Correction), v => Assert.False(v.IsByFirstHand));
    }

    [Fact]
    public void Drawn_variants_cover_every_kind_over_enough_draws() {
        // Act
        List<Variant> variants = Any.ListOf(Any.Variant()).WithCount(Draws).Generate();

        // Assert
        Assert.Equal(Enum.GetValues<VariantKind>().Length, variants.Select(v => v.Kind).Distinct().Count());
    }

    [Fact]
    public void A_variant_with_a_pinned_text_draws_a_kind_that_can_carry_it() {
        // Arrange
        AnyVariant blankText   = Any.Variant().WithText("   ");
        AnyVariant filledText  = Any.Variant().WithText("arma");

        // Act
        List<Variant> blank  = Any.ListOf(blankText).WithCount(Draws).Generate();
        List<Variant> filled = Any.ListOf(filledText).WithCount(Draws).Generate();

        // Assert
        Assert.All(blank, v => Assert.Equal(VariantKind.Omission, v.Kind));
        Assert.All(filled, v => Assert.NotEqual(VariantKind.Omission, v.Kind));
    }

    [Fact]
    public void A_variant_pinned_to_the_first_hand_draws_a_kind_the_first_hand_can_have() {
        // Act
        List<Variant> variants = Any.ListOf(Any.Variant().WithHand(1)).WithCount(Draws).Generate();

        // Assert
        Assert.All(variants, v => Assert.NotEqual(VariantKind.Correction, v.Kind));
    }

    [Fact]
    public void A_variant_pinned_to_a_correction_draws_a_later_hand() {
        // Act
        List<Variant> variants = Any.ListOf(Any.Variant().WithKind(VariantKind.Correction)).WithCount(Draws).Generate();

        // Assert
        Assert.All(variants, v => Assert.InRange(v.Hand, 2, Variant.MaxHand));
    }

    [Fact]
    public void A_drawn_lacuna_ends_where_it_starts_or_after() {
        // Act
        List<Lacuna> lacunae = Any.ListOf(Any.Lacuna()).WithCount(Draws).Generate();

        // Assert
        Assert.All(lacunae, l => Assert.True(l.From.CompareTo(l.To) <= 0));
    }

    [Fact]
    public void A_lacuna_pinned_at_one_end_keeps_that_end_and_draws_the_other_on_the_right_side() {
        // Arrange
        PassageReference pivot = Any.PassageReference().Generate();

        // Act
        List<Lacuna> startingThere = Any.ListOf(Any.Lacuna().WithFrom(pivot)).WithCount(Draws).Generate();
        List<Lacuna> endingThere   = Any.ListOf(Any.Lacuna().WithTo(pivot)).WithCount(Draws).Generate();

        // Assert
        Assert.All(startingThere, l => Assert.Equal(pivot, l.From));
        Assert.All(startingThere, l => Assert.True(pivot.CompareTo(l.To) <= 0));
        Assert.All(endingThere, l => Assert.Equal(pivot, l.To));
        Assert.All(endingThere, l => Assert.True(l.From.CompareTo(pivot) <= 0));
    }

    [Fact]
    public void A_drawn_hypothesis_is_a_conjecture_exactly_when_it_cites_no_witness() {
        // Act
        List<EditorialHypothesis> hypotheses = Any.ListOf(Any.EditorialHypothesis()).WithCount(Draws).Generate();

        // Assert
        Assert.All(hypotheses, h => Assert.Equal(h.IsConjecture, h.SupportingWitnesses.Count == 0));
        Assert.Contains(hypotheses, h => h.IsConjecture);
        Assert.Contains(hypotheses, h => !h.IsConjecture);
    }

    [Fact]
    public void A_hypothesis_pinned_to_its_witnesses_follows_them() {
        // Arrange
        WitnessSiglum[] cited = Scene.DistinctSigla(2);

        // Act
        EditorialHypothesis attested   = Any.EditorialHypothesis().WithSupportingWitnesses(cited).Generate();
        EditorialHypothesis conjecture = Any.EditorialHypothesis().WithSupportingWitnesses(Array.Empty<WitnessSiglum>()).Generate();

        // Assert
        Assert.False(attested.IsConjecture);
        Assert.True(conjecture.IsConjecture);
    }

    [Fact]
    public void A_drawn_copy_relation_keeps_exemplar_and_copy_apart_when_one_side_is_pinned() {
        // Arrange
        WitnessSiglum pinned = Any.WitnessSiglum().Generate();

        // Act
        List<CopyRelation> asExemplar = Any.ListOf(Any.CopyRelation().WithExemplar(pinned)).WithCount(Draws).Generate();
        List<CopyRelation> asCopy     = Any.ListOf(Any.CopyRelation().WithCopy(pinned)).WithCount(Draws).Generate();

        // Assert
        Assert.All(asExemplar, r => Assert.NotEqual(pinned, r.Copy));
        Assert.All(asCopy, r => Assert.NotEqual(pinned, r.Exemplar));
    }

    [Fact]
    public void A_drawn_witness_siglum_follows_the_pattern_of_the_domain() {
        // Act
        List<WitnessSiglum> sigla = Any.ListOf(Any.WitnessSiglum()).WithCount(Draws).Generate();

        // Assert
        Assert.All(sigla, s => Assert.InRange(s.Value.Length, 1, WitnessSiglum.MaxLength));
        Assert.Contains(sigla, s => s.Value.Length == WitnessSiglum.MaxLength);
    }

}
