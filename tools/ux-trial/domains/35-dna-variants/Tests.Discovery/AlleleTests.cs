using JustDummies;

using Xunit;

namespace DnaVariants.Domain;

public sealed class AlleleTests {

    [Fact]
    public void An_allele_may_be_empty() {
        Allele allele = new Allele(string.Empty);

        Assert.True(allele.IsEmpty);
        Assert.Equal(0, allele.Length);
        Assert.Equal("-", allele.ToString());
        Assert.Equal(Allele.Empty, allele);
    }

    [Theory]
    [InlineData("acgt", "ACGT")]
    [InlineData("A", "A")]
    [InlineData("gAtTaCa", "GATTACA")]
    public void Bases_are_stored_upper_case(string written, string stored) {
        Allele allele = new Allele(written);

        Assert.Equal(stored, allele.Bases);
        Assert.Equal(stored, allele.ToString());
        Assert.Equal(stored.Length, allele.Length);
        Assert.False(allele.IsEmpty);
    }

    [Fact]
    public void A_null_allele_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Allele(null!));
    }

    [Theory]
    [InlineData("N")]
    [InlineData("ACNGT")]
    [InlineData("acgu")]
    [InlineData("AC GT")]
    [InlineData("-")]
    public void An_allele_only_carries_called_bases(string written) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Allele(written));

        Assert.Equal("bases", refusal.ParamName);
    }

    [Fact]
    public void An_allele_of_the_maximum_length_is_accepted() {
        string anyBases = Any.String().WithChars("ACGT").WithLength(Allele.MaxLength).Generate();

        Allele allele = new Allele(anyBases);

        Assert.Equal(Allele.MaxLength, allele.Length);
    }

    [Fact]
    public void An_allele_longer_than_the_maximum_is_refused() {
        string anyBases = Any.String().WithChars("ACGT").WithLength(Allele.MaxLength + 1).Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Allele(anyBases));

        Assert.Contains((Allele.MaxLength + 1).ToString(), refusal.Message);
    }

    [Fact]
    public void Alleles_with_the_same_bases_are_equal_whatever_the_case_they_were_written_in() {
        Assert.Equal(new Allele("acg"), new Allele("ACG"));
        Assert.Equal(new Allele("acg").GetHashCode(), new Allele("ACG").GetHashCode());
        Assert.NotEqual(new Allele("ACG"), new Allele("ACT"));
        Assert.NotEqual(new Allele("A"), Allele.Empty);
    }

    [Fact]
    public void Any_generated_allele_may_be_empty_or_not_but_is_never_refused() {
        Exception? refusal = Record.Exception(() => Any.Allele().Generate());

        Assert.Null(refusal);
    }

}
