namespace Laboratory.Domain;

/// <summary>The pre-analytical quality of a specimen: volume drawn, hemolysis index, and allowed transit time.</summary>
public sealed class SpecimenQuality {

    public const double MinimumVolumeForChemistry = 0.5;

    public SpecimenQuality(double volumeMillilitres, int hemolysisIndex, TimeSpan maxTransit) {
        // Written this way so that NaN is refused too.
        if (!(volumeMillilitres > 0)) {
            throw new ArgumentOutOfRangeException(nameof(volumeMillilitres), "A specimen has a positive volume.");
        }
        if (volumeMillilitres > 50) {
            throw new ArgumentOutOfRangeException(nameof(volumeMillilitres), "A tube holds at most 50 mL.");
        }
        if (hemolysisIndex is < 0 or > 500) {
            throw new ArgumentOutOfRangeException(nameof(hemolysisIndex));
        }
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxTransit, TimeSpan.Zero);

        VolumeMillilitres = volumeMillilitres;
        HemolysisIndex    = hemolysisIndex;
        MaxTransit        = maxTransit;
    }

    public double   VolumeMillilitres { get; }
    public int      HemolysisIndex    { get; }
    public TimeSpan MaxTransit        { get; }

    /// <summary>Potassium is falsely raised by hemolysis: above an index of 100 it is not reported.</summary>
    public bool AcceptableForPotassium => HemolysisIndex <= 100 && VolumeMillilitres >= MinimumVolumeForChemistry;

}
