namespace Manufacturing.Domain.Tests.Expert;

/// <summary>The generators are code the team owns: they must never draw a value the domain refuses.</summary>
public sealed class GeneratorsTests {

    private const int Draws = 500;

    [Fact]
    public void Every_batch_code_drawn_is_accepted() {
        Assert.Equal(Draws, Any.ListOf(Any.BatchCode()).WithCount(Draws).Generate().Count);
    }

    [Fact]
    public void Every_cadence_drawn_is_accepted() {
        Assert.Equal(Draws, Any.ListOf(Any.Cadence()).WithCount(Draws).Generate().Count);
    }

    [Fact]
    public void Every_cycle_time_drawn_is_accepted() {
        Assert.Equal(Draws, Any.ListOf(Any.CycleTime()).WithCount(Draws).Generate().Count);
    }

    [Fact]
    public void Every_shift_window_drawn_is_accepted() {
        Assert.Equal(Draws, Any.ListOf(Any.ShiftWindow()).WithCount(Draws).Generate().Count);
    }

    [Fact]
    public void Every_tolerance_drawn_is_accepted() {
        Assert.Equal(Draws, Any.ListOf(Any.Tolerance()).WithCount(Draws).Generate().Count);
    }

    [Fact]
    public void Every_machine_drawn_is_accepted() {
        Assert.Equal(Draws, Any.ListOf(Any.Machine()).WithCount(Draws).Generate().Count);
    }

    [Fact]
    public void Every_work_order_drawn_is_accepted() {
        Assert.Equal(Draws, Any.ListOf(Any.WorkOrder()).WithCount(Draws).Generate().Count);
    }

}
