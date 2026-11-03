using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

using static ManuscriptReconstruction.Tests.Discovery.Fixture;

namespace ManuscriptReconstruction.Tests.Discovery.AggregateTests;

public sealed class TraditionAlignmentTests {

    [Fact]
    public void Nothing_aligns_where_no_witness_reads() {
        // Arrange
        Tradition tradition = TraditionOf("A", "B");

        // Assert
        Assert.Empty(tradition.Align(At("1.1.1")));
    }

    [Fact]
    public void Witnesses_reading_alike_form_one_column() {
        // Arrange
        Tradition tradition = WithReadings(TraditionOf("A", "B", "C"), "1.1.1", ("C", "verbum"), ("A", "verbum"), ("B", "verbum"));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(At("1.1.1"));

        // Assert
        AttestedReading column = Assert.Single(columns);
        Assert.Equal("verbum", column.Text);
        Assert.Equal(["A", "B", "C"], column.Witnesses.Select(w => w.Value));
        Assert.Equal(At("1.1.1"), column.Passage);
    }

    [Fact]
    public void The_largest_group_comes_first() {
        // Arrange
        Tradition tradition = WithReadings(TraditionOf("A", "B", "C", "D"), "1.1.1",
                                           ("A", "alpha"), ("B", "beta"), ("C", "beta"), ("D", "beta"));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(At("1.1.1"));

        // Assert
        Assert.Equal(["beta", "alpha"], columns.Select(c => c.Text));
        Assert.Equal([3, 1], columns.Select(c => c.Witnesses.Count));
    }

    [Fact]
    public void Groups_of_the_same_size_are_ordered_by_text() {
        // Arrange
        Tradition tradition = WithReadings(TraditionOf("A", "B", "C"), "1.1.1", ("A", "gamma"), ("B", "alpha"), ("C", "beta"));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(At("1.1.1"));

        // Assert
        Assert.Equal(["alpha", "beta", "gamma"], columns.Select(c => c.Text));
    }

    [Fact]
    public void Text_ordering_is_ordinal_so_capitals_come_first() {
        // Arrange
        Tradition tradition = WithReadings(TraditionOf("A", "B"), "1.1.1", ("A", "alpha"), ("B", "Zeta"));

        // Assert
        Assert.Equal(["Zeta", "alpha"], tradition.Align(At("1.1.1")).Select(c => c.Text));
    }

    [Fact]
    public void An_omission_is_a_reading_of_its_own() {
        // Arrange
        Tradition tradition = WithReadings(TraditionOf("A", "B", "C"), "1.1.1", ("A", "verbum"), ("B", "verbum"), ("C", ""));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(At("1.1.1"));

        // Assert
        Assert.Equal(["verbum", ""], columns.Select(c => c.Text));
        Assert.True(columns[1].IsOmission);
        Assert.Equal(["C"], columns[1].Witnesses.Select(w => w.Value));
    }

    [Fact]
    public void An_omission_comes_before_a_reading_of_the_same_weight() {
        // Arrange
        Tradition tradition = WithReadings(TraditionOf("A", "B"), "1.1.1", ("A", "verbum"), ("B", ""));

        // Assert
        Assert.Equal(["", "verbum"], tradition.Align(At("1.1.1")).Select(c => c.Text));
    }

    [Fact]
    public void A_witness_counts_for_its_final_reading_only() {
        // Arrange
        Tradition tradition = TraditionOf("A", "B");
        tradition.RecordVariant(Reads("A", "1.1.1", "first"));
        tradition.RecordVariant(Reads("A", "1.1.1", "corrected", hand: 2));
        tradition.RecordVariant(Reads("B", "1.1.1", "corrected"));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(At("1.1.1"));

        // Assert
        AttestedReading column = Assert.Single(columns);
        Assert.Equal("corrected", column.Text);
        Assert.Equal(["A", "B"], column.Witnesses.Select(w => w.Value));
    }

    [Fact]
    public void A_witness_lacunose_at_the_passage_is_absent_from_the_alignment() {
        // Arrange
        Tradition tradition = TraditionOf("A", "B");
        tradition.RecordLacuna(new Lacuna(Siglum("B"), At("1.1.1"), At("1.1.9"), "lost leaf"));
        tradition.RecordVariant(Reads("A", "1.1.1", "verbum"));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(At("1.1.1"));

        // Assert
        Assert.Equal(["A"], Assert.Single(columns).Witnesses.Select(w => w.Value));
    }

    [Fact]
    public void Only_the_passage_asked_for_is_aligned() {
        // Arrange
        Tradition tradition = TraditionOf("A");
        tradition.RecordVariant(Reads("A", "1.1.1", "verbum"));
        tradition.RecordVariant(Reads("A", "1.1.2", "aliud"));

        // Assert
        Assert.Equal(["aliud"], tradition.Align(At("1.1.2")).Select(c => c.Text));
    }

    [Fact]
    public void Aligning_nothing_is_refused() {
        Assert.Throws<ArgumentNullException>(() => TraditionOf("A").Align(null!));
    }

}
