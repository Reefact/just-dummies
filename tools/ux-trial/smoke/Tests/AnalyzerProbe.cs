using JustDummies;

namespace Smoke.Tests;

// Never run: it exists to be compiled. run.sh checks that the build reports JD014 here, which proves the analyzers
// bundled in the packed library load in a consumer's build.
internal static class AnalyzerProbe {

    internal static int TransposedBounds() {
        return Any.Int32().Between(10, 5).Generate();
    }

}
