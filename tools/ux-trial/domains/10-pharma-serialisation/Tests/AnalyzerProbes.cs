using JustDummies;
using Serialisation.Domain;

namespace Serialisation.Tests;

/// <summary>Chains written to see which analyzer fires. Never executed.</summary>
internal static class AnalyzerProbes {

    internal static void ConstantContradictions() {
        // String: a prefix the length cannot hold (JD015 expected).
        string shortShipment = Any.String().StartingWith("SHP-").Numeric().WithLength(3).Generate();

        // Integer: crossed bounds (JD023 expected).
        int crossedQuantity = Any.Int32().GreaterThan(500_000).LessThan(1).Generate();

        // Decimal: a range holding no value at scale 2 (no rule documented for decimals).
        decimal subCent = Any.Decimal().Between(0.001m, 0.009m).WithScale(2).Generate();

        // Dates: crossed bounds with constants (no rule documented for dates).
        DateOnly crossedExpiry = Any.DateOnly().After(new DateOnly(2030, 1, 1)).Before(new DateOnly(2020, 1, 1)).Generate();

        // Collections: more distinct elements than the element can produce (JD016 expected).
        List<char> elevenDigits = Any.ListOf(Any.Char().Numeric()).WithCount(11).Distinct().Generate();

        // Collections: distinctness over a type without value equality (JD028 expected).
        List<SerialisedPack> packs = Any.ListOf(new AnySerialisedPack()).WithCount(3).Distinct().Generate();

        // Regex: a lookahead, as a constant pattern (no rule documented).
        string serialWithDigit = Any.StringMatching(@"(?=.*\d)[A-Za-z0-9]{8,20}").Generate();

        // Enum: excluding every member (JD017 expected).
        PackStatus none = Any.Enum<PackStatus>().Except(PackStatus.Active, PackStatus.Dispensed, PackStatus.Destroyed, PackStatus.Stolen, PackStatus.Recalled).Generate();

        // A generator discarded as a statement (JD006 expected).
        Any.String().AlphaNumeric().WithLength(8);

        // Same bound twice (JD032 expected).
        int twice = Any.Int32().GreaterThan(0).GreaterThan(5).Generate();

        // A drawn value flowing into an assertion (no rule documented; the getting-started page calls it a property).
        SerialNumber anySerial = new AnySerialNumber().Generate();
        Xunit.Assert.Equal(anySerial.Value, new SerialNumber(anySerial.Value).Value);

        // The same JD028 case, composed with Combine so the chain provably builds a new instance.
        List<SerialisedPack> combinedPacks = Any.ListOf(Any.Combine(new AnyGtin(), new AnyBatchNumber(), Any.DateOnly(), new AnySerialNumber(),
                                                                    (gtin, batch, expiry, serial) => new SerialisedPack(gtin, batch, expiry, serial)))
                                                .WithCount(3).Distinct().Generate();

        // The same JD015 case, inside a lambda.
        Func<string> lazyShortShipment = () => Any.String().StartingWith("SHP-").Numeric().WithLength(3).Generate();

        _ = (combinedPacks, lazyShortShipment);
        _ = (shortShipment, crossedQuantity, subCent, crossedExpiry, elevenDigits, packs, serialWithDigit, none, twice);
    }

}
