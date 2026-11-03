using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Regular;

public sealed class TraditionReadingsTests {

    // ---- FinalReadingOf -------------------------------------------------------------------------

    [Fact]
    public void Nothing_recorded_means_no_final_reading() {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();
        Tradition     tradition = Fixtures.TraditionWith(anySiglum);

        // Assert
        Assert.Null(tradition.FinalReadingOf(anySiglum, Any.PassageReference().Generate()));
    }

    [Fact]
    public void The_only_reading_is_the_final_one() {
        // Arrange
        WitnessSiglum    anySiglum = Any.WitnessSiglum().Generate();
        PassageReference anyPlace  = Any.PassageReference().Generate();
        Tradition        tradition = Fixtures.TraditionWith(anySiglum);
        Variant          reading   = Fixtures.Reading(anySiglum, anyPlace, "arma");
        tradition.RecordVariant(reading);

        // Assert
        Assert.Same(reading, tradition.FinalReadingOf(anySiglum, anyPlace));
    }

    [Fact]
    public void The_latest_hand_has_the_final_word_whatever_the_order_of_recording() {
        // Arrange
        WitnessSiglum    anySiglum = Any.WitnessSiglum().Generate();
        PassageReference anyPlace  = Any.PassageReference().Generate();
        Tradition        tradition = Fixtures.TraditionWith(anySiglum);
        tradition.RecordVariant(Fixtures.Reading(anySiglum, anyPlace, "first hand"));
        tradition.RecordVariant(Fixtures.Reading(anySiglum, anyPlace, "third hand", hand: 3));
        tradition.RecordVariant(Fixtures.Reading(anySiglum, anyPlace, "second hand", hand: 2));

        // Act
        Variant? final = tradition.FinalReadingOf(anySiglum, anyPlace);

        // Assert
        Assert.Equal("third hand", final?.Text);
    }

    [Fact]
    public void The_final_reading_is_that_of_the_asked_witness_and_the_asked_passage() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla  = Fixtures.DistinctSigla(2);
        PassageReference             anyPlace  = Any.PassageReference().Generate();
        Tradition                    tradition = Fixtures.TraditionWith(anySigla[0], anySigla[1]);
        tradition.RecordVariant(Fixtures.Reading(anySigla[0], anyPlace, "ours"));
        tradition.RecordVariant(Fixtures.Reading(anySigla[1], anyPlace, "theirs"));
        tradition.RecordVariant(Fixtures.Reading(anySigla[0], Fixtures.AnotherPassage(anyPlace), "elsewhere"));

