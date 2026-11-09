using JustDummies;

namespace FxSettlement.Domain.Tests.Expert;

/// <summary>Currency codes that are well-formed but absent from the desk's ISO table, so they take any minor units.</summary>
public static class Codes {

    public static IAny<string> Unlisted() {
        return Any.StringMatching("[A-W][A-Z]{2}").Except(AnyCurrency.IsoCodes);
    }

}
