using System;

using JustDummies;

namespace BatchGenealogy.Domain;

public sealed partial class AnyLotNumber {

    // A refinement narrows a pattern by value set only, never by sub-language, so "a lot, not a
    // sub-lot" cannot be a lambda over AnyValidValue(): it is a second grammar, written here once.
    private const string WholeLotPattern = "[A-Z]{2,4}-[0-9]{6}";

    /// <summary>The number of a whole lot (a receipt, a blend), never of a sub-lot.</summary>
    public AnyLotNumber OfAWholeLot() {
        return OfAWholeLot(pattern => pattern);
    }

    /// <summary>The number of a whole lot, refined by <paramref name="configure" />.</summary>
    public AnyLotNumber OfAWholeLot(Func<AnyPattern, AnyPattern> configure) {
        return new AnyLotNumber(configure(Any.StringMatching(WholeLotPattern)));
    }

}
