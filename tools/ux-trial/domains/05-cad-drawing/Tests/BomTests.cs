using Drafting.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace Drafting.Tests;

[Reproducible]
public sealed class BomTests {

    [Fact]
    public void Quantities_multiply_down_the_tree() {
        // Arrange: 2 gearboxes per build, 3 shafts per gearbox, 4 keys per shaft.
        List<PartNumber> anyParts = Any.ListOf(Any.PartNumber()).WithCount(3).Distinct().Generate();
        PartNumber       key      = anyParts[2];

        BomItem anyKey      = Any.BomItem().WithPart(key).WithQuantity(4).WithDepth(1).Generate();
        BomItem anyShaft    = Any.BomItem().WithPart(anyParts[1]).WithQuantity(3).WithChildren([anyKey]).Generate();
        BomItem anyGearbox  = Any.BomItem().WithPart(anyParts[0]).WithQuantity(2).WithChildren([anyShaft]).Generate();

        // Act
        int keys = anyGearbox.TotalQuantityOf(key);

        // Assert
        Assert.Equal(24, keys);
    }

    [Fact]
    public void Flattening_adds_up_a_leaf_used_in_two_branches() {
        // Arrange: one screw, 2 in the housing and 5 in the cover; the assembly is built once.
        List<PartNumber> anyParts = Any.ListOf(Any.PartNumber()).WithCount(4).Distinct().Generate();
        PartNumber       screw    = anyParts[3];

        BomItem anyHousing  = Any.BomItem().WithPart(anyParts[1]).WithQuantity(1).WithChildren([Any.BomItem().WithPart(screw).WithQuantity(2).WithDepth(1).Generate()]).Generate();
        BomItem anyCover    = Any.BomItem().WithPart(anyParts[2]).WithQuantity(1).WithChildren([Any.BomItem().WithPart(screw).WithQuantity(5).WithDepth(1).Generate()]).Generate();
        BomItem anyAssembly = Any.BomItem().WithPart(anyParts[0]).WithQuantity(1).WithChildren([anyHousing, anyCover]).Generate();

        // Act
        IReadOnlyDictionary<PartNumber, int> leaves = anyAssembly.FlattenToLeaves();

        // Assert
        Assert.Equal(7, leaves[screw]);
    }

    [Fact]
    public void A_part_cannot_be_a_component_of_itself_two_levels_down() {
        // Arrange
        PartNumber anyPart       = Any.PartNumber().Generate();
        BomItem    anyGrandchild = Any.BomItem().WithPart(anyPart).WithDepth(1).Generate();
        BomItem    anyChild      = Any.BomItem().WithPart(p => p.WithValue(v => v.DifferentFrom(anyPart.Value))).WithChildren([anyGrandchild]).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Any.BomItem().WithPart(anyPart).WithChildren([anyChild]).Generate());

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void A_leaf_is_one_level_deep() {
        // Arrange
        BomItem anyLeaf = AnyBomItem.Leaf().Generate();

        // Act
        int depth = anyLeaf.Depth;

        // Assert
        Assert.Equal(1, depth);
    }

    [Fact]
    public void A_part_absent_from_the_tree_totals_zero() {
        // Arrange
        BomItem    anyTree    = Any.BomItem().WithDepth(3).Generate();
        PartNumber notInTree  = new("P000000-ZZ");

        // Act
        int total = anyTree.TotalQuantityOf(notInTree);

        // Assert
        Assert.Equal(0, total);
    }

}
