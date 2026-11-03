using JustDummies;

namespace Payments.Domain;

// Written by hand, beside the scaffolded AnyParty.cs.
public sealed partial class AnyParty {

    // The valid base of the name, for a test that builds a Party by its constructor and spoils another argument.
    internal static AnyString ValidNames() {
        return AnyValidName();
    }

}
