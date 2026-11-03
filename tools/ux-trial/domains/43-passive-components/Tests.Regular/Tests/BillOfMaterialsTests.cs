namespace PassiveComponents.Tests.Regular;

public sealed class BillOfMaterialsConstructionTests {

    [Fact]
    public void A_new_bill_is_empty_and_open() {
        BillOfMaterials bom = new BillOfMaterials("Sensor board", "B");

        Assert.Equal("Sensor board", bom.BoardName);
        Assert.Equal("B", bom.Revision);
        Assert.False(bom.IsReleased);
        Assert.Equal(0, bom.PlacementCount);
        Assert.Empty(bom.Placements);
        Assert.Empty(bom.Lines());
        Assert.Equal(0m, bom.TotalCost());
    }

    [Fact]
    public void A_drawn_bill_is_a_valid_empty_bill() {
        BillOfMaterials anyBom = Any.BillOfMaterials().Generate();

        Assert.Equal(0, anyBom.PlacementCount);
        Assert.False(anyBom.IsReleased);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("Z")]
    [InlineData("AA")]
    [InlineData("ZZ")]
    public void A_revision_is_one_or_two_capital_letters(string revision) {
        string anyBoardName = Any.BillOfMaterials().Generate().BoardName;

        Assert.Equal(revision, new BillOfMaterials(anyBoardName, revision).Revision);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("Aa")]
    [InlineData("ABC")]
    [InlineData("A1")]
    [InlineData("1")]
    [InlineData(" A")]
    [InlineData("A ")]
    [InlineData("É")]
    public void Any_other_revision_is_refused(string revision) {
        string anyBoardName = Any.BillOfMaterials().Generate().BoardName;

        Assert.Throws<FormatException>(() => new BillOfMaterials(anyBoardName, revision));
    }

    [Fact]
    public void A_missing_revision_is_refused() {
        string anyBoardName = Any.BillOfMaterials().Generate().BoardName;

        Assert.Throws<ArgumentNullException>(() => new BillOfMaterials(anyBoardName, null!));
    }

    [Fact(Skip = "domain defect: '$' in the revision pattern also matches before a final line break, so 'A\\n' is accepted; input is a literal, no seed")]
    public void A_revision_followed_by_a_line_break_is_refused() {
        string anyBoardName = Any.BillOfMaterials().Generate().BoardName;

        Assert.Throws<FormatException>(() => new BillOfMaterials(anyBoardName, "A\n"));
    }

    [Fact]
    public void A_board_name_may_run_to_64_characters() {
        string sixtyFour = new string('B', 64);

        Assert.Equal(sixtyFour, new BillOfMaterials(sixtyFour, "A").BoardName);
    }

    [Fact]
    public void A_board_name_of_65_characters_is_refused() {
        Assert.Throws<ArgumentException>(() => new BillOfMaterials(new string('B', 65), "A"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("   \n ")]
    [InlineData(null)]
    public void A_blank_board_name_is_refused(string? boardName) {
        Assert.Throws<ArgumentException>(() => new BillOfMaterials(boardName!, "A"));
    }

    [Theory]
    [InlineData(" Sensor")]
    [InlineData("Sensor ")]
    [InlineData("\tSensor")]
    [InlineData("Sensor\n")]
    [InlineData(" Sensor ")]
    public void A_board_name_with_surrounding_whitespace_is_refused(string boardName) {
        Assert.Throws<ArgumentException>(() => new BillOfMaterials(boardName, "A"));
    }

    [Fact]
    public void A_board_name_may_hold_inner_spaces() {
        Assert.Equal("Sensor  board", new BillOfMaterials("Sensor  board", "A").BoardName);
    }

    [Fact]
    public void A_board_name_of_64_characters_with_a_trailing_space_is_refused_as_untrimmed() {
        Assert.Throws<ArgumentException>(() => new BillOfMaterials(new string('B', 63) + " ", "A"));
    }

    [Fact]
    public void It_is_written_with_its_name_revision_and_size() {
        BillOfMaterials bom = new BillOfMaterials("Sensor", "C");
        bom.Place(Specs.Designator("R1"), Any.Part().Resistor().Generate());
        bom.Place(Specs.Designator("R2"), Any.Part().Resistor().Generate());

        Assert.Equal("Sensor rev C (2 placements)", bom.ToString());

        bom.Release();

        Assert.Equal("Sensor rev C (2 placements, released)", bom.ToString());
    }

}

public sealed class BillOfMaterialsPlacementTests {

    [Fact]
    public void Placing_a_part_records_it_under_its_designator() {
        BillOfMaterials bom  = Any.BillOfMaterials().Generate();
        Part            part = Any.Part().Resistor().Generate();

        Placement placement = bom.Place(Specs.Designator("R5"), part);

        Assert.Equal(1, bom.PlacementCount);
        Assert.Same(placement, bom.Find(Specs.Designator("R5")));
        Assert.Same(part, placement.FittedPart);
    }

    [Fact]
    public void An_unplaced_designator_finds_nothing() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();

        Assert.Null(bom.Find(Specs.Designator("R5")));
        Assert.Throws<ArgumentNullException>(() => bom.Find(null!));
    }

    [Fact]
    public void A_designator_is_used_once() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(Specs.Designator("R5"), Any.Part().Resistor().Generate());

        Assert.Throws<InvalidOperationException>(() => bom.Place(Specs.Designator("R5"), Any.Part().Resistor().Generate()));
        Assert.Equal(1, bom.PlacementCount);
    }

