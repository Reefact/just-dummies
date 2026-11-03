using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

using static ManuscriptReconstruction.Tests.Discovery.Fixture;

namespace ManuscriptReconstruction.Tests.Discovery.Entities;

public sealed class WitnessTests {

    [Fact]
    public void A_witness_keeps_what_it_was_built_from() {
        // Act
        Witness witness = new Witness(Siglum("Vat.3225"), "Biblioteca Apostolica Vaticana", 400, 450, 76);

        // Assert
        Assert.Equal(Siglum("Vat.3225"), witness.Siglum);
        Assert.Equal("Biblioteca Apostolica Vaticana", witness.Repository);
        Assert.Equal(400, witness.DatedFrom);
        Assert.Equal(450, witness.DatedTo);
        Assert.Equal(76, witness.FolioCount);
    }

    [Theory]
    [InlineData(-800, -800)]   // the earliest year, a single-year span
    [InlineData(1600, 1600)]   // the latest year
    [InlineData(-800, 1600)]   // the whole range
    [InlineData(-1, 1)]        // across the Common Era
    public void A_dating_span_within_the_range_is_accepted(int datedFrom, int datedTo) {
        // Act
        Witness witness = Any.Witness().WithDatedFrom(datedFrom).WithDatedTo(datedTo).Generate();

        // Assert
        Assert.Equal(datedFrom, witness.DatedFrom);
        Assert.Equal(datedTo, witness.DatedTo);
    }

    [Theory]
    [InlineData(-801, 0)]
    [InlineData(0, 1601)]
    [InlineData(-801, -801)]
    [InlineData(1601, 1601)]
    public void A_year_out_of_range_is_refused(int datedFrom, int datedTo) {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();

        // Act
        Action construct = () => new Witness(anySiglum, "Bodleian", datedFrom, datedTo, 10);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Fact]
    public void A_dating_span_cannot_start_after_it_ends() {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();

        // Act
        Action construct = () => new Witness(anySiglum, "Bodleian", 900, 899, 10);

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_witness_has_at_least_one_folio(int folioCount) {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();

        // Act
        Action construct = () => new Witness(anySiglum, "Bodleian", 900, 950, folioCount);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_witness_has_a_repository(string repository) {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();

        // Act
        Action construct = () => new Witness(anySiglum, repository, 900, 950, 10);

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_witness_has_a_siglum() {
        Assert.Throws<ArgumentNullException>(() => new Witness(null!, "Bodleian", 900, 950, 10));
    }

    [Theory]
    [InlineData(800, 899, 900, 950, true)]    // ends the year before the other begins
    [InlineData(800, 900, 900, 950, false)]   // touches: the same year might do for both
    [InlineData(800, 920, 900, 950, false)]   // overlaps
    [InlineData(900, 950, 900, 950, false)]   // same span
    [InlineData(960, 990, 900, 950, false)]   // younger
    [InlineData(-800, -500, -499, 100, true)] // across the Common Era
    public void A_witness_is_certainly_older_only_when_its_span_ends_before_the_others_begins(int fromThis, int toThis, int fromOther, int toOther, bool certainlyOlder) {
        // Arrange
        Witness witness = WitnessDated("A", fromThis, toThis);
        Witness other   = WitnessDated("B", fromOther, toOther);

        // Assert
        Assert.Equal(certainlyOlder, witness.IsCertainlyOlderThan(other));
    }

    [Fact]
    public void Comparing_the_age_with_nothing_is_refused() {
        Assert.Throws<ArgumentNullException>(() => WitnessOfTheSameAge("A").IsCertainlyOlderThan(null!));
    }

}
