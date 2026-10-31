// Written by hand: a ladder is not one constructor call but a rule across buckets
// (starts at day 0, contiguous, only the last open-ended), so it is built from distinct cut days.

using JustDummies;

namespace LiquidityGap.Domain;

/// <summary>A generator of arbitrary, valid bucket ladders: up to nine buckets over the first ten years.</summary>
public sealed class AnyBucketLadder : IAny<IReadOnlyList<TimeBucket>> {

    private static readonly IAny<HashSet<int>> AnyCutDays = Any.SetOf(Any.Int32().Between(1, 3_650)).WithMaxCount(8);

    public IReadOnlyList<TimeBucket> Generate() {
        List<int>        cuts    = AnyCutDays.Generate().Order().ToList();
        List<TimeBucket> buckets = new(cuts.Count + 1);
        int              start   = 0;
        foreach (int cut in cuts) {
            buckets.Add(new TimeBucket($"{start}-{cut}d", start, cut));
            start = cut;
        }

        buckets.Add(new TimeBucket($">{start}d", start, null));

        return buckets;
    }

}

public static class AnyBucketLadderEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary, valid bucket ladder.</summary>
        public static AnyBucketLadder BucketLadder() {
            return new AnyBucketLadder();
        }

    }

}