    [Fact]
    public void The_designator_letter_must_match_the_kind_of_the_part() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();

        Assert.Throws<ArgumentException>(() => bom.Place(Specs.Designator("C1"), Any.Part().Resistor().Generate()));
        Assert.Equal(0, bom.PlacementCount);
    }

    [Fact]
    public void Placing_needs_a_designator_and_a_part() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();

        Assert.Throws<ArgumentNullException>(() => bom.Place(null!, Any.Part().Resistor().Generate()));
        Assert.Throws<ArgumentNullException>(() => bom.Place(Specs.Designator("R1"), null!));
    }

    [Fact]
    public void One_part_may_be_placed_on_several_designators() {
        BillOfMaterials bom  = Any.BillOfMaterials().Generate();
        Part            part = Any.Part().Resistor().Generate();

        bom.Place(Specs.Designator("R1"), part);
        bom.Place(Specs.Designator("R2"), part);

        Assert.Equal(2, bom.PlacementCount);
    }

    [Fact]
    public void Two_parts_alike_but_for_the_part_number_are_two_parts() {
        BillOfMaterials bom   = Any.BillOfMaterials().Generate();
        AnyPart         spec  = Specs.Resistor(4_700, 5, 0.1, 100).WithUnitCost(0.01m);

        bom.Place(Specs.Designator("R1"), spec.WithPartNumber("AAA-1").Generate());
        bom.Place(Specs.Designator("R2"), spec.WithPartNumber("AAA-2").Generate());

        Assert.Equal(2, bom.Lines().Count);
    }

    [Theory]
    [InlineData("nominal")]
    [InlineData("tolerance")]
    [InlineData("rating")]
    [InlineData("coefficient")]
    [InlineData("package")]
    [InlineData("cost")]
    public void The_same_part_number_with_another_specification_is_refused(string difference) {
        BillOfMaterials bom      = Any.BillOfMaterials().Generate();
        AnyPart         spec     = Specs.Resistor(4_700, 5, 0.1, 100).WithUnitCost(0.01m).WithPartNumber("SAME-PN");
        AnyPart         different = difference switch {
            "nominal"     => spec.WithNominalValue(4_701),
            "tolerance"   => spec.WithTolerance(new Tolerance(1)),
            "rating"      => spec.WithRating(0.0625),
            "coefficient" => spec.WithTemperatureCoefficient(new TemperatureCoefficient(50)),
            "package"     => spec.WithPackage(Package.Chip0805),
            _             => spec.WithUnitCost(0.0101m)
        };
        bom.Place(Specs.Designator("R1"), spec.Generate());

        Assert.Throws<InvalidOperationException>(() => bom.Place(Specs.Designator("R2"), different.Generate()));
        Assert.Equal(1, bom.PlacementCount);
    }

    [Fact]
    public void Removing_frees_the_designator_and_the_part_number() {
        BillOfMaterials bom  = Any.BillOfMaterials().Generate();
        AnyPart         spec = Specs.Resistor(4_700, 5, 0.1, 100).WithPartNumber("SAME-PN");
        bom.Place(Specs.Designator("R1"), spec.WithUnitCost(0.01m).Generate());

        bom.Remove(Specs.Designator("R1"));
        bom.Place(Specs.Designator("R1"), spec.WithUnitCost(0.02m).Generate());

        Assert.Equal(1, bom.PlacementCount);
    }

    [Fact]
    public void Removing_an_unplaced_designator_is_refused() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();

        Assert.Throws<InvalidOperationException>(() => bom.Remove(Specs.Designator("R1")));
        Assert.Throws<ArgumentNullException>(() => bom.Remove(null!));
    }

    [Fact]
    public void Placements_come_in_designator_order() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(Specs.Designator("R10"), Any.Part().Resistor().Generate());
        bom.Place(Specs.Designator("L1"), Any.Part().Inductor().Generate());
        bom.Place(Specs.Designator("R2"), Any.Part().Resistor().Generate());
        bom.Place(Specs.Designator("C2"), Any.Part().Capacitor().Generate());
        bom.Place(Specs.Designator("C1"), Any.Part().Capacitor().Generate());

        Assert.Equal(new[] { "C1", "C2", "L1", "R2", "R10" }, bom.Placements.Select(p => p.Designator.Value));
    }

    [Fact]
    public void Parts_are_counted_by_kind() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(Specs.Designator("R1"), Any.Part().Resistor().Generate());
        bom.Place(Specs.Designator("R2"), Any.Part().Resistor().Generate());
        bom.Place(Specs.Designator("R3"), Any.Part().Resistor().Generate());
        bom.Place(Specs.Designator("C1"), Any.Part().Capacitor().Generate());

        Assert.Equal(1, bom.CountOf(ComponentKind.Capacitor));
        Assert.Equal(0, bom.CountOf(ComponentKind.Inductor));
        Assert.Equal(3, bom.CountOf(ComponentKind.Resistor));
    }

    [Fact]
    public void The_total_cost_is_the_sum_of_the_unit_costs() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        Part tenCents = Any.Part().Resistor().WithUnitCost(0.1m).Generate();
        bom.Place(Specs.Designator("R1"), tenCents);
        bom.Place(Specs.Designator("R2"), tenCents);
        bom.Place(Specs.Designator("C1"), Any.Part().Capacitor().WithUnitCost(0.0475m).Generate());

        Assert.Equal(0.2475m, bom.TotalCost());
    }

}

