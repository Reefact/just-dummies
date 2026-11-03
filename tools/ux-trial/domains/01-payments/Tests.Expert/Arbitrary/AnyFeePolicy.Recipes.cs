using JustDummies;

namespace Payments.Domain;

// Written by hand, beside the scaffolded AnyFeePolicy.cs.
public sealed partial class AnyFeePolicy {

    // The valid bases, for a test that spoils one parameter of the constructor and keeps the others valid.
    internal static AnyDecimal ValidFlatFees()  => AnyValidInstantFlatFee();
    internal static AnyDecimal ValidRates()     => AnyValidInstantRate();
    internal static AnyDecimal ValidThresholds() => AnyValidFreeStandardThreshold();

}
