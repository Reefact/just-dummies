using System;

using JustDummies;

namespace Payments.Domain;

// Written by hand, beside the scaffolded AnyCreditTransfer.cs.
public sealed partial class AnyCreditTransfer {

    // The valid bases, for a test that spoils one argument of the constructor and keeps the others valid.
    internal static AnyString        ValidEndToEndIds()   => AnyValidEndToEndId();
    internal static IAny<DateOnly>   ValidExecutionDates() => AnyValidRequestedExecutionDate();

}