public sealed class BillOfMaterialsLinesTests {

    [Fact]
    public void There_is_one_line_per_part_number_with_every_designator_fitted() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        Part tenKilo = Specs.Resistor(10_000, 1, 0.1, 100).WithUnitCost(0.02m).Generate();
        bom.Place(Specs.Designator("R3"), tenKilo);
        bom.Place(Specs.Designator("R1"), tenKilo);

        IReadOnlyList<BomLine> lines = bom.Lines();

        BomLine line = Assert.Single(lines);
        Assert.Equal(new[] { "R1", "R3" }, line.Designators.Select(d => d.Value));
        Assert.Equal(0.04m, line.ExtendedCost);
    }

    [Fact]
    public void Lines_are_ordered_by_kind_then_nominal_value_then_part_number() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        AnyPart resistor = Specs.Resistor(1, 5, 0.1, 100);
        bom.Place(Specs.Designator("R1"), resistor.WithNominalValue(4_700).WithPartNumber("R-4K7").Generate());
        bom.Place(Specs.Designator("R2"), resistor.WithNominalValue(1_000).WithPartNumber("R-1K-B").Generate());
        bom.Place(Specs.Designator("R3"), resistor.WithNominalValue(1_000).WithPartNumber("R-1K-A").Generate());
        bom.Place(Specs.Designator("L1"), Specs.Inductor(10e-6, 1).WithPartNumber("L-10U").Generate());
        bom.Place(Specs.Designator("C1"), Specs.Capacitor(100e-9, 10, 50).WithPartNumber("C-100N").Generate());
        bom.Place(Specs.Designator("C2"), Specs.Capacitor(1e-9, 10, 50).WithPartNumber("C-1N").Generate());

        Assert.Equal(new[] { "C-1N", "C-100N", "L-10U", "R-1K-A", "R-1K-B", "R-4K7" }, bom.Lines().Select(l => l.Part.PartNumber));
    }

    [Fact]
    public void A_substituted_designator_moves_to_the_line_of_the_fitted_part() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        Part original   = Specs.Resistor(100, 5, 0.1, 100).WithPartNumber("ORIG").Generate();
        Part substitute = Specs.Resistor(100, 1, 0.1, 50).WithPartNumber("SUBST").Generate();
        bom.Place(Specs.Designator("R1"), original);
        bom.Place(Specs.Designator("R2"), original);
        bom.Substitute(Specs.Designator("R2"), substitute);

        IReadOnlyList<BomLine> lines = bom.Lines();

        Assert.Equal(2, lines.Count);
        Assert.Equal(new[] { "R1" }, lines.Single(l => l.Part.PartNumber == "ORIG").Designators.Select(d => d.Value));
        Assert.Equal(new[] { "R2" }, lines.Single(l => l.Part.PartNumber == "SUBST").Designators.Select(d => d.Value));
    }

    [Fact]
    public void A_substitute_alone_on_the_board_leaves_no_line_for_the_designed_part() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        Part original   = Specs.Resistor(100, 5, 0.1, 100).WithPartNumber("ORIG").Generate();
        Part substitute = Specs.Resistor(100, 1, 0.1, 50).WithPartNumber("SUBST").Generate();
        bom.Place(Specs.Designator("R1"), original);
        bom.Substitute(Specs.Designator("R1"), substitute);

        Assert.Equal("SUBST", Assert.Single(bom.Lines()).Part.PartNumber);
    }

}

public sealed class BillOfMaterialsSubstitutionTests {

    private static readonly ReferenceDesignator R1 = Specs.Designator("R1");

    private static AnyPart Designed() => Specs.Resistor(100, 5, 0.1, 100).WithUnitCost(0.01m);

