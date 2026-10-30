namespace Clinical.Domain;

public enum RouteOfAdministration {
    Oral,
    Sublingual,
    Intravenous,
    Intramuscular,
    Subcutaneous,
    Topical,
    Inhaled,
    Rectal
}

public enum DoseUnit {
    Milligram,
    Microgram,
    Millilitre,
    InternationalUnit,
    Tablet,
    Puff
}

public enum PrescriptionStatus {
    Draft,
    Signed,
    Dispensed,
    Cancelled
}

public enum Sex {
    Male,
    Female
}
