using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

using static ManuscriptReconstruction.Tests.Discovery.Fixture;

namespace ManuscriptReconstruction.Tests.Discovery.ValueObjects;

public sealed class AttestedReadingTests {

    [Fact]
    public void The_witnesses_of_a_reading_are_sorted_by_siglum() {
        // Arrange
        WitnessSiglum[] unsorted = [Siglum("Vat.3225"), Siglum("B2"), Siglum("A"), Siglum("Ab")];

        // Act
        AttestedReading reading = Any.AttestedReading().WithWitnesses(unsorted).Generate();

        // Assert
        Assert.Equal(["A", "Ab", "B2", "Vat.3225"], reading.Witnesses.Select(w => w.Value));
    }

    [Fact]
    public void Sorting_is_ordinal_so_capitals_come_before_lowercase_letters() {
        // Act
        AttestedReading reading = Any.AttestedReading().WithWitnesses([Siglum("Aa"), Siglum("AB")]).Generate();

        // Assert
        Assert.Equal(["AB", "Aa"], reading.Witnesses.Select(w => w.Value));
    }

    [Fact]
    public void A_reading_keeps_its_passage() {
        // Act
        AttestedReading reading = Any.AttestedReading().WithPassage(At("2.3.4")).Generate();

        // Assert
        Assert.Equal(At("2.3.4"), reading.Passage);
    }

    [Fact]
    public void A_reading_is_normalised_like_a_variant() {
        // Act
        AttestedReading reading = Any.AttestedReading().WithText("  in   principio ").Generate();

        // Assert
        Assert.Equal("in principio", reading.Text);
        Assert.False(reading.IsOmission);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_reading_without_text_is_an_omission(string text) {
        // Act
        AttestedReading reading = Any.AttestedReading().WithText(text).Generate();

        // Assert
        Assert.True(reading.IsOmission);
        Assert.Equal(string.Empty, reading.Text);
    }

    [Fact]
    public void A_reading_needs_at_least_one_witness() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();

        // Act
        Action construct = () => new AttestedReading(anyPassage, "verbum", Array.Empty<WitnessSiglum>());

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_witness_cannot_attest_the_same_reading_twice() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();

        // Act
        Action construct = () => new AttestedReading(anyPassage, "verbum", [Siglum("A"), Siglum("B"), Siglum("A")]);

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_missing_witness_in_the_list_is_refused() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();

        // Act
        Action construct = () => new AttestedReading(anyPassage, "verbum", [Siglum("A"), null!]);

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void Nothing_is_not_a_part_of_a_reading() {
        // Arrange
        PassageReference anyPassage = Any.PassageReference().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new AttestedReading(null!, "verbum", [Siglum("A")]));
        Assert.Throws<ArgumentNullException>(() => new AttestedReading(anyPassage, null!, [Siglum("A")]));
        Assert.Throws<ArgumentNullException>(() => new AttestedReading(anyPassage, "verbum", null!));
    }

}
