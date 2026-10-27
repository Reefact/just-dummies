using JustDummies;
using Xunit;

namespace Repro20One;

public class EpsilonTests {

    [Fact]
    public void Draws_from_minus_to_plus_epsilon_reach_more_than_zero() {
        HashSet<double> seen = new HashSet<double>();
        IAny<double> generator = Any.Double().Between(-double.Epsilon, double.Epsilon);
        for (int i = 0; i < 2000; i++) {
            seen.Add(generator.Generate());
        }

        Assert.True(seen.Count > 1, "distinct values seen: " + string.Join(", ", seen));
    }

    [Fact]
    public void Two_distinct_values_from_minus_to_plus_epsilon_are_produced() {
        double[] values = Any.ArrayOf(Any.Double().Between(-double.Epsilon, double.Epsilon)).WithCount(2).Distinct().Generate();

        Assert.Equal(2, values.Distinct().Count());
    }

    [Fact]
    public void Control_two_epsilon_reaches_more_than_zero() {
        HashSet<double> seen = new HashSet<double>();
        IAny<double> generator = Any.Double().Between(-2 * double.Epsilon, 2 * double.Epsilon);
        for (int i = 0; i < 2000; i++) {
            seen.Add(generator.Generate());
        }

        Assert.True(seen.Count > 1);
    }

    [Fact]
    public void Typed_double_OneOf_pool_of_two_refuses_three_distinct() {
        Assert.ThrowsAny<Exception>(() => Any.ArrayOf(Any.Double().OneOf(1.0, 2.0)).WithCount(3).Distinct().Generate());
    }

}
