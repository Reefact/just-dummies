namespace Clinical.Domain;

/// <summary>One molecule, dosed, given by one route, for a duration.</summary>
public sealed class PrescriptionLine {

    public PrescriptionLine(Molecule molecule, Dosage dosage, RouteOfAdministration route, TreatmentDuration duration) {
        ArgumentNullException.ThrowIfNull(molecule);
        ArgumentNullException.ThrowIfNull(dosage);
        ArgumentNullException.ThrowIfNull(duration);
        _ = (dosage.Unit, route) switch {
            (DoseUnit.Tablet, not (RouteOfAdministration.Oral or RouteOfAdministration.Sublingual))
                => throw new ArgumentException($"A tablet cannot be given by the {route} route.", nameof(route)),
            (DoseUnit.Puff, not RouteOfAdministration.Inhaled)
                => throw new ArgumentException($"A puff can only be inhaled, not given by the {route} route.", nameof(route)),
            _ => route
        };

        Molecule = molecule;
        Dosage   = dosage;
        Route    = route;
        Duration = duration;
    }

    public Molecule Molecule { get; }

    public Dosage Dosage { get; }

    public RouteOfAdministration Route { get; }

    public TreatmentDuration Duration { get; }

    /// <summary>The total quantity to dispense over the whole treatment.</summary>
    public decimal TotalQuantity => Dosage.DailyAmount * Duration.Days;

}
