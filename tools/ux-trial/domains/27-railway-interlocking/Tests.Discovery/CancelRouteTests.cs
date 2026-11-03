using JustDummies;

using RailwayInterlocking.Domain;

namespace RailwayInterlocking.Tests.Discovery;

public sealed class CancelRouteTests {

    [Fact]
    public void Frees_the_sections_and_the_points_and_ends_the_route_before_the_train_has_entered() {
        // Arrange
        Interlocking interlocking = Yard.Build();
        interlocking.RequestRoute(Yard.Main, Yard.AnyTrain(), Yard.AnyInstant());

        // Act
        interlocking.CancelRoute(Yard.Main);

        // Assert
        Assert.Empty(interlocking.ActiveRoutes);
        Assert.False(interlocking.Section(Yard.A1).IsLocked);
        Assert.False(interlocking.Section(Yard.J1).IsLocked);
        Assert.False(interlocking.Section(Yard.M1).IsLocked);
        Assert.False(interlocking.Point("P1").IsLocked);
    }

    [Fact]
    public void Leaves_the_points_where_the_route_had_thrown_them() {
        // Arrange
        Interlocking interlocking = Yard.Build(PointPosition.Reverse);
        interlocking.RequestRoute(Yard.Main, Yard.AnyTrain(), Yard.AnyInstant());

        // Act
        interlocking.CancelRoute(Yard.Main);

        // Assert
        Assert.Equal(PointPosition.Normal, interlocking.Point("P1").Position);
    }

    [Fact]
    public void Leaves_the_other_active_routes_alone() {
        // Arrange
        Interlocking interlocking = Yard.Build();
        TrainId      anyTrain     = Yard.AnyTrain();
        interlocking.RequestRoute(Yard.Main, anyTrain, Yard.AnyInstant());
        interlocking.RequestRoute(Yard.Siding, Yard.AnyTrainOtherThan(anyTrain), Yard.AnyInstant());

        // Act
        interlocking.CancelRoute(Yard.Siding);

        // Assert
        Assert.Equal(Yard.Main, Assert.Single(interlocking.ActiveRoutes).Route.Name);
        Assert.Equal(Yard.Main, interlocking.Section(Yard.J1).LockedByRoute);
        Assert.False(interlocking.Section(Yard.A2).IsLocked);
    }

    [Fact]
    public void Lets_a_conflicting_route_be_set_afterwards() {
        // Arrange
        Interlocking interlocking = Yard.Build();
        TrainId      anyTrain     = Yard.AnyTrain();
        interlocking.RequestRoute(Yard.Main, anyTrain, Yard.AnyInstant());
        interlocking.CancelRoute(Yard.Main);

        // Act
        RouteLock routeLock = interlocking.RequestRoute(Yard.Branch, anyTrain, Yard.AnyInstant());

        // Assert
        Assert.Equal(PointPosition.Reverse, interlocking.Point("P1").Position);
        Assert.Equal(Yard.Branch, routeLock.Route.Name);
    }

    [Fact]
    public void Refuses_a_route_that_is_not_set() {
        // Arrange
        Interlocking interlocking = Yard.Build();

        // Act & Assert
        Yard.AssertRefused(RefusalReason.RouteNotSet, () => interlocking.CancelRoute(Yard.Main));
    }

    [Fact]
    public void Refuses_a_name_that_is_not_a_route_of_the_area() {
        // Arrange
        Interlocking interlocking = Yard.Build();

        // Act & Assert
        Yard.AssertRefused(RefusalReason.RouteNotSet, () => interlocking.CancelRoute("NOWHERE"));
    }

    [Fact]
    public void Refuses_a_route_already_cancelled() {
        // Arrange
        Interlocking interlocking = Yard.Build();
        interlocking.RequestRoute(Yard.Main, Yard.AnyTrain(), Yard.AnyInstant());
        interlocking.CancelRoute(Yard.Main);

        // Act & Assert
        Yard.AssertRefused(RefusalReason.RouteNotSet, () => interlocking.CancelRoute(Yard.Main));
    }

    [Theory]
    [InlineData("A1")]
    [InlineData("J1")]
    [InlineData("M1")]
    public void Refuses_once_the_train_has_entered_whichever_section_it_was_detected_in_and_keeps_the_locks(string detectedIn) {
        // Arrange
        Interlocking interlocking = Yard.Build();
        interlocking.RequestRoute(Yard.Main, Yard.AnyTrain(), Yard.AnyInstant());
        interlocking.OnSectionOccupied(new SectionId(detectedIn));
        string before = Yard.Snapshot(interlocking);

        // Act
        Yard.AssertRefused(RefusalReason.ApproachLocked, () => interlocking.CancelRoute(Yard.Main));

        // Assert
        Assert.Equal(before, Yard.Snapshot(interlocking));
    }

    [Fact]
    public void Refuses_after_the_train_has_entered_and_left_the_first_section() {
        // Arrange
        Interlocking interlocking = Yard.Build();
        interlocking.RequestRoute(Yard.Main, Yard.AnyTrain(), Yard.AnyInstant());
        interlocking.OnSectionOccupied(Yard.A1);
        interlocking.OnSectionCleared(Yard.A1);

        // Act & Assert
        Yard.AssertRefused(RefusalReason.ApproachLocked, () => interlocking.CancelRoute(Yard.Main));
    }

    [Fact]
    public void Is_not_prevented_by_an_occupancy_outside_the_route() {
        // Arrange
        Interlocking interlocking = Yard.Build();
        interlocking.RequestRoute(Yard.Main, Yard.AnyTrain(), Yard.AnyInstant());
        interlocking.OnSectionOccupied(Yard.C1);

        // Act
        interlocking.CancelRoute(Yard.Main);

        // Assert
        Assert.Empty(interlocking.ActiveRoutes);
    }

}
