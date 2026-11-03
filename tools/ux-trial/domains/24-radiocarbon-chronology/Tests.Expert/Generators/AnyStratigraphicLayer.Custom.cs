using JustDummies;

namespace RadiocarbonChronology.Domain;

// Written by hand, beside the scaffolded file: it survives `any generate --force`.
public sealed partial class AnyStratigraphicLayer {

    /// <summary>The valid base of a layer code, for every generator that needs a code a site could hold.</summary>
    internal static AnyPattern ValidCode() {
        return AnyValidCode();
    }

}
