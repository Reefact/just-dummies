using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

namespace ManuscriptReconstruction.Tests.Expert;

public sealed class CopyRelationTests {

    [Fact]
    public void A_relation_keeps_the_witnesses_the_confidence_and_the_count_it_was_given() {
        // Arrange
        WitnessSiglum exemplar = new WitnessSiglum("A");
        WitnessSiglum copy     = new WitnessSiglum("B");

        // Act
        CopyRelation relation = new CopyRelation(exemplar, copy, 0.75, 3);

        // Assert
        Assert.Same(exemplar, relation.Exemplar);
        Assert.Same(copy, relation.Copy);
        Assert.Equal(0.75, relation.Confidence);
        Assert.Equal(3, relation.SharedErrorCount);
    }

    [Fact]
    public void A_witness_cannot_be_a_copy_of_itself() {
        // Arrange: two instances of one siglum, so that it is the value that is compared, not the reference.
        string        anySiglumText = Any.WitnessSiglum().Generate().Value;
        WitnessSiglum exemplar      = new WitnessSiglum(anySiglumText);
        WitnessSiglum copy          = new WitnessSiglum(anySiglumText);
        double        anyConfidence = Any.Double().Positive().LessThanOrEqualTo(1d).Generate();
        int           anyCount      = Any.Int32().GreaterThanOrEqualTo(0).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new CopyRelation(exemplar, copy, anyConfidence, anyCount));

        // Assert
        Assert.Equal("copy", refusal.ParamName);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(0.5)]
    [InlineData(double.Epsilon)]      // the smallest confidence above zero
    public void A_confidence_in_zero_exclusive_to_one_inclusive_is_accepted(double confidence) {
        // Act
        CopyRelation relation = Any.CopyRelation().WithConfidence(confidence).Generate();

        // Assert
        Assert.Equal(confidence, relation.Confidence);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.5)]
    [InlineData(1.0000000000000002)]   // the double just above one
    [InlineData(2.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.MaxValue)]
    public void A_confidence_outside_zero_exclusive_to_one_inclusive_is_refused(double confidence) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.CopyRelation().WithConfidence(confidence).Generate());

        // Assert
        Assert.Equal("confidence", refusal.ParamName);
    }

    [Fact]
    public void Negative_zero_is_no_confidence_either() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.CopyRelation().WithConfidence(-0.0).Generate());
    }

    [Fact]
    public void Citing_no_shared_error_is_accepted() {
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
    public void A_missing_exemplar_and_a_missing_copy_are_refused() {
        // Arrange
        WitnessSiglum anyWitness = Any.WitnessSiglum().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new CopyRelation(null!, anyWitness, 0.5, 0));
        Assert.Throws<ArgumentNullException>(() => new CopyRelation(anyWitness, null!, 0.5, 0));
    }

}
