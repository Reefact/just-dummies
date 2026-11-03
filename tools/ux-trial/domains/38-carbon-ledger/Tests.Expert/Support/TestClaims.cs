using CarbonLedger.Domain;
using CarbonLedger.Tests.Expert.Generators;

using JustDummies;

namespace CarbonLedger.Tests.Expert.Support;

internal static class TestClaims {

    /// <summary>The claim an entry of this kind needs: a valid one for retiring <paramref name="serials" />, and none for any other kind.</summary>
    public static RetirementClaim? IfRetirement(EntryKind kind, IEnumerable<CreditSerial> serials) {
        if (kind != EntryKind.Retirement) { return null; }

        int latestVintage = serials.Max(serial => serial.Vintage);

        return Any.RetirementClaim().WithReportingYear(year => year.GreaterThanOrEqualTo(latestVintage)).Generate();
    }

}
