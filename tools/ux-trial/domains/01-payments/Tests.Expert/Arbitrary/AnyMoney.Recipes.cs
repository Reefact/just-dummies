using JustDummies;

namespace Payments.Domain;

// Written by hand, beside the scaffolded AnyMoney.cs.
public sealed partial class AnyMoney {

    // The valid base of the amount, for a test that needs an amount of its own, beside a currency it refuses.
    internal static AnyDecimal ValidAmounts() {
        return AnyValidAmount();
    }

}
