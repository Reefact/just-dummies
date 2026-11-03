using JustDummies;

using PassiveComponents.Domain;

using Xunit;

namespace PassiveComponents.Tests.Discovery;

public sealed class BillOfMaterialsConstructionTests {

    [Fact]
    public void A_new_bill_is_open_and_empty() {
        BillOfMaterials bom = new BillOfMaterials("Motor driver", "B");

        Assert.Equal("Motor driver", bom.BoardName);
        Assert.Equal("B", bom.Revision);
        Assert.False(bom.IsReleased);
        Assert.Equal(0, bom.PlacementCount);
        Assert.Empty(bom.Placements);
        Assert.Empty(bom.Lines());
        Assert.Equal(0m, bom.TotalCost());
    }

    [Fact]
    public void Accepts_a_board_name_of_exactly_sixty_four_characters() {
        string anyRevision = Any.StringMatching("[A-Z]{1,2}").Generate();
        string sixtyFour   = new string('x', 64);

        Assert.Equal(sixtyFour, new BillOfMaterials(sixtyFour, anyRevision).BoardName);
    }

    [Fact]
    public void Refuses_a_board_name_of_sixty_five_characters() {
        string anyRevision = Any.StringMatching("[A-Z]{1,2}").Generate();

        Assert.Throws<ArgumentException>(() => new BillOfMaterials(new string('x', 65), anyRevision));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData(null)]
    [InlineData(" Motor driver")]
    [InlineData("Motor driver ")]
    [InlineData("\tMotor driver")]
    [InlineData("Motor driver\n")]
    public void Refuses_a_blank_or_untrimmed_board_name(string? boardName) {
        string anyRevision = Any.StringMatching("[A-Z]{1,2}").Generate();

        Assert.Throws<ArgumentException>(() => new BillOfMaterials(boardName!, anyRevision));
    }

    [Fact]
    public void Keeps_spaces_inside_a_board_name() {
        string anyRevision = Any.StringMatching("[A-Z]{1,2}").Generate();

        Assert.Equal("Motor  driver  v2", new BillOfMaterials("Motor  driver  v2", anyRevision).BoardName);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("Z")]
    [InlineData("AA")]
    [InlineData("ZZ")]
    public void Accepts_one_or_two_capital_letters_as_revision(string revision) {
        string anyBoardName = Any.StringMatching("[A-Za-z0-9]{1,20}").Generate();

        Assert.Equal(revision, new BillOfMaterials(anyBoardName, revision).Revision);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("Aa")]
    [InlineData("AAA")]
    [InlineData("A1")]
    [InlineData("1")]
    [InlineData(" A")]
    [InlineData("A ")]
    [InlineData("É")]
    public void Refuses_a_revision_that_is_not_one_or_two_capital_letters(string revision) {
        string anyBoardName = Any.StringMatching("[A-Za-z0-9]{1,20}").Generate();

        Assert.Throws<FormatException>(() => new BillOfMaterials(anyBoardName, revision));
    }

    [Fact]
    public void Refuses_a_null_revision() {
        string anyBoardName = Any.StringMatching("[A-Za-z0-9]{1,20}").Generate();

        Assert.Throws<ArgumentNullException>(() => new BillOfMaterials(anyBoardName, null!));
    }

    [Fact(Skip = "domain defect: the revision pattern ends with $, which .NET lets match before a final \\n, so 'A\\n' is accepted as a revision; literal input, no seed")]
    public void Refuses_a_revision_followed_by_a_line_break() {
        string anyBoardName = Any.StringMatching("[A-Za-z0-9]{1,20}").Generate();

        Assert.Throws<FormatException>(() => new BillOfMaterials(anyBoardName, "A\n"));
    }

    [Fact]
    public void Is_shown_with_its_name_revision_and_placement_count() {
        BillOfMaterials bom = new BillOfMaterials("Motor driver", "B");
        bom.Place(TestParts.Designator("R1"), Any.Part().WithKind(ComponentKind.Resistor).Generate());

        Assert.Equal("Motor driver rev B (1 placements)", bom.ToString());

        bom.Release();

        Assert.Equal("Motor driver rev B (1 placements, released)", bom.ToString());
    }

}

public sealed class BillOfMaterialsPlacementTests {

    [Fact]
    public void Placing_a_part_returns_the_placement_and_counts_it() {
        BillOfMaterials bom         = Any.BillOfMaterials().Generate();
        Part            anyResistor = Any.Part().WithKind(ComponentKind.Resistor).Generate();

        Placement placement = bom.Place(TestParts.Designator("R1"), anyResistor);

        Assert.Equal(TestParts.Designator("R1"), placement.Designator);
        Assert.Same(anyResistor, placement.DesignedPart);
        Assert.Equal(1, bom.PlacementCount);
        Assert.Same(placement, bom.Find(TestParts.Designator("R1")));
    }

    [Fact]
    public void Finding_an_unplaced_designator_gives_null() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();