        // Assert
        Assert.Equal("ours", tradition.FinalReadingOf(anySigla[0], anyPlace)?.Text);
    }

    [Fact]
    public void Asking_for_a_final_reading_needs_a_witness_and_a_passage() {
        // Arrange
        Tradition tradition = Any.Tradition().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => tradition.FinalReadingOf(null!, Any.PassageReference().Generate()));
        Assert.Throws<ArgumentNullException>(() => tradition.FinalReadingOf(Any.WitnessSiglum().Generate(), null!));
    }

    // ---- Align ----------------------------------------------------------------------------------

    private static Tradition TraditionReading(PassageReference passage, params (WitnessSiglum Witness, string Text)[] readings) {
        Tradition tradition = Fixtures.TraditionWith(readings.Select(r => r.Witness).ToArray());
        foreach ((WitnessSiglum witness, string text) in readings) {
            tradition.RecordVariant(text.Length == 0 ? Fixtures.Omission(witness, passage) : Fixtures.Reading(witness, passage, text));
        }

        return tradition;
    }

    [Fact]
    public void Nothing_to_align_where_nobody_reads() {
        // Arrange
        Tradition tradition = Fixtures.TraditionWith(Fixtures.DistinctSigla(2).ToArray());

        // Assert
        Assert.Empty(tradition.Align(Any.PassageReference().Generate()));
    }

    [Fact]
    public void Witnesses_agreeing_form_one_column_with_their_provenance() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla = Fixtures.DistinctSigla(3);
        PassageReference             anyPlace = Any.PassageReference().Generate();
        Tradition                    tradition = TraditionReading(anyPlace, (anySigla[0], "arma"), (anySigla[1], "arma"), (anySigla[2], "arma"));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(anyPlace);

        // Assert
        AttestedReading column = Assert.Single(columns);
        Assert.Equal("arma", column.Text);
        Assert.Same(anyPlace, column.Passage);
        Assert.Equal(anySigla.Select(s => s.Value).Order(StringComparer.Ordinal), column.Witnesses.Select(w => w.Value));
    }

    [Fact]
    public void The_largest_group_comes_first() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla = Fixtures.DistinctSigla(4);
        PassageReference             anyPlace = Any.PassageReference().Generate();
        Tradition                    tradition = TraditionReading(anyPlace, (anySigla[0], "zeta"), (anySigla[1], "alpha"), (anySigla[2], "zeta"), (anySigla[3], "zeta"));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(anyPlace);

        // Assert
        Assert.Equal(["zeta", "alpha"], columns.Select(c => c.Text));
        Assert.Equal([3, 1], columns.Select(c => c.Witnesses.Count));
    }

    [Fact]
    public void Groups_of_the_same_size_are_ordered_by_text() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla = Fixtures.DistinctSigla(3);
        PassageReference             anyPlace = Any.PassageReference().Generate();
        Tradition                    tradition = TraditionReading(anyPlace, (anySigla[0], "beta"), (anySigla[1], "alpha"), (anySigla[2], "Gamma"));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(anyPlace);

        // Assert
        Assert.Equal(["Gamma", "alpha", "beta"], columns.Select(c => c.Text));
    }

    [Fact]
    public void Omissions_are_aligned_as_an_empty_reading() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla = Fixtures.DistinctSigla(3);
        PassageReference             anyPlace = Any.PassageReference().Generate();
        Tradition                    tradition = TraditionReading(anyPlace, (anySigla[0], "arma"), (anySigla[1], ""), (anySigla[2], ""));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(anyPlace);

        // Assert
        Assert.True(columns[0].IsOmission);
        Assert.Equal(2, columns[0].Witnesses.Count);
        Assert.Equal("arma", columns[1].Text);
    }

    [Fact]
    public void A_witness_stands_for_its_final_reading_only() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla = Fixtures.DistinctSigla(2);
        PassageReference             anyPlace = Any.PassageReference().Generate();
        Tradition                    tradition = TraditionReading(anyPlace, (anySigla[0], "arma"), (anySigla[1], "arma"));
        tradition.RecordVariant(Fixtures.Reading(anySigla[1], anyPlace, "arva", hand: 2));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(anyPlace);

        // Assert
        Assert.Equal(["arma", "arva"], columns.Select(c => c.Text));
        Assert.Equal(anySigla[1].Value, Assert.Single(columns[1].Witnesses).Value);
        Assert.Equal(anySigla[0].Value, Assert.Single(columns[0].Witnesses).Value);
    }

    [Fact]
    public void A_witness_appears_once_in_the_alignment() {
        // Arrange
        WitnessSiglum    anySiglum = Any.WitnessSiglum().Generate();
        PassageReference anyPlace  = Any.PassageReference().Generate();
        Tradition        tradition = TraditionReading(anyPlace, (anySiglum, "arma"));
        tradition.RecordVariant(Fixtures.Reading(anySiglum, anyPlace, "arva", hand: 2));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(anyPlace);

        // Assert
        Assert.Equal(1, columns.Sum(c => c.Witnesses.Count));
    }

    [Fact]
    public void Readings_of_other_passages_are_left_out() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla = Fixtures.DistinctSigla(2);
        PassageReference             anyPlace = Any.PassageReference().Generate();
        Tradition                    tradition = Fixtures.TraditionWith(anySigla[0], anySigla[1]);
        tradition.RecordVariant(Fixtures.Reading(anySigla[0], anyPlace, "arma"));
        tradition.RecordVariant(Fixtures.Reading(anySigla[1], Fixtures.AnotherPassage(anyPlace), "arva"));

        // Act
        IReadOnlyList<AttestedReading> columns = tradition.Align(anyPlace);

        // Assert
        Assert.Equal("arma", Assert.Single(columns).Text);
    }

    [Fact]
    public void Aligning_nothing_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.Tradition().Generate().Align(null!));
    }

}
