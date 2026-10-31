using System.Collections.Generic;
using System.Linq;

using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

using static ManuscriptReconstruction.Tests.Discovery.Traditions;

namespace ManuscriptReconstruction.Tests.Discovery;

public sealed class AlignmentTests {

    [Fact]
    public void Witnesses_agreeing_on_a_reading_form_one_column_listed_before_the_smaller_ones() {
        // Arrange
        List<WitnessSiglum> anySigla   = AnyDistinctSigla(3);
        PassageReference    anyPassage = Any.PassageReference().Generate();
        Tradition           tradition  = AnyTraditionWith(anySigla.ToArray());
        tradition.RecordVariant(Any.Variant().WithWitness(anySigla[0]).WithPassage(anyPassage).WithText("Musa mihi causas").Generate());
        tradition.RecordVariant(Any.Variant().WithWitness(anySigla[1]).WithPassage(anyPassage).WithText("Musa mihi causas").Generate());
        tradition.RecordVariant(Any.Variant().WithWitness(anySigla[2]).WithPassage(anyPassage).WithText("Musa mihi casus").Generate());

        // Act
        IReadOnlyList<AttestedReading> alignment = tradition.Align(anyPassage);

        // Assert
        Assert.Equal(["Musa mihi causas", "Musa mihi casus"], alignment.Select(r => r.Text));
        Assert.Equal([2, 1], alignment.Select(r => r.Witnesses.Count));
    }

    [Fact]
    public void Readings_differing_only_in_spacing_are_aligned_together() {
        // Arrange
        List<WitnessSiglum> anySigla   = AnyDistinctSigla(2);
        PassageReference    anyPassage = Any.PassageReference().Generate();
        Tradition           tradition  = AnyTraditionWith(anySigla.ToArray());
        tradition.RecordVariant(Any.Variant().WithWitness(anySigla[0]).WithPassage(anyPassage).WithText("quidve dolens").Generate());
        tradition.RecordVariant(Any.Variant().WithWitness(anySigla[1]).WithPassage(anyPassage).WithText("  quidve   dolens ").Generate());

        // Act
        IReadOnlyList<AttestedReading> alignment = tradition.Align(anyPassage);

        // Assert
        AttestedReading column = Assert.Single(alignment);
        Assert.Equal("quidve dolens", column.Text);
    }

    [Fact]
    public void Witnesses_omitting_the_passage_share_an_omission_column() {
        // Arrange
        List<WitnessSiglum> anySigla   = AnyDistinctSigla(3);
        PassageReference    anyPassage = Any.PassageReference().Generate();
        Tradition           tradition  = AnyTraditionWith(anySigla.ToArray());
        tradition.RecordVariant(Any.Variant().WithWitness(anySigla[0]).WithPassage(anyPassage).WithText("regina deum").Generate());
        tradition.RecordVariant(Any.Variant().WithWitness(anySigla[1]).WithPassage(anyPassage).WithKind(VariantKind.Omission).WithText("").Generate());
        tradition.RecordVariant(Any.Variant().WithWitness(anySigla[2]).WithPassage(anyPassage).WithKind(VariantKind.Omission).WithText("").Generate());

        // Act
        IReadOnlyList<AttestedReading> alignment = tradition.Align(anyPassage);

        // Assert
        Assert.True(alignment[0].IsOmission);
        Assert.Equal(2, alignment[0].Witnesses.Count);
    }

    [Fact]
    public void A_corrected_witness_is_aligned_on_its_corrected_reading() {
        // Arrange
        List<WitnessSiglum> anySigla   = AnyDistinctSigla(2);
        PassageReference    anyPassage = Any.PassageReference().Generate();
        Tradition           tradition  = AnyTraditionWith(anySigla.ToArray());
        tradition.RecordVariant(Any.Variant().WithWitness(anySigla[0]).WithPassage(anyPassage).WithText("tot uoluere casus").Generate());
        tradition.RecordVariant(Any.Variant().WithWitness(anySigla[1]).WithPassage(anyPassage).WithHand(Variant.FirstHand).WithText("tot uoluere causas").Generate());
        tradition.RecordVariant(Any.Variant().WithWitness(anySigla[1]).WithPassage(anyPassage).WithKind(VariantKind.Correction)
                                   .WithHand(h => h.GreaterThan(Variant.FirstHand)).WithText("tot uoluere casus").Generate());

        // Act
        IReadOnlyList<AttestedReading> alignment = tradition.Align(anyPassage);

        // Assert
        AttestedReading column = Assert.Single(alignment);
        Assert.Equal("tot uoluere casus", column.Text);
    }

}