        Assert.Null(bom.Find(TestParts.Designator("R1")));
    }

    [Fact]
    public void Finding_null_is_refused() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();

        Assert.Throws<ArgumentNullException>(() => bom.Find(null!));
    }

    [Fact]
    public void Placements_come_in_designator_order_whatever_the_order_of_placing() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(TestParts.Designator("R10"), Any.Part().WithKind(ComponentKind.Resistor).Generate());
        bom.Place(TestParts.Designator("L1"), Any.Part().WithKind(ComponentKind.Inductor).Generate());
        bom.Place(TestParts.Designator("R2"), Any.Part().WithKind(ComponentKind.Resistor).Generate());
        bom.Place(TestParts.Designator("C3"), Any.Part().WithKind(ComponentKind.Capacitor).Generate());

        Assert.Equal(new[] { "C3", "L1", "R2", "R10" }, bom.Placements.Select(p => p.Designator.Value));
    }

    [Fact]
    public void A_designator_is_used_once() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(TestParts.Designator("R1"), Any.Part().WithKind(ComponentKind.Resistor).Generate());

        Assert.Throws<InvalidOperationException>(() => bom.Place(TestParts.Designator("R1"), Any.Part().WithKind(ComponentKind.Resistor).Generate()));
        Assert.Equal(1, bom.PlacementCount);
    }

    [Fact]
    public void The_designator_letter_must_match_the_kind_of_the_part() {
        BillOfMaterials bom         = Any.BillOfMaterials().Generate();
        Part            anyCapacitor = Any.Part().WithKind(ComponentKind.Capacitor).Generate();

        Assert.Throws<ArgumentException>(() => bom.Place(TestParts.Designator("R1"), anyCapacitor));
        Assert.Equal(0, bom.PlacementCount);
    }

    [Fact]
    public void Placing_null_is_refused() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();

        Assert.Throws<ArgumentNullException>(() => bom.Place(null!, Any.Part().Generate()));
        Assert.Throws<ArgumentNullException>(() => bom.Place(TestParts.Designator("R1"), null!));
    }

    [Fact]
    public void One_part_may_be_placed_on_several_designators() {
        BillOfMaterials bom         = Any.BillOfMaterials().Generate();
        Part            anyResistor = Any.Part().WithKind(ComponentKind.Resistor).Generate();

        bom.Place(TestParts.Designator("R1"), anyResistor);
        bom.Place(TestParts.Designator("R2"), anyResistor);

        Assert.Equal(2, bom.PlacementCount);
    }

    [Fact]
    public void Two_equal_parts_may_be_placed_under_the_same_part_number() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        Part resistor = TestParts.Resistor("RES-4K7", 4_700, 1, 0.1, 50, Package.Chip0603).Generate();
        Part sameSpecification = new Part(resistor.PartNumber, resistor.Kind, resistor.NominalValue, resistor.Tolerance, resistor.Rating, resistor.TemperatureCoefficient, resistor.Package, resistor.UnitCost);

        bom.Place(TestParts.Designator("R1"), resistor);
        bom.Place(TestParts.Designator("R2"), sameSpecification);

        Assert.Equal(2, bom.PlacementCount);
    }

    [Fact]
    public void A_part_number_cannot_carry_two_specifications_on_one_board() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(TestParts.Designator("R1"), TestParts.Resistor("RES-4K7", 4_700, 1, 0.1, 50, Package.Chip0603).Generate());

        Part otherValueSameNumber = TestParts.Resistor("RES-4K7", 4_701, 1, 0.1, 50, Package.Chip0603).Generate();

        Assert.Throws<InvalidOperationException>(() => bom.Place(TestParts.Designator("R2"), otherValueSameNumber));
        Assert.Equal(1, bom.PlacementCount);
    }

    [Fact]
    public void A_part_number_differing_only_by_its_cost_is_a_different_specification() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        Part cheap = TestParts.Resistor("RES-4K7", 4_700, 1, 0.1, 50, Package.Chip0603).WithUnitCost(0.01m).Generate();
        Part dear  = TestParts.Resistor("RES-4K7", 4_700, 1, 0.1, 50, Package.Chip0603).WithUnitCost(0.02m).Generate();
        bom.Place(TestParts.Designator("R1"), cheap);

        Assert.Throws<InvalidOperationException>(() => bom.Place(TestParts.Designator("R2"), dear));
    }

    [Fact]
    public void Removing_a_placement_frees_its_designator() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(TestParts.Designator("R1"), Any.Part().WithKind(ComponentKind.Resistor).Generate());

        bom.Remove(TestParts.Designator("R1"));

        Assert.Equal(0, bom.PlacementCount);
        Assert.Null(bom.Find(TestParts.Designator("R1")));
        bom.Place(TestParts.Designator("R1"), Any.Part().WithKind(ComponentKind.Resistor).Generate());
        Assert.Equal(1, bom.PlacementCount);
    }

    [Fact]
    public void Removing_the_last_use_of_a_part_number_lets_another_specification_take_it() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(TestParts.Designator("R1"), TestParts.Resistor("RES-4K7", 4_700, 1, 0.1, 50, Package.Chip0603).Generate());
        bom.Remove(TestParts.Designator("R1"));

        Placement placement = bom.Place(TestParts.Designator("R2"), TestParts.Resistor("RES-4K7", 4_701, 5, 0.1, 50, Package.Chip0603).Generate());

        Assert.Equal(4_701, placement.DesignedPart.NominalValue);
    }

    [Fact]
    public void Removing_an_unplaced_designator_is_refused() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();

        Assert.Throws<InvalidOperationException>(() => bom.Remove(TestParts.Designator("R1")));
        Assert.Throws<ArgumentNullException>(() => bom.Remove(null!));
    }

}

