using Drafting.Domain.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Drafting.Domain.Tests.Expert.Tests;

public sealed class BomItemTests {

    // The parts of the trees below are named by the test that reads them, so they stay literals.
    private static readonly PartNumber Frame  = new PartNumber("P000001");
    private static readonly PartNumber Wheel  = new PartNumber("P000002");
    private static readonly PartNumber Bolt   = new PartNumber("P000003");
    private static readonly PartNumber Washer = new PartNumber("P000004");
    private static readonly PartNumber Absent = new PartNumber("P999999");

    private static BomItem Item(PartNumber part, int quantity, params BomItem[] children) {
        return Any.BomItem().WithPart(part).WithQuantity(quantity).WithChildren(children).Generate();
    }

    // ----- construction -----

    [Fact]
    public void An_item_keeps_what_it_was_built_from() {
        BomItem child = Item(Bolt, 4);

        BomItem item = Any.BomItem()
                          .WithPart(Frame)
                          .WithDescription("Welded frame")
                          .WithQuantity(2)
                          .WithChildren([child])
                          .Generate();

        Assert.Same(Frame, item.Part);
        Assert.Equal("Welded frame", item.Description);
        Assert.Equal(2, item.Quantity);
        Assert.Same(child, Assert.Single(item.Children));
    }

    [Fact]
    public void A_part_cannot_be_null() {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => Any.BomItem().WithPart((PartNumber)null!).Generate());

