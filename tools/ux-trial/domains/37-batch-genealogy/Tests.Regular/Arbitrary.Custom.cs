using JustDummies;

namespace BatchGenealogy.Domain;

/// <summary>Hand-written entry points for values that have no scaffolded generator.</summary>
public static partial class Arbitrary {

    /// <summary>A transformation id, <c>TRF-</c> and six digits.</summary>
    public static IAny<string> TransformationId() {
        return Any.StringMatching("TRF-[0-9]{6}");
    }

    /// <summary>A site code: four upper-case letters.</summary>
    public static IAny<string> SiteCode() {
        return Any.StringMatching("[A-Z]{4}");
    }

    /// <summary>A working day of 2025, when every lot drawn by <see cref="AnyLot" /> is consumable.</summary>
    public static IAny<DateOnly> ProductionDay() {
        return Any.DateOnly().Between(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
    }

}
