namespace VesselStowage.Domain;

/// <summary>
/// A bay/row/tier slot, ISO numbering: odd bays hold 20-foot boxes, even bays 40-foot boxes;
/// row 0 is the centreline, odd rows starboard, even rows port; tiers are even, 02-20 in the hold
/// and 82-98 on deck.
/// </summary>
public sealed class SlotPosition : IEquatable<SlotPosition>
{
    public const double RowWidthMetres = 2.44;
    public const double TierHeightMetres = 2.59;
    public const double HoldFloorMetres = 1.5;
    public const double HatchCoverMetres = 1.0;

    public SlotPosition(int bay, int row, int tier)
    {
        if (bay < 1 || bay > 99)
        {
            throw new ArgumentOutOfRangeException(nameof(bay), bay, "Bay must be between 1 and 99.");
        }

        if (row < 0 || row > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(row), row, "Row must be between 0 and 20.");
        }

        if (tier % 2 != 0 || !((tier >= 2 && tier <= 20) || (tier >= 82 && tier <= 98)))
        {
            throw new ArgumentOutOfRangeException(nameof(tier), tier, "Tier must be even, 02-20 (hold) or 82-98 (deck).");
        }

        Bay = bay;
        Row = row;
        Tier = tier;
    }

    public int Bay { get; }

    public int Row { get; }

    public int Tier { get; }

    public bool IsOnDeck => Tier >= 82;

    /// <summary>Zero-based level within its section (hold or deck).</summary>
    public int Level => IsOnDeck ? (Tier - 82) / 2 : (Tier - 2) / 2;

    public bool IsLowestOfSection => Level == 0;

    public ContainerSize AcceptedSize => Bay % 2 == 1 ? ContainerSize.TwentyFoot : ContainerSize.FortyFoot;

    /// <summary>Transverse offset from the centreline, positive to starboard.</summary>
    public double TransverseOffsetMetres => Row == 0 ? 0 : Row % 2 == 1 ? (Row + 1) / 2 * RowWidthMetres : -(Row / 2) * RowWidthMetres;

    /// <summary>Signed transverse index: 0 centreline, +n starboard, -n port.</summary>
    public int TransverseIndex => Row == 0 ? 0 : Row % 2 == 1 ? (Row + 1) / 2 : -(Row / 2);

    /// <summary>Height of the container's centre above the keel, assuming its weight sits mid-height.</summary>
    public double CentreHeightMetres(int holdTiers)
    {
        double baseHeight = IsOnDeck
            ? HoldFloorMetres + holdTiers * TierHeightMetres + HatchCoverMetres + Level * TierHeightMetres
            : HoldFloorMetres + Level * TierHeightMetres;
        return baseHeight + TierHeightMetres / 2;
    }

    public SlotPosition? Below() => IsLowestOfSection ? null : new SlotPosition(Bay, Row, Tier - 2);

    /// <summary>True when the two slots touch side by side or one on top of the other in the same bay and section.</summary>
    public bool IsAdjacentTo(SlotPosition other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other.Bay != Bay || other.IsOnDeck != IsOnDeck)
        {
            return false;
        }

        int dRow = Math.Abs(other.TransverseIndex - TransverseIndex);
        int dLevel = Math.Abs(other.Level - Level);
        return dRow + dLevel == 1;
    }

    public bool Equals(SlotPosition? other) => other is not null && other.Bay == Bay && other.Row == Row && other.Tier == Tier;

    public override bool Equals(object? obj) => Equals(obj as SlotPosition);

    public override int GetHashCode() => HashCode.Combine(Bay, Row, Tier);

    public override string ToString() => $"{Bay:D2}{Row:D2}{Tier:D2}";
}
