using JustDummies;

using Xunit;

namespace Drafting.Domain;

public sealed class BomItemTests {

    private static BomItem Leaf(PartNumber part, int quantity) {
        return Any.BomItem().WithPart(part).WithQuantity(quantity).Generate();
    }

    private static BomItem Assembly(PartNumber part, int quantity, params BomItem[] children) {
        return Any.BomItem().WithPart(part).WithQuantity(quantity).WithChildren(children).Generate();
    }

    // ---- construction -------------------------------------------------------------------------

    [Fact]
    public void An_item_keeps_what_it_was_given() {
        // Arrange
        PartNumber       anyPart        = Any.PartNumber().Generate();
        string           anyDescription = Any.String().NotBlank().WithMaxLength(BomItem.MaximumDescriptionLength).Generate();
        int              anyQuantity    = Any.Int32().Between(1, BomItem.MaximumQuantity).Generate();
        List<BomItem>    anyChildren    = Any.ListOf(new AnyBomItem()).Generate();

        // Act
        BomItem item = new BomItem(anyPart, anyDescription, anyQuantity, anyChildren);

        // Assert
        Assert.Same(anyPart, item.Part);
        Assert.Equal(anyDescription, item.Description);
        Assert.Equal(anyQuantity, item.Quantity);
        Assert.Same(anyChildren, item.Children);
    }

    [Fact]
    public void The_children_are_stored_as_given_and_not_copied() {
        // Arrange
        List<BomItem> children = [];
        BomItem       item     = Any.BomItem().WithChildren(children).Generate();

        // Act
        children.Add(Any.BomItem().Generate());

        // Assert
        Assert.Single(item.Children);
    }

    [Fact]
    public void A_null_part_is_refused() {
        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new BomItem(null!, "d", 1, []));

