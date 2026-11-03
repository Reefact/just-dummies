using JustDummies;

namespace Drafting.Domain.Tests.Expert.Generators;

public sealed partial class AnyDrawingReference {

    /// <summary>A drawing number the constructor accepts, for a test about another part of the reference.</summary>
    internal static AnyPattern ValidNumber() { return AnyValidNumber(); }

    /// <summary>A revision letter the constructor accepts, for a test about another part of the reference.</summary>
    internal static AnyChar ValidRevision() { return AnyValidRevision(); }

    /// <summary>A sheet the constructor accepts, for a test about another part of the reference.</summary>
    internal static AnyInt32 ValidSheet() { return AnyValidSheet(); }

    /// <summary>A revision letter the constructor accepts and that has a next one: anything but <c>Y</c>.</summary>
    public AnyDrawingReference WithANextRevision() {
        return WithRevision(r => r.Except('Y'));
    }

}
