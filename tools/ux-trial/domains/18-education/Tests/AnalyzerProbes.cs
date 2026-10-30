using Education.Domain;
using JustDummies;
using Xunit;

namespace Education.Tests;

// Never run: each method holds one mistake an analyzer could catch at build time.
internal static class AnalyzerProbes {

    public static decimal DecimalBoundsThatCross() => Any.Decimal().Between(0m, 20m).GreaterThan(25m).Generate();

    public static int IntegerBoundsThatCross() => Any.Int32().Between(1, 8).GreaterThan(10).Generate();

    public static decimal ScaleOutOfRange() => Any.Decimal().WithScale(29).Generate();

    public static decimal ScaleWithNoValueInRange() => Any.Decimal().Between(0.21m, 0.29m).WithScale(1).Generate();

    public static decimal ScaleDeclaredTwice() => Any.Decimal().WithScale(2).WithScale(1).Generate();

    public static List<Course> CountsThatCannotHold() => Any.ListOf(new AnyCourse()).WithCountBetween(1, 8).WithMinCount(9).Generate();

    public static HashSet<Term> MoreDistinctTermsThanExist() => Any.SetOf(Any.Enum<Term>()).WithCount(3).Generate();

    public static List<Student> DistinctWithoutValueEquality() => Any.ListOf(new AnyStudent()).WithCount(3).Distinct().Generate();

    public static decimal DuplicateGradeInPool() => Any.Decimal().OneOf(10m, 12.5m, 10m).Generate();

    public static decimal SameGradeWrittenAtTwoScales() => Any.Decimal().OneOf(12.5m, 12.50m).Generate();

    public static string UndeclaredLength() => Any.String().Generate();

    public static DateOnly DateAfterTheLastDay() => Any.DateOnly().After(DateOnly.MaxValue).Generate();

    public static void DrawnValueReachesTheAssertion() {
        Grade anyGrade = new AnyGrade().Generate();

        Assert.Equal(anyGrade.Value, new Grade(anyGrade.Value).Value);
    }

    public static void GeneratorDiscarded() {
        AnyDecimal grades = Any.Decimal().Between(0m, 20m);
        grades.WithScale(1);
    }
}
