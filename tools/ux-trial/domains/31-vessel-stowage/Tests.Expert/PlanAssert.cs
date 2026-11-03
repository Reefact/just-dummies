using Xunit;

namespace VesselStowage.Domain.Tests;

internal static class PlanAssert {

    /// <summary>The plan refuses the box in that slot: Check says so, Assign throws the same violation, and nothing changes.</summary>
    public static void Refuses(StowagePlan plan, Container container, SlotPosition slot, StowageViolation expected) {
        int stowedBefore = plan.Assignments.Count;

        Assert.Equal(expected, plan.Check(container, slot));

        StowageRefusedException refusal = Assert.Throws<StowageRefusedException>(() => plan.Assign(container, slot));

        Assert.Equal(expected, refusal.Violation);
        Assert.Contains(container.Id.Value, refusal.Message);
        Assert.Contains(slot.ToString(), refusal.Message);
        Assert.Equal(stowedBefore, plan.Assignments.Count);
    }

    /// <summary>The plan accepts the box in that slot: Check says so first and leaves the plan alone, then Assign stows it.</summary>
    public static StowageAssignment Accepts(StowagePlan plan, Container container, SlotPosition slot) {
        int stowedBefore = plan.Assignments.Count;

        Assert.Equal(StowageViolation.None, plan.Check(container, slot));
        Assert.Equal(stowedBefore, plan.Assignments.Count);

        StowageAssignment assignment = plan.Assign(container, slot);

        Assert.Equal(stowedBefore + 1, plan.Assignments.Count);

        return assignment;
    }

}
