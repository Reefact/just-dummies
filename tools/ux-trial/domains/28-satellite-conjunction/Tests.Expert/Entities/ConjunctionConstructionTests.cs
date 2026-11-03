using JustDummies;

using Xunit;

namespace SatelliteConjunction.Domain.Tests.Expert.Entities;

public sealed class ConjunctionConstructionTests {

    [Fact]
    public void A_new_conjunction_exposes_what_it_was_built_from_and_has_only_been_screened() {
        // Arrange
        Guid       id        = Any.Guid().NonEmpty().Generate();
        Epoch      tca       = Any.Epoch().Generate();
        Trajectory primary   = Any.Trajectory().Covering(tca).WithSpaceObject(o => o.Active()).Generate();
        Trajectory secondary = Any.Trajectory().Covering(tca).WithSpaceObject(o => o.WithCatalogNumber(n => n.DifferentFrom(primary.SpaceObject.CatalogNumber))).Generate();

        // Act
        Conjunction conjunction = new(id, primary, secondary, tca);

        // Assert
        Assert.Equal(id, conjunction.Id);
        Assert.Same(primary, conjunction.Primary);
        Assert.Same(secondary, conjunction.Secondary);
        Assert.Same(tca, conjunction.TimeOfClosestApproach);
        Assert.Equal(ConjunctionStatus.Screened, conjunction.Status);
    }

    [Fact]
    public void A_new_conjunction_has_no_assessment_no_history_and_no_closure_reason() {
        // Arrange
        Conjunction anyConjunction = Any.Conjunction().Generate();

        // Assert
        Assert.Null(anyConjunction.LatestAssessment);
        Assert.Empty(anyConjunction.History);
        Assert.Null(anyConjunction.ClosureReason);
    }

    [Fact]
    public void An_empty_identifier_is_refused() {
        // Arrange
        (Trajectory primary, Trajectory secondary, Epoch tca) = Pair();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Conjunction(Guid.Empty, primary, secondary, tca));

