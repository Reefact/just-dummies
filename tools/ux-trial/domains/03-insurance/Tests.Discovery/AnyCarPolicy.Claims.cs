using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace Insurance.Domain;

public sealed partial class AnyCarPolicy {

    /// <summary>The policy only accepts claims dated inside its period: move each drawn claim to a day of the period.</summary>
    private static IReadOnlyList<Claim> RedatedWithin(CoveragePeriod period, IReadOnlyList<Claim> claims) {
        return claims
              .Select(claim => new Claim(claim.Id,
                                         period.Start.AddDays(Any.Int32().Between(0, period.Days - 1).Generate()),
                                         claim.Damage,
                                         claim.Responsibility))
              .ToList();
    }

}
