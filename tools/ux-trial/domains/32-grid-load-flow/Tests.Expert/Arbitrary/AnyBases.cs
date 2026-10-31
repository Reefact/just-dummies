using JustDummies;

namespace GridLoadFlow.Domain;

// Written by hand, beside the scaffolded files: `any generate --force` never touches it.
// Each valid base stays at the one address the scaffolder gave it; these only let a recipe that
// draws several values at once (distinct ids, a pair of voltage levels) reuse that rule.

public sealed partial class AnyBus {

    /// <summary>The base every bus draws its nominal voltage from.</summary>
    public static AnyDouble BaseKvs() {
        return AnyValidBaseKv();
    }

}

public sealed partial class AnyLine {

    /// <summary>The base every line draws its id from; a transformer id obeys the same rule.</summary>
    public static AnyString Ids() {
        return AnyValidId();
    }

}
