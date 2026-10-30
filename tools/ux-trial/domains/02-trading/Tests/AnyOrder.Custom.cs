using JustDummies;

namespace Trading.Domain;

// Written by hand beside the scaffolded AnyOrder.cs: survives `any generate Order --force`.
public sealed partial class AnyOrder {

    /// <summary>A plain limit order: no stop price.</summary>
    private sealed class NoStop : IAny<Price?> {

        public Price? Generate() {
            return null;
        }

    }

}