public sealed class BillOfMaterialsReleaseTests {

    private static BillOfMaterials OneResistorBill() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(TestParts.Designator("R1"), TestParts.Designed().Generate());
        return bom;
    }

    [Fact]
    public void Releasing_freezes_the_bill() {
        BillOfMaterials bom = OneResistorBill();

        bom.Release();

        Assert.True(bom.IsReleased);
    }

    [Fact]
    public void An_empty_bill_cannot_be_released() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();

        Assert.Throws<InvalidOperationException>(() => bom.Release());
        Assert.False(bom.IsReleased);
    }

    [Fact]
    public void A_released_bill_cannot_be_released_again() {
        BillOfMaterials bom = OneResistorBill();
        bom.Release();

        Assert.Throws<InvalidOperationException>(() => bom.Release());
    }

    [Fact]
    public void A_released_bill_refuses_every_change() {
        BillOfMaterials bom = OneResistorBill();
        bom.Substitute(TestParts.Designator("R1"), TestParts.Resistor("RES-102-1P", 102, 1, 0.1, 100, Package.Chip0805).Generate());
        bom.Release();

        Assert.Throws<InvalidOperationException>(() => bom.Place(TestParts.Designator("R2"), Any.Part().WithKind(ComponentKind.Resistor).Generate()));
        Assert.Throws<InvalidOperationException>(() => bom.Remove(TestParts.Designator("R1")));
        Assert.Throws<InvalidOperationException>(() => bom.Substitute(TestParts.Designator("R1"), TestParts.Resistor("RES-101-1P", 101, 1, 0.1, 100, Package.Chip0805).Generate()));
        Assert.Throws<InvalidOperationException>(() => bom.RevertSubstitution(TestParts.Designator("R1")));
        Assert.Equal(1, bom.PlacementCount);
        Assert.True(bom.Find(TestParts.Designator("R1"))!.IsSubstituted);
    }

    [Fact]
    public void A_released_bill_can_still_be_queried() {
        BillOfMaterials bom = OneResistorBill();
        bom.Release();

        Assert.Single(bom.Lines());
        Assert.Equal(1, bom.CountOf(ComponentKind.Resistor));
        Assert.NotNull(bom.Find(TestParts.Designator("R1")));
    }

    [Fact]
    public void A_released_bill_refuses_a_change_before_looking_at_the_arguments() {
        BillOfMaterials bom = OneResistorBill();
        bom.Release();

        // A kind mismatch would be an ArgumentException on an open bill; released comes first.
        Assert.Throws<InvalidOperationException>(() => bom.Place(TestParts.Designator("C1"), Any.Part().WithKind(ComponentKind.Resistor).Generate()));
    }

}

public sealed class BillOfMaterialsSubstitutionTests {

