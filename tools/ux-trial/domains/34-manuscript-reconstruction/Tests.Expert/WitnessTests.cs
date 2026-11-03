using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Expert;

public sealed class WitnessTests {

    [Fact]
    public void A_witness_keeps_what_it_was_given() {
        // Arrange
        WitnessSiglum siglum = new WitnessSiglum("Vat.3225");

        // Act
        Witness witness = new Witness(siglum, "Biblioteca Apostolica Vaticana", 400, 450, 76);

        // Assert
        Assert.Same(siglum, witness.Siglum);
        Assert.Equal("Biblioteca Apostolica Vaticana", witness.Repository);
        Assert.Equal((400, 450, 76), (witness.DatedFrom, witness.DatedTo, witness.FolioCount));
    }

    [Theory]
    [InlineData(-800, -800)]   // the earliest year, as a one-year span
    [InlineData(1600, 1600)]   // the latest
    [InlineData(-800, 1600)]   // the widest span
    [InlineData(0, 0)]
    public void A_dating_span_may_run_from_the_earliest_to_the_latest_year(int from, int to) {
        // Act
        Witness witness = Any.Witness().WithDatedFrom(from).WithDatedTo(to).Generate();

        // Assert
        Assert.Equal((from, to), (witness.DatedFrom, witness.DatedTo));
    }

    [Theory]
    [InlineData(-801, 0)]
    [InlineData(int.MinValue, 0)]
    public void A_dating_that_starts_before_year_minus_800_is_refused(int from, int to) {
        // Arrange
        WitnessSiglum anySiglum     = Any.WitnessSiglum().Generate();
        string        anyRepository = Any.String().NotBlank().Generate();
        int           anyFolios     = Any.Int32().Positive().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Witness(anySiglum, anyRepository, from, to, anyFolios));

        // Assert
        Assert.Equal("datedFrom", refusal.ParamName);
    }

    [Theory]
    [InlineData(0, 1601)]
    [InlineData(0, int.MaxValue)]
    public void A_dating_that_ends_after_year_1600_is_refused(int from, int to) {
        // Arrange
        WitnessSiglum anySiglum     = Any.WitnessSiglum().Generate();
        string        anyRepository = Any.String().NotBlank().Generate();
        int           anyFolios     = Any.Int32().Positive().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Witness(anySiglum, anyRepository, from, to, anyFolios));

        // Assert
        Assert.Equal("datedTo", refusal.ParamName);
    }

    [Theory]
    [InlineData(100, 99)]
    [InlineData(1600, -800)]
    [InlineData(0, -1)]
    public void A_dating_cannot_end_before_it_starts(int from, int to) {
        // Arrange
        WitnessSiglum anySiglum     = Any.WitnessSiglum().Generate();
        string        anyRepository = Any.String().NotBlank().Generate();
        int           anyFolios     = Any.Int32().Positive().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Witness(anySiglum, anyRepository, from, to, anyFolios));

        // Assert
        Assert.Equal("datedFrom", refusal.ParamName);
    }

    [Fact]
    public void A_witness_has_at_least_one_folio() {
        Assert.Equal(1, Any.Witness().WithFolioCount(1).Generate().FolioCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_witness_without_folios_is_refused(int folios) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Witness().WithFolioCount(folios).Generate());

        // Assert
        Assert.Equal("folioCount", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void A_blank_repository_is_refused(string repository) {
        Assert.Throws<ArgumentException>(() => Any.Witness().WithRepository(repository).Generate());
    }

    [Fact]
    public void A_missing_repository_is_refused() {
        Assert.ThrowsAny<ArgumentException>(() => Any.Witness().WithRepository((string)null!).Generate());
    }

    [Fact]
    public void A_missing_siglum_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Witness().WithSiglum((WitnessSiglum)null!).Generate());
    }

    [Theory]
    [InlineData(-100, -50, 0, 100, true)]       // wholly before
    [InlineData(400, 450, 451, 500, true)]      // the very next year
    [InlineData(400, 450, 450, 500, false)]     // they may share the year 450
    [InlineData(400, 450, 440, 500, false)]     // overlapping
    [InlineData(400, 450, 300, 399, false)]     // wholly after
    [InlineData(400, 450, 410, 420, false)]     // the other lies inside
    [InlineData(400, 450, 400, 450, false)]     // the same span: neither is certainly older
    [InlineData(-800, 1600, 1600, 1600, false)] // an uncertain dating is older than nothing
    public void A_witness_is_certainly_older_only_when_its_span_ends_before_the_other_begins(int from, int to, int otherFrom, int otherTo, bool older) {
        // Arrange
        Witness witness = Any.Witness().WithDatedFrom(from).WithDatedTo(to).Generate();
        Witness other   = Any.Witness().WithDatedFrom(otherFrom).WithDatedTo(otherTo).Generate();

        // Assert
        Assert.Equal(older, witness.IsCertainlyOlderThan(other));
    }

    [Fact]
    public void Being_certainly_older_is_not_symmetric() {
        // Arrange
        Witness early = Any.Witness().WithDatedFrom(100).WithDatedTo(200).Generate();
        Witness late  = Any.Witness().WithDatedFrom(300).WithDatedTo(400).Generate();

        // Assert
        Assert.True(early.IsCertainlyOlderThan(late));
        Assert.False(late.IsCertainlyOlderThan(early));
    }

    [Fact]
    public void A_witness_is_not_certainly_older_than_a_missing_one() {
        // Arrange
        Witness anyWitness = Any.Witness().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyWitness.IsCertainlyOlderThan(null!));
    }

}
