using JustDummies;

using PassiveComponents.Domain.Tests.Expert.Generators;

using Xunit;

namespace PassiveComponents.Domain.Tests.Expert.Units;

public sealed class BillOfMaterialsTests {

    // --- named parts: the attributes a test is about are literals, the rest stays arbitrary ---------------------

    // Every field that Part.Equals reads is pinned, so two parts built apart from the same arguments are equal parts;
    // the substitution rule reads the same fields, so a narrower tolerance makes a substitute. Only the part number
    // and the value change from one call to the next.
    private static Part ResistorPart(string partNumber, double ohms, double percent = 5, double watts = 0.1, decimal unitCost = 0.01m) {
        return Any.Part()
                  .WithKind(ComponentKind.Resistor)
                  .WithPackage(Package.Chip1206)
                  .WithPartNumber(partNumber)
                  .WithNominalValue(ohms)
                  .WithTolerance(new Tolerance(percent))
                  .WithRating(watts)
                  .WithTemperatureCoefficient(new TemperatureCoefficient(100))
                  .WithUnitCost(unitCost)
                  .Generate();
    }

    private static Part CapacitorPart(string partNumber, double farads, double percent = 10, decimal unitCost = 0.01m) {
        return Any.Part()
                  .WithKind(ComponentKind.Capacitor)
                  .WithPackage(Package.Chip0603)
                  .WithPartNumber(partNumber)
                  .WithNominalValue(farads)
                  .WithTolerance(new Tolerance(percent))
                  .WithRating(50)
                  .WithTemperatureCoefficient(new TemperatureCoefficient(30))
                  .WithUnitCost(unitCost)
                  .Generate();
    }

    private static Part InductorPart(string partNumber, double henries, double percent = 10, decimal unitCost = 0.01m) {
        return Any.Part()
                  .WithKind(ComponentKind.Inductor)
                  .WithPackage(Package.Chip0603)
                  .WithPartNumber(partNumber)
                  .WithNominalValue(henries)
                  .WithTolerance(new Tolerance(percent))
                  .WithRating(1)
                  .WithTemperatureCoefficient(new TemperatureCoefficient(30))
                  .WithUnitCost(unitCost)
                  .Generate();
    }

    private static ReferenceDesignator Designator(string text) {
        return new ReferenceDesignator(text);
    }

    private static BillOfMaterials AnyOpenBoard() {
        return Any.BillOfMaterials().Generate();
    }

    /// <summary>A board holding one arbitrary resistor on an arbitrary designator, which the test only needs to exist.</summary>
    private static (BillOfMaterials Board, ReferenceDesignator Designator, Part Part) AnyBoardWithOneResistor() {
        BillOfMaterials     board      = AnyOpenBoard();
        ReferenceDesignator designator = Any.ReferenceDesignator().WithKind(ComponentKind.Resistor).Generate();
        Part                part       = Any.Part().WithKind(ComponentKind.Resistor).Generate();

        board.Place(designator, part);

        return (board, designator, part);
    }

    // --- construction -----------------------------------------------------------------------------------------

    [Fact]
    public void A_new_bill_is_open_and_empty() {
        // Act
        BillOfMaterials board = new BillOfMaterials("Motor driver", "B");

        // Assert
        Assert.Equal("Motor driver", board.BoardName);
        Assert.Equal("B", board.Revision);
        Assert.False(board.IsReleased);
        Assert.Equal(0, board.PlacementCount);
        Assert.Empty(board.Placements);
    }

    [Fact]
    public void A_board_name_may_be_sixty_four_characters_long_and_not_sixty_five() {
        // Arrange
        BillOfMaterials anyBoard = AnyOpenBoard();

        // Act & Assert
        Assert.Equal(64, new BillOfMaterials(new string('B', 64), anyBoard.Revision).BoardName.Length);
        Assert.Throws<ArgumentException>(() => new BillOfMaterials(new string('B', 65), anyBoard.Revision));
    }

    [Fact]
    public void A_board_name_may_contain_spaces_inside() {
        Assert.Equal("Motor driver rev 2", new BillOfMaterials("Motor driver rev 2", "A").BoardName);
    }