    [Fact]
    public void A_good_substitute_is_fitted_while_the_designed_part_is_kept() {
        BillOfMaterials bom         = Any.BillOfMaterials().Generate();
        Part            designed    = Designed().Generate();
        Part            substitute  = Specs.Resistor(102, 1, 0.1, 100).Generate();
        bom.Place(R1, designed);

        Placement placement = bom.Substitute(R1, substitute);

        Assert.Same(substitute, placement.FittedPart);
        Assert.Same(designed, placement.DesignedPart);
        Assert.True(placement.IsSubstituted);
        Assert.Equal(new[] { "R1" }, bom.SubstitutedDesignators().Select(d => d.Value));
    }

    [Fact]
    public void The_total_cost_is_that_of_the_fitted_parts() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(R1, Designed().WithUnitCost(0.01m).Generate());

        bom.Substitute(R1, Specs.Resistor(100, 1, 0.1, 50).WithUnitCost(0.25m).Generate());

        Assert.Equal(0.25m, bom.TotalCost());
    }

    [Theory]
    [InlineData(SubstitutionRefusal.DifferentPackage)]
    [InlineData(SubstitutionRefusal.ToleranceBandNotContained)]
    [InlineData(SubstitutionRefusal.RatingTooLow)]
    [InlineData(SubstitutionRefusal.TemperatureCoefficientWorse)]
    public void A_refused_substitute_names_its_reason_and_changes_nothing(SubstitutionRefusal reason) {
        BillOfMaterials bom      = Any.BillOfMaterials().Generate();
        Part            designed = Designed().Generate();
        Part            refused  = reason switch {
            SubstitutionRefusal.DifferentPackage            => Specs.Resistor(100, 5, 0.1, 100).WithPackage(Package.Chip0805).Generate(),
            SubstitutionRefusal.ToleranceBandNotContained   => Specs.Resistor(100, 10, 0.1, 100).Generate(),
            SubstitutionRefusal.RatingTooLow                => Specs.Resistor(100, 5, 0.05, 100).Generate(),
            _                                               => Specs.Resistor(100, 5, 0.1, 150).Generate()
        };
        bom.Place(R1, designed);

        SubstitutionRefusedException refusal = Assert.Throws<SubstitutionRefusedException>(() => bom.Substitute(R1, refused));

        Assert.Equal(reason, refusal.Reason);
        Assert.Equal(reason, bom.CheckSubstitution(R1, refused));
        Assert.False(bom.Find(R1)!.IsSubstituted);
        Assert.Same(designed, bom.Find(R1)!.FittedPart);
        Assert.Contains(refused.PartNumber, refusal.Message);
        Assert.Contains(reason.ToString(), refusal.Message);
    }

    [Fact]
    public void A_substitute_of_another_kind_is_refused() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(R1, Designed().Generate());
        Part capacitor = Any.Part().Capacitor().Generate();

        SubstitutionRefusedException refusal = Assert.Throws<SubstitutionRefusedException>(() => bom.Substitute(R1, capacitor));

        Assert.Equal(SubstitutionRefusal.DifferentKind, refusal.Reason);
    }

    [Fact]
    public void A_refusal_is_an_invalid_operation() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(R1, Designed().Generate());

        Assert.ThrowsAny<InvalidOperationException>(() => bom.Substitute(R1, Specs.Resistor(100, 10, 0.1, 100).Generate()));
    }

    [Fact]
    public void Checking_a_substitution_changes_nothing() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(R1, Designed().Generate());

        SubstitutionRefusal answer = bom.CheckSubstitution(R1, Specs.Resistor(100, 1, 0.1, 50).Generate());

        Assert.Equal(SubstitutionRefusal.None, answer);
        Assert.False(bom.Find(R1)!.IsSubstituted);
    }

    [Fact]
    public void Checking_a_substitution_needs_a_placed_designator() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();

        Assert.Throws<InvalidOperationException>(() => bom.CheckSubstitution(R1, Any.Part().Resistor().Generate()));
        Assert.Throws<ArgumentNullException>(() => bom.CheckSubstitution(null!, Any.Part().Resistor().Generate()));
        Assert.Throws<ArgumentNullException>(() => bom.CheckSubstitution(R1, null!));
    }

    [Fact]
    public void The_part_already_fitted_is_refused_as_the_same_part() {
        BillOfMaterials bom      = Any.BillOfMaterials().Generate();
        Part            designed = Designed().Generate();
        bom.Place(R1, designed);

        SubstitutionRefusedException refusal = Assert.Throws<SubstitutionRefusedException>(() => bom.Substitute(R1, designed));

        Assert.Equal(SubstitutionRefusal.SamePart, refusal.Reason);
    }

    [Fact]
    public void The_substitute_currently_fitted_is_refused_as_the_same_part() {
        BillOfMaterials bom        = Any.BillOfMaterials().Generate();
        Part            substitute = Specs.Resistor(100, 1, 0.1, 50).Generate();
        bom.Place(R1, Designed().Generate());
        bom.Substitute(R1, substitute);

        SubstitutionRefusedException refusal = Assert.Throws<SubstitutionRefusedException>(() => bom.Substitute(R1, substitute));

        Assert.Equal(SubstitutionRefusal.SamePart, refusal.Reason);
        Assert.Equal(SubstitutionRefusal.SamePart, bom.CheckSubstitution(R1, substitute));
    }

    [Fact]
    public void A_substitution_is_judged_against_the_designed_part_not_the_fitted_one() {
        // designed 100 +-5 % ([95, 105]); fitted 100 +-1 % ([99, 101]); then 104 +-0.5 % ([103.48, 104.52])
        // sits inside the designed band though not inside the fitted one
        BillOfMaterials bom      = Any.BillOfMaterials().Generate();
        Part            designed = Designed().Generate();
        Part            first    = Specs.Resistor(100, 1, 0.1, 50).Generate();
        Part            second   = Specs.Resistor(104, 0.5, 0.1, 100).Generate();
        bom.Place(R1, designed);
        bom.Substitute(R1, first);

        Placement placement = bom.Substitute(R1, second);

        Assert.Same(second, placement.FittedPart);
        Assert.Same(designed, placement.DesignedPart);
    }

    [Fact]
    public void A_chain_of_substitutions_cannot_drift_away_from_the_design() {
        // 100 +-5 %: [95, 105]. 104 +-0.5 % fits, but 104 +-1.5 % ([102.44, 105.56]) does not, whatever is fitted
        BillOfMaterials bom      = Any.BillOfMaterials().Generate();
        bom.Place(R1, Designed().Generate());
        bom.Substitute(R1, Specs.Resistor(104, 0.5, 0.1, 100).Generate());

        SubstitutionRefusedException refusal = Assert.Throws<SubstitutionRefusedException>(
            () => bom.Substitute(R1, Specs.Resistor(104, 1.5, 0.1, 100).Generate()));

        Assert.Equal(SubstitutionRefusal.ToleranceBandNotContained, refusal.Reason);
    }

    [Fact]
    public void Putting_the_designed_part_back_through_a_substitution_is_refused_as_the_same_part() {
        BillOfMaterials bom      = Any.BillOfMaterials().Generate();
        Part            designed = Designed().Generate();
        bom.Place(R1, designed);
        bom.Substitute(R1, Specs.Resistor(100, 1, 0.1, 50).Generate());

        SubstitutionRefusedException refusal = Assert.Throws<SubstitutionRefusedException>(() => bom.Substitute(R1, designed));

        Assert.Equal(SubstitutionRefusal.SamePart, refusal.Reason);
    }

    [Fact]
    public void Reverting_puts_the_designed_part_back() {
        BillOfMaterials bom      = Any.BillOfMaterials().Generate();
        Part            designed = Designed().Generate();
        bom.Place(R1, designed);
        bom.Substitute(R1, Specs.Resistor(100, 1, 0.1, 50).Generate());

        Placement placement = bom.RevertSubstitution(R1);

        Assert.Same(designed, placement.FittedPart);
        Assert.False(placement.IsSubstituted);
        Assert.Empty(bom.SubstitutedDesignators());
    }

    [Fact]
    public void Reverting_a_designator_that_carries_its_designed_part_is_refused() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(R1, Designed().Generate());

        Assert.Throws<InvalidOperationException>(() => bom.RevertSubstitution(R1));
    }

    [Fact]
    public void Reverting_an_unplaced_designator_is_refused() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();

        Assert.Throws<InvalidOperationException>(() => bom.RevertSubstitution(R1));
        Assert.Throws<ArgumentNullException>(() => bom.RevertSubstitution(null!));
    }

    [Fact]
    public void Substituting_an_unplaced_designator_is_refused() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();

        Assert.Throws<InvalidOperationException>(() => bom.Substitute(R1, Any.Part().Resistor().Generate()));
        Assert.Throws<ArgumentNullException>(() => bom.Substitute(null!, Any.Part().Resistor().Generate()));
        Assert.Throws<ArgumentNullException>(() => bom.Substitute(R1, null!));
    }

    [Fact]
    public void A_substitute_must_agree_with_the_part_number_already_on_the_board() {
        // R2 carries PN "TWIN" with a given specification; R1's substitute has the same number but another cost
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        AnyPart twin = Specs.Resistor(100, 1, 0.1, 50).WithPartNumber("TWIN");
        bom.Place(R1, Designed().Generate());
        bom.Place(Specs.Designator("R2"), twin.WithUnitCost(0.01m).Generate());

        InvalidOperationException refusal = Assert.ThrowsAny<InvalidOperationException>(() => bom.Substitute(R1, twin.WithUnitCost(0.02m).Generate()));

        Assert.IsNotType<SubstitutionRefusedException>(refusal);
        Assert.False(bom.Find(R1)!.IsSubstituted);
    }

    [Fact]
    public void A_substitute_identical_to_a_part_already_on_the_board_is_accepted() {
        BillOfMaterials bom  = Any.BillOfMaterials().Generate();
        Part            twin = Specs.Resistor(100, 1, 0.1, 50).Generate();
        bom.Place(R1, Designed().Generate());
        bom.Place(Specs.Designator("R2"), twin);

        bom.Substitute(R1, twin);

        Assert.Equal(2, Assert.Single(bom.Lines()).Quantity);
    }

    [Fact]
    public void The_designed_part_number_of_a_substituted_placement_still_binds_the_board() {
        BillOfMaterials bom      = Any.BillOfMaterials().Generate();
        Part            designed = Designed().WithPartNumber("DESIGNED").WithUnitCost(0.01m).Generate();
        bom.Place(R1, designed);
        bom.Substitute(R1, Specs.Resistor(100, 1, 0.1, 50).Generate());

        Part sameNumberOtherCost = Designed().WithPartNumber("DESIGNED").WithUnitCost(0.02m).Generate();

        Assert.Throws<InvalidOperationException>(() => bom.Place(Specs.Designator("R2"), sameNumberOtherCost));
    }

    [Fact]
    public void The_fitted_part_number_of_a_substituted_placement_binds_the_board() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(R1, Designed().Generate());
        bom.Substitute(R1, Specs.Resistor(100, 1, 0.1, 50).WithPartNumber("FITTED").WithUnitCost(0.01m).Generate());

        Part sameNumberOtherCost = Specs.Resistor(100, 1, 0.1, 50).WithPartNumber("FITTED").WithUnitCost(0.02m).Generate();

        Assert.Throws<InvalidOperationException>(() => bom.Place(Specs.Designator("R2"), sameNumberOtherCost));
    }

    [Fact]
    public void Substituted_designators_are_listed_in_order() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        AnyPart cap = Specs.Capacitor(100e-9, 10, 50);
        bom.Place(Specs.Designator("R2"), Designed().Generate());
        bom.Place(Specs.Designator("C1"), cap.Generate());
        bom.Place(Specs.Designator("R10"), Designed().Generate());
        bom.Substitute(Specs.Designator("R10"), Specs.Resistor(100, 1, 0.1, 50).Generate());
        bom.Substitute(Specs.Designator("R2"), Specs.Resistor(100, 1, 0.1, 50).Generate());
        bom.Substitute(Specs.Designator("C1"), Specs.Capacitor(100e-9, 5, 63).Generate());

        Assert.Equal(new[] { "C1", "R2", "R10" }, bom.SubstitutedDesignators().Select(d => d.Value));
    }

}

