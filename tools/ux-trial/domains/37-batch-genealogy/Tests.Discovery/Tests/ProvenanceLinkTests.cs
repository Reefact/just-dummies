using BatchGenealogy.Domain;

using JustDummies;

using Xunit;

namespace BatchGenealogy.Tests.Discovery;

public sealed class ProvenanceLinkTests {

    [Fact]
    public void A_link_records_where_a_lot_came_from() {
        // Arrange
        LotNumber anySource = Any.LotNumber().Generate();
        LotNumber anyTarget = Any.LotNumber().Generate();
        string    anyId     = Any.TransformationId().Generate();

        // Act
        ProvenanceLink link = new ProvenanceLink(anySource, anyTarget, anyId, 0.4m);

        // Assert
        Assert.Same(anySource, link.Source);
        Assert.Same(anyTarget, link.Target);
        Assert.Equal(anyId, link.TransformationId);
        Assert.Equal(0.4m, link.Share);
    }

    [Fact]
    public void A_whole_share_is_accepted() {
        // Arrange
        ProvenanceLink anyLink = Any.ProvenanceLink().WithShare(1m).Generate();

        // Assert
        Assert.Equal(1m, anyLink.Share);
    }

    [Fact]
    public void A_lot_cannot_be_its_own_provenance() {
        // Arrange
        LotNumber anyLot = Any.LotNumber().Generate();
        string    anyId  = Any.TransformationId().Generate();

        // Act
        Exception? thrown = Record.Exception(() => new ProvenanceLink(anyLot, anyLot, anyId, 0.5m));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void Two_instances_of_the_same_lot_number_are_the_same_lot() {
        // Arrange
        string anyValue = Any.LotNumber().Generate().Value;
        string anyId    = Any.TransformationId().Generate();

        // Act
        Exception? thrown = Record.Exception(() => new ProvenanceLink(new LotNumber(anyValue), new LotNumber(anyValue), anyId, 0.5m));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(1.0000001)]
    [InlineData(2)]
    public void A_share_outside_zero_exclusive_to_one_inclusive_is_refused(double share) {
        // Arrange
        LotNumber anySource = Any.LotNumber().Generate();
        LotNumber anyTarget = Any.LotNumber().Generate();
        string    anyId     = Any.TransformationId().Generate();

        // Act
        Exception? thrown = Record.Exception(() => new ProvenanceLink(anySource, anyTarget, anyId, (decimal)share));

        // Assert
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(thrown);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_transformation_id_is_refused(string id) {
        // Arrange
        LotNumber anySource = Any.LotNumber().Generate();
        LotNumber anyTarget = Any.LotNumber().Generate();

        // Act
        Exception? thrown = Record.Exception(() => new ProvenanceLink(anySource, anyTarget, id, 0.5m));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void A_missing_transformation_id_source_or_target_is_refused() {
        // Arrange
        LotNumber anySource = Any.LotNumber().Generate();
        LotNumber anyTarget = Any.LotNumber().Generate();
        string    anyId     = Any.TransformationId().Generate();

        // Act
        Exception? noId     = Record.Exception(() => new ProvenanceLink(anySource, anyTarget, null!, 0.5m));
        Exception? noSource = Record.Exception(() => new ProvenanceLink(null!, anyTarget, anyId, 0.5m));
        Exception? noTarget = Record.Exception(() => new ProvenanceLink(anySource, null!, anyId, 0.5m));

        // Assert
        Assert.IsType<ArgumentNullException>(noId);
        Assert.IsType<ArgumentNullException>(noSource);
        Assert.IsType<ArgumentNullException>(noTarget);
    }

    [Fact]
    public void A_link_reads_as_the_share_one_lot_gave_another() {
        // Arrange
        LotNumber source = new LotNumber("API-100001");
        LotNumber target = new LotNumber("API-100002");

        // Act
        string text = new ProvenanceLink(source, target, "TRF-000007", 0.25m).ToString();

        // Assert
        Assert.Contains("API-100001 -> API-100002", text);
        Assert.Contains("TRF-000007", text);
    }

}