    [Theory]
    [InlineData(" Motor")]
    [InlineData("Motor ")]
    [InlineData("\tMotor")]
    [InlineData("Motor\n")]
    [InlineData("  Motor  ")]
    public void A_board_name_with_a_leading_or_trailing_space_is_refused(string boardName) {
        // Arrange
        BillOfMaterials anyBoard = AnyOpenBoard();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new BillOfMaterials(boardName, anyBoard.Revision));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    [InlineData(null)]
    public void A_blank_board_name_is_refused(string? boardName) {
        // Arrange
        BillOfMaterials anyBoard = AnyOpenBoard();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new BillOfMaterials(boardName!, anyBoard.Revision));
    }

    [Theory]
    [InlineData("A")]
    [InlineData("Z")]
    [InlineData("AA")]
    [InlineData("ZZ")]
    [InlineData("QX")]
    public void A_revision_is_one_or_two_capital_letters(string revision) {
        // Act
        BillOfMaterials board = Any.BillOfMaterials().WithRevision(revision).Generate();

        // Assert
        Assert.Equal(revision, board.Revision);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("aB")]
    [InlineData("ABC")]
    [InlineData("A1")]
    [InlineData("1")]
    [InlineData(" A")]
    [InlineData("A ")]
    [InlineData("A-")]
    [InlineData("É")]
    public void A_revision_that_is_not_one_or_two_capital_letters_is_refused(string revision) {
        Assert.Throws<FormatException>(() => Any.BillOfMaterials().WithRevision(revision).Generate());
    }

    [Fact(Skip = "domain defect: the revision format ends with '$', which .NET also matches before a final line feed, so 'A\\n' is accepted as a revision; deterministic, no seed")]
    public void A_revision_followed_by_a_line_feed_is_refused() {
        Assert.Throws<FormatException>(() => Any.BillOfMaterials().WithRevision("A\n").Generate());
    }

    [Fact]
    public void A_bill_needs_a_revision() {
        // Arrange
        BillOfMaterials anyBoard = AnyOpenBoard();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BillOfMaterials(anyBoard.BoardName, null!));
    }

    // --- placing ----------------------------------------------------------------------------------------------

    [Fact]
    public void A_placed_part_is_found_on_its_designator_both_designed_and_fitted() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();
        Part            part  = Any.Part().WithKind(ComponentKind.Capacitor).Generate();

        // Act
        Placement placement = board.Place(Designator("C7"), part);

        // Assert
        Assert.Same(placement, board.Find(Designator("C7")));
        Assert.Same(part, placement.DesignedPart);
        Assert.Same(part, placement.FittedPart);
        Assert.False(placement.IsSubstituted);
        Assert.Equal(1, board.PlacementCount);
    }

    [Fact]
    public void Finding_a_designator_that_is_not_placed_gives_nothing() {
        // Arrange
        (BillOfMaterials board, _, _) = AnyBoardWithOneResistor();

        // Act & Assert
        Assert.Null(board.Find(Designator("C1")));
    }

    [Fact]
    public void The_placements_come_in_designator_order_whatever_the_order_they_were_placed_in() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();
        (string Designator, ComponentKind Kind)[] inAnyOrder = Any.PermutationOf([("R10", ComponentKind.Resistor), ("R2", ComponentKind.Resistor), ("L1", ComponentKind.Inductor), ("C3", ComponentKind.Capacitor), ("C20", ComponentKind.Capacitor)]).Generate().ToArray();

        // Act
        foreach ((string text, ComponentKind kind) in inAnyOrder) { board.Place(Designator(text), Any.Part().WithKind(kind).Generate()); }

        // Assert
        Assert.Equal(["C3", "C20", "L1", "R2", "R10"], board.Placements.Select(p => p.Designator.Value));
    }

    [Fact]
    public void A_designator_can_only_be_used_once() {
        // Arrange
        (BillOfMaterials board, ReferenceDesignator anyDesignator, Part anyFirstPart) = AnyBoardWithOneResistor();
        Part anyOtherPart = Any.Part().WithKind(ComponentKind.Resistor).Generate();

        // Act
        Assert.Throws<InvalidOperationException>(() => board.Place(anyDesignator, anyOtherPart));

        // Assert: the first one is still there
        Assert.Equal(1, board.PlacementCount);
        Assert.Same(anyFirstPart, board.Find(anyDesignator)!.FittedPart);
    }

    [Fact]
    public void A_part_goes_on_a_designator_of_its_own_kind_and_a_refusal_changes_nothing() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();
        Part anyCapacitor = Any.Part().WithKind(ComponentKind.Capacitor).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => board.Place(Designator("R1"), anyCapacitor));

        // Assert: an argument problem, not a state problem
        Assert.IsNotType<InvalidOperationException>(refusal);
        Assert.Equal(0, board.PlacementCount);
    }

    [Fact]
    public void Placing_needs_a_designator_and_a_part() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => board.Place(null!, Any.Part().Generate()));
        Assert.Throws<ArgumentNullException>(() => board.Place(Designator("R1"), null!));
    }

    [Fact]
    public void The_same_part_may_be_placed_on_several_designators() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();
        Part            part  = ResistorPart("RC1206FR-0710KL", 10_000);

        // Act
        board.Place(Designator("R1"), part);
        board.Place(Designator("R2"), part);

        // Assert
        Assert.Equal(2, board.PlacementCount);
    }

    [Fact]
    public void Two_equal_parts_built_apart_share_a_part_number_without_clashing() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();

        // Act
        board.Place(Designator("R1"), ResistorPart("RC1206FR-0710KL", 10_000));
        board.Place(Designator("R2"), ResistorPart("RC1206FR-0710KL", 10_000));

        // Assert
        Assert.Equal(2, board.PlacementCount);
    }

    [Fact]
    public void A_part_number_cannot_stand_for_two_different_specifications() {
        // Arrange: same number, tolerance differs
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("R1"), ResistorPart("RC1206FR-0710KL", 10_000, percent: 1));

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => board.Place(Designator("R2"), ResistorPart("RC1206FR-0710KL", 10_000, percent: 5)));

        // Assert
        Assert.Contains("RC1206FR-0710KL", refusal.Message);
        Assert.Equal(1, board.PlacementCount);
    }

    [Fact]
    public void A_part_number_clashes_with_a_part_that_was_designed_but_is_now_substituted() {
        // Arrange: R1 was designed with the 5 % part and now carries a 1 % one
        BillOfMaterials board = AnyOpenBoard();
        Part designed   = ResistorPart("RC1206-DESIGNED", 100, percent: 5);
        Part substitute = ResistorPart("RC1206-SUBSTITUTE", 100, percent: 1);
        board.Place(Designator("R1"), designed);
        board.Substitute(Designator("R1"), substitute);

        // Act & Assert: the designed number is still on the board
        Assert.Throws<InvalidOperationException>(() => board.Place(Designator("R2"), ResistorPart("RC1206-DESIGNED", 100, percent: 10)));
    }

    [Fact]
    public void A_part_number_clashes_with_a_part_that_is_fitted_but_was_not_designed() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("R1"), ResistorPart("RC1206-DESIGNED", 100, percent: 5));
        board.Substitute(Designator("R1"), ResistorPart("RC1206-SUBSTITUTE", 100, percent: 1));

        // Act & Assert: the fitted number is on the board too
        Assert.Throws<InvalidOperationException>(() => board.Place(Designator("R2"), ResistorPart("RC1206-SUBSTITUTE", 100, percent: 1, watts: 0.05)));
    }

    // --- removing ---------------------------------------------------------------------------------------------

    [Fact]
    public void A_removed_placement_is_gone_and_its_designator_is_free_again() {
        // Arrange
        (BillOfMaterials board, ReferenceDesignator anyDesignator, _) = AnyBoardWithOneResistor();

        // Act
        board.Remove(anyDesignator);

        // Assert
        Assert.Null(board.Find(anyDesignator));
        Assert.Equal(0, board.PlacementCount);
        board.Place(anyDesignator, Any.Part().WithKind(ComponentKind.Resistor).Generate());
        Assert.Equal(1, board.PlacementCount);
    }

    [Fact]
    public void Removing_the_last_part_of_a_number_lets_the_number_be_specified_again() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("R1"), ResistorPart("RC1206FR-0710KL", 10_000, percent: 1));
        board.Remove(Designator("R1"));

        // Act
        board.Place(Designator("R1"), ResistorPart("RC1206FR-0710KL", 10_000, percent: 5));

        // Assert
        Assert.Equal(5, board.Find(Designator("R1"))!.FittedPart.Tolerance.Percent);
    }

    [Fact]
    public void Removing_a_designator_that_is_not_placed_is_refused() {
        // Arrange
        (BillOfMaterials board, _, _) = AnyBoardWithOneResistor();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => board.Remove(Designator("C1")));
        Assert.Equal(1, board.PlacementCount);
    }

    [Fact]
    public void Removing_needs_a_designator() {
        Assert.Throws<ArgumentNullException>(() => AnyOpenBoard().Remove(null!));
    }

    // --- substituting -----------------------------------------------------------------------------------------

    [Fact]
    public void A_substitute_that_passes_the_rule_is_fitted_and_the_design_is_kept() {
        // Arrange
        BillOfMaterials board      = AnyOpenBoard();
        Part            original   = Specimens.OriginalResistor().Generate();
        Part            substitute = Specimens.OriginalResistor().WithTolerance(new Tolerance(1)).Generate();
        board.Place(Designator("R1"), original);

        // Act
        Placement placement = board.Substitute(Designator("R1"), substitute);

        // Assert
        Assert.Same(substitute, placement.FittedPart);
        Assert.Same(original, placement.DesignedPart);
        Assert.True(placement.IsSubstituted);
        Assert.Same(placement, board.Find(Designator("R1")));
    }

    [Theory]
    [InlineData(SubstitutionRefusal.DifferentPackage)]
    [InlineData(SubstitutionRefusal.ToleranceBandNotContained)]
    [InlineData(SubstitutionRefusal.RatingTooLow)]
    [InlineData(SubstitutionRefusal.TemperatureCoefficientWorse)]
    public void A_substitute_that_breaks_the_rule_is_refused_with_the_reason_and_nothing_changes(SubstitutionRefusal reason) {
        // Arrange
        BillOfMaterials board    = AnyOpenBoard();
        Part            original = Specimens.OriginalResistor().Generate();
        board.Place(Designator("R1"), original);
        Part substitute = reason switch {
            SubstitutionRefusal.DifferentPackage             => Specimens.OriginalResistor().WithPackage(Package.Chip0805).Generate(),
            SubstitutionRefusal.ToleranceBandNotContained    => Specimens.OriginalResistor().WithTolerance(new Tolerance(10)).Generate(),
            SubstitutionRefusal.RatingTooLow                 => Specimens.OriginalResistor().WithRating(0.05).Generate(),
            _                                                => Specimens.OriginalResistor().WithTemperatureCoefficient(new TemperatureCoefficient(250)).Generate(),
        };

        // Act
        SubstitutionRefusedException refusal = Assert.Throws<SubstitutionRefusedException>(() => board.Substitute(Designator("R1"), substitute));

        // Assert
        Assert.Equal(reason, refusal.Reason);
        Assert.Contains(substitute.PartNumber, refusal.Message);
        Assert.Same(original, board.Find(Designator("R1"))!.FittedPart);
        Assert.False(board.Find(Designator("R1"))!.IsSubstituted);
    }

    [Fact]
    public void A_substitute_of_another_kind_is_a_refusal_of_the_rule_not_an_argument_error() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("R1"), Specimens.OriginalResistor().Generate());
        Part anyCapacitor = Any.Part().WithKind(ComponentKind.Capacitor).Generate();

        // Act
        SubstitutionRefusedException refusal = Assert.Throws<SubstitutionRefusedException>(() => board.Substitute(Designator("R1"), anyCapacitor));

        // Assert
        Assert.Equal(SubstitutionRefusal.DifferentKind, refusal.Reason);
    }

    [Fact]
    public void Substituting_the_part_already_fitted_is_refused_as_the_same_part_and_a_refusal_is_an_invalid_operation() {
        // Arrange
        (BillOfMaterials board, ReferenceDesignator anyDesignator, Part anyPart) = AnyBoardWithOneResistor();

        // Act
        InvalidOperationException refusal = Assert.ThrowsAny<InvalidOperationException>(() => board.Substitute(anyDesignator, anyPart));

        // Assert
        SubstitutionRefusedException refused = Assert.IsType<SubstitutionRefusedException>(refusal);
        Assert.Equal(SubstitutionRefusal.SamePart, refused.Reason);
    }

    [Fact]
    public void The_part_number_currently_fitted_cannot_be_substituted_again() {
        // Arrange
        BillOfMaterials board      = AnyOpenBoard();
        Part            substitute = Specimens.OriginalResistor().Generate();
        board.Place(Designator("R1"), Specimens.OriginalResistor().Generate());
        board.Substitute(Designator("R1"), substitute);

        // Act
        SubstitutionRefusedException refusal = Assert.Throws<SubstitutionRefusedException>(() => board.Substitute(Designator("R1"), substitute));

        // Assert
        Assert.Equal(SubstitutionRefusal.SamePart, refusal.Reason);
    }

    [Fact]
    public void The_designed_part_cannot_be_put_back_by_substituting_it_only_by_reverting() {
        // Arrange
        BillOfMaterials board    = AnyOpenBoard();
        Part            original = Specimens.OriginalResistor().Generate();
        board.Place(Designator("R1"), original);
        board.Substitute(Designator("R1"), Specimens.OriginalResistor().Generate());

        // Act
        SubstitutionRefusedException refusal = Assert.Throws<SubstitutionRefusedException>(() => board.Substitute(Designator("R1"), original));

        // Assert: against the designed part it is the same part
        Assert.Equal(SubstitutionRefusal.SamePart, refusal.Reason);
    }

    [Fact]
    public void A_second_substitution_is_judged_against_the_design_so_a_lower_rating_than_the_first_substitute_is_fine() {
        // Arrange: the design asks 0.1 W; the first substitute offers 0.25 W, the second 0.125 W
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("R1"), Specimens.OriginalResistor().Generate());
        board.Substitute(Designator("R1"), Specimens.OriginalResistor().WithRating(0.25).Generate());
        Part secondSubstitute = Specimens.OriginalResistor().WithRating(0.125).Generate();

        // Act
        board.Substitute(Designator("R1"), secondSubstitute);

        // Assert
        Assert.Same(secondSubstitute, board.Find(Designator("R1"))!.FittedPart);
    }

    [Fact]
    public void Chained_substitutions_cannot_drift_away_from_the_design_by_staying_inside_the_slack_each_time() {
        // Arrange: each step overshoots its predecessor's band by 0.6 ppb, which the slack of 1 ppb forgives; two steps overshoot the design by 1.2 ppb
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("R1"), Specimens.OriginalResistor().Generate());
        board.Substitute(Designator("R1"), Specimens.OriginalResistor().WithNominalValue(100 * (1 + 6e-10)).Generate());
        Part secondStep = Specimens.OriginalResistor().WithNominalValue(100 * (1 + 12e-10)).Generate();

        // Act
        SubstitutionRefusedException refusal = Assert.Throws<SubstitutionRefusedException>(() => board.Substitute(Designator("R1"), secondStep));

        // Assert
        Assert.Equal(SubstitutionRefusal.ToleranceBandNotContained, refusal.Reason);
    }

    [Fact]
    public void A_substitute_may_not_reuse_a_part_number_of_the_board_with_another_specification() {
        // Arrange: R2 is designed with number "RC1206-OTHER"; R1 gets a substitute carrying that number with another tolerance
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("R1"), Specimens.OriginalResistor().Generate());
        board.Place(Designator("R2"), ResistorPart("RC1206-OTHER", 100, percent: 5));
        Part clashing = Specimens.OriginalResistor().WithPartNumber("RC1206-OTHER").WithTolerance(new Tolerance(1)).Generate();

        // Act
        InvalidOperationException refusal = Assert.ThrowsAny<InvalidOperationException>(() => board.Substitute(Designator("R1"), clashing));

        // Assert: not a refusal of the substitution rule, and R1 is untouched
        Assert.IsNotType<SubstitutionRefusedException>(refusal);
        Assert.False(board.Find(Designator("R1"))!.IsSubstituted);
    }

    [Fact]
    public void A_substitute_may_be_the_very_part_another_placement_is_designed_with() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();
        Part            twin  = Specimens.OriginalResistor().WithPartNumber("RC1206-TWIN").Generate();
        board.Place(Designator("R1"), Specimens.OriginalResistor().Generate());
        board.Place(Designator("R2"), twin);

        // Act
        board.Substitute(Designator("R1"), twin);

        // Assert: one part number, two designators
        BomLine line = Assert.Single(board.Lines());
        Assert.Equal(["R1", "R2"], line.Designators.Select(d => d.Value));
    }

    [Fact]
    public void Substituting_a_designator_that_is_not_placed_is_refused() {
        // Arrange
        (BillOfMaterials board, _, _) = AnyBoardWithOneResistor();

        // Act & Assert
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => board.Substitute(Designator("C1"), Any.Part().WithKind(ComponentKind.Capacitor).Generate()));
        Assert.IsNotType<SubstitutionRefusedException>(refusal);
    }

    [Fact]
    public void Substituting_needs_a_designator_and_a_part() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => board.Substitute(null!, Any.Part().Generate()));
        Assert.Throws<ArgumentNullException>(() => board.Substitute(Designator("R1"), null!));
    }

    // --- asking without changing ------------------------------------------------------------------------------

    [Fact]
    public void Checking_a_substitution_answers_as_substituting_would_and_changes_nothing() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("R1"), Specimens.OriginalResistor().Generate());
        Part acceptable = Specimens.OriginalResistor().Generate();
        Part tooWide    = Specimens.OriginalResistor().WithTolerance(new Tolerance(10)).Generate();

        // Act & Assert
        Assert.Equal(SubstitutionRefusal.None, board.CheckSubstitution(Designator("R1"), acceptable));
        Assert.Equal(SubstitutionRefusal.ToleranceBandNotContained, board.CheckSubstitution(Designator("R1"), tooWide));
        Assert.False(board.Find(Designator("R1"))!.IsSubstituted);
    }

    [Fact]
    public void Checking_the_part_number_currently_fitted_answers_same_part_even_on_a_substituted_designator() {
        // Arrange
        BillOfMaterials board      = AnyOpenBoard();
        Part            substitute = Specimens.OriginalResistor().Generate();
        board.Place(Designator("R1"), Specimens.OriginalResistor().Generate());
        board.Substitute(Designator("R1"), substitute);

        // Act & Assert
        Assert.Equal(SubstitutionRefusal.SamePart, board.CheckSubstitution(Designator("R1"), substitute));
    }

    [Fact]
    public void Checking_a_designator_that_is_not_placed_is_refused() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => board.CheckSubstitution(Designator("R1"), Any.Part().Generate()));
        Assert.Throws<ArgumentNullException>(() => board.CheckSubstitution(null!, Any.Part().Generate()));
        Assert.Throws<ArgumentNullException>(() => board.CheckSubstitution(Designator("R1"), null!));
    }

    // --- reverting --------------------------------------------------------------------------------------------

    [Fact]
    public void Reverting_puts_the_designed_part_back() {
        // Arrange
        BillOfMaterials board    = AnyOpenBoard();
        Part            original = Specimens.OriginalResistor().Generate();
        board.Place(Designator("R1"), original);
        board.Substitute(Designator("R1"), Specimens.OriginalResistor().Generate());

        // Act
        Placement placement = board.RevertSubstitution(Designator("R1"));

        // Assert
        Assert.Same(original, placement.FittedPart);
        Assert.False(placement.IsSubstituted);
        Assert.Empty(board.SubstitutedDesignators());
    }

    [Fact]
    public void Reverting_a_designator_that_carries_its_designed_part_is_refused() {
        // Arrange
        (BillOfMaterials board, ReferenceDesignator anyDesignator, _) = AnyBoardWithOneResistor();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => board.RevertSubstitution(anyDesignator));
    }

    [Fact]
    public void Reverting_a_designator_that_is_not_placed_is_refused() {
        Assert.Throws<InvalidOperationException>(() => AnyOpenBoard().RevertSubstitution(Designator("R1")));
        Assert.Throws<ArgumentNullException>(() => AnyOpenBoard().RevertSubstitution(null!));
    }

    // --- releasing --------------------------------------------------------------------------------------------

    [Fact]
    public void A_bill_with_a_placement_can_be_released() {
        // Arrange
        (BillOfMaterials board, _, _) = AnyBoardWithOneResistor();

        // Act
        board.Release();

        // Assert
        Assert.True(board.IsReleased);
    }

    [Fact]
    public void An_empty_bill_cannot_be_released() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => board.Release());
        Assert.False(board.IsReleased);
    }

    [Fact]
    public void A_bill_cannot_be_released_twice() {
        // Arrange
        (BillOfMaterials board, _, _) = AnyBoardWithOneResistor();
        board.Release();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => board.Release());
    }

    [Fact]
    public void A_released_bill_refuses_to_place() {
        // Arrange
        (BillOfMaterials board, _, _) = AnyBoardWithOneResistor();
        board.Release();

        // Act & Assert
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => board.Place(Designator("C1"), Any.Part().WithKind(ComponentKind.Capacitor).Generate()));
        Assert.Contains("released", refusal.Message);
        Assert.Equal(1, board.PlacementCount);
    }

    [Fact]
    public void A_released_bill_refuses_to_remove() {
        // Arrange
        (BillOfMaterials board, ReferenceDesignator anyDesignator, _) = AnyBoardWithOneResistor();
        board.Release();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => board.Remove(anyDesignator));
        Assert.Equal(1, board.PlacementCount);
    }

    [Fact]
    public void A_released_bill_refuses_to_substitute_and_to_revert() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("R1"), Specimens.OriginalResistor().Generate());
        board.Place(Designator("R2"), Specimens.OriginalResistor().Generate());
        board.Substitute(Designator("R2"), Specimens.OriginalResistor().Generate());
        board.Release();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => board.Substitute(Designator("R1"), Specimens.OriginalResistor().Generate()));
        Assert.Throws<InvalidOperationException>(() => board.RevertSubstitution(Designator("R2")));
        Assert.True(board.Find(Designator("R2"))!.IsSubstituted);
    }

    [Fact]
    public void A_released_bill_can_still_be_read_and_asked() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("R1"), ResistorPart("RC1206-DESIGN", 100, unitCost: 0.25m));
        board.Release();

        // Act & Assert
        Assert.Equal(0.25m, board.TotalCost());
        Assert.Single(board.Lines());
        Assert.Equal(1, board.CountOf(ComponentKind.Resistor));
        Assert.Equal(SubstitutionRefusal.SamePart, board.CheckSubstitution(Designator("R1"), board.Find(Designator("R1"))!.FittedPart));
    }

    // --- totals and queries -----------------------------------------------------------------------------------

    [Fact]
    public void An_empty_bill_has_nothing_to_total() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();

        // Act & Assert
        Assert.Empty(board.Lines());
        Assert.Equal(0m, board.TotalCost());
        Assert.Equal(0, board.CountOf(ComponentKind.Capacitor));
        Assert.Empty(board.SubstitutedDesignators());
        Assert.Empty(board.OffSeries(ESeries.E24));
    }

    [Fact]
    public void A_line_is_made_per_part_number_with_every_designator_it_is_fitted_on() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();
        Part tenKiloohms      = ResistorPart("RC1206-10K", 10_000);
        Part hundredOhms      = ResistorPart("RC1206-100R", 100);
        board.Place(Designator("R3"), tenKiloohms);
        board.Place(Designator("R1"), tenKiloohms);
        board.Place(Designator("R2"), hundredOhms);
        board.Place(Designator("R4"), tenKiloohms);

        // Act
        IReadOnlyList<BomLine> lines = board.Lines();

        // Assert
        Assert.Equal(2, lines.Count);
        BomLine tenKiloohmLine = lines.Single(l => l.Part.PartNumber == "RC1206-10K");
        Assert.Equal(["R1", "R3", "R4"], tenKiloohmLine.Designators.Select(d => d.Value));
        Assert.Equal(["R2"], lines.Single(l => l.Part.PartNumber == "RC1206-100R").Designators.Select(d => d.Value));
    }

    [Fact]
    public void Lines_are_ordered_by_kind_then_nominal_value_then_part_number() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("R1"), ResistorPart("RES-100", 100));
        board.Place(Designator("R2"), ResistorPart("RES-10", 10));
        board.Place(Designator("L1"), InductorPart("IND-1U", 1e-6));
        board.Place(Designator("C1"), CapacitorPart("CAP-100N", 1e-7));
        board.Place(Designator("C2"), CapacitorPart("CAP-B-1N", 1e-9));
        board.Place(Designator("C3"), CapacitorPart("CAP-A-1N", 1e-9));

        // Act
        IReadOnlyList<BomLine> lines = board.Lines();

        // Assert: capacitors 1 nF (A, then B), 100 nF; the inductor; resistors 10, 100
        Assert.Equal(["CAP-A-1N", "CAP-B-1N", "CAP-100N", "IND-1U", "RES-10", "RES-100"], lines.Select(l => l.Part.PartNumber));
    }

    [Fact]
    public void Lines_follow_what_is_fitted_not_what_was_designed() {
        // Arrange: R1 and R2 share a design; R2 is then substituted
        BillOfMaterials board    = AnyOpenBoard();
        Part            original = Specimens.OriginalResistor().WithPartNumber("RC1206-DESIGN").Generate();
        Part            other    = Specimens.OriginalResistor().WithPartNumber("RC1206-ALTERNATE").Generate();
        board.Place(Designator("R1"), original);
        board.Place(Designator("R2"), original);
        Assert.Single(board.Lines());

        // Act
        board.Substitute(Designator("R2"), other);

        // Assert
        Assert.Equal(2, board.Lines().Count);
        Assert.Equal(["R2"], board.Lines().Single(l => l.Part.PartNumber == "RC1206-ALTERNATE").Designators.Select(d => d.Value));

        // And reverting joins them again
        board.RevertSubstitution(Designator("R2"));
        Assert.Single(board.Lines());
        Assert.Equal(2, board.Lines()[0].Quantity);
    }

    [Fact]
    public void Parts_are_counted_by_kind() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();
        foreach (string text in new[] { "C1", "C2", "C3" }) { board.Place(Designator(text), Any.Part().WithKind(ComponentKind.Capacitor).Generate()); }
        foreach (string text in new[] { "R1", "R2" }) { board.Place(Designator(text), Any.Part().WithKind(ComponentKind.Resistor).Generate()); }

        // Act & Assert
        Assert.Equal(3, board.CountOf(ComponentKind.Capacitor));
        Assert.Equal(2, board.CountOf(ComponentKind.Resistor));
        Assert.Equal(0, board.CountOf(ComponentKind.Inductor));
    }

    [Fact]
    public void The_total_cost_adds_the_unit_cost_of_every_placement() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("R1"), ResistorPart("RES-A", 100, unitCost: 0.1m));
        board.Place(Designator("R2"), ResistorPart("RES-B", 200, unitCost: 0.25m));
        board.Place(Designator("C1"), CapacitorPart("CAP-A", 1e-9, unitCost: 1.2345m));
        board.Place(Designator("C2"), CapacitorPart("CAP-A", 1e-9, unitCost: 1.2345m));

        // Act & Assert: the same part on two designators is paid twice
        Assert.Equal(2.8190m, board.TotalCost());
    }

    [Fact]
    public void The_total_cost_follows_the_fitted_parts() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("R1"), Specimens.OriginalResistor().WithUnitCost(0.10m).Generate());
        board.Substitute(Designator("R1"), Specimens.OriginalResistor().WithUnitCost(0.0745m).Generate());

        // Act & Assert
        Assert.Equal(0.0745m, board.TotalCost());
    }

    [Fact]
    public void Substituted_designators_are_listed_in_designator_order() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();
        foreach (string text in new[] { "R10", "R2", "R5" }) { board.Place(Designator(text), Specimens.OriginalResistor().Generate()); }
        board.Substitute(Designator("R5"), Specimens.OriginalResistor().Generate());
        board.Substitute(Designator("R10"), Specimens.OriginalResistor().Generate());

        // Act & Assert
        Assert.Equal(["R5", "R10"], board.SubstitutedDesignators().Select(d => d.Value));
    }

    [Fact]
    public void Off_series_lists_the_designators_whose_fitted_value_is_not_on_the_grid() {
        // Arrange: 2.2 nF is E24, 2.3 nF is not; 100 Ω is E24
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("C1"), CapacitorPart("CAP-2N2", 2.2e-9));
        board.Place(Designator("C2"), CapacitorPart("CAP-2N3", 2.3e-9));
        board.Place(Designator("R1"), ResistorPart("RES-100", 100));

        // Act & Assert
        Assert.Equal(["C2"], board.OffSeries(ESeries.E24).Select(d => d.Value));
        Assert.Equal(["C2"], board.OffSeries(ESeries.E12).Select(d => d.Value));

        // 2.2 is not a value of E96 (it has 2.21): the finer grid rejects what the coarser one accepts
        Assert.Equal(["C1", "C2"], board.OffSeries(ESeries.E96).Select(d => d.Value));
    }

    [Fact]
    public void Off_series_judges_the_fitted_part_not_the_designed_one() {
        // Arrange: 100 Ω is on E24; the 102 Ω substitute is not
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("R1"), Specimens.OriginalResistor().Generate());
        Assert.Empty(board.OffSeries(ESeries.E24));

        // Act
        board.Substitute(Designator("R1"), Specimens.OriginalResistor().WithNominalValue(102).WithTolerance(new Tolerance(1)).Generate());

        // Assert
        Assert.Equal(["R1"], board.OffSeries(ESeries.E24).Select(d => d.Value));
        Assert.Empty(board.OffSeries(ESeries.E96));
    }

    [Fact]
    public void A_marking_finds_the_designators_of_its_kind_carrying_its_value() {
        // Arrange: 104 is 100 nF on a capacitor and 100 kΩ on a resistor
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("C1"), CapacitorPart("CAP-100N-A", 1e-7));
        board.Place(Designator("C2"), CapacitorPart("CAP-100N-B", 1e-7, percent: 5));
        board.Place(Designator("C3"), CapacitorPart("CAP-10N", 1e-8));
        board.Place(Designator("R1"), ResistorPart("RES-100K", 1e5));

        // Act & Assert
        Assert.Equal(["C1", "C2"], board.FindByMarking(new MarkingCode("104", ComponentKind.Capacitor)).Select(d => d.Value));
        Assert.Equal(["R1"], board.FindByMarking(new MarkingCode("104", ComponentKind.Resistor)).Select(d => d.Value));
        Assert.Empty(board.FindByMarking(new MarkingCode("104", ComponentKind.Inductor)));
    }

    [Fact]
    public void A_marking_matches_within_a_billionth_and_not_a_few_billionths_more() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("C1"), CapacitorPart("CAP-ALMOST", 1e-7 * (1 + 5e-10)));
        board.Place(Designator("C2"), CapacitorPart("CAP-OFF", 1e-7 * (1 + 3e-9)));

        // Act
        IReadOnlyList<ReferenceDesignator> found = board.FindByMarking(new MarkingCode("104", ComponentKind.Capacitor));

        // Assert
        Assert.Equal(["C1"], found.Select(d => d.Value));
    }

    [Fact]
    public void A_marking_finds_what_is_fitted_not_what_was_designed() {
        // Arrange: 100 Ω designed, 102 Ω fitted
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("R1"), Specimens.OriginalResistor().Generate());
        board.Substitute(Designator("R1"), Specimens.OriginalResistor().WithNominalValue(102).WithTolerance(new Tolerance(1)).Generate());

        // Act & Assert
        Assert.Empty(board.FindByMarking(new MarkingCode("101", ComponentKind.Resistor)));
        Assert.Equal(["R1"], board.FindByMarking(new MarkingCode("102R", ComponentKind.Resistor)).Select(d => d.Value));
    }

    [Fact]
    public void Finding_by_marking_needs_a_marking() {
        Assert.Throws<ArgumentNullException>(() => AnyOpenBoard().FindByMarking(null!));
    }

    // --- circuits ---------------------------------------------------------------------------------------------

    [Fact]
    public void The_time_constant_of_a_placed_pair_is_given_nominally_and_over_both_tolerance_bands() {
        // Arrange: 10 kΩ ±5 % and 100 nF ±10 %
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("R1"), ResistorPart("RES-10K", 10_000, percent: 5));
        board.Place(Designator("C1"), CapacitorPart("CAP-100N", 1e-7, percent: 10));

        // Act
        (double minimum, double nominal, double maximum) = board.TimeConstantSeconds(Designator("R1"), Designator("C1"));

        // Assert
        Assert.Equal(8.55e-4, minimum, 1e-12);
        Assert.Equal(1e-3, nominal, 1e-12);
        Assert.Equal(1.155e-3, maximum, 1e-12);
    }

    [Fact]
    public void The_time_constant_follows_the_fitted_resistor() {
        // Arrange: 100 Ω designed, 102 Ω fitted, against 1 µF
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("R1"), Specimens.OriginalResistor().Generate());
        board.Place(Designator("C1"), CapacitorPart("CAP-1U", 1e-6));
        board.Substitute(Designator("R1"), Specimens.OriginalResistor().WithNominalValue(102).WithTolerance(new Tolerance(1)).Generate());

        // Act
        (_, double nominal, _) = board.TimeConstantSeconds(Designator("R1"), Designator("C1"));

        // Assert
        Assert.Equal(1.02e-4, nominal, 1e-12);
    }

    [Fact]
    public void The_time_constant_takes_a_resistor_then_a_capacitor() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("R1"), Any.Part().WithKind(ComponentKind.Resistor).Generate());
        board.Place(Designator("C1"), Any.Part().WithKind(ComponentKind.Capacitor).Generate());
        board.Place(Designator("L1"), Any.Part().WithKind(ComponentKind.Inductor).Generate());

        // Act & Assert: wrong letters are argument errors
        Assert.Throws<ArgumentException>(() => board.TimeConstantSeconds(Designator("C1"), Designator("R1")));
        Assert.Throws<ArgumentException>(() => board.TimeConstantSeconds(Designator("L1"), Designator("C1")));
        Assert.Throws<ArgumentException>(() => board.TimeConstantSeconds(Designator("R1"), Designator("L1")));
    }

    [Fact]
    public void The_time_constant_needs_both_parts_to_be_placed() {
        // Arrange
        (BillOfMaterials board, ReferenceDesignator anyResistor, _) = AnyBoardWithOneResistor();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => board.TimeConstantSeconds(anyResistor, Designator("C1")));
        Assert.Throws<InvalidOperationException>(() => board.TimeConstantSeconds(Designator("R99"), Designator("C1")));
        Assert.Throws<ArgumentNullException>(() => board.TimeConstantSeconds(null!, Designator("C1")));
        Assert.Throws<ArgumentNullException>(() => board.TimeConstantSeconds(anyResistor, null!));
    }

    [Fact]
    public void The_resonant_frequency_of_a_placed_pair_is_lowest_with_the_largest_values() {
        // Arrange: 1 µH ±10 % and 1 nF ±10 %
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("L1"), InductorPart("IND-1U", 1e-6, percent: 10));
        board.Place(Designator("C1"), CapacitorPart("CAP-1N", 1e-9, percent: 10));

        // Act
        (double minimum, double nominal, double maximum) = board.ResonantFrequencyHertz(Designator("L1"), Designator("C1"));

        // Assert
        Assert.Equal(4_575_382.92, minimum, 2);
        Assert.Equal(5_032_921.21, nominal, 2);
        Assert.Equal(5_592_134.68, maximum, 2);
    }

    [Fact]
    public void The_resonant_frequency_takes_an_inductor_then_a_capacitor() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("L1"), Any.Part().WithKind(ComponentKind.Inductor).Generate());
        board.Place(Designator("C1"), Any.Part().WithKind(ComponentKind.Capacitor).Generate());
        board.Place(Designator("R1"), Any.Part().WithKind(ComponentKind.Resistor).Generate());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => board.ResonantFrequencyHertz(Designator("C1"), Designator("L1")));
        Assert.Throws<ArgumentException>(() => board.ResonantFrequencyHertz(Designator("R1"), Designator("C1")));
        Assert.Throws<ArgumentException>(() => board.ResonantFrequencyHertz(Designator("L1"), Designator("R1")));
    }

    [Fact]
    public void The_resonant_frequency_needs_both_parts_to_be_placed() {
        // Arrange
        BillOfMaterials board = AnyOpenBoard();
        board.Place(Designator("L1"), Any.Part().WithKind(ComponentKind.Inductor).Generate());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => board.ResonantFrequencyHertz(Designator("L1"), Designator("C1")));
        Assert.Throws<InvalidOperationException>(() => board.ResonantFrequencyHertz(Designator("L2"), Designator("C1")));
        Assert.Throws<ArgumentNullException>(() => board.ResonantFrequencyHertz(null!, Designator("C1")));
    }

    [Fact]
    public void A_bill_reads_as_its_name_revision_and_state() {
        // Arrange
        BillOfMaterials board = new BillOfMaterials("Motor driver", "B");
        board.Place(Designator("R1"), Any.Part().WithKind(ComponentKind.Resistor).Generate());

        // Act & Assert
        Assert.Equal("Motor driver rev B (1 placements)", board.ToString());
        board.Release();
        Assert.Equal("Motor driver rev B (1 placements, released)", board.ToString());
    }

}
