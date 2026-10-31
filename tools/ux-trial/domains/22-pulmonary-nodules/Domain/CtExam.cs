using System.Text.RegularExpressions;

namespace PulmonaryNodules.Domain;

/// <summary>One thoracic CT acquisition: its identity, geometry and the landmarks located in it.</summary>
public sealed partial class CtExam
{
    public const double MinSliceThicknessMm = 0.5;
    public const double MaxSliceThicknessMm = 5.0;

    public CtExam(string accessionNumber, DateOnly acquiredOn, VoxelSpacing spacing, double sliceThicknessMm, IReadOnlyCollection<AnatomicalLandmark> landmarks)
    {
        ArgumentNullException.ThrowIfNull(accessionNumber);
        if (!AccessionFormat().IsMatch(accessionNumber))
        {
            throw new FormatException($"'{accessionNumber}' is not an accession number: expected 'CT' followed by 8 digits.");
        }

        ArgumentNullException.ThrowIfNull(spacing);
        if (double.IsNaN(sliceThicknessMm) || sliceThicknessMm < MinSliceThicknessMm || sliceThicknessMm > MaxSliceThicknessMm)
        {
            throw new ArgumentOutOfRangeException(nameof(sliceThicknessMm), sliceThicknessMm, $"A slice thickness must lie within [{MinSliceThicknessMm}, {MaxSliceThicknessMm}] mm.");
        }

        if (spacing.SliceMm > sliceThicknessMm)
        {
            throw new ArgumentException($"The slice spacing ({spacing.SliceMm} mm) cannot exceed the slice thickness ({sliceThicknessMm} mm): the volume would have gaps.", nameof(spacing));
        }

        ArgumentNullException.ThrowIfNull(landmarks);
        if (landmarks.Any(l => l is null))
        {
            throw new ArgumentException("A landmark list cannot contain null.", nameof(landmarks));
        }

        LandmarkKind[] duplicates = landmarks.GroupBy(l => l.Kind).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
        if (duplicates.Length > 0)
        {
            throw new ArgumentException($"Each landmark can be located once per exam; duplicated: {string.Join(", ", duplicates)}.", nameof(landmarks));
        }

        AccessionNumber = accessionNumber;
        AcquiredOn = acquiredOn;
        Spacing = spacing;
        SliceThicknessMm = sliceThicknessMm;
        Landmarks = landmarks.ToArray();
    }

    public string AccessionNumber { get; }

    public DateOnly AcquiredOn { get; }

    public VoxelSpacing Spacing { get; }

    public double SliceThicknessMm { get; }

    public IReadOnlyList<AnatomicalLandmark> Landmarks { get; }

    /// <summary>The transform mapping this exam's coordinates into those of <paramref name="followUp"/>.</summary>
    public TranslationTransform RegisterTo(CtExam followUp)
    {
        ArgumentNullException.ThrowIfNull(followUp);
        return TranslationTransform.Estimate(Landmarks, followUp.Landmarks);
    }

    [GeneratedRegex("^CT[0-9]{8}$")]
    private static partial Regex AccessionFormat();
}
