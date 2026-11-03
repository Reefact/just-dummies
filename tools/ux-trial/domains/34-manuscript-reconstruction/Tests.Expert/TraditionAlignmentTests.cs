using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Expert;

public sealed class TraditionAlignmentTests {

    private static readonly WitnessSiglum A = new WitnessSiglum("A");
    private static readonly WitnessSiglum B = new WitnessSiglum("B");
    private static readonly WitnessSiglum C = new WitnessSiglum("C");
    private static readonly WitnessSiglum D = new WitnessSiglum("D");

    [Fact]
    public void A_passage_nobody_reads_has_an_empty_alignment() {
        // Arrange
        Tradition tradition = Scene.TraditionOf(A, B);

        // Assert
        Assert.Empty(tradition.Align(Any.PassageReference().Generate()));
    }

    [Fact]
    public void Witnesses_that_read_alike_form_one_column() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B);
        tradition.RecordVariant(Scene.Reads(B, passage, "arma"));
        tradition.RecordVariant(Scene.Reads(A, passage, "arma"));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(passage);

        // Assert
        AttestedReading column = Assert.Single(columns);
        Assert.Equal("arma", column.Text);
        Assert.Equal([A, B], column.Witnesses);
        Assert.Same(passage, column.Passage);
    }

    [Fact]
    public void The_largest_group_of_witnesses_comes_first() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B, C, D);
        tradition.RecordVariant(Scene.Reads(A, passage, "alpha"));
        tradition.RecordVariant(Scene.Reads(B, passage, "beta"));
        tradition.RecordVariant(Scene.Reads(C, passage, "beta"));
        tradition.RecordVariant(Scene.Reads(D, passage, "beta"));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(passage);

        // Assert
        Assert.Equal(["beta", "alpha"], columns.Select(c => c.Text));
        Assert.Equal([3, 1], columns.Select(c => c.Witnesses.Count));
    }

    [Fact]
    public void Groups_of_the_same_size_are_ordered_by_their_text() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B, C);
        tradition.RecordVariant(Scene.Reads(A, passage, "gamma"));
        tradition.RecordVariant(Scene.Reads(B, passage, "alpha"));
        tradition.RecordVariant(Scene.Reads(C, passage, "beta"));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(passage);

        // Assert
        Assert.Equal(["alpha", "beta", "gamma"], columns.Select(c => c.Text));
    }

    [Fact]
    public void The_text_of_equally_large_groups_is_compared_ordinally_so_capitals_come_first() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B);
        tradition.RecordVariant(Scene.Reads(A, passage, "apex"));
        tradition.RecordVariant(Scene.Reads(B, passage, "Zephyr"));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(passage);

        // Assert
        Assert.Equal(["Zephyr", "apex"], columns.Select(c => c.Text));
    }

    [Fact]
    public void Readings_that_differ_only_by_case_are_different_columns() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B);
        tradition.RecordVariant(Scene.Reads(A, passage, "Rex"));
        tradition.RecordVariant(Scene.Reads(B, passage, "rex"));

        // Assert
        Assert.Equal(2, tradition.Align(passage).Count);
    }

    [Fact]
    public void Readings_that_differ_only_by_whitespace_are_one_column() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B);
        tradition.RecordVariant(Scene.Reads(A, passage, "arma  virumque"));
        tradition.RecordVariant(Scene.Reads(B, passage, " arma virumque\t"));

        // Assert
        Assert.Single(tradition.Align(passage));
    }

    [Fact]
    public void Only_the_final_reading_of_a_witness_counts() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B);
        tradition.RecordVariant(Scene.Reads(A, passage, "arma"));
        tradition.RecordVariant(Scene.Corrects(A, passage, "armi", hand: 2));
        tradition.RecordVariant(Scene.Reads(B, passage, "armi"));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(passage);

        // Assert
        AttestedReading column = Assert.Single(columns);
        Assert.Equal("armi", column.Text);
        Assert.Equal([A, B], column.Witnesses);
    }

    [Fact]
    public void A_witness_appears_once_however_many_hands_read_the_passage() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A);
        tradition.RecordVariant(Scene.Reads(A, passage, "arma"));
        tradition.RecordVariant(Scene.Corrects(A, passage, "armi", hand: 2));
        tradition.RecordVariant(Scene.Corrects(A, passage, "armo", hand: 3));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(passage);

        // Assert
        AttestedReading column = Assert.Single(columns);
        Assert.Equal("armo", column.Text);
        Assert.Equal([A], column.Witnesses);
    }

    [Fact]
    public void Witnesses_that_omit_the_passage_form_an_omission_column() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B, C);
        tradition.RecordVariant(Scene.Reads(A, passage, "arma"));
        tradition.RecordVariant(Scene.Omits(B, passage));
        tradition.RecordVariant(Scene.Omits(C, passage));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(passage);

        // Assert
        Assert.Equal(2, columns.Count);
        Assert.True(columns[0].IsOmission);
        Assert.Equal([B, C], columns[0].Witnesses);
        Assert.False(columns[1].IsOmission);
    }

    [Fact]
    public void A_single_omission_column_comes_before_a_single_reading_of_equal_size() {
        // Arrange: the empty text is the smallest text.
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B);
        tradition.RecordVariant(Scene.Reads(A, passage, "arma"));
        tradition.RecordVariant(Scene.Omits(B, passage));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(passage);

        // Assert
        Assert.True(columns[0].IsOmission);
    }

    [Fact]
    public void A_corrector_who_restores_an_omission_moves_the_witness_to_the_restored_reading() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        Tradition        tradition = Scene.TraditionOf(A, B);
        tradition.RecordVariant(Scene.Omits(A, passage));
        tradition.RecordVariant(Scene.Corrects(A, passage, "arma", hand: 2));
        tradition.RecordVariant(Scene.Reads(B, passage, "arma"));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(passage);

        // Assert
        AttestedReading column = Assert.Single(columns);
        Assert.Equal([A, B], column.Witnesses);
    }

    [Fact]
    public void Only_the_readings_of_the_requested_passage_are_aligned() {
        // Arrange
        PassageReference passage   = Any.PassageReference().Generate();
        PassageReference elsewhere = Scene.AnyPassageOtherThan(passage);
        Tradition        tradition = Scene.TraditionOf(A, B);
        tradition.RecordVariant(Scene.Reads(A, passage, "arma"));
        tradition.RecordVariant(Scene.Reads(B, elsewhere, "armi"));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(passage);

        // Assert
        AttestedReading column = Assert.Single(columns);
        Assert.Equal([A], column.Witnesses);
    }

    [Fact]
    public void A_witness_with_a_lacuna_at_the_passage_is_in_no_column() {
        // Arrange
        PassageReference passage   = PassageReference.Parse("2.6.3");
        Tradition        tradition = Scene.TraditionOf(A, B);
        tradition.RecordLacuna(Scene.Gap(B, "2.5.1", "2.7.10"));
        tradition.RecordVariant(Scene.Reads(A, passage, "arma"));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(passage);

        // Assert
        Assert.Equal([A], Assert.Single(columns).Witnesses);
    }

    [Fact]
    public void A_missing_passage_cannot_be_aligned() {
        Assert.Throws<ArgumentNullException>(() => Any.Tradition().Generate().Align(null!));
    }

}
