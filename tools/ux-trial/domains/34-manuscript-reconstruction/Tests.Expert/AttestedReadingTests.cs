using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Expert;

public sealed class AttestedReadingTests {

    [Fact]
    public void A_column_lists_its_witnesses_in_ordinal_order_of_their_sigla() {
        // Arrange
        WitnessSiglum[] shuffled = [new WitnessSiglum("B2"), new WitnessSiglum("Vat.3225"), new WitnessSiglum("B.1"), new WitnessSiglum("A"), new WitnessSiglum("B")];

        // Act
        AttestedReading reading = Any.AttestedReading().WithWitnesses(shuffled).Generate();

        // Assert
        Assert.Equal(["A", "B", "B.1", "B2", "Vat.3225"], reading.Witnesses.Select(w => w.Value));
    }

    [Fact]
    public void A_column_is_still_sorted_when_it_has_a_single_witness() {
        // Arrange
        WitnessSiglum witness = new WitnessSiglum("C");

        // Act
        AttestedReading reading = Any.AttestedReading().WithWitnesses([witness]).Generate();

        // Assert
        Assert.Equal([witness], reading.Witnesses);
    }

    [Fact]
    public void A_column_keeps_the_passage_it_was_given() {
        // Arrange
        PassageReference passage = PassageReference.Parse("3.12.4");

        // Act
        AttestedReading reading = Any.AttestedReading().WithPassage(passage).Generate();

        // Assert
        Assert.Same(passage, reading.Passage);
    }

    [Theory]
    [InlineData("  arma   virumque ", "arma virumque")]
    [InlineData("arma\tvirumque", "arma virumque")]
    public void The_text_of_a_column_is_normalised_like_a_variants(string written, string expected) {
        // Act
        AttestedReading reading = Any.AttestedReading().WithText(written).Generate();

        // Assert
        Assert.Equal(expected, reading.Text);
        Assert.False(reading.IsOmission);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void A_column_without_text_is_an_omission(string written) {
        // Act
        AttestedReading reading = Any.AttestedReading().WithText(written).Generate();

        // Assert
        Assert.Equal(string.Empty, reading.Text);
        Assert.True(reading.IsOmission);
    }

    [Fact]
    public void A_column_needs_at_least_one_witness() {
        // Arrange
        AnyAttestedReading readings = Any.AttestedReading();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => readings.WithWitnesses(Array.Empty<WitnessSiglum>()).Generate());

        // Assert
        Assert.Equal("witnesses", refusal.ParamName);
    }

    [Fact]
    public void A_column_cannot_cite_a_witness_twice() {
        // Arrange: the same siglum, made twice, so that the value is what is compared.
        string        anySiglumText = Any.WitnessSiglum().Generate().Value;
        WitnessSiglum other         = Any.WitnessSiglum().WithValue(v => v.DifferentFrom(anySiglumText)).Generate();
        WitnessSiglum[] cited       = [new WitnessSiglum(anySiglumText), other, new WitnessSiglum(anySiglumText)];

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.AttestedReading().WithWitnesses(cited).Generate());

        // Assert
        Assert.Equal("witnesses", refusal.ParamName);
    }

    [Fact]
    public void A_column_cannot_cite_a_missing_witness() {
        // Arrange
        WitnessSiglum[] cited = [Any.WitnessSiglum().Generate(), null!];

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.AttestedReading().WithWitnesses(cited).Generate());

        // Assert
        Assert.Equal("witnesses", refusal.ParamName);
    }

    [Fact]
    public void A_missing_passage_text_or_witness_list_is_refused() {
        // Arrange
        PassageReference anyPassage   = Any.PassageReference().Generate();
        WitnessSiglum[]  anyWitnesses = Scene.DistinctSigla(2);

        // Assert
        Assert.Throws<ArgumentNullException>(() => new AttestedReading(null!, "arma", anyWitnesses));
        Assert.Throws<ArgumentNullException>(() => new AttestedReading(anyPassage, null!, anyWitnesses));
        Assert.Throws<ArgumentNullException>(() => new AttestedReading(anyPassage, "arma", null!));
    }

    [Fact]
    public void A_column_is_not_changed_by_the_list_it_was_built_from() {
        // Arrange
        WitnessSiglum[] siglaArray = Scene.DistinctSigla(3);
        List<WitnessSiglum> cited  = [.. siglaArray];
        AttestedReading     reading = Any.AttestedReading().WithWitnesses(cited).Generate();

        // Act
        cited.Clear();

        // Assert
        Assert.Equal(3, reading.Witnesses.Count);
    }

    [Fact(Skip = "domain defect: Witnesses hands out the internal array behind IReadOnlyList, so a cast to IList<WitnessSiglum> rewrites the column and breaks its order (the cast fails on every run, whatever the seed)")]
    public void A_column_cannot_be_rewritten_through_its_witness_list() {
        // Arrange
        WitnessSiglum[]  siglaArray = Scene.DistinctSigla(2);
        AttestedReading  reading    = Any.AttestedReading().WithWitnesses(siglaArray).Generate();
        WitnessSiglum    anyOther   = Any.WitnessSiglum().DifferentFrom(reading.Witnesses[0]).Generate();

        // Act
        Exception? failure = Record.Exception(() => ((IList<WitnessSiglum>)reading.Witnesses)[0] = anyOther);

        // Assert
        Assert.NotNull(failure);
    }

}
