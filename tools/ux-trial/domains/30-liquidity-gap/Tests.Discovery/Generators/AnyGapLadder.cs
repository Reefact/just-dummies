using System;
using System.Collections.Generic;

using JustDummies;

namespace LiquidityGap.Domain;

/// <summary>A ladder of contiguous buckets from day 0, the last one open-ended. Written by hand.</summary>
public sealed class AnyBucketLadder : IAny<IReadOnlyList<TimeBucket>> {

    public IReadOnlyList<TimeBucket> Generate() {
        int count = Any.Int32().Between(1, 6).Generate();

        List<TimeBucket> buckets = new();
        int              start   = 0;
        for (int i = 0; i < count; i++) {
            string label = Any.String().NotBlank().Generate();
            if (i == count - 1) {
                buckets.Add(new TimeBucket(label, start, null));
            } else {
                int end = start + Any.Int32().Between(1, 400).Generate();
                buckets.Add(new TimeBucket(label, start, end));
                start = end;
            }
        }

        return buckets;
    }

}

/// <summary>Gaps over a ladder whose cumulative gaps are the running sum of the gaps. Written by hand.</summary>
public sealed class AnyGapLadder : IAny<IReadOnlyList<BucketGap>> {

    public IReadOnlyList<BucketGap> Generate() {
        IReadOnlyList<TimeBucket> ladder = new AnyBucketLadder().Generate();

        List<BucketGap> gaps       = new();
        decimal         cumulative = 0m;
        foreach (TimeBucket bucket in ladder) {
            decimal inflows  = Any.Decimal().Between(0m, 1_000_000m).WithScale(2).Generate();
            decimal outflows = Any.Decimal().Between(0m, 1_000_000m).WithScale(2).Generate();
            cumulative += inflows - outflows;
            gaps.Add(new BucketGap(bucket, inflows, outflows, cumulative));
        }

        return gaps;
    }

}
