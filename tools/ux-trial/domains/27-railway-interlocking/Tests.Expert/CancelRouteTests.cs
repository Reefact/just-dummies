using RailwayInterlocking.Domain.Tests.Expert.Support;

namespace RailwayInterlocking.Domain.Tests.Expert;

public sealed class CancelRouteTests {

    [Fact]
    public void A_route_the_train_has_not_entered_can_be_cancelled_and_frees_its_sections_and_points() {
        Junction junction = Junction.Build();
        junction.Interlocking.RequestRoute(junction.ToMain.Name, Any.TrainId().Generate(), Instants.AnyLockTime());

        junction.Interlocking.CancelRoute(junction.ToMain.Name);

        Assert.Empty(junction.Interlocking.ActiveRoutes);
        Assert.All(new[] { junction.Approach, junction.Throat, junction.Main }, id => Assert.False(junction.Interlocking.Section(id).IsLocked));
        Assert.False(junction.Interlocking.Point(junction.PointName).IsLocked);
    }

    [Fact]
    public void Cancelling_a_route_leaves_the_other_routes_set() {
        Junction junction = Junction.Build();
        TrainId  anyTrain = Any.TrainId().Generate();
        junction.Interlocking.RequestRoute(junction.ToMain.Name, anyTrain, Instants.AnyLockTime());
        RouteLock goods = junction.Interlocking.RequestRoute(junction.Goods.Name, Trains.AnyOtherThan(anyTrain), Instants.AnyLockTime());

        junction.Interlocking.CancelRoute(junction.ToMain.Name);

        Assert.Same(goods, Assert.Single(junction.Interlocking.ActiveRoutes));
        Assert.Equal(junction.Goods.Name, junction.Interlocking.Section(junction.Yard).LockedByRoute);
    }

    [Fact]
    public void A_cancelled_route_lets_a_conflicting_route_be_set_and_its_train_ask_again() {
        Junction junction = Junction.Build();
        TrainId  anyTrain = Any.TrainId().Generate();
        junction.Interlocking.RequestRoute(junction.ToMain.Name, anyTrain, Instants.AnyLockTime());
        junction.Interlocking.CancelRoute(junction.ToMain.Name);

        RouteLock onTheBranch = junction.Interlocking.RequestRoute(junction.ToSiding.Name, anyTrain, Instants.AnyLockTime());

        Assert.Same(junction.ToSiding, onTheBranch.Route);
        Assert.Equal(PointPosition.Reverse, junction.Interlocking.Point(junction.PointName).Position);
    }

    [Fact]
    public void A_route_that_is_not_set_cannot_be_cancelled() {
        Junction junction = Junction.Build();

        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(() => junction.Interlocking.CancelRoute(junction.ToMain.Name));

        Assert.Equal(RefusalReason.RouteNotSet, refusal.Reason);
    }

    [Fact]
    public void A_route_the_interlocking_does_not_know_cannot_be_cancelled_either() {
        Junction junction = Junction.Build();

        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(() => junction.Interlocking.CancelRoute(junction.AnyUnknownRouteName()));

        Assert.Equal(RefusalReason.RouteNotSet, refusal.Reason);
    }

    [Theory]
    [InlineData(0)]   // the train has just come in
    [InlineData(1)]   // it is in the middle
    [InlineData(2)]   // it has gone straight to the last section
    public void A_route_cannot_be_cancelled_once_the_train_is_on_any_of_its_sections(int occupiedPosition) {
        Junction junction = Junction.Build();
        junction.Interlocking.RequestRoute(junction.ToMain.Name, Any.TrainId().Generate(), Instants.AnyLockTime());
        junction.Interlocking.OnSectionOccupied(junction.ToMain.Sections[occupiedPosition]);

        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(() => junction.Interlocking.CancelRoute(junction.ToMain.Name));

        Assert.Equal(RefusalReason.ApproachLocked, refusal.Reason);
    }

    [Fact]
    public void A_refused_cancellation_leaves_the_route_set_and_its_points_locked() {
        Junction junction = Junction.Build();
        RouteLock routeLock = junction.Interlocking.RequestRoute(junction.ToMain.Name, Any.TrainId().Generate(), Instants.AnyLockTime());
        junction.Interlocking.OnSectionOccupied(junction.Approach);

        Assert.Throws<InterlockingRefusedException>(() => junction.Interlocking.CancelRoute(junction.ToMain.Name));

        Assert.Same(routeLock, Assert.Single(junction.Interlocking.ActiveRoutes));
        Assert.All(new[] { junction.Approach, junction.Throat, junction.Main }, id => Assert.True(junction.Interlocking.Section(id).IsLocked));
        Assert.True(junction.Interlocking.Point(junction.PointName).IsLocked);
    }

}
