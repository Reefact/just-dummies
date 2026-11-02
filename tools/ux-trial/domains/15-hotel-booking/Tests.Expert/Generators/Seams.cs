// Written by hand, beside the scaffolded files: reaches the private valid bases so that a test
// can draw a value of that kind on its own. `any generate --force` leaves this file alone.

using JustDummies;

namespace Booking.Domain;

public sealed partial class AnyStayPeriod {

    internal static AnyDateOnly ValidCheckIn() { return AnyValidCheckIn(); }

}

public sealed partial class AnyRoom {

    internal static AnyMoney ValidNightlyRate() { return AnyValidNightlyRate(); }

}

public sealed partial class AnyMoney {

    internal static AnyPattern ValidCurrency() { return AnyValidCurrency(); }

}
