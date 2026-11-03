using JustDummies;

namespace Drafting.Domain.Tests.Expert.Generators;

public sealed partial class AnyDrawingScale {

    /// <summary>A scale term the constructor accepts on its own (at least 1), for a test about the other term.</summary>
    internal static AnyInt32 ValidTerm() { return AnyValidNumerator(); }

}
