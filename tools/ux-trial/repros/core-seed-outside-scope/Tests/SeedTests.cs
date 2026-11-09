using JustDummies;
using Xunit;

namespace SeedOutsideScope.Tests;

public class SeedTests {

    private static AnyGenerationException Fail() {
        return Assert.Throws<AnyGenerationException>(() => Any.ListOf(Any.StringMatching("^A$")).WithCount(2).Distinct().Generate());
    }

    private static void Log(string line) {
        TestContext.Current.SendDiagnosticMessage(line);
        Console.WriteLine(line);
    }

    // Variant 1 (the claim): two failures in one run report the same seed, and the seed replays from the run's first draw.
    [Fact]
    public void V1_two_failures_report_the_flow_seed() {
        int firstDraw = Any.Int32().Generate();
        AnyGenerationException early = Fail();
        int laterDraw = Any.Int32().Generate();
        AnyGenerationException late = Fail();
        int replayed = 0;
        Any.Reproducibly(late.Seed!.Value, () => replayed = Any.Int32().Generate());
        Log($"V1 early.Seed={early.Seed} late.Seed={late.Seed} first={firstDraw} later={laterDraw} replayedFirst={replayed}");
        Log($"V1 message: {late.Message}");
        Assert.Equal(early.Seed, late.Seed);
        Assert.Equal(firstDraw, replayed);
    }

    // Variant 2: wrapping the whole body in Any.Reproducibly(S, ...) - what the message says - reproduces the run up to the failing draw.
    [Fact]
    public void V2_replaying_the_whole_body_reaches_the_same_failing_point() {
        int a = Any.Int32().Generate();
        int b = Any.Int32().Generate();
        AnyGenerationException failure = Fail();
        int ra = 0, rb = 0;
        Any.Reproducibly(failure.Seed!.Value, () => { ra = Any.Int32().Generate(); rb = Any.Int32().Generate(); });
        Log($"V2 a={a} b={b} ra={ra} rb={rb}");
        Assert.Equal((a, b), (ra, rb));
    }

    // Variant 3: the first draw happens in an awaited async helper; the lazily-picked seed does not flow back to the caller.
    [Fact]
    public async Task V3_first_draw_in_an_awaited_helper() {
        int a = await DrawAsync();
        int b = Any.Int32().Generate();
        AnyGenerationException failure = Fail();
        int ra = 0, rb = 0;
        Any.Reproducibly(failure.Seed!.Value, () => { ra = Any.Int32().Generate(); rb = Any.Int32().Generate(); });
        Log($"V3 a={a} b={b} ra={ra} rb={rb} seed={failure.Seed}");
        Assert.Equal((a, b), (ra, rb));
    }

    // Variant 3b: the faithful replay of V3's body under ReproduciblyAsync(S, ...) - what an async body needs.
    [Fact]
    public async Task V3b_faithful_async_replay_of_the_reported_seed() {
        int a = await DrawAsync();
        int b = Any.Int32().Generate();
        AnyGenerationException failure = Fail();
        int ra = 0, rb = 0;
        await Any.ReproduciblyAsync(failure.Seed!.Value, async () => { ra = await DrawAsync(); rb = Any.Int32().Generate(); });
        Log($"V3b a={a} b={b} ra={ra} rb={rb} seed={failure.Seed}");
        Assert.Equal((a, b), (ra, rb));
    }

    private static async Task<int> DrawAsync() {
        await Task.Yield();
        return Any.Int32().Generate();
    }
}

// Variant 4: does a lazily-picked seed leak across sequential tests of one class?
public class LeakTests {
    [Fact] public void L1() { Check("L1"); }
    [Fact] public void L2() { Check("L2"); }
    [Fact] public void L3() { Check("L3"); }

    private static void Check(string name) {
        int first = Any.Int32().Generate();
        AnyGenerationException failure = Assert.Throws<AnyGenerationException>(() => Any.ListOf(Any.StringMatching("^A$")).WithCount(2).Distinct().Generate());
        int replayed = 0;
        Any.Reproducibly(failure.Seed!.Value, () => replayed = Any.Int32().Generate());
        Console.WriteLine($"{name} seed={failure.Seed} first={first} replayed={replayed}");
        Assert.Equal(first, replayed);
    }
}