        // Assert
        Assert.Equal("id", refusal.ParamName);
    }

    [Fact]
    public void A_conjunction_without_a_primary_is_refused() {
        // Arrange
        (_, Trajectory secondary, Epoch tca) = Pair();
        Guid anyId = Any.Guid().NonEmpty().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Conjunction(anyId, null!, secondary, tca));
    }

    [Fact]
    public void A_conjunction_without_a_secondary_is_refused() {
        // Arrange
        (Trajectory primary, _, Epoch tca) = Pair();
        Guid anyId = Any.Guid().NonEmpty().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Conjunction(anyId, primary, null!, tca));
    }

    [Fact]
    public void A_conjunction_without_a_time_of_closest_approach_is_refused() {
        // Arrange
        (Trajectory primary, Trajectory secondary, _) = Pair();
        Guid anyId = Any.Guid().NonEmpty().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Conjunction(anyId, primary, secondary, null!));
    }

    [Theory]
    [InlineData(ObjectKind.Payload)]
    [InlineData(ObjectKind.RocketBody)]
    [InlineData(ObjectKind.Debris)]
    public void A_primary_that_is_not_active_is_refused_whatever_its_kind(ObjectKind kind) {
        // Arrange
        Epoch      tca       = Any.Epoch().Generate();
        Trajectory inactive  = Any.Trajectory().Covering(tca).WithSpaceObject(o => o.WithKind(kind).Inactive()).Generate();
        Trajectory secondary = Any.Trajectory().Covering(tca).WithSpaceObject(o => o.WithCatalogNumber(n => n.DifferentFrom(inactive.SpaceObject.CatalogNumber))).Generate();
        Guid       anyId     = Any.Guid().NonEmpty().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Conjunction(anyId, inactive, secondary, tca));

        // Assert
        Assert.Equal("primary", refusal.ParamName);
    }

    [Theory]
    [InlineData(ObjectKind.Payload)]
    [InlineData(ObjectKind.RocketBody)]
    [InlineData(ObjectKind.Debris)]
    public void A_secondary_may_be_of_any_kind_and_need_not_be_active(ObjectKind kind) {
        // Arrange
        Epoch      tca     = Any.Epoch().Generate();
        Trajectory primary = Any.Trajectory().Covering(tca).WithSpaceObject(o => o.Active()).Generate();
        Trajectory other   = Any.Trajectory().Covering(tca).WithSpaceObject(o => o.WithCatalogNumber(n => n.DifferentFrom(primary.SpaceObject.CatalogNumber)).WithKind(kind)).Generate();
        Guid       anyId   = Any.Guid().NonEmpty().Generate();

        // Act
        Conjunction conjunction = new(anyId, primary, other, tca);

        // Assert
        Assert.Equal(kind, conjunction.Secondary.SpaceObject.Kind);
    }

    [Fact]
    public void An_object_cannot_be_in_conjunction_with_itself() {
        // Arrange
        Epoch      tca     = Any.Epoch().Generate();
        Trajectory primary = Any.Trajectory().Covering(tca).WithSpaceObject(o => o.Active()).Generate();
        Guid       anyId   = Any.Guid().NonEmpty().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Conjunction(anyId, primary, primary, tca));

        // Assert
        Assert.Equal("secondary", refusal.ParamName);
    }

    [Fact]
    public void Two_trajectories_of_the_same_catalogue_number_are_the_same_object_even_when_their_names_differ() {
        // Arrange
        Epoch       tca       = Any.Epoch().Generate();
        SpaceObject active    = Any.SpaceObject().Active().Generate();
        SpaceObject renamed   = Any.SpaceObject().WithCatalogNumber(active.CatalogNumber).Generate();
        Trajectory  primary   = Any.Trajectory().Covering(tca).WithSpaceObject(active).Generate();
        Trajectory  secondary = Any.Trajectory().Covering(tca).WithSpaceObject(renamed).Generate();
        Guid        anyId     = Any.Guid().NonEmpty().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Conjunction(anyId, primary, secondary, tca));
    }

    [Fact]
    public void The_time_of_closest_approach_may_be_the_first_or_the_last_instant_both_trajectories_cover() {
        // Arrange
        Epoch      start     = Any.Epoch().Generate();
        Trajectory primary   = From(start, 8, Any.SpaceObject().Active().Generate());
        Trajectory secondary = From(start, 8, Any.SpaceObject().WithCatalogNumber(n => n.DifferentFrom(primary.SpaceObject.CatalogNumber)).Generate());
        Guid       anyId     = Any.Guid().NonEmpty().Generate();

        // Act
        Conjunction atTheStart = new(anyId, primary, secondary, start);
        Conjunction atTheEnd   = new(anyId, primary, secondary, primary.End);

        // Assert
        Assert.Equal(ConjunctionStatus.Screened, atTheStart.Status);
        Assert.Equal(ConjunctionStatus.Screened, atTheEnd.Status);
    }

    [Fact]
    public void A_time_of_closest_approach_before_the_primary_starts_is_refused() {
        // Arrange
        Epoch      start     = Any.Epoch().Generate();
        Trajectory primary   = From(start.Add(TimeSpan.FromMinutes(10)), 8, Any.SpaceObject().Active().Generate());
        Trajectory secondary = From(start, 30, Any.SpaceObject().WithCatalogNumber(n => n.DifferentFrom(primary.SpaceObject.CatalogNumber)).Generate());
        Guid       anyId     = Any.Guid().NonEmpty().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Conjunction(anyId, primary, secondary, start.Add(TimeSpan.FromMinutes(5))));

        // Assert
        Assert.Equal("timeOfClosestApproach", refusal.ParamName);
    }

    [Fact]
    public void A_time_of_closest_approach_after_the_primary_ends_is_refused() {
        // Arrange
        Epoch      start     = Any.Epoch().Generate();
        Trajectory primary   = From(start, 8, Any.SpaceObject().Active().Generate());
        Trajectory secondary = From(start, 30, Any.SpaceObject().WithCatalogNumber(n => n.DifferentFrom(primary.SpaceObject.CatalogNumber)).Generate());
        Guid       anyId     = Any.Guid().NonEmpty().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Conjunction(anyId, primary, secondary, primary.End.Add(TimeSpan.FromTicks(1))));
    }

    [Fact]
    public void A_time_of_closest_approach_before_the_secondary_starts_is_refused() {
        // Arrange
        Epoch      start     = Any.Epoch().Generate();
        Trajectory primary   = From(start, 30, Any.SpaceObject().Active().Generate());
        Trajectory secondary = From(start.Add(TimeSpan.FromMinutes(10)), 8, Any.SpaceObject().WithCatalogNumber(n => n.DifferentFrom(primary.SpaceObject.CatalogNumber)).Generate());
        Guid       anyId     = Any.Guid().NonEmpty().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Conjunction(anyId, primary, secondary, start.Add(TimeSpan.FromMinutes(5))));
    }

    [Fact]
    public void A_time_of_closest_approach_after_the_secondary_ends_is_refused() {
        // Arrange
        Epoch      start     = Any.Epoch().Generate();
        Trajectory primary   = From(start, 30, Any.SpaceObject().Active().Generate());
        Trajectory secondary = From(start, 8, Any.SpaceObject().WithCatalogNumber(n => n.DifferentFrom(primary.SpaceObject.CatalogNumber)).Generate());
        Guid       anyId     = Any.Guid().NonEmpty().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Conjunction(anyId, primary, secondary, start.Add(TimeSpan.FromMinutes(20))));
    }

    private static (Trajectory Primary, Trajectory Secondary, Epoch TimeOfClosestApproach) Pair() {
        Conjunction anyConjunction = Any.Conjunction().Generate();

        return (anyConjunction.Primary, anyConjunction.Secondary, anyConjunction.TimeOfClosestApproach);
    }

    // A trajectory of two states, <length> minutes apart (never more than ten).
    private static Trajectory From(Epoch start, int lengthMinutes, SpaceObject spaceObject) {
        OrbitalState first = Any.OrbitalState().WithEpoch(start).Generate();

        List<OrbitalState> states = [first];
        for (int elapsed = 0; elapsed < lengthMinutes; elapsed += 5) {
            int step = Math.Min(5, lengthMinutes - elapsed);
            states.Add(Any.OrbitalState().WithEpoch(start.Add(TimeSpan.FromMinutes(elapsed + step))).Generate());
        }

        return new Trajectory(spaceObject, states);
    }

}
