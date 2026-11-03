using JustDummies;

namespace Drafting.Domain.Tests.Expert.Generators;

public sealed partial class AnyIsoTolerance {

    /// <summary>A hole class: upper-case <c>H</c> or <c>JS</c>.</summary>
    public AnyIsoTolerance ForHole() {
        return WithZone(z => z.Except("h", "js"));
    }

    /// <summary>A shaft class: lower-case <c>h</c> or <c>js</c>.</summary>
    public AnyIsoTolerance ForShaft() {
        return WithZone(z => z.Except("H", "JS"));
    }

    /// <summary>A grade the constructor accepts, for a test about the zone.</summary>
    internal static AnyInt32 ValidGrade() { return AnyValidGrade(); }

    /// <summary>A zone the constructor accepts, for a test about the grade.</summary>
    internal static AnyString ValidZone() { return AnyValidZone(); }

}
