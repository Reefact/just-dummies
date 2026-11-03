using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace LiquidityGap.Domain;

/// <summary>
///     A ladder of buckets (starting at day 0, contiguous, the last one open-ended), as a
///     <see cref="Portfolio" /> or a <see cref="LiquidityProfile" /> requires.
/// </summary>
public sealed class AnyBucketLadder : IAny<IReadOnlyList<TimeBucket>> {

    private readonly int _minCount;
    private readonly int _maxCount;

    public AnyBucketLadder()
        : this(1, 8) { }

    private AnyBucketLadder(int minCount, int maxCount) {
        _minCount = minCount;
        _maxCount = maxCount;
    }

    public AnyBucketLadder WithCount(int count) => new AnyBucketLadder(count, count);

    public IReadOnlyList<TimeBucket> Generate() {
        int count = Any.Int32().Between(_minCount, _maxCount).Generate();
        List<int> edges = Any.ListOf(Any.Int32().Between(1, 5_000)).WithCount(count - 1).Distinct().Generate()
                             .OrderBy(static e => e).ToList();
        List<TimeBucket> buckets = [];
        int start = 0;
        for (int i = 0; i < count; i++) {
            int? end = i < count - 1 ? edges[i] : null;
            buckets.Add(new TimeBucket($"B{i}-{Any.String().Alpha().WithLengthBetween(1, 8).Generate()}", start, end));
            start = end ?? start;
        }

        return buckets;
    }

}

/// <summary>Bucket gaps on a ladder, each cumulative gap being the running sum of the gaps so far.</summary>
public sealed class AnyBucketGaps : IAny<IReadOnlyList<BucketGap>> {

    private readonly IAny<IReadOnlyList<TimeBucket>> _ladder;

    public AnyBucketGaps()
        : this(new AnyBucketLadder()) { }

    private AnyBucketGaps(IAny<IReadOnlyList<TimeBucket>> ladder) {
        _ladder = ladder;
    }

    public AnyBucketGaps OnLadder(IReadOnlyList<TimeBucket> ladder) => new AnyBucketGaps(Any.Fixed(ladder));

    public IReadOnlyList<BucketGap> Generate() {
        IReadOnlyList<TimeBucket> ladder = _ladder.Generate();
        List<BucketGap> gaps = [];
        decimal cumulative = 0m;
        foreach (TimeBucket bucket in ladder) {
            decimal inflows  = Any.Decimal().Between(0m, 1_000_000m).WithScale(2).Generate();
            decimal outflows = Any.Decimal().Between(0m, 1_000_000m).WithScale(2).Generate();
            cumulative += inflows - outflows;
            gaps.Add(new BucketGap(bucket, inflows, outflows, cumulative));
        }

        return gaps;
    }

}
