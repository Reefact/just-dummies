using CaptiveBreeding.Domain;
using JustDummies;

namespace CaptiveBreeding.Tests.Generators;

public sealed partial class AnyIndividual {

    /// <summary>A wild-caught founder: neither sire nor dam is recorded.</summary>
    public AnyIndividual AsFounder() {
        return WithSireId((StudbookId?)null).WithDamId((StudbookId?)null);
    }

}
