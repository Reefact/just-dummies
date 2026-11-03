using RailwayInterlocking.Domain.Tests.Expert.Generators;

namespace RailwayInterlocking.Domain.Tests.Expert.Support;

internal static class Trains {

    /// <summary>A second train, distinct from the first: two trains are two headcodes.</summary>
    public static TrainId AnyOtherThan(TrainId train) {
        return new AnyTrainId().WithHeadcode(headcode => headcode.DifferentFrom(train.Headcode)).Generate();
    }

}
