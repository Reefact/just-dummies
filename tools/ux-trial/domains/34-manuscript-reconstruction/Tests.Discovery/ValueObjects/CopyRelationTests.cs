using JustDummies;

using ManuscriptReconstruction.Domain;

using Xunit;

using static ManuscriptReconstruction.Tests.Discovery.Fixture;

namespace ManuscriptReconstruction.Tests.Discovery.ValueObjects;

public sealed class CopyRelationTests {

    [Fact]
    public void A_relation_keeps_what_it_was_built_from() {
        // Act
        CopyRelation relation = new CopyRelation(Siglum("A"), Siglum("B"), 0.75, 4);

        // Assert
        Assert.Equal(Siglum("A"), relation.Exemplar);
        Assert.Equal(Siglum("B"), relation.Copy);
        Assert.Equal(0.75, relation.Confidence);
        Assert.Equal(4, relation.SharedErrorCount);
    }

    [Fact]
    public void A_witness_cannot_be_a_copy_of_itself() {
        // Arrange
        WitnessSiglum anySiglum = Any.WitnessSiglum().Generate();

        // Act
        Action construct = () => new CopyRelation(anySiglum, new WitnessSiglum(anySiglum.Value), 0.5, 0);

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(construct);
        Assert.Equal("copy", refusal.ParamName);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(double.Epsilon)]
    [InlineData(0.5)]
    public void A_confidence_in_the_half_open_unit_interval_is_accepted(double confidence) {
        // Act
        CopyRelation relation = Any.CopyRelation().WithConfidence(confidence).Generate();

        // Assert
        Assert.Equal(confidence, relation.Confidence);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    [InlineData(1.0000000000000002)]
    [InlineData(2.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_confidence_outside_the_half_open_unit_interval_is_refused(double confidence) {
        // Arrange
        WitnessSiglum anyExemplar = Any.WitnessSiglum().WithValue("A").Generate();
        WitnessSiglum anyCopy     = Any.WitnessSiglum().WithValue("B").Generate();

        // Act
        Action construct = () => new CopyRelation(anyExemplar, anyCopy, confidence, 0);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Fact]
    public void No_shared_error_is_a_legitimate_claim() {
        // Act
        CopyRelation relation = Any.CopyRelation().WithSharedErrorCount(0).Generate();

        // Assert
        Assert.Equal(0, relation.SharedErrorCount);
    }

    [Fact]
    public void A_negative_count_of_shared_errors_is_refused() {
        // Arrange
        WitnessSiglum anyExemplar = Any.WitnessSiglum().WithValue("A").Generate();
        WitnessSiglum anyCopy     = Any.WitnessSiglum().WithValue("B").Generate();

        // Act
        Action construct = () => new CopyRelation(anyExemplar, anyCopy, 0.5, -1);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Fact]
    public void Nothing_is_not_a_part_of_a_relation() {
        Assert.Throws<ArgumentNullException>(() => new CopyRelation(null!, Siglum("B"), 0.5, 0));
        Assert.Throws<ArgumentNullException>(() => new CopyRelation(Siglum("A"), null!, 0.5, 0));
    }

}