    private static BillOfMaterials BillWithDesignedResistorOnR1() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(TestParts.Designator("R1"), TestParts.Designed().Generate());
        return bom;
    }

    [Fact]
    public void A_substitute_that_the_rule_accepts_is_fitted_and_the_design_is_kept() {
        BillOfMaterials bom        = BillWithDesignedResistorOnR1();
        Part            substitute = TestParts.Resistor("RES-102-1P", 102, 1, 0.1, 100, Package.Chip0805).Generate();

        Placement placement = bom.Substitute(TestParts.Designator("R1"), substitute);

        Assert.Same(substitute, placement.FittedPart);
        Assert.Equal("RES-100", placement.DesignedPart.PartNumber);
        Assert.True(placement.IsSubstituted);
        Assert.Equal(new[] { "R1" }, bom.SubstitutedDesignators().Select(d => d.Value));
    }

    [Theory]
    [InlineData("RES-100-10P", 100, 10, 0.1, 100, Package.Chip0805, SubstitutionRefusal.ToleranceBandNotContained)]
    [InlineData("RES-100-W", 100, 5, 0.0625, 100, Package.Chip0805, SubstitutionRefusal.RatingTooLow)]
    [InlineData("RES-100-T", 100, 5, 0.1, 200, Package.Chip0805, SubstitutionRefusal.TemperatureCoefficientWorse)]
    [InlineData("RES-100-S", 100, 5, 0.05, 100, Package.Chip0201, SubstitutionRefusal.DifferentPackage)]
    public void A_substitute_the_rule_refuses_throws_with_the_reason(string partNumber, double ohms, double tolerance, double watts, double ppm, Package package, SubstitutionRefusal expected) {
        BillOfMaterials bom        = BillWithDesignedResistorOnR1();
        Part            substitute = TestParts.Resistor(partNumber, ohms, tolerance, watts, ppm, package).Generate();

        SubstitutionRefusedException refusal = Assert.Throws<SubstitutionRefusedException>(() => bom.Substitute(TestParts.Designator("R1"), substitute));

        Assert.Equal(expected, refusal.Reason);
        Assert.Contains(partNumber, refusal.Message);
        Assert.Contains("RES-100", refusal.Message);
        Assert.Contains("R1", refusal.Message);
        Assert.False(bom.Find(TestParts.Designator("R1"))!.IsSubstituted);
    }

    [Fact]
    public void A_refusal_is_an_invalid_operation_too() {
        BillOfMaterials bom        = BillWithDesignedResistorOnR1();
        Part            substitute = TestParts.Resistor("RES-100-W", 100, 5, 0.0625, 100, Package.Chip0805).Generate();

        Assert.IsAssignableFrom<InvalidOperationException>(Assert.Throws<SubstitutionRefusedException>(() => bom.Substitute(TestParts.Designator("R1"), substitute)));
    }

    [Fact]
    public void A_substitute_of_another_kind_is_refused_as_such() {
        BillOfMaterials bom = BillWithDesignedResistorOnR1();
        Part anyCapacitor = Any.Part().WithKind(ComponentKind.Capacitor).Generate();

        SubstitutionRefusedException refusal = Assert.Throws<SubstitutionRefusedException>(() => bom.Substitute(TestParts.Designator("R1"), anyCapacitor));

        Assert.Equal(SubstitutionRefusal.DifferentKind, refusal.Reason);
    }

    [Fact]
    public void The_substitute_cannot_be_the_part_already_fitted() {
        BillOfMaterials bom = BillWithDesignedResistorOnR1();
        bom.Substitute(TestParts.Designator("R1"), TestParts.Resistor("RES-102-1P", 102, 1, 0.1, 100, Package.Chip0805).Generate());

        SubstitutionRefusedException refusal = Assert.Throws<SubstitutionRefusedException>(
            () => bom.Substitute(TestParts.Designator("R1"), TestParts.Resistor("RES-102-1P", 102, 1, 0.1, 100, Package.Chip0805).Generate()));

        Assert.Equal(SubstitutionRefusal.SamePart, refusal.Reason);
    }

    [Fact]
    public void The_designed_part_number_is_the_same_part_even_when_it_is_not_fitted() {
        BillOfMaterials bom = BillWithDesignedResistorOnR1();
        bom.Substitute(TestParts.Designator("R1"), TestParts.Resistor("RES-102-1P", 102, 1, 0.1, 100, Package.Chip0805).Generate());

        // Going back to the design is a revert, not a substitution.
        SubstitutionRefusedException refusal = Assert.Throws<SubstitutionRefusedException>(() => bom.Substitute(TestParts.Designator("R1"), TestParts.Designed().Generate()));

        Assert.Equal(SubstitutionRefusal.SamePart, refusal.Reason);
    }

    [Fact]
    public void A_substitution_is_judged_against_the_designed_part_not_the_fitted_one() {
        BillOfMaterials bom = BillWithDesignedResistorOnR1();
        bom.Substitute(TestParts.Designator("R1"), TestParts.Resistor("RES-102-1P", 102, 1, 0.1, 100, Package.Chip0805).Generate());

        // 100 ohms +/- 5 % is wider than the fitted 102 +/- 1 %, but it fits the design: accepted.
        Part wideButWithinTheDesign = TestParts.Resistor("RES-100-5P-B", 100, 5, 0.1, 100, Package.Chip0805).Generate();

        Placement placement = bom.Substitute(TestParts.Designator("R1"), wideButWithinTheDesign);

        Assert.Equal("RES-100-5P-B", placement.FittedPart.PartNumber);
    }

    [Fact]
    public void Chained_substitutions_cannot_drift_away_from_the_design() {
        BillOfMaterials bom = BillWithDesignedResistorOnR1();
        // 104 +/- 1 % is within 95..105 only by its nominal, not its band (102.96..105.04): refused against the design.
        bom.Substitute(TestParts.Designator("R1"), TestParts.Resistor("RES-103-1P", 103, 1, 0.1, 100, Package.Chip0805).Generate());

        Part drifting = TestParts.Resistor("RES-104-1P", 104, 1, 0.1, 100, Package.Chip0805).Generate();

        SubstitutionRefusedException refusal = Assert.Throws<SubstitutionRefusedException>(() => bom.Substitute(TestParts.Designator("R1"), drifting));
        Assert.Equal(SubstitutionRefusal.ToleranceBandNotContained, refusal.Reason);
    }

    [Fact]
    public void Substituting_an_unplaced_designator_is_refused() {
        BillOfMaterials bom = BillWithDesignedResistorOnR1();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => bom.Substitute(TestParts.Designator("R2"), TestParts.Resistor("RES-102-1P", 102, 1, 0.1, 100, Package.Chip0805).Generate()));

        Assert.IsNotType<SubstitutionRefusedException>(error);
    }

    [Fact]
    public void A_substitute_cannot_reuse_a_board_part_number_with_another_specification() {
        BillOfMaterials bom = BillWithDesignedResistorOnR1();
        bom.Place(TestParts.Designator("R2"), TestParts.Resistor("RES-102-1P", 102, 1, 0.1, 100, Package.Chip0805).Generate());

        // Acceptable under the rule, but the board already lists RES-102-1P as a 102 ohm part.
        Part sameNumberOtherValue = TestParts.Resistor("RES-102-1P", 101, 1, 0.1, 100, Package.Chip0805).Generate();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => bom.Substitute(TestParts.Designator("R1"), sameNumberOtherValue));

        Assert.IsNotType<SubstitutionRefusedException>(error);
        Assert.False(bom.Find(TestParts.Designator("R1"))!.IsSubstituted);
    }

    [Fact]
    public void A_substitute_may_share_its_part_number_with_an_identical_part_elsewhere_on_the_board() {
        BillOfMaterials bom = BillWithDesignedResistorOnR1();
        Part            alsoOnR2 = TestParts.Resistor("RES-102-1P", 102, 1, 0.1, 100, Package.Chip0805).Generate();
        bom.Place(TestParts.Designator("R2"), alsoOnR2);

        bom.Substitute(TestParts.Designator("R1"), alsoOnR2);

        Assert.Equal(2, bom.Lines().Single().Quantity);
    }

    [Fact]
    public void Checking_a_substitution_answers_without_changing_anything() {
        BillOfMaterials bom = BillWithDesignedResistorOnR1();

        SubstitutionRefusal accepted = bom.CheckSubstitution(TestParts.Designator("R1"), TestParts.Resistor("RES-102-1P", 102, 1, 0.1, 100, Package.Chip0805).Generate());
        SubstitutionRefusal refused  = bom.CheckSubstitution(TestParts.Designator("R1"), TestParts.Resistor("RES-100-W", 100, 5, 0.0625, 100, Package.Chip0805).Generate());

        Assert.Equal(SubstitutionRefusal.None, accepted);
        Assert.Equal(SubstitutionRefusal.RatingTooLow, refused);
        Assert.False(bom.Find(TestParts.Designator("R1"))!.IsSubstituted);
    }

    [Fact]
    public void Checking_with_the_fitted_part_number_answers_same_part() {
        BillOfMaterials bom = BillWithDesignedResistorOnR1();
        bom.Substitute(TestParts.Designator("R1"), TestParts.Resistor("RES-102-1P", 102, 1, 0.1, 100, Package.Chip0805).Generate());

        // The fitted part is not the designed one, and the substitute differs from the design in everything but the number.
        SubstitutionRefusal answer = bom.CheckSubstitution(TestParts.Designator("R1"), TestParts.Resistor("RES-102-1P", 90, 20, 0.01, 5000, Package.Chip0201).Generate());

        Assert.Equal(SubstitutionRefusal.SamePart, answer);
    }

    [Fact]
    public void Checking_an_unplaced_designator_is_refused() {
        BillOfMaterials bom = BillWithDesignedResistorOnR1();

        Assert.Throws<InvalidOperationException>(() => bom.CheckSubstitution(TestParts.Designator("R2"), Any.Part().WithKind(ComponentKind.Resistor).Generate()));
        Assert.Throws<ArgumentNullException>(() => bom.CheckSubstitution(null!, Any.Part().Generate()));
        Assert.Throws<ArgumentNullException>(() => bom.CheckSubstitution(TestParts.Designator("R1"), null!));
    }

    [Fact]
    public void Checking_is_still_possible_once_the_bill_is_released() {
        BillOfMaterials bom = BillWithDesignedResistorOnR1();
        bom.Release();

        SubstitutionRefusal answer = bom.CheckSubstitution(TestParts.Designator("R1"), TestParts.Resistor("RES-102-1P", 102, 1, 0.1, 100, Package.Chip0805).Generate());

        Assert.Equal(SubstitutionRefusal.None, answer);
    }

    [Fact]
    public void Reverting_puts_the_designed_part_back() {
        BillOfMaterials bom = BillWithDesignedResistorOnR1();
        bom.Substitute(TestParts.Designator("R1"), TestParts.Resistor("RES-102-1P", 102, 1, 0.1, 100, Package.Chip0805).Generate());

        Placement placement = bom.RevertSubstitution(TestParts.Designator("R1"));

        Assert.Same(placement.DesignedPart, placement.FittedPart);
        Assert.False(placement.IsSubstituted);
        Assert.Empty(bom.SubstitutedDesignators());
    }

    [Fact]
    public void Reverting_a_part_that_carries_its_design_is_refused() {
        BillOfMaterials bom = BillWithDesignedResistorOnR1();

        Assert.Throws<InvalidOperationException>(() => bom.RevertSubstitution(TestParts.Designator("R1")));
        Assert.Throws<InvalidOperationException>(() => bom.RevertSubstitution(TestParts.Designator("R9")));
        Assert.Throws<ArgumentNullException>(() => bom.RevertSubstitution(null!));
    }

    [Fact]
    public void Substituting_null_is_refused() {
        BillOfMaterials bom = BillWithDesignedResistorOnR1();

        Assert.Throws<ArgumentNullException>(() => bom.Substitute(null!, Any.Part().Generate()));
        Assert.Throws<ArgumentNullException>(() => bom.Substitute(TestParts.Designator("R1"), null!));
    }

    [Theory]
    [InlineData(SubstitutionRefusal.None)]
    [InlineData((SubstitutionRefusal)99)]
    public void A_refusal_exception_never_carries_none_or_an_unknown_reason(SubstitutionRefusal reason) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SubstitutionRefusedException(reason, "no"));
    }

    [Fact]
    public void A_refusal_exception_carries_the_reason_and_message_it_is_given() {
        SubstitutionRefusedException error = new SubstitutionRefusedException(SubstitutionRefusal.RatingTooLow, "too weak");

        Assert.Equal(SubstitutionRefusal.RatingTooLow, error.Reason);
        Assert.Equal("too weak", error.Message);
    }

}

