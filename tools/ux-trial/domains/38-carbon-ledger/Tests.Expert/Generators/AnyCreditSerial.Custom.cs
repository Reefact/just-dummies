using CarbonLedger.Domain;
using JustDummies;

namespace CarbonLedger.Tests.Expert.Generators;

public sealed partial class AnyCreditSerial {

    /// <summary>A registry code on its own, for a test that needs a ledger's registry before drawing its serials.</summary>
    internal static AnyString ValidRegistry() {
        return AnyValidRegistry();
    }

}
