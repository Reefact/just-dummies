namespace RealEstate.Domain;

// Written by hand, beside the scaffolded file.
public sealed partial class AnyProperty {

    /// <summary>A property that can be let from 2025 to 2030 and for years after: never banned (rated A to D), diagnosed between 2022 and 2024.</summary>
    public AnyProperty Lettable() {
        return WithDiagnosis(d => d.WithPrimaryEnergyKwhPerSquareMeterYear(e => e.LessThan(250))
                                   .WithEmissionsKgCo2PerSquareMeterYear(e => e.LessThan(50))
                                   .WithIssuedOn(i => i.Between(new DateOnly(2022, 1, 1), new DateOnly(2024, 12, 31))));
    }

}
