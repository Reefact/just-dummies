namespace ShiftPlanning.Domain.Tests.Expert.Support;

internal static class Refusal {

    /// <summary>Runs the action, expects the roster to refuse it, and returns why.</summary>
    public static RefusalReason Of(Action action) {
        RosterRefusedException refusal = Assert.Throws<RosterRefusedException>(action);

        return refusal.Reason;
    }

}
