using JustDummies;

namespace RailwayInterlocking.Domain.Tests.Expert.Generators;

public sealed partial class AnyPoint {

    /// <summary>The valid base of a point name, for a test that needs a name a point would accept.</summary>
    internal static AnyString ValidName() {
        return AnyValidName();
    }

}
