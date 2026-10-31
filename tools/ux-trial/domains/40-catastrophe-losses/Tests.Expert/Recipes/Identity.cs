using CatastropheLosses.Domain;

namespace CatastropheLosses.Tests.Expert.Recipes;

/// <summary>Equality by the identity the domain enforces, for distinct collections of entities.</summary>
public static class EventIdentity {

    public static readonly IEqualityComparer<CatastropheEvent> Comparer =
        EqualityComparer<CatastropheEvent>.Create((left, right) => left?.Id == right?.Id, e => e.Id.GetHashCode(StringComparison.Ordinal));

}

public static class PropertyIdentity {

    public static readonly IEqualityComparer<Exposure> Comparer =
        EqualityComparer<Exposure>.Create((left, right) => left?.Property.Id == right?.Property.Id, e => e.Property.Id.GetHashCode(StringComparison.Ordinal));

}
