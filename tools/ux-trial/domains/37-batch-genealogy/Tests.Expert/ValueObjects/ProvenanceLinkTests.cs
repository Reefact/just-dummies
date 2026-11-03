using JustDummies;

using Xunit;

namespace BatchGenealogy.Domain.Tests.ValueObjects;

public sealed class ProvenanceLinkTests {

    [Fact]
    public void A_link_says_which_lot_a_target_came_from_through_which_transformation_and_for_what_share() {
        // Arrange
        LotNumber source = new LotNumber("API-240117");
        LotNumber target = new LotNumber("BLD-240118");

        // Act
        ProvenanceLink link = new ProvenanceLink(source, target, "TRF-000042", 0.25m);

        // Assert
        Assert.Same(source, link.Source);
        Assert.Same(target, link.Target);
        Assert.Equal("TRF-000042", link.TransformationId);
        Assert.Equal(0.25m, link.Share);
    }

    [Theory]
    [InlineData("0.0000000000000000000000000001")]
    [InlineData("0.5")]
    [InlineData("1")]
    public void A_share_in_the_half_open_interval_zero_to_one_is_accepted(string share) {
        // Act
        ProvenanceLink link = Any.ProvenanceLink().WithShare(decimal.Parse(share, System.Globalization.CultureInfo.InvariantCulture)).Generate();

        // Assert
        Assert.Equal(decimal.Parse(share, System.Globalization.CultureInfo.InvariantCulture), link.Share);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.1")]
    [InlineData("1.0000000000000000000000000001")]
    [InlineData("2")]
    public void A_share_of_zero_or_below_or_above_one_is_refused(string share) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.ProvenanceLink().WithShare(decimal.Parse(share, System.Globalization.CultureInfo.InvariantCulture)).Generate());
    }

    [Fact]
    public void A_lot_cannot_be_its_own_provenance() {
        // Arrange
        LotNumber anyLot = Any.LotNumber().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new ProvenanceLink(anyLot, new LotNumber(anyLot.Value), Any.StringMatching("TRF-[0-9]{6}").Generate(), 1m));
    }

    [Fact]
    public void A_sub_lot_may_come_from_its_own_base_lot() {
        // Arrange
        LotNumber baseLot = Any.LotNumber().Base().Generate();

        // Act
        ProvenanceLink link = Any.ProvenanceLink().WithSource(baseLot).WithTarget(baseLot.SubLot(1)).Generate();

        // Assert
        Assert.NotEqual(link.Source, link.Target);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void A_blank_transformation_id_is_refused(string id) {
        Assert.Throws<ArgumentException>(() => Any.ProvenanceLink().WithTransformationId(id).Generate());
    }

    [Fact]
    public void A_missing_transformation_id_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.ProvenanceLink().WithTransformationId((string)null!).Generate());
    }

    [Fact]
    public void A_missing_source_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new ProvenanceLink(null!, Any.LotNumber().Generate(), "TRF-000001", 1m));
    }

    [Fact]
    public void A_missing_target_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new ProvenanceLink(Any.LotNumber().Generate(), null!, "TRF-000001", 1m));
    }

    [Fact]
    public void A_link_prints_its_source_target_share_and_transformation() {
        // Arrange
        ProvenanceLink link = new ProvenanceLink(new LotNumber("API-240117"), new LotNumber("BLD-240118"), "TRF-000042", 0.25m);

        // Act
        string text = CultureInfoScope.Use("", () => link.ToString());

        // Assert
        Assert.Equal("API-240117 -> BLD-240118 (25.0 % via TRF-000042)", text);
    }

}
