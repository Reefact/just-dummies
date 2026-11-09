// Scaffolded by `any`, then rewritten by hand: Currency ties its code to its minor units through the ISO table
// the constructor owns, which no per-parameter recipe can express. The generator draws from that table.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace FxSettlement.Domain.Tests.Regular;

public sealed partial class AnyCurrency : IAny<Currency> {

    private static readonly (string Code, int MinorUnits)[] IsoTable = [
        ("AUD", 2), ("BHD", 3), ("CAD", 2), ("CHF", 2), ("CLF", 4), ("CLP", 0), ("EUR", 2), ("GBP", 2), ("JOD", 3),
        ("JPY", 0), ("KRW", 0), ("KWD", 3), ("NOK", 2), ("OMR", 3), ("SEK", 2), ("TND", 3), ("USD", 2),
    ];

    /// <summary>The codes of the desk's ISO table.</summary>
    public static IReadOnlyList<string> IsoCodes { get; } = IsoTable.Select(entry => entry.Code).ToList();

    private readonly IReadOnlyList<Currency> _pool;

    public AnyCurrency()
        : this(IsoTable.Select(entry => new Currency(entry.Code, entry.MinorUnits)).ToList()) { }

    private AnyCurrency(IReadOnlyList<Currency> pool) {
        _pool = pool;
    }

    /// <summary>The currencies this generator can draw.</summary>
    public IReadOnlyList<Currency> Pool => _pool;

    /// <summary>Only currencies carrying exactly this number of minor units.</summary>
    public AnyCurrency WithMinorUnits(int minorUnits) {
        return new AnyCurrency(_pool.Where(currency => currency.MinorUnits == minorUnits).ToList());
    }

    /// <summary>Only currencies carrying at least this number of minor units.</summary>
    public AnyCurrency WithMinorUnitsAtLeast(int minorUnits) {
        return new AnyCurrency(_pool.Where(currency => currency.MinorUnits >= minorUnits).ToList());
    }

    public AnyCurrency Except(params Currency[] currencies) {
        return new AnyCurrency(_pool.Where(currency => !currencies.Contains(currency)).ToList());
    }

    public Currency Generate() {
        return Any.ElementOf(_pool).Generate();
    }

}