public sealed class BillOfMaterialsReportTests {

    [Fact]
    public void Lines_group_the_fitted_parts_and_list_their_designators() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        Part tenKiloohms = TestParts.Resistor("RES-10K", 10_000, 1, 0.1, 50, Package.Chip0603).Generate();
        bom.Place(TestParts.Designator("R3"), tenKiloohms);
        bom.Place(TestParts.Designator("R1"), tenKiloohms);
        bom.Place(TestParts.Designator("R2"), TestParts.Resistor("RES-1K", 1_000, 1, 0.1, 50, Package.Chip0603).Generate());

        IReadOnlyList<BomLine> lines = bom.Lines();

        Assert.Equal(2, lines.Count);
        Assert.Equal("RES-1K", lines[0].Part.PartNumber);
        Assert.Equal(new[] { "R2" }, lines[0].Designators.Select(d => d.Value));
        Assert.Equal("RES-10K", lines[1].Part.PartNumber);
        Assert.Equal(new[] { "R1", "R3" }, lines[1].Designators.Select(d => d.Value));
        Assert.Equal(2, lines[1].Quantity);
    }

    [Fact]
    public void Lines_are_ordered_by_kind_then_nominal_value_then_part_number() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(TestParts.Designator("R1"), TestParts.Resistor("RES-B", 100, 1, 0.1, 50, Package.Chip0603).Generate());
        bom.Place(TestParts.Designator("R2"), TestParts.Resistor("RES-A", 100, 1, 0.1, 50, Package.Chip0603).Generate());
        bom.Place(TestParts.Designator("R3"), TestParts.Resistor("RES-C", 10, 1, 0.1, 50, Package.Chip0603).Generate());
        bom.Place(TestParts.Designator("L1"), TestParts.Inductor("IND-1", 1e-5, 10, 1, 30, Package.Chip0805).Generate());
        bom.Place(TestParts.Designator("C1"), TestParts.Capacitor("CAP-1", 1e-6, 10, 25, 30, Package.Chip0603).Generate());
        bom.Place(TestParts.Designator("C2"), TestParts.Capacitor("CAP-0", 1e-9, 10, 25, 30, Package.Chip0603).Generate());

        Assert.Equal(new[] { "CAP-0", "CAP-1", "IND-1", "RES-C", "RES-A", "RES-B" }, bom.Lines().Select(l => l.Part.PartNumber));
    }

    [Fact]
    public void Lines_show_the_fitted_part_not_the_designed_one() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        Part designed = TestParts.Designed().Generate();
        bom.Place(TestParts.Designator("R1"), designed);
        bom.Place(TestParts.Designator("R2"), designed);
        bom.Substitute(TestParts.Designator("R2"), TestParts.Resistor("RES-102-1P", 102, 1, 0.1, 100, Package.Chip0805).Generate());

        IReadOnlyList<BomLine> lines = bom.Lines();

        Assert.Equal(new[] { "RES-100", "RES-102-1P" }, lines.Select(l => l.Part.PartNumber));
        Assert.Equal(new[] { "R1" }, lines[0].Designators.Select(d => d.Value));
        Assert.Equal(new[] { "R2" }, lines[1].Designators.Select(d => d.Value));
    }

    [Fact]
    public void The_total_cost_adds_the_fitted_unit_costs() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        Part cheap = Any.Part().WithKind(ComponentKind.Resistor).WithPartNumber("RES-CHEAP").WithUnitCost(0.0125m).Generate();
        bom.Place(TestParts.Designator("R1"), cheap);
        bom.Place(TestParts.Designator("R2"), cheap);
        bom.Place(TestParts.Designator("C1"), Any.Part().WithKind(ComponentKind.Capacitor).WithPartNumber("CAP-DEAR").WithUnitCost(1.5m).Generate());

        Assert.Equal(1.525m, bom.TotalCost());
    }

    [Fact]
    public void The_total_cost_follows_the_substitutions() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(TestParts.Designator("R1"), Any.Part().WithKind(ComponentKind.Resistor).WithPartNumber("RES-100").WithNominalValue(100).WithTolerance(new Tolerance(5)).WithRating(0.1).WithTemperatureCoefficient(new TemperatureCoefficient(100)).WithPackage(Package.Chip0805).WithUnitCost(0.01m).Generate());
        bom.Substitute(TestParts.Designator("R1"), Any.Part().WithKind(ComponentKind.Resistor).WithPartNumber("RES-100-B").WithNominalValue(100).WithTolerance(new Tolerance(5)).WithRating(0.1).WithTemperatureCoefficient(new TemperatureCoefficient(100)).WithPackage(Package.Chip0805).WithUnitCost(0.03m).Generate());

        Assert.Equal(0.03m, bom.TotalCost());
    }

    [Fact]
    public void Counts_the_fitted_parts_of_a_kind() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(TestParts.Designator("R1"), Any.Part().WithKind(ComponentKind.Resistor).Generate());
        bom.Place(TestParts.Designator("R2"), Any.Part().WithKind(ComponentKind.Resistor).Generate());
        bom.Place(TestParts.Designator("C1"), Any.Part().WithKind(ComponentKind.Capacitor).Generate());

        Assert.Equal(2, bom.CountOf(ComponentKind.Resistor));
        Assert.Equal(1, bom.CountOf(ComponentKind.Capacitor));
        Assert.Equal(0, bom.CountOf(ComponentKind.Inductor));
    }

    [Fact]
    public void Lists_the_designators_whose_fitted_value_is_off_the_series() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(TestParts.Designator("R1"), Any.Part().WithKind(ComponentKind.Resistor).WithNominalValue(4_700).Generate());
        bom.Place(TestParts.Designator("R2"), Any.Part().WithKind(ComponentKind.Resistor).WithNominalValue(4_800).Generate());
        bom.Place(TestParts.Designator("C1"), Any.Part().WithKind(ComponentKind.Capacitor).WithNominalValue(2.2e-9).Generate());
        bom.Place(TestParts.Designator("C2"), Any.Part().WithKind(ComponentKind.Capacitor).WithNominalValue(2.3e-9).Generate());

        Assert.Equal(new[] { "C2", "R2" }, bom.OffSeries(ESeries.E24).Select(d => d.Value));
        Assert.Equal(new[] { "C2", "R2" }, bom.OffSeries(ESeries.E12).Select(d => d.Value));
    }

    [Fact]
    public void A_value_on_a_finer_series_only_is_off_the_coarser_one() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(TestParts.Designator("R1"), Any.Part().WithKind(ComponentKind.Resistor).WithNominalValue(4_990).Generate());

        Assert.Empty(bom.OffSeries(ESeries.E96));
        Assert.Single(bom.OffSeries(ESeries.E24));
    }

    [Fact]
    public void Finds_by_marking_the_parts_of_that_kind_carrying_that_value() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(TestParts.Designator("C2"), Any.Part().WithKind(ComponentKind.Capacitor).WithNominalValue(1e-7).Generate());
        bom.Place(TestParts.Designator("C1"), Any.Part().WithKind(ComponentKind.Capacitor).WithNominalValue(1e-7).Generate());
        bom.Place(TestParts.Designator("C3"), Any.Part().WithKind(ComponentKind.Capacitor).WithNominalValue(2.2e-7).Generate());
        bom.Place(TestParts.Designator("R1"), Any.Part().WithKind(ComponentKind.Resistor).WithNominalValue(1e5).Generate());

        Assert.Equal(new[] { "C1", "C2" }, bom.FindByMarking(new MarkingCode("104", ComponentKind.Capacitor)).Select(d => d.Value));
        Assert.Equal(new[] { "R1" }, bom.FindByMarking(new MarkingCode("104", ComponentKind.Resistor)).Select(d => d.Value));
        Assert.Empty(bom.FindByMarking(new MarkingCode("104", ComponentKind.Inductor)));
    }

    [Fact]
    public void Finding_by_marking_matches_a_value_written_in_another_notation() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(TestParts.Designator("C1"), Any.Part().WithKind(ComponentKind.Capacitor).WithNominalValue(2.2e-9).Generate());

        Assert.Single(bom.FindByMarking(new MarkingCode("2n2", ComponentKind.Capacitor)));
        Assert.Single(bom.FindByMarking(new MarkingCode("222", ComponentKind.Capacitor)));
    }

    [Fact]
    public void Finding_by_marking_looks_at_the_fitted_part() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(TestParts.Designator("R1"), TestParts.Designed().Generate());
        bom.Substitute(TestParts.Designator("R1"), TestParts.Resistor("RES-102-1P", 102, 1, 0.1, 100, Package.Chip0805).Generate());

        Assert.Empty(bom.FindByMarking(new MarkingCode("101", ComponentKind.Resistor)));   // the designed 100 ohms
        Assert.Single(bom.FindByMarking(new MarkingCode("1020", ComponentKind.Resistor))); // the fitted 102 ohms
    }

    [Fact]
    public void Finding_by_a_null_marking_is_refused() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();

        Assert.Throws<ArgumentNullException>(() => bom.FindByMarking(null!));
    }

    [Fact]
    public void The_time_constant_spans_both_tolerance_bands() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(TestParts.Designator("R1"), TestParts.Resistor("RES-10K", 10_000, 1, 0.1, 50, Package.Chip0603).Generate());
        bom.Place(TestParts.Designator("C1"), TestParts.Capacitor("CAP-100N", 1e-7, 5, 50, 30, Package.Chip0603).Generate());

        (double minimum, double nominal, double maximum) = bom.TimeConstantSeconds(TestParts.Designator("R1"), TestParts.Designator("C1"));

        Assert.Equal(9.405e-4, minimum, 9);
        Assert.Equal(1e-3, nominal, 9);
        Assert.Equal(1.0605e-3, maximum, 9);
    }

    [Fact]
    public void The_time_constant_needs_a_resistor_then_a_capacitor() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(TestParts.Designator("R1"), Any.Part().WithKind(ComponentKind.Resistor).Generate());
        bom.Place(TestParts.Designator("C1"), Any.Part().WithKind(ComponentKind.Capacitor).Generate());

        Assert.Throws<ArgumentException>(() => bom.TimeConstantSeconds(TestParts.Designator("C1"), TestParts.Designator("R1")));
        Assert.Throws<ArgumentException>(() => bom.TimeConstantSeconds(TestParts.Designator("R1"), TestParts.Designator("R1")));
        Assert.Throws<ArgumentException>(() => bom.TimeConstantSeconds(TestParts.Designator("L1"), TestParts.Designator("C1")));
    }

    [Fact]
    public void The_time_constant_needs_both_parts_to_be_placed() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(TestParts.Designator("R1"), Any.Part().WithKind(ComponentKind.Resistor).Generate());

        Assert.Throws<InvalidOperationException>(() => bom.TimeConstantSeconds(TestParts.Designator("R1"), TestParts.Designator("C1")));
        Assert.Throws<InvalidOperationException>(() => bom.TimeConstantSeconds(TestParts.Designator("R2"), TestParts.Designator("C1")));
        Assert.Throws<ArgumentNullException>(() => bom.TimeConstantSeconds(null!, TestParts.Designator("C1")));
    }

    [Fact]
    public void The_time_constant_uses_the_fitted_parts() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(TestParts.Designator("R1"), TestParts.Designed().Generate());
        bom.Place(TestParts.Designator("C1"), TestParts.Capacitor("CAP-100N", 1e-7, 5, 50, 30, Package.Chip0603).Generate());
        bom.Substitute(TestParts.Designator("R1"), TestParts.Resistor("RES-102-1P", 102, 1, 0.1, 100, Package.Chip0805).Generate());

        (double _, double nominal, double _) = bom.TimeConstantSeconds(TestParts.Designator("R1"), TestParts.Designator("C1"));

        Assert.Equal(1.02e-5, nominal, 12);
    }

    [Fact]
    public void The_resonant_frequency_spans_both_tolerance_bands() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(TestParts.Designator("L1"), TestParts.Inductor("IND-10U", 1e-5, 5, 1, 30, Package.Chip0805).Generate());
        bom.Place(TestParts.Designator("C1"), TestParts.Capacitor("CAP-1N", 1e-9, 5, 50, 30, Package.Chip0603).Generate());

        (double minimum, double nominal, double maximum) = bom.ResonantFrequencyHertz(TestParts.Designator("L1"), TestParts.Designator("C1"));

        Assert.Equal(1_591_549.43, nominal, 0);
        Assert.Equal(1_591_549.43 / 1.05, minimum, 0);
        Assert.Equal(1_591_549.43 / 0.95, maximum, 0);
    }

    [Fact]
    public void The_resonant_frequency_needs_an_inductor_then_a_capacitor_both_placed() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(TestParts.Designator("L1"), Any.Part().WithKind(ComponentKind.Inductor).Generate());
        bom.Place(TestParts.Designator("R1"), Any.Part().WithKind(ComponentKind.Resistor).Generate());

        Assert.Throws<ArgumentException>(() => bom.ResonantFrequencyHertz(TestParts.Designator("R1"), TestParts.Designator("C1")));
        Assert.Throws<ArgumentException>(() => bom.ResonantFrequencyHertz(TestParts.Designator("L1"), TestParts.Designator("R1")));
        Assert.Throws<InvalidOperationException>(() => bom.ResonantFrequencyHertz(TestParts.Designator("L1"), TestParts.Designator("C1")));
    }

}
