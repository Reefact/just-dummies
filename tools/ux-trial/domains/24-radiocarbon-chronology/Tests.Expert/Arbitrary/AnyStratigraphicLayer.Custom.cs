using JustDummies;

namespace RadiocarbonChronology.Domain;

// Written by hand: `any generate StratigraphicLayer --force` leaves this file alone.
public sealed partial class AnyStratigraphicLayer {

    /// <summary>The excavation's layer code format, for a test that needs a code without a layer.</summary>
    public static AnyPattern Codes() {
        return AnyValidCode();
    }

}
