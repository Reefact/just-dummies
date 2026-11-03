using Xunit;

using Genealogy = BatchGenealogy.Domain.BatchGenealogy;

namespace BatchGenealogy.Domain.Tests;

/// <summary>What a genealogy holds at a moment, to show that a refused operation changed nothing.</summary>
internal sealed class Snapshot {

    private readonly int                              _transformations;
    private readonly int                              _links;
    private readonly Dictionary<string, string>       _lots;
    private readonly Genealogy                        _genealogy;

    private Snapshot(Genealogy genealogy) {
        _genealogy       = genealogy;
        _transformations = genealogy.Transformations.Count;
        _links           = genealogy.Links.Count;
        _lots            = genealogy.Lots.ToDictionary(lot => lot.Number.Value, lot => $"{lot.Status}|{lot.RemainingQuantity.Amount} {lot.RemainingQuantity.Unit}");
    }

    public static Snapshot Of(Genealogy genealogy) {
        return new Snapshot(genealogy);
    }

    public void AssertUnchanged() {
        Assert.Equal(_transformations, _genealogy.Transformations.Count);
        Assert.Equal(_links, _genealogy.Links.Count);
        Assert.Equal(_lots.OrderBy(pair => pair.Key), _genealogy.Lots.ToDictionary(lot => lot.Number.Value, lot => $"{lot.Status}|{lot.RemainingQuantity.Amount} {lot.RemainingQuantity.Unit}").OrderBy(pair => pair.Key));
    }

}
