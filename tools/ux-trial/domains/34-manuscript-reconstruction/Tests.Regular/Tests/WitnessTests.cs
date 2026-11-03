using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Regular;

public sealed class WitnessTests {

    private static Witness DatedWitness(int from, int to) {
        return Any.Witness().WithDatedFrom(from).WithDatedTo(to).Generate();
    }

    [Fact]
    public void A_witness_keeps_what_it_was_given() {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();

        // Act
        Witness witness = new Witness(anySiglum, "Biblioteca Apostolica Vaticana", 400, 500, 309);

        // Assert
        Assert.Same(anySiglum, witness.Siglum);
        Assert.Equal("Biblioteca Apostolica Vaticana", witness.Repository);
        Assert.Equal(400, witness.DatedFrom);
        Assert.Equal(500, witness.DatedTo);
        Assert.Equal(309, witness.FolioCount);
    }

    [Theory]
    [InlineData(-800, -800)]
    [InlineData(-800, 1600)]
    [InlineData(1600, 1600)]
    [InlineData(0, 0)]
    public void The_whole_span_of_years_is_accepted(int from, int to) {
        // Act
        Witness witness = DatedWitness(from, to);

        // Assert
        Assert.Equal((from, to), (witness.DatedFrom, witness.DatedTo));
    }

    [Theory]
    [InlineData(-801, 0, "datedFrom")]
    [InlineData(0, 1601, "datedTo")]
    [InlineData(-1000, 2000, "datedFrom")]
    public void A_dating_outside_the_span_is_refused_and_named(int from, int to, string parameter) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => DatedWitness(from, to));

        // Assert
        Assert.Equal(parameter, refusal.ParamName);
    }

    [Fact]
    public void A_dating_cannot_start_after_it_ends() {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Witness(anySiglum, "Bodleian", 101, 100, 10));

        // Assert
        Assert.Equal("datedFrom", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_witness_has_at_least_one_folio(int folios) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Witness().WithFolioCount(folios).Generate());

        // Assert
        Assert.Equal("folioCount", refusal.ParamName);
    }

    [Fact]
    public void A_single_folio_is_enough() {
        Assert.Equal(1, Any.Witness().WithFolioCount(1).Generate().FolioCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_witness_is_kept_somewhere(string blank) {
        Assert.Throws<ArgumentException>(() => Any.Witness().WithRepository(blank).Generate());
    }

    [Fact]
    public void A_witness_needs_a_siglum_and_a_repository() {
        // Assert
        Assert.Throws<ArgumentNullException>(() => new Witness(null!, "Bodleian", 0, 100, 10));
        Assert.Throws<ArgumentNullException>(() => new Witness(Any.WitnessSiglum().Generate(), null!, 0, 100, 10));
    }

    [Theory]
    [InlineData(-100, -50, -49, 10, true)]
    [InlineData(-100, -50, 100, 200, true)]
    [InlineData(-100, -50, -50, 0, false)]
    [InlineData(-100, -50, -60, 0, false)]
    [InlineData(-100, -50, -200, -150, false)]
    [InlineData(-100, -50, -100, -50, false)]
    [InlineData(1400, 1450, 1451, 1500, true)]
    public void A_witness_is_certainly_older_when_its_span_ends_before_the_other_begins(int from, int to, int otherFrom, int otherTo, bool certainlyOlder) {
        // Arrange
        Witness witness = DatedWitness(from, to);
        Witness other   = DatedWitness(otherFrom, otherTo);

        // Assert
        Assert.Equal(certainlyOlder, witness.IsCertainlyOlderThan(other));
    }

    [Fact]
    public void Two_witnesses_cannot_both_be_certainly_older_than_the_other() {
        // Arrange
        Witness anyWitness      = Any.Witness().Generate();
        Witness anyOtherWitness = Any.Witness().Generate();

        // Assert
        Assert.False(anyWitness.IsCertainlyOlderThan(anyOtherWitness) && anyOtherWitness.IsCertainlyOlderThan(anyWitness));
    }

    [Fact]
    public void Comparing_with_nothing_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Witness().Generate().IsCertainlyOlderThan(null!));
    }

}
