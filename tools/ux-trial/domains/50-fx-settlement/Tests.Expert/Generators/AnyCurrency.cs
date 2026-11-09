// Scaffolded by `any` (JustDummies), then rewritten by hand: Currency ties its code to its minor units
// (an ISO code carries its ISO minor units), which no per-parameter recipe can honour. This generator
// draws from the desk's ISO table and is narrowed by whole currencies, never by parameter.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace FxSettlement.Domain.Tests.Expert;

/// <summary>A generator of arbitrary currencies of the desk's ISO 4217 table.</summary>
public sealed class AnyCurrency : IAny<Currency> {

    /// <summary>The desk's ISO table, as MODEL.md states it.</summary>
    public static readonly IReadOnlyList<string> IsoCodes = [
        "AUD", "BHD", "CAD", "CHF", "CLF", "CLP", "EUR", "GBP", "JOD", "JPY", "KRW", "KWD", "NOK", "OMR", "SEK", "TND", "USD"
    ];

    private readonly IReadOnlyList<Currency> _pool;

    public AnyCurrency()
        : this(IsoCodes.Select(Currency.Iso).ToList()) { }

    private AnyCurrency(IReadOnlyList<Currency> pool) {
        _pool = pool;
    }

    /// <summary>The currencies this generator draws from.</summary>
    public IReadOnlyList<Currency> Pool => _pool;

    /// <summary>Keeps the currencies with exactly <paramref name="minorUnits" /> minor units.</summary>
    public AnyCurrency WithMinorUnits(int minorUnits) {
        return new AnyCurrency(_pool.Where(currency => currency.MinorUnits == minorUnits).ToList());
    }

    /// <summary>Keeps the currencies with at least <paramref name="minorUnits" /> minor units.</summary>
    public AnyCurrency WithMinorUnitsAtLeast(int minorUnits) {
        return new AnyCurrency(_pool.Where(currency => currency.MinorUnits >= minorUnits).ToList());
    }

    /// <summary>Keeps the currencies that are none of <paramref name="excluded" />.</summary>
    public AnyCurrency Except(params Currency[] excluded) {
        return new AnyCurrency(_pool.Where(currency => !excluded.Contains(currency)).ToList());
    }

    public Currency Generate() {
        return Any.ElementOf(_pool).Generate();
    }

}
