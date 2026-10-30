using JustDummies;
using JustDummies.Xunit;
using Telecom.Domain;
using Xunit;

namespace Telecom.Tests;

/// <summary>Code an analyzer should catch, compiled but never run. Each line notes the rule expected.</summary>
internal static class AnalyzerProbes {

    public static void EnumExclusionEmptiesTheUniverse() {
        // JD017 expected: every declared member excluded.
        PlanTier never = Any.Enum<PlanTier>().Except(PlanTier.Prepaid, PlanTier.Postpaid, PlanTier.Unlimited).Generate();
    }

    public static void IntegerChainAdmitsNothing() {
        // JD023 expected.
        int never = Any.Int32().Between(1, 10).MultipleOf(50).Generate();
    }

    public static void DecimalScaleAdmitsNothing() {
        // No rule expected (JD023 is integers only): a constant decimal chain with no value at that scale.
        decimal never = Any.Decimal().Between(0.001m, 0.009m).WithScale(2).Generate();
    }

    public static void DateOnlyAfterMaxValue() {
        // No rule documented for temporal chains; recorded to see.
        DateOnly never = Any.DateOnly().After(DateOnly.MaxValue).Generate();
    }

    public static void DiscardedConstraint() {
        // JD006 expected.
        AnyPattern msisdn = Any.StringMatching(@"[1-9][0-9]{7,14}");
        msisdn.DifferentFrom("33612345678");
    }

    public static string GeneratorAsText() {
        // JD005 expected.
        // Recorded: JD005 fired as an error on $"IMEI {Arbitrary.Imei()}"; kept as Generate() so the project builds.
        return $"IMEI {Arbitrary.Imei().Generate().Value}";
    }

    public static void DistinctPlansWithoutValueEquality() {
        // JD028 expected: Plan does not override Equals.
        List<Plan> plans = Any.ListOf(Arbitrary.Plan()).WithCount(3).Distinct().Generate();
        List<Plan> viaAs = Any.ListOf(Any.Int32().Positive().As(gb => new Plan("p", PlanTier.Prepaid, 1m, gb, 1m))).WithCount(3).Distinct().Generate();
        IAny<Plan> typed = Arbitrary.Plan();
        List<Plan> viaInterface = Any.ListOf(typed).WithCount(3).Distinct().Generate();
    }

    public static void DuplicatePool() {
        // JD025 expected.
        string tier = Any.String().OneOf("prepaid", "postpaid", "prepaid").Generate();
    }

    public static void ImpossiblePattern() {
        // A refused construct in a constant pattern: is there a build-time rule? None documented.
        string never = Any.StringMatching(@"^(?!0)\d{15}$").Generate();
    }

    public static void AnyValueInAssertion() {
        // No rule exists for a drawn value reaching an assertion (the definition of a property).
        Msisdn anyNumber = Arbitrary.Msisdn().Generate();
        Assert.Equal(anyNumber.Digits, anyNumber.ToString().Substring(1));
    }

}

[Reproducible]
public sealed class DrawInConstructorProbe {

    private readonly Plan _plan;
    private readonly int  _limit;

    public DrawInConstructorProbe() {
        // JD007 expected: drawn before the seed scope opens.
        _plan  = Arbitrary.Plan().Generate();
        _limit = Any.Int32().Positive().Generate();
    }

    [Fact]
    public void Uses_the_plan() {
        Assert.NotNull(_plan);
        Assert.True(_limit > 0);
    }

}