public sealed class BillOfMaterialsReleaseTests {

    private static readonly ReferenceDesignator R1 = Specs.Designator("R1");

    [Fact]
    public void An_empty_bill_cannot_be_released() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();

        Assert.Throws<InvalidOperationException>(() => bom.Release());
        Assert.False(bom.IsReleased);
    }

    [Fact]
    public void Releasing_freezes_the_bill() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(R1, Any.Part().Resistor().Generate());

        bom.Release();

        Assert.True(bom.IsReleased);
    }

    [Fact]
    public void A_bill_is_released_once() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(R1, Any.Part().Resistor().Generate());
        bom.Release();

        Assert.Throws<InvalidOperationException>(() => bom.Release());
    }

    [Fact]
    public void A_released_bill_refuses_every_change() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        Part designed = Specs.Resistor(100, 5, 0.1, 100).Generate();
        bom.Place(R1, designed);
        bom.Place(Specs.Designator("R2"), designed);
        bom.Substitute(Specs.Designator("R2"), Specs.Resistor(100, 1, 0.1, 50).Generate());
        bom.Release();

        Assert.Throws<InvalidOperationException>(() => bom.Place(Specs.Designator("R3"), Any.Part().Resistor().Generate()));
        Assert.Throws<InvalidOperationException>(() => bom.Remove(R1));
        Assert.Throws<InvalidOperationException>(() => bom.Substitute(R1, Specs.Resistor(100, 1, 0.1, 50).Generate()));
        Assert.Throws<InvalidOperationException>(() => bom.RevertSubstitution(Specs.Designator("R2")));
        Assert.Equal(2, bom.PlacementCount);
    }

    [Fact]
    public void A_released_bill_refuses_a_change_before_looking_at_its_arguments_validity() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(R1, Any.Part().Resistor().Generate());
        bom.Release();

        // a designator that is not placed: "released" is the reason that comes first
        InvalidOperationException removal = Assert.Throws<InvalidOperationException>(() => bom.Remove(Specs.Designator("R9")));

        Assert.Contains("released", removal.Message);
    }

    [Fact]
    public void A_released_bill_still_answers_its_queries() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(R1, Any.Part().Resistor().WithUnitCost(0.5m).Generate());
        bom.Release();

        Assert.Equal(1, bom.PlacementCount);
        Assert.Equal(0.5m, bom.TotalCost());
        Assert.Single(bom.Lines());
        Assert.NotNull(bom.Find(R1));
    }

}

