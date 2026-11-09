using JustDummies;

namespace Zone.Tests;

internal static class AnalyzerProbe {

    // Expected JD034 on the direct chain; nothing expected inside the lambda.
    internal static string Direct() {
        return Any.String().OneOf("H", "h", "JS", "js").OneOf("H", "JS").Generate();
    }

    internal static Zone.Domain.Tolerance ThroughLambda() {
        return new Zone.Domain.AnyTolerance().WithZone(z => z.OneOf("H", "JS")).Generate();
    }

}
