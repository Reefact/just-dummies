namespace DnaVariants.Tests.Regular;

public sealed class AlleleTests {

    [Fact]
    public void Bases_are_stored_upper_case() {
        Allele allele = new Allele("acGt");

        Assert.Equal("ACGT", allele.Bases);
        Assert.Equal(4, allele.Length);
        Assert.False(allele.IsEmpty);
    }

    [Fact]
    public void An_allele_may_be_empty_and_prints_as_a_dash() {
        Allele allele = new Allele(string.Empty);

        Assert.True(allele.IsEmpty);
        Assert.Equal(0, allele.Length);
        Assert.Equal("-", allele.ToString());
        Assert.Equal(Allele.Empty, allele);
    }

    [Fact]
    public void Any_drawn_allele_is_made_of_called_bases_only() {
        // Arrange
        Allele anyAllele = new AnyAllele().Generate();

        // Assert
        Assert.All(anyAllele.Bases, b => Assert.Contains(b, "ACGT"));
    }

    [Fact]
    public void An_unknown_base_is_refused_and_named() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Allele("ACGNT"));

        Assert.Contains("'N' at offset 3", refusal.Message);
    }

    [Fact]
    public void An_allele_of_the_maximum_length_is_accepted() {
        string exactlyTheMaximum = Any.String().WithChars("ACGT").WithLength(Allele.MaxLength).Generate();

        Allele allele = new Allele(exactlyTheMaximum);

        Assert.Equal(Allele.MaxLength, allele.Length);
    }

    [Fact]
    public void An_allele_longer_than_the_maximum_is_refused() {
        string oneTooMany = Any.String().WithChars("ACGT").WithLength(Allele.MaxLength + 1).Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Allele(oneTooMany));

        Assert.Contains("10001", refusal.Message);
    }

    [Fact]
    public void A_null_allele_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Allele(null!));
    }

    [Fact]
    public void Two_alleles_written_the_same_are_equal_whatever_the_case() {
        Assert.Equal(new Allele("acgt"), new Allele("ACGT"));
        Assert.Equal(new Allele("acgt").GetHashCode(), new Allele("ACGT").GetHashCode());
        Assert.NotEqual(new Allele("ACGT"), new Allele("ACG"));
    }

}
