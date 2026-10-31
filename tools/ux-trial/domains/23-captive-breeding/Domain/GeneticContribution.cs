namespace CaptiveBreeding.Domain;

/// <summary>The expected proportion of the living population's genes that descends from one founder.</summary>
public sealed class GeneticContribution
{
    public GeneticContribution(StudbookId founderId, double proportion)
    {
        ArgumentNullException.ThrowIfNull(founderId);
        if (double.IsNaN(proportion) || proportion < 0.0 || proportion > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(proportion), proportion, "A genetic contribution lies in [0, 1].");
        }

        FounderId = founderId;
        Proportion = proportion;
    }

    public StudbookId FounderId { get; }

    public double Proportion { get; }

    public bool IsLost => Proportion == 0.0;

    public override string ToString() => $"{FounderId}: {Proportion:P1}";
}