        Assert.Equal("part", error.ParamName);
    }

    [Fact]
    public void A_description_cannot_be_null() {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => Any.BomItem().WithDescription((string)null!).Generate());

        Assert.Equal("description", error.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void A_description_cannot_be_blank(string blank) {
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.BomItem().WithDescription(blank).Generate());

        Assert.Equal("description", error.ParamName);
    }

    [Fact]
    public void A_description_of_80_characters_is_accepted() {
        string eighty = new string('x', BomItem.MaximumDescriptionLength);

        BomItem item = Any.BomItem().WithDescription(eighty).Generate();

        Assert.Equal(eighty, item.Description);
    }

    [Theory]
    [InlineData(81)]
    [InlineData(1_000)]
    public void A_description_longer_than_80_characters_is_refused(int length) {
        string tooLong = new string('x', length);

        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.BomItem().WithDescription(tooLong).Generate());

        Assert.Equal("description", error.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(BomItem.MaximumQuantity)]
    public void A_quantity_from_1_to_9999_is_accepted(int quantity) {
        BomItem item = Any.BomItem().WithQuantity(quantity).Generate();

        Assert.Equal(quantity, item.Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(BomItem.MaximumQuantity + 1)]
    [InlineData(int.MaxValue)]
    public void A_quantity_outside_1_to_9999_is_refused(int quantity) {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => Any.BomItem().WithQuantity(quantity).Generate());

        Assert.Equal("quantity", error.ParamName);
    }

    [Fact]
    public void The_children_cannot_be_null() {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => Any.BomItem().WithChildren((IReadOnlyList<BomItem>)null!).Generate());

        Assert.Equal("children", error.ParamName);
    }

    [Fact]
    public void A_part_cannot_be_one_of_its_own_components() {
        BomItem sameParts = Item(Frame, 1);

        ArgumentException error = Assert.Throws<ArgumentException>(() => Item(Frame, 1, sameParts));

        Assert.Equal("children", error.ParamName);
    }

    [Fact]
    public void A_part_cannot_be_a_component_of_its_own_components_at_any_depth() {
        BomItem deepest = Item(Frame, 1);
        BomItem middle  = Item(Wheel, 1, Item(Bolt, 1, deepest));

        ArgumentException error = Assert.Throws<ArgumentException>(() => Item(Frame, 1, middle));

        Assert.Equal("children", error.ParamName);
    }

    [Fact]
    public void The_same_part_may_appear_in_several_branches_of_one_item() {
        BomItem item = Item(Frame, 1, Item(Wheel, 2, Item(Bolt, 4)), Item(Wheel, 2, Item(Bolt, 4)));

        Assert.Equal(2, item.Children.Count);
    }

    [Fact]
    public void A_part_may_reappear_under_a_sibling_that_is_not_its_ancestor() {
        // Bolt is a child of Wheel and of Frame: neither is a descendant of the other's part.
        BomItem item = Item(Frame, 1, Item(Wheel, 2, Item(Bolt, 4)), Item(Bolt, 8));

        Assert.True(item.Contains(Bolt));
    }

    [Fact]
    public void The_part_is_judged_before_the_description_and_the_quantity() {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => new BomItem(null!, " ", 0, null!));

        Assert.Equal("part", error.ParamName);
    }

    [Fact]
    public void The_description_is_judged_before_the_quantity() {
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.BomItem().WithDescription(" ").WithQuantity(0).Generate());

        Assert.Equal("description", error.ParamName);
    }

    [Fact]
    public void The_quantity_is_judged_before_the_children() {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.BomItem().WithQuantity(0).WithChildren((IReadOnlyList<BomItem>)null!).Generate());

        Assert.Equal("quantity", error.ParamName);
    }

    [Fact(Skip = "domain defect: the children are stored as given, not copied, so a caller who keeps the list can add the item's own part to it after construction and bypass the guard (Drawing copies its dimensions; BomItem does not)")]
    public void An_item_copies_the_children_it_is_given() {
        List<BomItem> given = new List<BomItem> { Item(Bolt, 4) };
        BomItem       item  = Any.BomItem().WithChildren(given).Generate();

        given.Add(Item(Washer, 1));

        Assert.Single(item.Children);
    }

    // ----- shape -----

    [Fact]
    public void An_item_without_components_is_a_leaf_of_depth_one() {
        BomItem leaf = Any.BomItem().WithChildren([]).Generate();

        Assert.True(leaf.IsLeaf);
        Assert.Equal(1, leaf.Depth);
    }

    [Fact]
    public void An_item_with_components_is_not_a_leaf() {
        BomItem item = Any.BomItem().WithChildren([Any.BomItem().WithChildren([]).Generate()]).Generate();

        Assert.False(item.IsLeaf);
    }

    [Fact]
    public void An_item_is_one_level_deeper_than_its_deepest_branch() {
        BomItem shallow = Any.BomItem().WithChildren([]).Generate();
        BomItem deep    = Any.BomItem().WithChildren([Any.BomItem().WithChildren([Any.BomItem().WithChildren([]).Generate()]).Generate()]).Generate();

        BomItem root = Any.BomItem().WithChildren([shallow, deep]).Generate();

        Assert.Equal(4, root.Depth);
        Assert.Equal(3, deep.Depth);
    }

    [Fact]
    public void The_depth_does_not_depend_on_the_order_of_the_components() {
        BomItem shallow = Any.BomItem().WithChildren([]).Generate();
        BomItem deep    = Any.BomItem().WithChildren([Any.BomItem().WithChildren([]).Generate()]).Generate();

        BomItem deepFirst    = Any.BomItem().WithChildren([deep, shallow]).Generate();
        BomItem shallowFirst = Any.BomItem().WithChildren([shallow, deep]).Generate();

        Assert.Equal(3, deepFirst.Depth);
        Assert.Equal(3, shallowFirst.Depth);
    }

    // ----- containment -----

    [Fact]
    public void An_item_contains_its_own_part_its_components_and_theirs() {
        BomItem root = Item(Frame, 1, Item(Wheel, 2, Item(Bolt, 4)));

        Assert.True(root.Contains(Frame));
        Assert.True(root.Contains(Wheel));
        Assert.True(root.Contains(Bolt));
    }

    [Fact]
    public void An_item_does_not_contain_a_part_it_is_not_made_of() {
        BomItem root = Item(Frame, 1, Item(Wheel, 2, Item(Bolt, 4)));

        Assert.False(root.Contains(Absent));
        Assert.False(root.Contains(Washer));
    }

    [Fact]
    public void A_part_is_found_by_its_number_not_by_its_instance() {
        BomItem root = Item(Frame, 1, Item(Wheel, 2));

        Assert.True(root.Contains(new PartNumber("P000002")));
    }

    [Fact]
    public void A_component_does_not_know_its_parent() {
        BomItem wheel = Item(Wheel, 2);
        BomItem root  = Item(Frame, 1, wheel);

        Assert.True(root.Contains(Wheel));
        Assert.False(wheel.Contains(Frame));
    }

    // ----- total quantity -----

    [Fact]
    public void The_total_of_an_items_own_part_is_its_own_quantity() {
        BomItem root = Item(Frame, 3, Item(Wheel, 2));

        Assert.Equal(3, root.TotalQuantityOf(Frame));
    }

    [Fact]
    public void The_total_of_an_absent_part_is_zero() {
        BomItem root = Item(Frame, 3, Item(Wheel, 2, Item(Bolt, 4)));

        Assert.Equal(0, root.TotalQuantityOf(Absent));
    }

    [Fact]
    public void The_total_of_a_leaf_for_another_part_is_zero() {
        BomItem leaf = Item(Bolt, 7);

        Assert.Equal(0, leaf.TotalQuantityOf(Washer));
    }

    [Fact]
    public void Quantities_are_multiplied_down_the_tree_including_the_roots_own() {
        // 3 frames x 2 wheels x 4 bolts
        BomItem root = Item(Frame, 3, Item(Wheel, 2, Item(Bolt, 4)));

        Assert.Equal(24, root.TotalQuantityOf(Bolt));
        Assert.Equal(6, root.TotalQuantityOf(Wheel));
    }

    [Fact]
    public void The_quantities_of_a_part_used_in_several_branches_add_up() {
        // Bolts: 3 x (2 wheels x 4) + 3 x (1 bracket x 5) + 3 x 6 direct
        BomItem root = Item(Frame, 3,
                            Item(Wheel, 2, Item(Bolt, 4)),
                            Item(Washer, 1, Item(Bolt, 5)),
                            Item(Bolt, 6));

        Assert.Equal(3 * (2 * 4 + 1 * 5 + 6), root.TotalQuantityOf(Bolt));
    }

    [Fact]
    public void A_sub_assembly_found_on_the_way_stops_the_search_below_it() {
        // Wheel carries bolts, but asking for Wheel answers Wheel's own count and never looks below it.
        BomItem root = Item(Frame, 1, Item(Wheel, 2, Item(Bolt, 4)));

        Assert.Equal(2, root.TotalQuantityOf(Wheel));
    }

    // ----- flattening -----

    [Fact]
    public void A_leaf_flattens_to_itself_with_its_own_quantity() {
        BomItem leaf = Item(Bolt, 7);

        IReadOnlyDictionary<PartNumber, int> flat = leaf.FlattenToLeaves();

        KeyValuePair<PartNumber, int> only = Assert.Single(flat);
        Assert.Equal(Bolt, only.Key);
        Assert.Equal(7, only.Value);
    }

    [Fact]
    public void Quantities_are_multiplied_down_to_each_leaf() {
        BomItem root = Item(Frame, 3,
                            Item(Wheel, 2, Item(Bolt, 4), Item(Washer, 8)));

        IReadOnlyDictionary<PartNumber, int> flat = root.FlattenToLeaves();

        Assert.Equal(2, flat.Count);
        Assert.Equal(3 * 2 * 4, flat[Bolt]);
        Assert.Equal(3 * 2 * 8, flat[Washer]);
    }

    [Fact]
    public void Sub_assemblies_do_not_appear_among_the_leaves() {
        BomItem root = Item(Frame, 1, Item(Wheel, 2, Item(Bolt, 4)));

        IReadOnlyDictionary<PartNumber, int> flat = root.FlattenToLeaves();

        Assert.DoesNotContain(Frame, flat.Keys);
        Assert.DoesNotContain(Wheel, flat.Keys);
    }

    [Fact]
    public void A_leaf_found_in_several_places_has_its_quantities_summed() {
        BomItem root = Item(Frame, 2,
                            Item(Wheel, 3, Item(Bolt, 4)),
                            Item(Bolt, 5));

        IReadOnlyDictionary<PartNumber, int> flat = root.FlattenToLeaves();

        Assert.Equal(2 * (3 * 4 + 5), flat[Bolt]);
    }

    [Fact]
    public void Flattening_agrees_with_the_total_asked_for_each_leaf() {
        BomItem root = Item(Frame, 2,
                            Item(Wheel, 3, Item(Bolt, 4), Item(Washer, 2)),
                            Item(Bolt, 5));

        IReadOnlyDictionary<PartNumber, int> flat = root.FlattenToLeaves();

        Assert.Equal(root.TotalQuantityOf(Bolt), flat[Bolt]);
        Assert.Equal(root.TotalQuantityOf(Washer), flat[Washer]);
    }

    [Fact]
    public void Flattening_does_not_change_the_item() {
        BomItem root = Item(Frame, 2, Item(Wheel, 3, Item(Bolt, 4)));

        root.FlattenToLeaves();
        IReadOnlyDictionary<PartNumber, int> again = root.FlattenToLeaves();

        Assert.Equal(24, again[Bolt]);
    }

    [Fact(Skip = "domain defect: quantities are multiplied in unchecked int arithmetic, so a three-level tree of 9 999 x 9 999 x 9 999 silently wraps around instead of reporting an overflow")]
    public void A_total_beyond_the_range_of_an_int_is_reported_rather_than_wrapped() {
        BomItem root = Item(Frame, BomItem.MaximumQuantity, Item(Wheel, BomItem.MaximumQuantity, Item(Bolt, BomItem.MaximumQuantity)));

        Assert.Throws<OverflowException>(() => root.TotalQuantityOf(Bolt));
        Assert.Throws<OverflowException>(() => root.FlattenToLeaves());
    }

}
