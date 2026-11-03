using JustDummies;

namespace RailwayInterlocking.Domain.Tests.Expert.Generators;

public sealed partial class AnyTrackSection {

    /// <summary>The valid base of a length, for a test that needs a length a section would accept.</summary>
    internal static AnyDouble ValidLengthMetres() {
        return AnyValidLengthMetres();
    }

    /// <summary>The valid base of a line speed, for a test that needs a speed a section would accept.</summary>
    internal static AnyDouble ValidLineSpeedKmh() {
        return AnyValidLineSpeedKmh();
    }

}
