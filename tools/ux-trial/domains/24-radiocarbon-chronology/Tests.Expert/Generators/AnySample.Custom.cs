using JustDummies;

namespace RadiocarbonChronology.Domain;

// Written by hand, beside the scaffolded file: it survives `any generate --force`.
public sealed partial class AnySample {

    /// <summary>The valid base of a laboratory code, for a test that needs one on its own.</summary>
    internal static AnyPattern ValidLabCode() {
        return AnyValidLabCode();
    }

}
