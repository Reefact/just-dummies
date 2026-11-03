using Clinical.Domain;

using JustDummies;

using Xunit;

namespace Clinical.Tests.Discovery;

public sealed class PrescriptionLineTests {

    public static TheoryData<RouteOfAdministration> AllRoutes => new TheoryData<RouteOfAdministration>(Enum.GetValues<RouteOfAdministration>());

    public static TheoryData<RouteOfAdministration> RoutesOtherThanOralAndSublingual =>
        new TheoryData<RouteOfAdministration>(Enum.GetValues<RouteOfAdministration>().Where(route => route is not (RouteOfAdministration.Oral or RouteOfAdministration.Sublingual)));

    public static TheoryData<RouteOfAdministration> RoutesOtherThanInhaled =>
        new TheoryData<RouteOfAdministration>(Enum.GetValues<RouteOfAdministration>().Where(route => route != RouteOfAdministration.Inhaled));

    [Fact]
    public void A_line_keeps_what_it_was_given() {
        // Arrange
        Molecule          anyMolecule = Any.Molecule().Generate();
        TreatmentDuration anyDuration = Any.TreatmentDuration().Generate();
        Dosage            dosage      = new Dosage(500m, DoseUnit.Milligram, 3);

        // Act
        PrescriptionLine line = new PrescriptionLine(anyMolecule, dosage, RouteOfAdministration.Oral, anyDuration);

        // Assert
        Assert.Same(anyMolecule, line.Molecule);
        Assert.Same(dosage, line.Dosage);
        Assert.Equal(RouteOfAdministration.Oral, line.Route);
        Assert.Same(anyDuration, line.Duration);
    }

    [Fact]
    public void A_null_molecule_is_refused() {
        // Arrange
        Dosage            anyDosage   = Any.Dosage().WithUnit(unit => unit.Except(DoseUnit.Tablet, DoseUnit.Puff)).Generate();
        TreatmentDuration anyDuration = Any.TreatmentDuration().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new PrescriptionLine(null!, anyDosage, RouteOfAdministration.Oral, anyDuration));

