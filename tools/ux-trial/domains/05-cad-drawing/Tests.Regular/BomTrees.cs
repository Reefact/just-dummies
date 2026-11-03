using JustDummies;

namespace Drafting.Domain;

/// <summary>Hand-written: a bill of materials of arbitrary shape, whose part numbers are distinct by construction.</summary>
public static class BomTrees {

    private const int MaximumChildren = 3;

    /// <summary>A tree at most <paramref name="maximumDepth" /> levels deep (a leaf is one level).</summary>
    public static BomItem AnyTree(int maximumDepth) {
        // 1 + 3 + 9 + 27 nodes at the very most for four levels: 60 distinct numbers are always enough.
        Queue<PartNumber> pool = new Queue<PartNumber>(Any.ListOf(new AnyPartNumber()).WithCount(60).Distinct().Generate());

        return Build(maximumDepth, pool);
    }

    private static BomItem Build(int remainingDepth, Queue<PartNumber> pool) {
        PartNumber part = pool.Dequeue();

        List<BomItem> children = new List<BomItem>();
        if (remainingDepth > 1) {
            int childCount = Any.Int32().Between(0, MaximumChildren).Generate();
            for (int i = 0; i < childCount; i++) {
                children.Add(Build(remainingDepth - 1, pool));
            }
        }

        return new AnyBomItem().WithPart(part).WithChildren(children).Generate();
    }

}
