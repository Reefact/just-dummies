using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Regular;

public sealed class CopyRelationTests {

    [Fact]
    public void A_relation_keeps_its_two_witnesses_its_confidence_and_its_evidence() {
        // Arrange
        IReadOnlyList<WitnessSiglum> anySigla = Fixtures.DistinctSigla(2);

        // Act
        CopyRelation relation = new CopyRelation(anySigla[0], anySigla[1], 0.75, 4);

        // Assert
        Assert.Same(anySigla[0], relation.Exemplar);
        Assert.Same(anySigla[1], relation.Copy);
        Assert.Equal(0.75, relation.Confidence);
        Assert.Equal(4, relation.SharedErrorCount);
    }

    [Fact]
    public void A_witness_cannot_be_a_copy_of_itself() {
        // Arrange
        string anyValue = Any.WitnessSiglum().Generate().Value;

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new CopyRelation(new WitnessSiglum(anyValue), new WitnessSiglum(anyValue), 0.5, 0));

        // Assert
        Assert.Equal("copy", refusal.ParamName);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(0.5)]
    [InlineData(double.Epsilon)]
    public void A_confidence_in_the_half_open_unit_interval_is_accepted(double confidence) {
        // Act
        CopyRelation relation = Any.CopyRelation().WithConfidence(confidence).Generate();

        // Assert
        Assert.Equal(confidence, relation.Confidence);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.5)]
    [InlineData(1.0000001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_confidence_outside_zero_exclusive_to_one_inclusive_is_refused(double confidence) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.CopyRelation().WithConfidence(confidence).Generate());

        // Assert
        Assert.Equal("confidence", refusal.ParamName);
    }

    [Fact]
    public void No_shared_error_is_a_legitimate_claim() {
        // Act
        CopyRelation relation = Any.CopyRelation().WithSharedErrorCount(0).Generate();

        // Assert
        Assert.Equal(0, relation.SharedErrorCount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_negative_count_of_shared_errors_is_refused(int count) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.CopyRelation().WithSharedErrorCount(count).Generate());

        // Assert
        Assert.Equal("sharedErrorCount", refusal.ParamName);
    }

    [Fact]
    public void A_relation_needs_both_witnesses() {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new CopyRelation(null!, anySiglum, 0.5, 0));
        Assert.Throws<ArgumentNullException>(() => new CopyRelation(anySiglum, null!, 0.5, 0));
    }

}
