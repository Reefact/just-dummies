namespace PulmonaryNodules.Domain;

/// <summary>A nodule as segmented in one exam.</summary>
public sealed class NoduleObservation
{
    public NoduleObservation(CtExam exam, Segmentation segmentation)
    {
        ArgumentNullException.ThrowIfNull(exam);
        ArgumentNullException.ThrowIfNull(segmentation);
        if (!segmentation.Spacing.Equals(exam.Spacing))
        {
            throw new ArgumentException($"The segmentation spacing ({segmentation.Spacing}) differs from the exam's ({exam.Spacing}): it was not drawn on this exam.", nameof(segmentation));
        }

        Exam = exam;
        Segmentation = segmentation;
    }

    public CtExam Exam { get; }

    public Segmentation Segmentation { get; }

    public MeasuredVolume Volume => Segmentation.Measure();
}
