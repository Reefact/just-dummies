namespace BatchGenealogy.Domain.Tests;

[Reproducible]
public sealed class ProvenanceLinkTests {

    [Fact]
    public void A_link_keeps_what_it_was_given() {
        // Arrange
        LotNumber source = Any.LotNumber().WhollyLot().Generate();
        LotNumber target = Any.LotNumber().SubLot().Generate();
        string    anyId  = Any.StringMatching("TRF-[0-9]{6}").Generate();

        // Act
        ProvenanceLink link = new ProvenanceLink(source, target, anyId, 0.4m);

        // Assert
        Assert.Same(source, link.Source);
        Assert.Same(target, link.Target);
        Assert.Equal(anyId, link.TransformationId);
        Assert.Equal(0.4m, link.Share);
    }

    [Fact]
    public void A_lot_cannot_be_its_own_provenance() {
        // Arrange
        string    anyValue = Any.LotNumber().Generate().Value;
        string    anyId    = Any.StringMatching("TRF-[0-9]{6}").Generate();

        // Act / Assert: two instances, one value
        Assert.Throws<ArgumentException>(() => new ProvenanceLink(new LotNumber(anyValue), new LotNumber(anyValue), anyId, 0.5m));
    }

    [Fact]
    public void A_whole_share_is_allowed() {
        // Arrange
        ProvenanceLink anyLink = Any.ProvenanceLink().Generate();

        // Act
        ProvenanceLink whole = new ProvenanceLink(anyLink.Source, anyLink.Target, anyLink.TransformationId, 1m);

        // Assert
        Assert.Equal(1m, whole.Share);
    }

    [Fact]
    public void A_null_share_is_refused() {
        // Arrange
        ProvenanceLink anyLink = Any.ProvenanceLink().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProvenanceLink(anyLink.Source, anyLink.Target, anyLink.TransformationId, 0m));
    }

    [Fact]
    public void A_share_beyond_the_whole_is_refused() {
        // Arrange
        ProvenanceLink anyLink     = Any.ProvenanceLink().Generate();
        decimal        anyAboveOne = Any.Decimal().GreaterThan(1m).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProvenanceLink(anyLink.Source, anyLink.Target, anyLink.TransformationId, anyAboveOne));
    }

    [Fact]
    public void A_negative_share_is_refused() {
        // Arrange
        ProvenanceLink anyLink     = Any.ProvenanceLink().Generate();
        decimal        anyNegative = Any.Decimal().Negative().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProvenanceLink(anyLink.Source, anyLink.Target, anyLink.TransformationId, anyNegative));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_transformation_id_is_refused(string blank) {
        // Arrange
        ProvenanceLink anyLink = Any.ProvenanceLink().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new ProvenanceLink(anyLink.Source, anyLink.Target, blank, 0.5m));
    }

    [Fact]
    public void Missing_ends_are_refused() {
        // Arrange
        LotNumber anyLot = Any.LotNumber().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new ProvenanceLink(null!, anyLot, "TRF-000001", 0.5m));
        Assert.Throws<ArgumentNullException>(() => new ProvenanceLink(anyLot, null!, "TRF-000001", 0.5m));
    }

    [Fact]
    public void Printing_shows_source_target_share_and_step() {
        // Act
        ProvenanceLink link = new ProvenanceLink(new LotNumber("API-000001"), new LotNumber("BLD-000002"), "TRF-000003", 0.25m);

        // Assert
        Assert.Contains("API-000001 -> BLD-000002", link.ToString());
        Assert.Contains("TRF-000003", link.ToString());
    }

    [Fact]
    public void A_drawn_link_never_loops_back_on_itself() {
        // Arrange
        ProvenanceLink anyLink = Any.ProvenanceLink().Generate();

        // Assert
        Assert.NotEqual(anyLink.Source, anyLink.Target);
    }

}
