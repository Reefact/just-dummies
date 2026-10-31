namespace VesselStowage.Domain;

/// <summary>The ship's geometry and stability limits, from its loading manual.</summary>
public sealed class VesselProfile
{
    public VesselProfile(
        string name,
        int bayCount,
        int rowCount,
        int holdTiers,
        int deckTiers,
        decimal maxStackWeightTonnes,
        decimal lightshipTonnes,
        double lightshipVcgMetres,
        decimal maxDisplacementTonnes,
        double maxVcgMetres,
        double maxTcgMetres)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (bayCount < 1 || bayCount > 99)
        {
            throw new ArgumentOutOfRangeException(nameof(bayCount), bayCount, "Bay count must be between 1 and 99.");
        }

        if (rowCount < 1 || rowCount > 21)
        {
            throw new ArgumentOutOfRangeException(nameof(rowCount), rowCount, "Row count must be between 1 and 21.");
        }

        if (holdTiers < 1 || holdTiers > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(holdTiers), holdTiers, "Hold tiers must be between 1 and 10.");
        }

        if (deckTiers < 0 || deckTiers > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(deckTiers), deckTiers, "Deck tiers must be between 0 and 9.");
        }

        if (maxStackWeightTonnes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxStackWeightTonnes), maxStackWeightTonnes, "Stack weight limit must be positive.");
        }

        if (lightshipTonnes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lightshipTonnes), lightshipTonnes, "Lightship weight must be positive.");
        }

        if (maxDisplacementTonnes <= lightshipTonnes)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDisplacementTonnes), maxDisplacementTonnes, "Maximum displacement must exceed the lightship weight.");
        }

        if (lightshipVcgMetres <= 0 || double.IsNaN(lightshipVcgMetres) || double.IsInfinity(lightshipVcgMetres))
        {
            throw new ArgumentOutOfRangeException(nameof(lightshipVcgMetres), lightshipVcgMetres, "Lightship VCG must be a positive height.");
        }

        if (!(maxVcgMetres > lightshipVcgMetres) || double.IsInfinity(maxVcgMetres))
        {
            throw new ArgumentOutOfRangeException(nameof(maxVcgMetres), maxVcgMetres, "Maximum VCG must be above the lightship VCG.");
        }

        if (!(maxTcgMetres > 0) || double.IsInfinity(maxTcgMetres))
        {
            throw new ArgumentOutOfRangeException(nameof(maxTcgMetres), maxTcgMetres, "Maximum TCG must be positive.");
        }

        Name = name;
        BayCount = bayCount;
        RowCount = rowCount;
        HoldTiers = holdTiers;
        DeckTiers = deckTiers;
        MaxStackWeightTonnes = maxStackWeightTonnes;
        LightshipTonnes = lightshipTonnes;
        LightshipVcgMetres = lightshipVcgMetres;
        MaxDisplacementTonnes = maxDisplacementTonnes;
        MaxVcgMetres = maxVcgMetres;
        MaxTcgMetres = maxTcgMetres;
    }

    public string Name { get; }

    public int BayCount { get; }

    public int RowCount { get; }

    public int HoldTiers { get; }

    public int DeckTiers { get; }

    public decimal MaxStackWeightTonnes { get; }

    public decimal LightshipTonnes { get; }

    public double LightshipVcgMetres { get; }

    public decimal MaxDisplacementTonnes { get; }

    public double MaxVcgMetres { get; }

    public double MaxTcgMetres { get; }

    public bool Contains(SlotPosition slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        return slot.Bay <= BayCount
            && slot.Row < RowCount
            && slot.Level < (slot.IsOnDeck ? DeckTiers : HoldTiers);
    }

    public IEnumerable<SlotPosition> AllSlots()
    {
        for (int bay = 1; bay <= BayCount; bay++)
        {
            for (int row = 0; row < RowCount; row++)
            {
                for (int level = 0; level < HoldTiers; level++)
                {
                    yield return new SlotPosition(bay, row, 2 + 2 * level);
                }

                for (int level = 0; level < DeckTiers; level++)
                {
                    yield return new SlotPosition(bay, row, 82 + 2 * level);
                }
            }
        }
    }
}
