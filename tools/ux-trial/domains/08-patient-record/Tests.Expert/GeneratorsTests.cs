using Clinical.Domain;
using Clinical.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Clinical.Tests.Expert;

/// <summary>
///     The generators are code the suite relies on: a draw they got wrong would fail some other test on some runs only.
///     Each is run many times in one test, so a flaw shows up here, named, rather than somewhere else, at random.
/// </summary>
public sealed class GeneratorsTests {

    private const int Draws = 300;

    [Fact]
    public void Every_drawn_nir_is_accepted_by_the_domain() {
        // Act
        List<Nir> nirs = Any.ListOf(Any.Nir()).WithCount(Draws).Generate();

        // Assert
        Assert.Equal(Draws, nirs.Count);
    }

    [Fact]
    public void A_drawn_nir_reaches_corsica_and_both_sexes_over_many_draws() {
        // Act
        List<Nir> nirs = Any.ListOf(Any.Nir()).WithCount(2_000).Generate();

        // Assert
        Assert.Contains(nirs, n => n.Value.Substring(5, 2) == "2A");
        Assert.Contains(nirs, n => n.Value.Substring(5, 2) == "2B");
        Assert.Contains(nirs, n => n.Sex == Sex.Male);
        Assert.Contains(nirs, n => n.Sex == Sex.Female);
    }

    [Fact]
    public void The_key_the_generator_computes_agrees_with_the_domain_on_every_draw() {
        // Act
        List<Nir> nirs = Any.ListOf(Any.Nir()).WithCount(Draws).Generate();

        // Assert
        Assert.All(nirs, n => Assert.Equal(Nir.ComputeKey(n.Value.Substring(0, 13)), int.Parse(n.Value.Substring(13, 2))));
    }

    [Fact]
    public void Every_drawn_dosage_is_accepted_by_the_domain() {
        // Act
        List<Dosage> dosages = Any.ListOf(Any.Dosage()).WithCount(Draws).Generate();

        // Assert
        Assert.Equal(Draws, dosages.Count);
    }

    [Fact]
    public void Every_drawn_molecule_is_accepted_by_the_domain() {
        // Act
        List<Molecule> molecules = Any.ListOf(Any.Molecule()).WithCount(Draws).Generate();

        // Assert
        Assert.Equal(Draws, molecules.Count);
    }

    [Fact]
    public void Every_drawn_patient_is_accepted_by_the_domain() {
        // Act
        List<Patient> patients = Any.ListOf(Any.Patient()).WithCount(Draws).Generate();

        // Assert
        Assert.Equal(Draws, patients.Count);
    }

    [Fact]
    public void A_patient_whose_nir_is_pinned_is_born_in_the_year_and_month_that_nir_carries() {
        // Arrange
        Nir nirBornInJune1985 = Any.Nir().BornIn(85, 6).Generate();

        // Act
        List<Patient> patients = Any.ListOf(Any.Patient().WithNir(nirBornInJune1985)).WithCount(Draws).Generate();

        // Assert
        Assert.All(patients, p => Assert.Equal(1985, p.BirthDate.Year));
        Assert.All(patients, p => Assert.Equal(6, p.BirthDate.Month));
    }

    [Fact]
    public void Every_drawn_line_is_accepted_by_the_domain_whatever_is_pinned_on_it() {
        // Act
        List<PrescriptionLine> free       = Any.ListOf(Any.PrescriptionLine()).WithCount(Draws).Generate();
        List<PrescriptionLine> tablets    = Any.ListOf(Any.PrescriptionLine().WithDosage(d => d.WithUnit(DoseUnit.Tablet))).WithCount(Draws).Generate();
        List<PrescriptionLine> puffs      = Any.ListOf(Any.PrescriptionLine().WithRoute(RouteOfAdministration.Inhaled)).WithCount(Draws).Generate();
        List<PrescriptionLine> injections = Any.ListOf(Any.PrescriptionLine().WithRoute(RouteOfAdministration.Intravenous)).WithCount(Draws).Generate();

        // Assert
        Assert.All(tablets, l => Assert.True(l.Route is RouteOfAdministration.Oral or RouteOfAdministration.Sublingual));
        Assert.All(injections, l => Assert.NotEqual(DoseUnit.Tablet, l.Dosage.Unit));
        Assert.All(injections, l => Assert.NotEqual(DoseUnit.Puff, l.Dosage.Unit));
        Assert.All(puffs, l => Assert.NotEqual(DoseUnit.Tablet, l.Dosage.Unit));
        Assert.Equal(Draws, free.Count);
    }

    [Fact]
    public void Every_drawn_prescription_is_accepted_by_the_domain_whatever_is_pinned_on_it() {
        // Act
        List<Prescription> free    = Any.ListOf(Any.Prescription()).WithCount(Draws).Generate();
        List<Prescription> issued  = Any.ListOf(Any.Prescription().WithIssuedOn(Dates.On("1950-06-15"))).WithCount(Draws).Generate();
        Patient            patient = Any.Patient().WithBirthDate(Dates.On("2012-12-12")).Generate();
        List<Prescription> forThem = Any.ListOf(Any.Prescription().WithPatient(patient)).WithCount(Draws).Generate();

        // Assert
        Assert.All(issued, p => Assert.True(p.Patient.BirthDate <= Dates.On("1950-06-15")));
        Assert.All(forThem, p => Assert.True(p.IssuedOn >= Dates.On("2012-12-12")));
        Assert.Equal(Draws, free.Count);
    }

}
