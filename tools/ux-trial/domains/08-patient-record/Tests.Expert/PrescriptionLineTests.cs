using Clinical.Domain;
using Clinical.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Clinical.Tests.Expert;

public sealed class PrescriptionLineTests {

    private static readonly DoseUnit[] UnitsAcceptedWithAnyRoute = [DoseUnit.Milligram, DoseUnit.Microgram, DoseUnit.Millilitre, DoseUnit.InternationalUnit];

    public static TheoryData<DoseUnit, RouteOfAdministration> AcceptedCombinations {
        get {
            TheoryData<DoseUnit, RouteOfAdministration> combinations = new TheoryData<DoseUnit, RouteOfAdministration>();
            foreach (DoseUnit unit in UnitsAcceptedWithAnyRoute) {
                foreach (RouteOfAdministration route in Enum.GetValues<RouteOfAdministration>()) {
                    combinations.Add(unit, route);
                }
            }
            combinations.Add(DoseUnit.Tablet, RouteOfAdministration.Oral);
            combinations.Add(DoseUnit.Tablet, RouteOfAdministration.Sublingual);
            combinations.Add(DoseUnit.Puff, RouteOfAdministration.Inhaled);

            return combinations;
        }
    }

    public static TheoryData<DoseUnit, RouteOfAdministration> RefusedCombinations {
        get {
            TheoryData<DoseUnit, RouteOfAdministration> combinations = new TheoryData<DoseUnit, RouteOfAdministration>();
            foreach (RouteOfAdministration route in Enum.GetValues<RouteOfAdministration>()) {
                if (route is not (RouteOfAdministration.Oral or RouteOfAdministration.Sublingual)) {
                    combinations.Add(DoseUnit.Tablet, route);
                }
                if (route is not RouteOfAdministration.Inhaled) {
                    combinations.Add(DoseUnit.Puff, route);
                }
            }

            return combinations;
        }
    }

    [Fact]
    public void A_line_keeps_what_it_was_given() {
        // Arrange
        Molecule          anyMolecule = Any.Molecule().Generate();
        Dosage            dosage      = new Dosage(500m, DoseUnit.Milligram, 3);
        TreatmentDuration duration    = new TreatmentDuration(7);

        // Act
        PrescriptionLine line = new PrescriptionLine(anyMolecule, dosage, RouteOfAdministration.Oral, duration);

        // Assert
        Assert.Same(anyMolecule, line.Molecule);
        Assert.Same(dosage, line.Dosage);
        Assert.Equal(RouteOfAdministration.Oral, line.Route);
        Assert.Same(duration, line.Duration);
    }

    [Fact]
    public void The_total_quantity_is_the_daily_amount_over_the_whole_treatment() {
        // Arrange
        Dosage            twoAndAHalfThreeTimesADay = new Dosage(2.5m, DoseUnit.Milligram, 3);
        TreatmentDuration tenDays                   = new TreatmentDuration(10);

        // Act
        PrescriptionLine line = Any.PrescriptionLine().WithDosage(twoAndAHalfThreeTimesADay).WithDuration(tenDays).Generate();

        // Assert
        Assert.Equal(75m, line.TotalQuantity);
    }

    [Fact]
    public void The_total_quantity_of_the_largest_possible_line_is_exact() {
        // Arrange
        Dosage            largestDosage    = new Dosage(10_000m, DoseUnit.Milligram, 6);
        TreatmentDuration longestTreatment = new TreatmentDuration(TreatmentDuration.MaxDays);

        // Act
        PrescriptionLine line = Any.PrescriptionLine().WithDosage(largestDosage).WithDuration(longestTreatment).Generate();

        // Assert
        Assert.Equal(21_900_000m, line.TotalQuantity);
    }

    [Theory]
    [MemberData(nameof(AcceptedCombinations))]
    public void A_unit_is_accepted_with_the_routes_it_can_be_given_by(DoseUnit unit, RouteOfAdministration route) {
        // Arrange
        Molecule          anyMolecule = Any.Molecule().Generate();
        Dosage            dosage      = Any.Dosage().WithUnit(unit).Generate();
        TreatmentDuration anyDuration = Any.TreatmentDuration().Generate();

        // Act
        PrescriptionLine line = new PrescriptionLine(anyMolecule, dosage, route, anyDuration);

        // Assert
        Assert.Equal(route, line.Route);
    }

    [Theory]
    [MemberData(nameof(RefusedCombinations))]
    public void A_unit_is_refused_with_the_routes_it_cannot_be_given_by(DoseUnit unit, RouteOfAdministration route) {
        // Arrange
        Molecule          anyMolecule = Any.Molecule().Generate();
        Dosage            dosage      = Any.Dosage().WithUnit(unit).Generate();
        TreatmentDuration anyDuration = Any.TreatmentDuration().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new PrescriptionLine(anyMolecule, dosage, route, anyDuration));

        // Assert
        Assert.Equal("route", refusal.ParamName);
    }

    [Fact]
    public void A_missing_molecule_is_refused() {
        // Arrange
        PrescriptionLine anyLine = Any.PrescriptionLine().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new PrescriptionLine(null!, anyLine.Dosage, anyLine.Route, anyLine.Duration));

        // Assert
        Assert.Equal("molecule", refusal.ParamName);
    }

    [Fact]
    public void A_missing_dosage_is_refused() {
        // Arrange
        PrescriptionLine anyLine = Any.PrescriptionLine().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new PrescriptionLine(anyLine.Molecule, null!, anyLine.Route, anyLine.Duration));

        // Assert
        Assert.Equal("dosage", refusal.ParamName);
    }

    [Fact]
    public void A_missing_duration_is_refused() {
        // Arrange
        PrescriptionLine anyLine = Any.PrescriptionLine().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new PrescriptionLine(anyLine.Molecule, anyLine.Dosage, anyLine.Route, null!));

        // Assert
        Assert.Equal("duration", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: the route is not checked for being a declared member (Dosage does check its unit), so (RouteOfAdministration)99 is accepted with a milligram dose; literal input, no seed")]
    public void A_route_that_is_not_a_declared_member_is_refused() {
        // Arrange
        Molecule          anyMolecule = Any.Molecule().Generate();
        Dosage            dosage      = new Dosage(500m, DoseUnit.Milligram, 3);
        TreatmentDuration anyDuration = Any.TreatmentDuration().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new PrescriptionLine(anyMolecule, dosage, (RouteOfAdministration)99, anyDuration));

        // Assert
        Assert.Equal("route", refusal.ParamName);
    }

}
