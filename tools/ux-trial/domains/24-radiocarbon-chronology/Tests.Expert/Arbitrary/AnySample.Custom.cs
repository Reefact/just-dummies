using JustDummies;

namespace RadiocarbonChronology.Domain;

// Written by hand: `any generate Sample --force` leaves this file alone.
public sealed partial class AnySample {

    /// <summary>The laboratory code format, for a test that needs codes on their own (distinct ones, for instance).</summary>
    public static AnyPattern LabCodes() {
        return AnyValidLabCode();
    }

}
