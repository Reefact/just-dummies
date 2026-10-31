using JustDummies;

namespace SatelliteConjunction.Tests.Expert.Arbitrary;

/// <summary>Hangs <c>Any.Conjunction()</c> off the library's own entry point, like the scaffolded ones.</summary>
public static class AnyConjunctionEntry {

    extension(Any) {

        public static AnyConjunction Conjunction() {
            return new AnyConjunction();
        }

    }

}
