using JustDummies;

using Xunit;

namespace Clinical.Domain;

public sealed class PrescriptionLineTests {

    [Fact]
    public void The_total_quantity_is_the_daily_amount_times_the_days_of_treatment() {
        // Arrange
        PrescriptionLine line = Any.PrescriptionLine()
                                   .WithDosage(d => d.WithAmount(2.5m).WithTimesPerDay(3))
                                   .WithDuration(new TreatmentDuration(10))
                                   .Generate();

        // Assert
        Assert.Equal(75m, line.TotalQuantity);
    }

    [Fact]
    public void The_total_quantity_keeps_the_decimals_of_the_dose() {
        // Arrange
        PrescriptionLine line = Any.PrescriptionLine()
                                   .WithDosage(d => d.WithAmount(0.001m).WithTimesPerDay(6))
                                   .WithDuration(new TreatmentDuration(365))
                                   .Generate();

        // Assert
        Assert.Equal(2.19m, line.TotalQuantity);
    }

    [Fact]
    public void The_line_keeps_what_it_was_given() {
        // Arrange
        Molecule          molecule = new Molecule("amoxicilline");
        Dosage            dosage   = new Dosage(1000m, DoseUnit.Milligram, 3);
        TreatmentDuration duration = new TreatmentDuration(7);

        // Act
        PrescriptionLine line = new PrescriptionLine(molecule, dosage, RouteOfAdministration.Oral, duration);

        // Assert
        Assert.Same(molecule, line.Molecule);
        Assert.Same(dosage, line.Dosage);
        Assert.Equal(RouteOfAdministration.Oral, line.Route);
        Assert.Same(duration, line.Duration);
    }

    [Fact]
    public void A_generated_line_is_always_accepted() {
        // Arrange
        PrescriptionLine anyLine = Any.PrescriptionLine().Generate();

        // Assert
        Assert.True(anyLine.TotalQuantity > 0m);
    }

    [Fact]
    public void A_null_molecule_is_a_null_argument_exception() {
        Dosage            anyDosage   = Any.Dosage().Generate();
        TreatmentDuration anyDuration = Any.TreatmentDuration().Generate();

        Assert.Throws<ArgumentNullException>(() => new PrescriptionLine(null!, anyDosage, RouteOfAdministration.Oral, anyDuration));
    }

    [Fact]
    public void A_null_dosage_is_a_null_argument_exception() {
        Molecule          anyMolecule = Any.Molecule().Generate();
        TreatmentDuration anyDuration = Any.TreatmentDuration().Generate();

        Assert.Throws<ArgumentNullException>(() => new PrescriptionLine(anyMolecule, null!, RouteOfAdministration.Oral, anyDuration));
    }

    [Fact]
    public void A_null_duration_is_a_null_argument_exception() {
        Molecule anyMolecule = Any.Molecule().Generate();
        Dosage   anyDosage   = Any.Dosage().Generate();

        Assert.Throws<ArgumentNullException>(() => new PrescriptionLine(anyMolecule, anyDosage, RouteOfAdministration.Oral, null!));
    }

    [Theory]
    [InlineData(RouteOfAdministration.Oral)]
    [InlineData(RouteOfAdministration.Sublingual)]
    public void A_tablet_dose_is_accepted_by_mouth(RouteOfAdministration route) {
        // Arrange
        Molecule          anyMolecule = Any.Molecule().Generate();
        Dosage            tablets     = Any.Dosage().WithUnit(DoseUnit.Tablet).Generate();
        TreatmentDuration anyDuration = Any.TreatmentDuration().Generate();

        // Act
        PrescriptionLine line = new PrescriptionLine(anyMolecule, tablets, route, anyDuration);

        // Assert
        Assert.Equal(route, line.Route);
    }

    [Theory]
    [InlineData(RouteOfAdministration.Intravenous)]
    [InlineData(RouteOfAdministration.Intramuscular)]
    [InlineData(RouteOfAdministration.Subcutaneous)]
    [InlineData(RouteOfAdministration.Topical)]
    [InlineData(RouteOfAdministration.Inhaled)]
    [InlineData(RouteOfAdministration.Rectal)]
    public void A_tablet_dose_is_refused_by_any_other_route(RouteOfAdministration route) {
        // Arrange
        Molecule          anyMolecule = Any.Molecule().Generate();
        Dosage            tablets     = Any.Dosage().WithUnit(DoseUnit.Tablet).Generate();
        TreatmentDuration anyDuration = Any.TreatmentDuration().Generate();

        // Act
        Action construct = () => new PrescriptionLine(anyMolecule, tablets, route, anyDuration);

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_puff_dose_is_accepted_inhaled() {
        // Arrange
        Molecule          anyMolecule = Any.Molecule().Generate();
        Dosage            puffs       = Any.Dosage().WithUnit(DoseUnit.Puff).Generate();
        TreatmentDuration anyDuration = Any.TreatmentDuration().Generate();

        // Act
        PrescriptionLine line = new PrescriptionLine(anyMolecule, puffs, RouteOfAdministration.Inhaled, anyDuration);

        // Assert
        Assert.Equal(RouteOfAdministration.Inhaled, line.Route);
    }

    [Fact]
    public void A_puff_dose_is_refused_by_any_route_but_inhaled() {
        // Arrange
        Molecule                anyMolecule      = Any.Molecule().Generate();
        Dosage                  puffs            = Any.Dosage().WithUnit(DoseUnit.Puff).Generate();
        TreatmentDuration       anyDuration      = Any.TreatmentDuration().Generate();
        RouteOfAdministration   anyOtherRoute    = Any.Enum<RouteOfAdministration>().DifferentFrom(RouteOfAdministration.Inhaled).Generate();

        // Act
        Action construct = () => new PrescriptionLine(anyMolecule, puffs, anyOtherRoute, anyDuration);

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_measured_dose_is_accepted_by_any_route() {
        // Arrange
        Molecule              anyMolecule  = Any.Molecule().Generate();
        DoseUnit              anyMeasure   = Any.Enum<DoseUnit>().Except(DoseUnit.Tablet, DoseUnit.Puff).Generate();
        Dosage                dosage       = Any.Dosage().WithUnit(anyMeasure).Generate();
        RouteOfAdministration anyRoute     = Any.Enum<RouteOfAdministration>().Generate();
        TreatmentDuration     anyDuration  = Any.TreatmentDuration().Generate();

        // Act
        PrescriptionLine line = new PrescriptionLine(anyMolecule, dosage, anyRoute, anyDuration);

        // Assert
        Assert.Same(dosage, line.Dosage);
    }

}
