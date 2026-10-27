using JustDummies;

namespace Drafting.Domain;

public sealed partial class AnyBomItem {

    private static readonly IAny<IReadOnlyList<BomItem>> NoChildren = new FixedValue<IReadOnlyList<BomItem>>(Array.Empty<BomItem>());

    /// <summary>A part with no components: the base of every subtree.</summary>
    public static AnyBomItem Leaf() {
        return new AnyBomItem(AnyValidPart(), AnyValidDescription(), AnyValidQuantity(), NoChildren);
    }

    /// <summary>
    ///     Draws a tree exactly <paramref name="depth" /> levels deep: every level but the last has
    ///     between one and three components, so the deepest branch reaches the depth asked for.
    /// </summary>
    public AnyBomItem WithDepth(int depth) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(depth);
        if (depth == 1) {
            return new AnyBomItem(_part, _description, _quantity, NoChildren);
        }

        IAny<IReadOnlyList<BomItem>> children = Any.ListOf(Leaf().WithDepth(depth - 1)).WithCountBetween(1, 3);

        return new AnyBomItem(_part, _description, _quantity, children);
    }

}
