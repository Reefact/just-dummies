using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Regular;

public sealed class AttestedReadingTests {

    [Fact]
    public void The_witnesses_are_listed_in_ordinal_order_whatever_the_order_given() {
        // Arrange
        WitnessSiglum[] given = [new WitnessSiglum("Vat.3"), new WitnessSiglum("B2"), new WitnessSiglum("B10"), new WitnessSiglum("A")];

        // Act
        AttestedReading reading = Any.AttestedReading().WithWitnesses(given).Generate();

        // Assert
        Assert.Equal(["A", "B10", "B2", "Vat.3"], reading.Witnesses.Select(w => w.Value));
    }

    [Fact]
    public void A_reading_keeps_its_passage() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();

        // Act
        AttestedReading reading = Any.AttestedReading().WithPassage(anyPassage).Generate();

        // Assert
        Assert.Same(anyPassage, reading.Passage);
    }

    [Fact]
    public void The_text_is_normalised() {
        // Act
        AttestedReading reading = Any.AttestedReading().WithText("  arma \n  virumque ").Generate();

        // Assert
        Assert.Equal("arma virumque", reading.Text);
        Assert.False(reading.IsOmission);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void A_blank_text_is_an_omission(string blank) {
        // Act
        AttestedReading reading = Any.AttestedReading().WithText(blank).Generate();

        // Assert
        Assert.Equal(string.Empty, reading.Text);
        Assert.True(reading.IsOmission);
    }

    [Fact]
    public void A_reading_with_no_witness_has_no_provenance_and_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.AttestedReading().WithWitnesses(Array.Empty<WitnessSiglum>()).Generate());

        // Assert
        Assert.Equal("witnesses", refusal.ParamName);
    }

    [Fact]
    public void A_witness_cannot_be_counted_twice() {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.AttestedReading().WithWitnesses([anySiglum, new WitnessSiglum(anySiglum.Value)]).Generate());

        // Assert
        Assert.Equal("witnesses", refusal.ParamName);
    }

    [Fact]
    public void A_null_witness_is_refused() {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.AttestedReading().WithWitnesses([anySiglum, null!]).Generate());

        // Assert
        Assert.Equal("witnesses", refusal.ParamName);
    }

    [Fact]
    public void A_reading_needs_a_passage_a_text_and_witnesses() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();
        WitnessSiglum[]  anyOne     = [Any.WitnessSiglum().Generate()];

        // Assert
        Assert.Throws<ArgumentNullException>(() => new AttestedReading(null!, "arma", anyOne));
        Assert.Throws<ArgumentNullException>(() => new AttestedReading(anyPassage, null!, anyOne));
        Assert.Throws<ArgumentNullException>(() => new AttestedReading(anyPassage, "arma", null!));
    }

    [Fact]
    public void Changing_the_list_given_does_not_change_the_reading() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla = Fixtures.DistinctSigla(2);
        List<WitnessSiglum>          given    = [anySigla[0], anySigla[1]];
        AttestedReading              reading  = Any.AttestedReading().WithWitnesses(given).Generate();

        // Act
        given.Clear();

        // Assert
        Assert.Equal(2, reading.Witnesses.Count);
    }

}
