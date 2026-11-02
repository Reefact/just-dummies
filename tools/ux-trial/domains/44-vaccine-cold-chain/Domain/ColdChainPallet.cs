using Serialisation.Domain;
using Warehouse.Domain;

namespace ColdChain.Domain;

/// <summary>
/// An SSCC-labelled pallet of serialised packs of one product, travelling with its own data logger.
/// Several batches of the product may share a pallet; every pack must still be active in the repository.
/// </summary>
public sealed class ColdChainPallet
{
    public const int MaxPacks = 5_000;

    private readonly SerialisedPack[] _packs;

    public ColdChainPallet(Sscc sscc, DataLogger logger, IReadOnlyList<SerialisedPack> packs)
    {
        ArgumentNullException.ThrowIfNull(sscc);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(packs);
        if (packs.Count == 0 || packs.Count > MaxPacks)
        {
            throw new ArgumentException($"A pallet carries 1 to {MaxPacks} packs, not {packs.Count}.", nameof(packs));
        }

        if (packs.Any(p => p is null))
        {
            throw new ArgumentException("A pallet cannot list a missing pack.", nameof(packs));
        }

        if (packs.Select(p => p.Serial).Distinct().Count() != packs.Count)
        {
            throw new ArgumentException("A serial number appears twice on the pallet.", nameof(packs));
        }

        if (packs.Any(p => p.Status != PackStatus.Active))
        {
            throw new ArgumentException("Only active packs are shipped; a decommissioned pack has left the supply chain.", nameof(packs));
        }

        Gtin gtin = packs[0].Gtin;
        if (packs.Any(p => !p.Gtin.Equals(gtin)))
        {
            throw new ArgumentException("A cold-chain pallet carries a single product (one GTIN).", nameof(packs));
        }

        Sscc = sscc;
        Logger = logger;
        _packs = packs.ToArray();
    }

    public Sscc Sscc { get; }

    public DataLogger Logger { get; }

    public IReadOnlyList<SerialisedPack> Packs => _packs;

    public Gtin Gtin => _packs[0].Gtin;

    public int PackCount => _packs.Length;

    public IReadOnlyCollection<BatchNumber> Batches => _packs.Select(p => p.Batch).Distinct().ToArray();

    public DateOnly EarliestExpiry => _packs.Min(p => p.ExpiresOn);

    public bool Carries(SerialNumber serial) => _packs.Any(p => p.Serial.Equals(serial));

    public override string ToString() => $"{Sscc} ({PackCount} packs)";
}
