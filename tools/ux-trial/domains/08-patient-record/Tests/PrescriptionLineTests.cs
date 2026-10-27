using Clinical.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace Clinical.Tests;

[Reproducible]
public sealed class PrescriptionLineTests {

    [Fact]
    public void Total_quantity_is_the_dose_times_the_daily_intakes_times_the_days() {
        // Arrange
        PrescriptionLine anyLine = Any.PrescriptionLine()
                                      .WithDosage(d => d.WithAmount(500m).WithTimesPerDay(3))
                                      .WithDuration(new TreatmentDuration(7))
                                      .Generate();

        // Act
        decimal total = anyLine.TotalQuantity;

        // Assert
        Assert.Equal(10_500m, total);
    }

    [Fact]
    public void A_tablet_given_intravenously_is_refused() {
        // Arrange
        Molecule          anyMolecule = Any.Molecule().Generate();
        Dosage            anyTablets  = Any.Dosage().WithUnit(DoseUnit.Tablet).Generate();
        TreatmentDuration anyDuration = Any.TreatmentDuration().Generate();

        // Act
        Action prescribing = () => new PrescriptionLine(anyMolecule, anyTablets, RouteOfAdministration.Intravenous, anyDuration);

        // Assert
        Assert.Throws<ArgumentException>("route", prescribing);
    }

    [Fact]
    public void A_puff_given_by_a_route_other_than_inhalation_is_refused() {
        // Arrange
        Molecule              anyMolecule        = Any.Molecule().Generate();
        Dosage                anyPuffs           = Any.Dosage().WithUnit(DoseUnit.Puff).Generate();
        RouteOfAdministration anyNonInhaledRoute = Any.Enum<RouteOfAdministration>().DifferentFrom(RouteOfAdministration.Inhaled).Generate();
        TreatmentDuration     anyDuration        = Any.TreatmentDuration().Generate();

        // Act
        Action prescribing = () => new PrescriptionLine(anyMolecule, anyPuffs, anyNonInhaledRoute, anyDuration);

        // Assert
        Assert.Throws<ArgumentException>("route", prescribing);
    }

    [Fact]
    public void A_tablet_may_be_melted_under_the_tongue() {
        // Arrange
        PrescriptionLine anySublingualTabletLine = Any.PrescriptionLine()
                                                      .WithDosage(d => d.WithUnit(DoseUnit.Tablet))
                                                      .WithRoute(RouteOfAdministration.Sublingual)
                                                      .Generate();

        // Act
        RouteOfAdministration route = anySublingualTabletLine.Route;

        // Assert
        Assert.Equal(RouteOfAdministration.Sublingual, route);
    }

}
