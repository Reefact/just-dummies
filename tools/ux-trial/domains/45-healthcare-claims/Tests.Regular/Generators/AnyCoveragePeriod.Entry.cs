using JustDummies;

namespace HealthcareClaims.Tests.Regular.Generators;

/// <summary>Hangs <c>Any.CoveragePeriod()</c> off the library's own entry point.</summary>
public static class AnyCoveragePeriodEntry {

    extension(Any) {

        public static AnyCoveragePeriod CoveragePeriod() {
            return new AnyCoveragePeriod();
        }

    }

}