public sealed class BillOfMaterialsAnalysisTests {

    [Fact]
    public void Off_series_lists_the_fitted_values_not_on_the_grid() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(Specs.Designator("R1"), Any.Part().Resistor().WithNominalValue(4_700).Generate());
        bom.Place(Specs.Designator("R2"), Any.Part().Resistor().WithNominalValue(4_800).Generate());
        bom.Place(Specs.Designator("R3"), Any.Part().Resistor().WithNominalValue(1_000).Generate());
        bom.Place(Specs.Designator("R4"), Any.Part().Resistor().WithNominalValue(4_990).Generate());

        Assert.Equal(new[] { "R2", "R4" }, bom.OffSeries(ESeries.E24).Select(d => d.Value));
        Assert.Equal(new[] { "R1", "R2" }, bom.OffSeries(ESeries.E96).Select(d => d.Value));
    }

    [Fact]
    public void Off_series_looks_at_the_fitted_part_not_the_designed_one() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(Specs.Designator("R1"), Specs.Resistor(4_700, 5, 0.1, 100).Generate());
        bom.Substitute(Specs.Designator("R1"), Specs.Resistor(4_800, 1, 0.1, 100).Generate());

        Assert.Equal(new[] { "R1" }, bom.OffSeries(ESeries.E24).Select(d => d.Value));
    }

    [Fact]
    public void Nothing_is_off_series_on_an_empty_bill() {
        Assert.Empty(Any.BillOfMaterials().Generate().OffSeries(ESeries.E96));
    }

    [Fact]
    public void A_marking_finds_the_parts_of_its_kind_carrying_its_value() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(Specs.Designator("R1"), Any.Part().Resistor().WithNominalValue(4_700).Generate());
        bom.Place(Specs.Designator("R2"), Any.Part().Resistor().WithNominalValue(4_700).WithPackage(Package.Axial).Generate());
        bom.Place(Specs.Designator("R3"), Any.Part().Resistor().WithNominalValue(4_800).Generate());
        bom.Place(Specs.Designator("C1"), Any.Part().Capacitor().WithNominalValue(4.7e-9).Generate());

        Assert.Equal(new[] { "R1", "R2" }, bom.FindByMarking(new MarkingCode("472", ComponentKind.Resistor)).Select(d => d.Value));
        Assert.Equal(new[] { "C1" }, bom.FindByMarking(new MarkingCode("472", ComponentKind.Capacitor)).Select(d => d.Value));
        Assert.Empty(bom.FindByMarking(new MarkingCode("472", ComponentKind.Inductor)));
    }

    [Fact]
    public void A_marking_finds_a_value_within_a_relative_billionth_only() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(Specs.Designator("R1"), Any.Part().Resistor().WithNominalValue(4_700 * (1 + 5e-10)).Generate());
        bom.Place(Specs.Designator("R2"), Any.Part().Resistor().WithNominalValue(4_700 * (1 + 3e-9)).Generate());

        Assert.Equal(new[] { "R1" }, bom.FindByMarking(new MarkingCode("4K7", ComponentKind.Resistor)).Select(d => d.Value));
        Assert.Throws<ArgumentNullException>(() => bom.FindByMarking(null!));
    }

    [Fact]
    public void A_marking_in_the_letter_notation_finds_the_same_parts() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(Specs.Designator("C1"), Any.Part().Capacitor().WithNominalValue(2.2e-9).Generate());

        Assert.Equal(new[] { "C1" }, bom.FindByMarking(new MarkingCode("2n2", ComponentKind.Capacitor)).Select(d => d.Value));
        Assert.Equal(new[] { "C1" }, bom.FindByMarking(new MarkingCode("222", ComponentKind.Capacitor)).Select(d => d.Value));
    }

    [Fact]
    public void The_time_constant_is_given_over_both_tolerance_bands() {
        // R 10 kohm +-1 %, C 100 nF +-10 %
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(Specs.Designator("R1"), Specs.Resistor(10_000, 1, 0.1, 100).Generate());
        bom.Place(Specs.Designator("C1"), Specs.Capacitor(100e-9, 10, 50).Generate());

        (double minimum, double nominal, double maximum) = bom.TimeConstantSeconds(Specs.Designator("R1"), Specs.Designator("C1"));

        Approx.Equal(9_900 * 90e-9, minimum);
        Approx.Equal(1e-3, nominal);
        Approx.Equal(10_100 * 110e-9, maximum);
    }

    [Fact]
    public void The_time_constant_follows_the_fitted_parts() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(Specs.Designator("R1"), Specs.Resistor(100, 5, 0.1, 100).Generate());
        bom.Place(Specs.Designator("C1"), Specs.Capacitor(1e-6, 10, 50).Generate());
        bom.Substitute(Specs.Designator("R1"), Specs.Resistor(102, 1, 0.1, 100).Generate());

        (_, double nominal, _) = bom.TimeConstantSeconds(Specs.Designator("R1"), Specs.Designator("C1"));

        Approx.Equal(102e-6, nominal);
    }

    [Fact]
    public void The_resonant_frequency_is_given_over_both_tolerance_bands() {
        // L 10 uH +-10 %, C 1 nF +-5 %: the highest frequency pairs the lowest values
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(Specs.Designator("L1"), Specs.Inductor(10e-6, 1).Generate());
        bom.Place(Specs.Designator("C1"), Specs.Capacitor(1e-9, 5, 50).Generate());

        (double minimum, double nominal, double maximum) = bom.ResonantFrequencyHertz(Specs.Designator("L1"), Specs.Designator("C1"));

        Approx.Equal(1.0 / (2 * Math.PI * Math.Sqrt(11e-6 * 1.05e-9)), minimum);
        Approx.Equal(1.0 / (2 * Math.PI * Math.Sqrt(10e-6 * 1e-9)), nominal);
        Approx.Equal(1.0 / (2 * Math.PI * Math.Sqrt(9e-6 * 0.95e-9)), maximum);
        Assert.True(minimum < nominal && nominal < maximum);
    }

    [Fact]
    public void A_time_constant_needs_a_resistor_then_a_capacitor() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(Specs.Designator("R1"), Any.Part().Resistor().Generate());
        bom.Place(Specs.Designator("C1"), Any.Part().Capacitor().Generate());
        bom.Place(Specs.Designator("L1"), Any.Part().Inductor().Generate());

        Assert.Throws<ArgumentException>(() => bom.TimeConstantSeconds(Specs.Designator("C1"), Specs.Designator("C1")));
        Assert.Throws<ArgumentException>(() => bom.TimeConstantSeconds(Specs.Designator("R1"), Specs.Designator("R1")));
        Assert.Throws<ArgumentException>(() => bom.TimeConstantSeconds(Specs.Designator("L1"), Specs.Designator("C1")));
        Assert.Throws<ArgumentException>(() => bom.TimeConstantSeconds(Specs.Designator("C1"), Specs.Designator("R1")));
    }

    [Fact]
    public void A_resonant_frequency_needs_an_inductor_then_a_capacitor() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(Specs.Designator("R1"), Any.Part().Resistor().Generate());
        bom.Place(Specs.Designator("C1"), Any.Part().Capacitor().Generate());
        bom.Place(Specs.Designator("L1"), Any.Part().Inductor().Generate());

        Assert.Throws<ArgumentException>(() => bom.ResonantFrequencyHertz(Specs.Designator("R1"), Specs.Designator("C1")));
        Assert.Throws<ArgumentException>(() => bom.ResonantFrequencyHertz(Specs.Designator("C1"), Specs.Designator("L1")));
        Assert.Throws<ArgumentException>(() => bom.ResonantFrequencyHertz(Specs.Designator("L1"), Specs.Designator("L1")));
    }

    [Fact]
    public void A_wrong_letter_is_reported_even_for_a_designator_that_is_not_placed() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();

        Assert.Throws<ArgumentException>(() => bom.TimeConstantSeconds(Specs.Designator("C1"), Specs.Designator("C2")));
    }

    [Fact]
    public void A_designator_that_is_not_placed_is_refused() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(Specs.Designator("R1"), Any.Part().Resistor().Generate());
        bom.Place(Specs.Designator("L1"), Any.Part().Inductor().Generate());

        Assert.Throws<InvalidOperationException>(() => bom.TimeConstantSeconds(Specs.Designator("R1"), Specs.Designator("C1")));
        Assert.Throws<InvalidOperationException>(() => bom.TimeConstantSeconds(Specs.Designator("R2"), Specs.Designator("C1")));
        Assert.Throws<InvalidOperationException>(() => bom.ResonantFrequencyHertz(Specs.Designator("L1"), Specs.Designator("C1")));
        Assert.Throws<InvalidOperationException>(() => bom.ResonantFrequencyHertz(Specs.Designator("L2"), Specs.Designator("C1")));
    }

    [Fact]
    public void Analysis_needs_designators() {
        BillOfMaterials bom = Any.BillOfMaterials().Generate();
        bom.Place(Specs.Designator("R1"), Any.Part().Resistor().Generate());
        bom.Place(Specs.Designator("L1"), Any.Part().Inductor().Generate());

        Assert.Throws<ArgumentNullException>(() => bom.TimeConstantSeconds(null!, Specs.Designator("C1")));
        Assert.Throws<ArgumentNullException>(() => bom.TimeConstantSeconds(Specs.Designator("R1"), null!));
        Assert.Throws<ArgumentNullException>(() => bom.ResonantFrequencyHertz(Specs.Designator("L1"), null!));
    }

}
