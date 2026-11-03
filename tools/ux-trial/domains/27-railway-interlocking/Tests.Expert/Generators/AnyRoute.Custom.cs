using JustDummies;

namespace RailwayInterlocking.Domain.Tests.Expert.Generators;

public sealed partial class AnyRoute {

    /// <summary>The valid base of a route name, for a test that needs a name a route would accept.</summary>
    internal static AnyString ValidName() {
        return AnyValidName();
    }

}
