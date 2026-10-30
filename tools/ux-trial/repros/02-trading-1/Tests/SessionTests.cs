using JustDummies;
using Repro.Domain;
using Xunit;

namespace Repro.Tests;

public sealed class SessionTests {

    [Fact]
    public void The_scaffolded_bases_cannot_state_opensAt_before_closesAt() {
        int rejected = 0;
        for (int seed = 1; seed <= 200; seed++) {
            using (Any.UseSeed(seed)) {
                try {
                    new AnySession().Generate();
                } catch (ArgumentException) {
                    rejected++;
                }
            }
        }
        TestContext.Current.TestOutputHelper!.WriteLine($"rejected {rejected} of 200");
        Assert.Equal(0, rejected);
    }

}
