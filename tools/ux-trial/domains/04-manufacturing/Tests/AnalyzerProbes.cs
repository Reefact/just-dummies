using JustDummies;
using Manufacturing.Domain;
using Xunit;

namespace Manufacturing.Tests;

/// <summary>Code an analyzer should catch. Built, not run for its values: each body expects the refusal.</summary>
public sealed class AnalyzerProbes {

    [Fact]
    public void Integer_lattice_with_no_point() {
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.Int32().Between(1, 10).MultipleOf(50));             // expect JD023
    }

    [Fact]
    public void Seven_distinct_states_of_six() {
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.SetOf(Any.Enum<MachineState>()).WithCount(7).Generate()); // expect JD016
    }

    [Fact]
    public void Undeclared_shift_kind() {
        Assert.Throws<ArgumentException>(() => Any.Enum<ShiftKind>().OneOf((ShiftKind)0));                             // expect JD017
    }

    [Fact]
    public void Distinct_machines_without_equality() {
        Assert.Equal(3, Any.ListOf(new AnyMachine()).WithCount(3).Distinct().Generate().Count);                      // expect JD028
    }

    [Fact]
    public void Discarded_constraint() {
        AnyInt32 cadence = Any.Int32();
        cadence.Between(1, 10_000);                                                                                     // expect JD006
        Assert.NotNull(cadence);
    }

    [Fact]
    public void Prefix_longer_than_length() {
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.String().StartingWith("LYN-").WithLength(3));       // expect JD015
    }

    [Fact]
    public void Paired_inclusive_bounds() {
        Assert.InRange(Any.Int32().GreaterThanOrEqualTo(1).LessThanOrEqualTo(10_000).Generate(), 1, 10_000);           // expect JD031 (info)
    }

    [Fact]
    public void TimeSpan_bounds_crossing_from_constants() {
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.TimeSpan().Between(TimeSpan.FromHours(1), TimeSpan.FromHours(2)).LessThan(TimeSpan.FromMinutes(30))); // no rule claims it
    }

    [Fact]
    public void Decimal_bounds_crossing_from_literals() {
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.Decimal().GreaterThan(10m).LessThan(1m));           // JD023 is integer-only per its doc
    }

    [Fact]
    public void Drawn_value_reaching_the_assertion() {
        int anyUnitsPerHour = Any.Int32().Between(1, 10_000).Generate();

        Cadence cadence = new Cadence(anyUnitsPerHour);

        Assert.Equal(anyUnitsPerHour, cadence.UnitsPerHour);                                                            // a property in disguise; no rule claims it
    }

}
