using DnaVariants.Domain;
using DnaVariants.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace DnaVariants.Tests.Expert;

public sealed class AlleleTests {

    [Fact]
    public void An_allele_may_be_empty() {
        // Act
        Allele allele = new Allele("");

        // Assert
        Assert.True(allele.IsEmpty);
        Assert.Equal(0, allele.Length);
        Assert.Equal(Allele.Empty, allele);
    }

    [Fact]
    public void An_allele_is_stored_upper_case() {
        // Act
        Allele allele = new Allele("acGt");

        // Assert
        Assert.Equal("ACGT", allele.Bases);
        Assert.Equal(4, allele.Length);
    }

    [Fact]
    public void An_allele_carries_called_bases_only() {
        // Arrange
        string anyBasesBefore = Any.String().WithChars("ACGT").WithLengthBetween(0, 5).Generate();
        string anyBasesAfter  = Any.String().WithChars("ACGT").WithLengthBetween(0, 5).Generate();
        string unknownBase    = anyBasesBefore + "N" + anyBasesAfter;

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Allele(unknownBase));

        // Assert
        Assert.Equal("bases", refusal.ParamName);
        Assert.Contains($"'N' at offset {anyBasesBefore.Length}", refusal.Message);
    }

    [Theory]
    [InlineData("U")]
    [InlineData("AC GT")]
    [InlineData("A-C")]
    [InlineData("ACG\n")]
    [InlineData("AĊG")]
    public void A_character_outside_acgt_is_refused(string bases) {
        // Act
        Exception refusal = Record.Exception(() => new Allele(bases))!;

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void An_allele_of_exactly_the_maximum_length_is_accepted() {
        // Arrange
        string anyMaximumLengthBases = Any.String().WithChars("ACGT").WithLength(Allele.MaxLength).Generate();

        // Act
        Allele allele = new Allele(anyMaximumLengthBases);

        // Assert
        Assert.Equal(Allele.MaxLength, allele.Length);
    }

    [Fact]
    public void An_allele_one_base_over_the_maximum_length_is_refused() {
        // Arrange
        string anyTooLongBases = Any.String().WithChars("ACGT").WithLength(Allele.MaxLength + 1).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Allele(anyTooLongBases));

        // Assert
        Assert.Equal("bases", refusal.ParamName);
    }

    [Fact]
    public void A_null_allele_is_refused_as_null() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Allele(null!));

        // Assert
        Assert.Equal("bases", refusal.ParamName);
    }

    [Fact]
    public void Alleles_written_with_different_casing_are_equal() {
        // Arrange
        Allele upper = new Allele("ACGT");
        Allele lower = new Allele("acgt");

        // Assert
        Assert.Equal(upper, lower);
        Assert.Equal(upper.GetHashCode(), lower.GetHashCode());
    }

    [Fact]
    public void Alleles_with_different_bases_are_not_equal_and_the_empty_one_equals_none_of_them() {
        // Arrange
        Allele first  = new Allele("ACG");
        Allele second = new Allele("ACT");

        // Assert
        Assert.NotEqual(first, second);
        Assert.NotEqual(Allele.Empty, first);
    }

    [Fact]
    public void An_empty_allele_prints_as_a_dash_and_others_as_their_bases() {
        // Assert
        Assert.Equal("-", Allele.Empty.ToString());
        Assert.Equal("ACG", new Allele("acg").ToString());
    }

}
