// Scaffolded by `any` (JustDummies), then rewritten by hand: the five parameters are tied together by the
// constructor's guards (gross >= refunded + deductions, carryover only without dividends), so Generate()
// draws them in dependency order rather than independently.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace ParimutuelBetting.Domain;

/// <summary>A generator of arbitrary <see cref="Settlement" /> values, either paid out or carried over.</summary>
public sealed partial class AnySettlement : IAny<Settlement> {

    public Settlement Generate() {
        decimal gross      = Any.Decimal().Between(0m, 1_000_000m).WithScale(2).Generate();
        decimal refunded   = Any.Decimal().Between(0m, gross).WithScale(2).Generate();
        decimal deductions = Any.Decimal().Between(0m, gross - refunded).WithScale(2).Generate();
        decimal net        = gross - refunded - deductions;

        if (Any.Boolean().Generate()) {
            List<Dividend> dividends = Any.ListOf(new AnyDividend())
                                          .NonEmpty()
                                          .WithMaxCount(10)
                                          .Generate();
            return new Settlement(gross, refunded, deductions, dividends, 0m);
        }

        decimal carryover = Any.Decimal().Between(0m, net).WithScale(2).Generate();
        return new Settlement(gross, refunded, deductions, Array.Empty<Dividend>(), carryover);
    }

}
