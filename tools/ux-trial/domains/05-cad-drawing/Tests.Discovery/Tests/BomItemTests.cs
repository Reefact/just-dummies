using JustDummies;

using Xunit;

namespace Drafting.Domain.Tests;

public sealed class BomItemTests {

    private static PartNumber Part(int n) {
        return new PartNumber($"P{n:D6}");
    }

    private static BomItem Leaf(int part, int quantity) {
        return Any.BomItem().WithPart(Part(part)).WithQuantity(quantity).Generate();
    }

    private static BomItem Node(int part, int quantity, params BomItem[] children) {
        return Any.BomItem().WithPart(Part(part)).WithQuantity(quantity).WithChildren(children).Generate();
    }

    // --- construction ---------------------------------------------------------------------

    [Fact]
    public void A_new_item_keeps_what_it_was_given() {
        // Arrange
        PartNumber      part     = Part(4217);
        BomItem         child    = Leaf(1, 1);
        List<BomItem>   children = [child];

        // Act
        BomItem item = new BomItem(part, "Flange", 4, children);

        // Assert
        Assert.Same(part, item.Part);
        Assert.Equal("Flange", item.Description);
        Assert.Equal(4, item.Quantity);
        Assert.Same(children, item.Children);
    }

    [Fact]
    public void A_null_part_is_refused() {
        // Arrange
        BomItem anyItem = Any.BomItem().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new BomItem(null!, anyItem.Description, anyItem.Quantity, anyItem.Children));
    }

    [Fact]
    public void A_null_description_is_refused_as_null() {
        // Arrange
        BomItem anyItem = Any.BomItem().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new BomItem(anyItem.Part, null!, anyItem.Quantity, anyItem.Children));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_description_is_refused_as_an_argument_error(string blank) {
        // Arrange
        BomItem anyItem = Any.BomItem().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new BomItem(anyItem.Part, blank, anyItem.Quantity, anyItem.Children));
    }

    [Fact]
    public void A_description_of_80_characters_is_accepted() {
        // Act
        BomItem item = Any.BomItem().WithDescription(new string('d', BomItem.MaximumDescriptionLength)).Generate();

        // Assert
        Assert.Equal(80, item.Description.Length);
    }

    [Fact]
    public void A_description_of_81_characters_is_refused() {
        // Arrange
        BomItem anyItem = Any.BomItem().Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new BomItem(anyItem.Part, new string('d', 81), anyItem.Quantity, anyItem.Children));

        // Assert
        Assert.Equal("description", error.ParamName);
    }

    [Fact]
    public void The_limits_are_published_as_constants() {
        Assert.Equal(80, BomItem.MaximumDescriptionLength);
        Assert.Equal(9_999, BomItem.MaximumQuantity);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(9_999)]
    public void A_quantity_from_1_to_9999_is_accepted(int quantity) {
        // Act
        BomItem item = Any.BomItem().WithQuantity(quantity).Generate();

        // Assert
        Assert.Equal(quantity, item.Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10_000)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void A_quantity_outside_1_to_9999_is_refused(int quantity) {
        // Arrange
        BomItem anyItem = Any.BomItem().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new BomItem(anyItem.Part, anyItem.Description, quantity, anyItem.Children));

        // Assert
        Assert.Equal("quantity", error.ParamName);
    }

    [Fact]
    public void Null_children_are_refused() {
        // Arrange
        BomItem anyItem = Any.BomItem().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new BomItem(anyItem.Part, anyItem.Description, anyItem.Quantity, null!));
    }

    [Fact]
    public void An_item_cannot_be_one_of_its_own_direct_components() {
        // Arrange
        BomItem anyItem = Any.BomItem().Generate();
        BomItem sameParts = Any.BomItem().WithPart(anyItem.Part).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new BomItem(anyItem.Part, anyItem.Description, anyItem.Quantity, [sameParts]));

        // Assert
        Assert.Equal("children", error.ParamName);
    }

    [Fact]
    public void An_item_cannot_be_one_of_its_own_nested_components() {
        // Arrange
        BomItem grandChild = Leaf(1, 1);
        BomItem child      = Node(2, 1, grandChild);

        // Act
        Assert.Throws<ArgumentException>(() => Any.BomItem().WithPart(Part(1)).WithChildren([child]).Generate());
    }

    [Fact]
    public void The_same_part_may_appear_in_two_different_branches() {
        // Arrange
        BomItem screwInTheCover = Leaf(7, 4);
        BomItem screwInTheBase  = Leaf(7, 2);

        // Act
        BomItem assembly = Node(1, 1, Node(2, 1, screwInTheCover), Node(3, 1, screwInTheBase));

        // Assert
        Assert.Equal(2, assembly.Children.Count);
    }

    [Fact]
    public void The_same_part_may_appear_twice_among_siblings() {
        Assert.Equal(2, Node(1, 1, Leaf(7, 1), Leaf(7, 1)).Children.Count);
    }

    [Fact]
    public void A_part_that_differs_only_by_its_variant_suffix_is_not_the_same_component() {
        // Arrange
        BomItem variant = Any.BomItem().WithPart(new PartNumber("P004217-A2")).Generate();

        // Act
        BomItem assembly = Any.BomItem().WithPart(new PartNumber("P004217")).WithChildren([variant]).Generate();

        // Assert
        Assert.Single(assembly.Children);
    }

    [Fact(Skip = "domain defect: a null element in children makes the constructor fail with NullReferenceException instead of an argument error; no seed involved")]
    public void A_null_child_is_refused_with_an_argument_error() {
        // Arrange
        BomItem anyItem = Any.BomItem().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new BomItem(anyItem.Part, anyItem.Description, anyItem.Quantity, new BomItem[] { null! }));
    }

    [Fact(Skip = "domain defect: the children list is stored by reference, so adding the item's own part to the list afterwards bypasses the cycle check; no seed involved")]
    public void Changing_the_source_list_afterwards_does_not_change_the_item() {
        // Arrange
        List<BomItem> source = [];
        BomItem       item   = Any.BomItem().WithChildren(source).Generate();

        // Act
        source.Add(Leaf(1, 1));

        // Assert
        Assert.True(item.IsLeaf);
    }

    // --- shape ------------------------------------------------------------------------------

    [Fact]
    public void An_item_without_children_is_a_leaf_of_depth_one() {
        // Arrange
        BomItem leaf = Any.BomItem().WithChildren([]).Generate();

        // Assert
        Assert.True(leaf.IsLeaf);
        Assert.Equal(1, leaf.Depth);
    }

    [Fact]
    public void An_item_with_children_is_not_a_leaf() {
        Assert.False(Node(1, 1, Leaf(2, 1)).IsLeaf);
    }

    [Fact]
    public void The_depth_counts_the_deepest_branch() {
        // Arrange: 1 -> (2 -> 3 -> 4), 5
        BomItem tree = Node(1, 1, Node(2, 1, Node(3, 1, Leaf(4, 1))), Leaf(5, 1));

        // Assert
        Assert.Equal(4, tree.Depth);
        Assert.Equal(1, tree.Children[1].Depth);
        Assert.Equal(3, tree.Children[0].Depth);
    }

    [Fact]
    public void An_item_contains_its_own_part() {
        // Arrange
        BomItem anyItem = Any.BomItem().Generate();

        // Assert
        Assert.True(anyItem.Contains(anyItem.Part));
    }

    [Fact]
    public void An_item_contains_the_parts_of_all_its_descendants() {
        // Arrange
        BomItem tree = Node(1, 1, Node(2, 1, Leaf(3, 1)), Leaf(4, 1));

        // Assert
        Assert.True(tree.Contains(Part(2)));
        Assert.True(tree.Contains(Part(3)));
        Assert.True(tree.Contains(Part(4)));
    }

    [Fact]
    public void An_item_does_not_contain_a_part_that_is_nowhere_in_it() {
        // Arrange
        BomItem tree = Node(1, 1, Node(2, 1, Leaf(3, 1)), Leaf(4, 1));

        // Assert
        Assert.False(tree.Contains(Part(5)));
    }

    [Fact]
    public void The_items_of_a_tree_do_not_contain_their_ancestors() {
        // Arrange
        BomItem tree = Node(1, 1, Leaf(2, 1));

        // Assert
        Assert.False(tree.Children[0].Contains(Part(1)));
    }

    // --- quantities -------------------------------------------------------------------------

    [Fact]
    public void The_total_of_the_item_s_own_part_is_its_own_quantity() {
        // Arrange
        BomItem item = Node(1, 3, Leaf(2, 5));

        // Assert
        Assert.Equal(3, item.TotalQuantityOf(Part(1)));
    }

    [Fact]
    public void The_total_of_a_direct_child_is_multiplied_by_the_parent_quantity() {
        // Arrange: 2 x P1, each made of 3 x P2
        BomItem item = Node(1, 2, Leaf(2, 3));

        // Assert
        Assert.Equal(6, item.TotalQuantityOf(Part(2)));
    }

    [Fact]
    public void The_total_multiplies_down_a_nested_tree() {
        // Arrange: 2 x P1, each 3 x P2, each 4 x P3
        BomItem tree = Node(1, 2, Node(2, 3, Leaf(3, 4)));

        // Assert
        Assert.Equal(24, tree.TotalQuantityOf(Part(3)));
        Assert.Equal(6, tree.TotalQuantityOf(Part(2)));
    }

    [Fact]
    public void The_total_sums_a_part_used_in_several_branches() {
        // Arrange: 2 x P1 ( 3 x P2 ( 4 x P9 ), 5 x P9 )
        BomItem tree = Node(1, 2, Node(2, 3, Leaf(9, 4)), Leaf(9, 5));

        // Assert: 2 * (3*4 + 5)
        Assert.Equal(34, tree.TotalQuantityOf(Part(9)));
    }

    [Fact]
    public void The_total_of_an_absent_part_is_zero() {
        // Arrange
        BomItem tree = Node(1, 2, Node(2, 3, Leaf(3, 4)));

        // Assert
        Assert.Equal(0, tree.TotalQuantityOf(Part(99)));
    }

    [Fact]
    public void The_total_of_a_leaf_for_another_part_is_zero() {
        Assert.Equal(0, Leaf(1, 7).TotalQuantityOf(Part(2)));
    }

    [Fact(Skip = "domain defect: quantities are multiplied in unchecked int arithmetic, so a deep tree of legal quantities (9999 x 9999 x 9999) silently wraps around to a wrong total; no seed involved")]
    public void A_total_beyond_the_range_of_an_int_is_not_silently_wrong() {
        // Arrange
        BomItem tree = Node(1, 9_999, Node(2, 9_999, Leaf(3, 9_999)));

        // Act / Assert
        Assert.ThrowsAny<OverflowException>(() => tree.TotalQuantityOf(Part(3)));
    }

    // --- flattening -------------------------------------------------------------------------

    [Fact]
    public void A_leaf_flattens_to_itself_with_its_own_quantity() {
        // Arrange
        BomItem leaf = Leaf(1, 7);

        // Act
        IReadOnlyDictionary<PartNumber, int> flat = leaf.FlattenToLeaves();

        // Assert
        KeyValuePair<PartNumber, int> only = Assert.Single(flat);
        Assert.Equal(Part(1), only.Key);
        Assert.Equal(7, only.Value);
    }

    [Fact]
    public void Flattening_multiplies_the_quantities_down_to_the_leaves() {
        // Arrange: 2 x P1 ( 3 x P2 ( 4 x P3 ), 5 x P4 )
        BomItem tree = Node(1, 2, Node(2, 3, Leaf(3, 4)), Leaf(4, 5));

        // Act
        IReadOnlyDictionary<PartNumber, int> flat = tree.FlattenToLeaves();

        // Assert: the assemblies P1 and P2 are not leaves
        Assert.Equal(2, flat.Count);
        Assert.Equal(24, flat[Part(3)]);
        Assert.Equal(10, flat[Part(4)]);
    }

    [Fact]
    public void A_leaf_in_several_places_has_its_quantities_summed() {
        // Arrange: 2 x P1 ( 3 x P2 ( 4 x P9 ), 5 x P9 )
        BomItem tree = Node(1, 2, Node(2, 3, Leaf(9, 4)), Leaf(9, 5));

        // Act
        IReadOnlyDictionary<PartNumber, int> flat = tree.FlattenToLeaves();

        // Assert
        Assert.Equal(34, Assert.Single(flat).Value);
    }

    [Fact]
    public void The_flattened_total_of_a_leaf_agrees_with_the_total_quantity_of_that_part() {
        // Arrange
        BomItem tree = Node(1, 2, Node(2, 3, Leaf(9, 4)), Leaf(9, 5), Leaf(8, 6));

        // Act
        IReadOnlyDictionary<PartNumber, int> flat = tree.FlattenToLeaves();

        // Assert
        Assert.Equal(tree.TotalQuantityOf(Part(9)), flat[Part(9)]);
        Assert.Equal(tree.TotalQuantityOf(Part(8)), flat[Part(8)]);
    }

    [Fact]
    public void Flattening_does_not_change_the_item() {
        // Arrange
        BomItem tree = Node(1, 2, Leaf(2, 3));

        // Act
        tree.FlattenToLeaves();
        tree.FlattenToLeaves();

        // Assert
        Assert.Equal(6, tree.FlattenToLeaves()[Part(2)]);
    }

    [Fact]
    public void Variants_of_a_part_are_different_leaves() {
        // Arrange
        BomItem plain   = Any.BomItem().WithPart(new PartNumber("P000009")).WithQuantity(1).Generate();
        BomItem variant = Any.BomItem().WithPart(new PartNumber("P000009-A2")).WithQuantity(2).Generate();
        BomItem tree    = Node(1, 1, plain, variant);

        // Act
        IReadOnlyDictionary<PartNumber, int> flat = tree.FlattenToLeaves();

        // Assert
        Assert.Equal(2, flat.Count);
    }

}