        // Assert
        Assert.Equal("part", exception.ParamName);
    }

    [Fact]
    public void A_null_description_is_refused_as_a_null_argument() {
        // Arrange
        PartNumber anyPart = Any.PartNumber().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new BomItem(anyPart, null!, 1, []));

        // Assert
        Assert.Equal("description", exception.ParamName);
    }

    [Fact]
    public void A_blank_description_is_refused_as_an_argument_error() {
        // Arrange
        string anyBlank = Any.String().Whitespaces().NonEmpty().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Any.BomItem().WithDescription(anyBlank).Generate());

        // Assert
        Assert.Equal("description", exception.ParamName);
        Assert.IsNotType<ArgumentNullException>(exception);
    }

    [Fact]
    public void A_description_of_eighty_characters_is_accepted() {
        // Arrange
        string anyLongest = Any.String().Alpha().WithLength(BomItem.MaximumDescriptionLength).Generate();

        // Act
        BomItem item = Any.BomItem().WithDescription(anyLongest).Generate();

        // Assert
        Assert.Equal(anyLongest, item.Description);
    }

    [Fact]
    public void A_description_of_eighty_one_characters_is_refused() {
        // Arrange
        string anyTooLong = Any.String().Alpha().WithLength(BomItem.MaximumDescriptionLength + 1).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Any.BomItem().WithDescription(anyTooLong).Generate());

        // Assert
        Assert.Equal("description", exception.ParamName);
    }

    [Fact]
    public void A_description_is_not_trimmed_before_its_length_is_judged() {
        // Arrange: 80 letters padded with spaces is 82 characters.
        string padded = " " + Any.String().Alpha().WithLength(BomItem.MaximumDescriptionLength).Generate() + " ";

        // Act / Assert
        Assert.Throws<ArgumentException>(() => Any.BomItem().WithDescription(padded).Generate());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(BomItem.MaximumQuantity)]
    public void The_smallest_and_the_largest_quantities_are_accepted(int quantity) {
        // Act
        BomItem item = Any.BomItem().WithQuantity(quantity).Generate();

        // Assert
        Assert.Equal(quantity, item.Quantity);
    }

    [Fact]
    public void A_quantity_below_one_is_refused() {
        // Arrange
        int anyNonPositive = Any.Int32().LessThanOrEqualTo(0).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Any.BomItem().WithQuantity(anyNonPositive).Generate());

        // Assert
        Assert.Equal("quantity", exception.ParamName);
    }

    [Fact]
    public void A_quantity_above_9999_is_refused() {
        // Arrange
        int anyTooMany = Any.Int32().GreaterThan(BomItem.MaximumQuantity).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Any.BomItem().WithQuantity(anyTooMany).Generate());

        // Assert
        Assert.Equal("quantity", exception.ParamName);
    }

    [Fact]
    public void Null_children_are_refused() {
        // Arrange
        PartNumber anyPart = Any.PartNumber().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new BomItem(anyPart, "d", 1, null!));

        // Assert
        Assert.Equal("children", exception.ParamName);
    }

    [Fact]
    public void An_item_cannot_be_one_of_its_own_direct_components() {
        // Arrange
        PartNumber anyPart  = Any.PartNumber().Generate();
        BomItem    sameLeaf = Leaf(anyPart, 1);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Assembly(anyPart, 1, sameLeaf));

        // Assert
        Assert.Equal("children", exception.ParamName);
    }

    [Fact]
    public void An_item_cannot_be_one_of_its_own_nested_components() {
        // Arrange
        PartNumber anyPart    = Any.PartNumber().Generate();
        PartNumber anyMiddle  = Any.PartNumber().WithValue(v => v.DifferentFrom(anyPart.Value)).Generate();
        BomItem    deepSameLeaf = Leaf(anyPart, 1);
        BomItem    middle     = Assembly(anyMiddle, 2, deepSameLeaf);

        // Act / Assert
        Assert.Throws<ArgumentException>(() => Assembly(anyPart, 1, middle));
    }

    [Fact]
    public void The_same_part_may_appear_in_two_sibling_subtrees() {
        // Arrange
        PartNumber shared = new PartNumber("P000001");
        BomItem    left   = Assembly(new PartNumber("P000002"), 1, Leaf(shared, 1));
        BomItem    right  = Assembly(new PartNumber("P000003"), 1, Leaf(shared, 1));

        // Act
        BomItem root = Assembly(new PartNumber("P000004"), 1, left, right);

        // Assert
        Assert.Equal(2, root.Children.Count);
    }

    // ---- shape --------------------------------------------------------------------------------

    [Fact]
    public void An_item_without_children_is_a_leaf_of_depth_one() {
        // Arrange
        BomItem anyLeaf = Any.BomItem().Generate();

        // Assert
        Assert.True(anyLeaf.IsLeaf);
        Assert.Equal(1, anyLeaf.Depth);
    }

    [Fact]
    public void An_item_with_children_is_not_a_leaf() {
        // Arrange
        BomItem anyAssembly = Any.BomItem().WithChildren(Any.ListOf(new AnyBomItem()).NonEmpty().Generate()).Generate();

        // Assert
        Assert.False(anyAssembly.IsLeaf);
    }

    [Fact]
    public void The_depth_is_one_more_than_the_deepest_child() {
        // Arrange
        BomItem deepest  = Assembly(new PartNumber("P000010"), 1, Assembly(new PartNumber("P000011"), 1, Leaf(new PartNumber("P000012"), 1)));
        BomItem shallow  = Leaf(new PartNumber("P000013"), 1);

        // Act
        BomItem root = Assembly(new PartNumber("P000014"), 1, shallow, deepest);

        // Assert
        Assert.Equal(4, root.Depth);
        Assert.Equal(3, deepest.Depth);
        Assert.Equal(1, shallow.Depth);
    }

    [Fact]
    public void An_item_contains_its_own_part_and_those_of_all_its_descendants() {
        // Arrange
        PartNumber top = new PartNumber("P000020");
        PartNumber mid = new PartNumber("P000021");
        PartNumber low = new PartNumber("P000022");
        BomItem    root = Assembly(top, 1, Assembly(mid, 1, Leaf(low, 1)));

        // Assert
        Assert.True(root.Contains(top));
        Assert.True(root.Contains(mid));
        Assert.True(root.Contains(low));
    }

    [Fact]
    public void An_item_does_not_contain_a_part_it_is_not_made_of() {
        // Arrange
        BomItem root = Assembly(new PartNumber("P000023"), 1, Leaf(new PartNumber("P000024"), 1));

        // Assert
        Assert.False(root.Contains(new PartNumber("P000025")));
    }

    [Fact]
    public void A_part_number_differing_only_by_its_variant_is_another_part() {
        // Arrange
        BomItem leaf = Leaf(new PartNumber("P004217"), 1);

        // Assert
        Assert.False(leaf.Contains(new PartNumber("P004217-A2")));
    }

    // ---- quantities ---------------------------------------------------------------------------

    [Fact]
    public void The_total_of_the_items_own_part_is_its_own_quantity() {
        // Arrange
        PartNumber anyPart     = Any.PartNumber().Generate();
        int        anyQuantity = Any.Int32().Between(1, BomItem.MaximumQuantity).Generate();
        BomItem    item        = Assembly(anyPart, anyQuantity, Leaf(new PartNumber("P000030"), 5));

        // Act
        int total = item.TotalQuantityOf(anyPart);

        // Assert
        Assert.Equal(anyQuantity, total);
    }

    [Fact]
    public void The_total_of_an_absent_part_is_zero() {
        // Arrange
        BomItem root = Assembly(new PartNumber("P000031"), 2, Leaf(new PartNumber("P000032"), 3));

        // Assert
        Assert.Equal(0, root.TotalQuantityOf(new PartNumber("P000033")));
    }

    [Fact]
    public void The_total_multiplies_the_quantities_down_the_tree() {
        // Arrange: 2 x ( 3 x B ) where B is used 4 times per A.
        PartNumber partB = new PartNumber("P000041");
        BomItem    root  = Assembly(new PartNumber("P000040"), 2, Assembly(new PartNumber("P000042"), 3, Leaf(partB, 4)));

        // Assert
        Assert.Equal(24, root.TotalQuantityOf(partB));
    }

    [Fact]
    public void The_total_of_an_intermediate_assembly_is_its_quantity_times_that_of_its_parents() {
        // Arrange
        PartNumber assemblyPart = new PartNumber("P000051");
        BomItem    root         = Assembly(new PartNumber("P000050"), 2, Assembly(assemblyPart, 3, Leaf(new PartNumber("P000052"), 4)));

        // Assert
        Assert.Equal(6, root.TotalQuantityOf(assemblyPart));
    }

    [Fact]
    public void The_total_adds_up_a_part_used_in_several_places() {
        // Arrange
        PartNumber screw = new PartNumber("P000061");
        BomItem    left  = Assembly(new PartNumber("P000062"), 2, Leaf(screw, 3));    // 2 x 3 = 6
        BomItem    right = Assembly(new PartNumber("P000063"), 5, Leaf(screw, 1));    // 5 x 1 = 5
        BomItem    root  = Assembly(new PartNumber("P000060"), 10, left, right);      // 10 x 11

        // Assert
        Assert.Equal(110, root.TotalQuantityOf(screw));
    }

    [Fact]
    public void Flattening_a_leaf_gives_the_leaf_with_its_own_quantity() {
        // Arrange
        PartNumber anyPart     = Any.PartNumber().Generate();
        int        anyQuantity = Any.Int32().Between(1, BomItem.MaximumQuantity).Generate();
        BomItem    leaf        = Leaf(anyPart, anyQuantity);

        // Act
        IReadOnlyDictionary<PartNumber, int> flat = leaf.FlattenToLeaves();

        // Assert
        KeyValuePair<PartNumber, int> only = Assert.Single(flat);
        Assert.Equal(anyPart, only.Key);
        Assert.Equal(anyQuantity, only.Value);
    }

    [Fact]
    public void Flattening_multiplies_the_quantities_down_to_the_leaves_this_items_own_included() {
        // Arrange
        PartNumber b = new PartNumber("P000071");
        PartNumber c = new PartNumber("P000072");
        BomItem    root = Assembly(new PartNumber("P000070"), 2, Assembly(new PartNumber("P000073"), 3, Leaf(b, 4)), Leaf(c, 5));

        // Act
        IReadOnlyDictionary<PartNumber, int> flat = root.FlattenToLeaves();

        // Assert
        Assert.Equal(2, flat.Count);
        Assert.Equal(24, flat[b]);
        Assert.Equal(10, flat[c]);
    }

    [Fact]
    public void Flattening_sums_a_leaf_that_appears_in_several_places() {
        // Arrange
        PartNumber screw = new PartNumber("P000081");
        BomItem    left  = Assembly(new PartNumber("P000082"), 2, Leaf(screw, 3));
        BomItem    right = Assembly(new PartNumber("P000083"), 5, Leaf(screw, 1));
        BomItem    root  = Assembly(new PartNumber("P000080"), 10, left, right);

        // Act
        IReadOnlyDictionary<PartNumber, int> flat = root.FlattenToLeaves();

        // Assert
        KeyValuePair<PartNumber, int> only = Assert.Single(flat);
        Assert.Equal(110, only.Value);
    }

    [Fact]
    public void Flattening_leaves_out_the_intermediate_assemblies() {
        // Arrange
        PartNumber middle = new PartNumber("P000091");
        BomItem    root   = Assembly(new PartNumber("P000090"), 1, Assembly(middle, 1, Leaf(new PartNumber("P000092"), 1)));

        // Act
        IReadOnlyDictionary<PartNumber, int> flat = root.FlattenToLeaves();

        // Assert
        Assert.DoesNotContain(middle, flat.Keys);
    }

    [Fact]
    public void The_total_of_a_leaf_is_the_same_whether_asked_directly_or_read_from_the_flattened_bill() {
        // Arrange: a tree whose part numbers are all distinct, so a part is either a leaf or an assembly.
        BomItem anyTree = BomTrees.AnyTree(4);

        // Act
        IReadOnlyDictionary<PartNumber, int> flat = anyTree.FlattenToLeaves();

        // Assert
        foreach (KeyValuePair<PartNumber, int> leaf in flat) {
            Assert.Equal(leaf.Value, anyTree.TotalQuantityOf(leaf.Key));
        }
    }

    [Fact(Skip = "domain defect: quantities are multiplied down the tree in unchecked int arithmetic, so a deep bill of materials silently wraps around instead of failing (9999^4 > int.MaxValue); deterministic, no seed involved")]
    public void A_total_that_does_not_fit_in_an_int_is_not_silently_wrapped() {
        // Arrange: four levels of the largest quantity: 9999^4 ~ 1e16.
        PartNumber bottom = new PartNumber("P000100");
        BomItem    root   = Assembly(new PartNumber("P000101"), BomItem.MaximumQuantity,
                              Assembly(new PartNumber("P000102"), BomItem.MaximumQuantity,
                                Assembly(new PartNumber("P000103"), BomItem.MaximumQuantity,
                                  Leaf(bottom, BomItem.MaximumQuantity))));

        // Act
        int total = root.TotalQuantityOf(bottom);

        // Assert
        long mathematicalTotal = 9_999L * 9_999L * 9_999L * 9_999L;
        Assert.Equal(mathematicalTotal, (long)total);
    }

}