        // Assert
        Assert.Equal("molecule", refusal.ParamName);
    }

    [Fact]
    public void A_null_dosage_is_refused() {
        // Arrange
        Molecule          anyMolecule = Any.Molecule().Generate();
        TreatmentDuration anyDuration = Any.TreatmentDuration().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new PrescriptionLine(anyMolecule, null!, RouteOfAdministration.Oral, anyDuration));

        // Assert
        Assert.Equal("dosage", refusal.ParamName);
    }

    [Fact]
    public void A_null_duration_is_refused() {
        // Arrange
        Molecule anyMolecule = Any.Molecule().Generate();
        Dosage   anyDosage   = Any.Dosage().WithUnit(unit => unit.Except(DoseUnit.Tablet, DoseUnit.Puff)).Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new PrescriptionLine(anyMolecule, anyDosage, RouteOfAdministration.Oral, null!));

        // Assert
        Assert.Equal("duration", refusal.ParamName);
    }

    [Theory]
    [InlineData(RouteOfAdministration.Oral)]
    [InlineData(RouteOfAdministration.Sublingual)]
    public void A_tablet_is_given_by_mouth_or_under_the_tongue(RouteOfAdministration route) {
        // Arrange
        Molecule          anyMolecule = Any.Molecule().Generate();
        TreatmentDuration anyDuration = Any.TreatmentDuration().Generate();
        Dosage            tablets     = new Dosage(1m, DoseUnit.Tablet, 2);

        // Act
        PrescriptionLine line = new PrescriptionLine(anyMolecule, tablets, route, anyDuration);

        // Assert
        Assert.Equal(route, line.Route);
    }

    [Theory]
    [MemberData(nameof(RoutesOtherThanOralAndSublingual))]
    public void A_tablet_cannot_be_given_by_any_other_route(RouteOfAdministration route) {
        // Arrange
        Molecule          anyMolecule = Any.Molecule().Generate();
        TreatmentDuration anyDuration = Any.TreatmentDuration().Generate();
        Dosage            tablets     = new Dosage(1m, DoseUnit.Tablet, 2);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new PrescriptionLine(anyMolecule, tablets, route, anyDuration));

        // Assert
        Assert.Equal("route", refusal.ParamName);
    }

    [Fact]
    public void A_puff_is_inhaled() {
        // Arrange
        Molecule          anyMolecule = Any.Molecule().Generate();
        TreatmentDuration anyDuration = Any.TreatmentDuration().Generate();
        Dosage            puffs       = new Dosage(2m, DoseUnit.Puff, 4);

        // Act
        PrescriptionLine line = new PrescriptionLine(anyMolecule, puffs, RouteOfAdministration.Inhaled, anyDuration);

        // Assert
        Assert.Equal(RouteOfAdministration.Inhaled, line.Route);
    }

    [Theory]
    [MemberData(nameof(RoutesOtherThanInhaled))]
    public void A_puff_cannot_be_given_by_any_other_route(RouteOfAdministration route) {
        // Arrange
        Molecule          anyMolecule = Any.Molecule().Generate();
        TreatmentDuration anyDuration = Any.TreatmentDuration().Generate();
        Dosage            puffs       = new Dosage(2m, DoseUnit.Puff, 4);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new PrescriptionLine(anyMolecule, puffs, route, anyDuration));

        // Assert
        Assert.Equal("route", refusal.ParamName);
    }

    [Theory]
    [MemberData(nameof(AllRoutes))]
    public void A_milligram_dose_goes_with_any_route(RouteOfAdministration route) {
        // Arrange
        Molecule          anyMolecule = Any.Molecule().Generate();
        TreatmentDuration anyDuration = Any.TreatmentDuration().Generate();
        Dosage            milligrams  = new Dosage(10m, DoseUnit.Milligram, 1);

        // Act
        PrescriptionLine line = new PrescriptionLine(anyMolecule, milligrams, route, anyDuration);

        // Assert
        Assert.Equal(route, line.Route);
    }

    [Theory]
    [InlineData(DoseUnit.Microgram)]
    [InlineData(DoseUnit.Millilitre)]
    [InlineData(DoseUnit.InternationalUnit)]
    public void The_other_units_go_with_any_route(DoseUnit unit) {
        // Arrange
        Molecule          anyMolecule = Any.Molecule().Generate();
        TreatmentDuration anyDuration = Any.TreatmentDuration().Generate();
        Dosage            dosage      = new Dosage(10m, unit, 1);

        // Act: the ones a tablet or a puff cannot take
        PrescriptionLine intravenous = new PrescriptionLine(anyMolecule, dosage, RouteOfAdministration.Intravenous, anyDuration);
        PrescriptionLine inhaled     = new PrescriptionLine(anyMolecule, dosage, RouteOfAdministration.Inhaled, anyDuration);

        // Assert
        Assert.Equal(RouteOfAdministration.Intravenous, intravenous.Route);
        Assert.Equal(RouteOfAdministration.Inhaled, inhaled.Route);
    }

    [Fact]
    public void A_route_that_is_not_a_declared_value_is_accepted_for_a_unit_with_no_route_rule() {
        // Arrange: the model states the route is not checked for being a defined value.
        Molecule          anyMolecule = Any.Molecule().Generate();
        TreatmentDuration anyDuration = Any.TreatmentDuration().Generate();
        Dosage            milligrams  = new Dosage(10m, DoseUnit.Milligram, 1);

        // Act
        PrescriptionLine line = new PrescriptionLine(anyMolecule, milligrams, (RouteOfAdministration)99, anyDuration);

        // Assert
        Assert.Equal((RouteOfAdministration)99, line.Route);
    }

    [Fact]
    public void An_undeclared_route_is_still_refused_for_a_tablet() {
        // Arrange
        Molecule          anyMolecule = Any.Molecule().Generate();
        TreatmentDuration anyDuration = Any.TreatmentDuration().Generate();
        Dosage            tablets     = new Dosage(1m, DoseUnit.Tablet, 1);

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new PrescriptionLine(anyMolecule, tablets, (RouteOfAdministration)99, anyDuration));
    }

    [Fact]
    public void The_total_quantity_is_the_daily_amount_over_the_whole_treatment() {
        // Arrange
        Molecule anyMolecule = Any.Molecule().Generate();
        Dosage   dosage      = new Dosage(2.5m, DoseUnit.Millilitre, 3);   // 7.5 a day

        PrescriptionLine line = new PrescriptionLine(anyMolecule, dosage, RouteOfAdministration.Oral, new TreatmentDuration(10));

        // Act
        decimal total = line.TotalQuantity;

        // Assert
        Assert.Equal(75m, total);
    }

    [Fact]
    public void The_largest_possible_total_quantity_does_not_overflow() {
        // Arrange: 10 000 x 6 intakes x 365 days
        Molecule anyMolecule = Any.Molecule().Generate();
        Dosage   dosage      = new Dosage(10_000m, DoseUnit.Milligram, 6);

        PrescriptionLine line = new PrescriptionLine(anyMolecule, dosage, RouteOfAdministration.Oral, new TreatmentDuration(365));

        // Act / Assert
        Assert.Equal(21_900_000m, line.TotalQuantity);
    }

}
